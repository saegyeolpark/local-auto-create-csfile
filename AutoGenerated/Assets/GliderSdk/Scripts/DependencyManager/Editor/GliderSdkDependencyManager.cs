#if UNITY_EDITOR

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine.Networking;
using UnityEngine.Serialization;

namespace GliderSdk.Scripts.DependencyManager.Editor
{
    [Serializable]
    public class Dependencies
    {
        public ModuleDependency[] module;

        [Serializable]
        public class ModuleDependency
        {
            public string moduleName;
            public VersionDependency[] versions;

            [Serializable]
            public class VersionDependency
            {
                public string version;
                public string[] unity;
                public string[] glider;
            }
        }
    }

    [Serializable]
    public class LocalPayload
    {
        public string[] modules;
        public string[] gliders;
    }

    public class GliderSdkDependencyManager
    {
        private static GliderSdkDependencyManager instance = new GliderSdkDependencyManager();
        public static GliderSdkDependencyManager Instance => instance;


        public Action<string /*glider module name*/> gliderImportCallback;
        public Action<string[] /*glider module names*/> gliderImportAllCallback;

        private Dependencies _dependencies;
        public Dependencies Dependencies => _dependencies;

        private LocalPayload _localPayload = new LocalPayload()
        {
            modules = new string[] { }
        };

        private const string LocalPayloadPath = "Assets/GliderSdk/Scripts/DependencyManager/Editor/LocalPayload.json";
        private const string PatchApiUrl = "https://center.test-gameduo.com/glider/module/Dependencies/1.0.0";

        private const string DownloadUrl =
            "https://duogame-glider-unity-sdk.s3.ap-northeast-2.amazonaws.com/Dependencies.json";

        public const string LocalDependenciesPath =
            "Assets/GliderSdk/Scripts/DependencyManager/Editor/Dependencies.json";


        /// <summary>
        /// Dependencies 파일 서버 동기화
        /// </summary>
        /// <param name="callback"></param>
        /// <returns></returns>
        public IEnumerator DownloadDependenciesFile(Action<bool> callback)
        {
            Debug.Log("[Dependency] fetching Dependencies file...");
            var url = DownloadUrl;

            using (var www = UnityWebRequest.Get(url))
            {
                var request = www.SendWebRequest();
                while (request.isDone == false) yield return null;

                try
                {
                    File.WriteAllText(LocalDependenciesPath,
                        www.downloadHandler.text);
                    Debug.Log("[Dependency] Dependencies.json save complete");
                }
                catch (Exception e)
                {
                    callback(false);
                    Debug.Log("[Dependency] Dependencies.json save failed");

                    yield break;
                }
            }

            callback(true);
        }

        public IEnumerator UploadDependenciesFile(string bearerToken, Action<bool> callback)
        {
            Debug.Log("[Dependency] uploading Dependencies file...");
            var url = PatchApiUrl;

            var fileData = File.ReadAllBytes(LocalDependenciesPath);
            var formData = new List<IMultipartFormSection>();
            formData.Add(new MultipartFormDataSection("pluginFileName", "Dependencies.json"));
            formData.Add(new MultipartFormDataSection("fileFullName", "Dependencies.json"));
            formData.Add(new MultipartFormDataSection("downloadUrl",
                "https://duogame-glider-unity-sdk.s3.ap-northeast-2.amazonaws.com/Dependencies.json"));
            formData.Add(new MultipartFormDataSection("dependenciesFilePath",
                "Assets/GliderSdk/Scripts/DependencyManager/Editor/Dependencies.json"));
            formData.Add(new MultipartFormDataSection("latestVersion", "1.0.0"));

            formData.Add(new MultipartFormFileSection("fileBuffer", fileData, "Dependencies.json",
                "application/json"));

            using (var www = UnityWebRequest.Post(url, formData))
            {
                www.method = "PATCH";
                www.SetRequestHeader("Authorization", "Bearer " + bearerToken);

                var operation = www.SendWebRequest();
                while (operation.isDone == false) yield return null;
                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.Log(www.error);
                    Debug.Log(www.result);
                    Debug.Log(www.downloadHandler.text);
                    callback(false);
                }
                else
                {
                    Debug.Log("upload dependencies complete!");
                    callback(true);
                }
            }

            callback(true);
        }

        public void OpenDependenciesFile()
        {
            Application.OpenURL(Application.dataPath + LocalDependenciesPath.Replace("Assets", ""));
        }

        private void ReadDependencies()
        {
            var json = File.ReadAllText(Application.dataPath +
                                        "/GliderSdk/Scripts/DependencyManager/Editor/Dependencies.json");
            // Debug.Log(json);
            _dependencies = JsonUtility.FromJson<Dependencies>(json);
        }

        public IEnumerator CheckDependencies(string moduleName, string moduleVersion, string[] notInstalledGliders,
            Action<bool> callback)
        {
            ReadDependencies();

            // 현재 설치된 패키지 정보 로드
            var listRequest = Client.List();
            while (listRequest.IsCompleted == false)
                yield return null;
            var importedList = listRequest.Result;

            var importedSet = new HashSet<string>();
            foreach (var imported in importedList)
            {
                importedSet.Add(imported.packageId);
            }

            var unityRequirements = new List<string>();
            // unity 의존정보 로드
            var unityDependencies = GetUnityDependencies(moduleName, moduleVersion);
            if (unityDependencies is not null)
            {
                if (unityDependencies.Length > 0)
                {
                    foreach (var requirement in unityDependencies)
                    {
                        if (importedSet.Contains(requirement) == false)
                        {
                            unityRequirements.Add(requirement);
                        }
                    }
                }
            }

            var gliderRequirements = new List<string>();
            importedSet.Clear();
            foreach (var gliders in notInstalledGliders)
            {
                importedSet.Add(gliders);
            }

            // glider 의존정보 로드
            var gliderDependencies = GetGliderDependencies(moduleName, moduleVersion);
            if (gliderDependencies is not null)
            {
                if (gliderDependencies.Length > 0)
                {
                    foreach (var requirement in gliderDependencies)
                    {
                        if (importedSet.Contains(requirement) == true)
                        {
                            gliderRequirements.Add(requirement);
                        }
                    }
                }
            }

            if (unityRequirements.Count > 0 || gliderRequirements.Count > 0)
            {
                var localPayload = new LocalPayload();
                if (unityRequirements.Count > 0)
                {
                    localPayload.modules = unityRequirements.ToArray();
                }

                if (gliderRequirements.Count > 0)
                {
                    localPayload.gliders = gliderRequirements.ToArray();
                }

                SaveLocalPayload(localPayload);
                GliderSdkDependencyManagerWindow.ShowManager();
                callback(false);
                yield break;
            }

            callback(true);
        }

        private string RemovePackageIdAnnotation(string packageId)
        {
            var seperator = packageId.IndexOf("@");
            return packageId.Substring(0, seperator);
        }

        public IEnumerator InstallArrayDependencies(string[] packages, Action<bool> callback)
        {
            EditorApplication.LockReloadAssemblies();
            foreach (var package in packages)
            {
                var addRequest = Client.Add(package);
                while (addRequest.IsCompleted == false) yield return null;
                yield return new WaitForSeconds(1);
            }

            EditorApplication.UnlockReloadAssemblies();
            callback(true);
        }

        public IEnumerator InstallAllDependencies(Action<bool> callback)
        {
            ReadDependencies();
            foreach (var module in _dependencies.module)
            {
                if (module.versions.Length < 1)
                    continue;
                var list = module.versions[module.versions.Length - 1].unity;
                foreach (var package in list)
                {
                    Debug.Log($"[Unity dependency] installing {package}...");
                    var addRequest = Client.Add(package);
                    while (addRequest.IsCompleted == false) yield return null;
                    AssetDatabase.Refresh();
                    yield return new WaitForSeconds(.1f);
                }
            }

            callback(true);
            Debug.Log("dependencies done");
        }


        private string[] GetUnityDependencies(string moduleName, string moduleVersion)
        {
            if (Dependencies is null)
                ReadDependencies();
            foreach (var module in Dependencies.module)
            {
                if (module.moduleName == moduleName)
                {
                    foreach (var version in module.versions)
                    {
                        if (version.version == moduleVersion)
                        {
                            return version.unity;
                        }
                    }
                }
            }

            return null;
        }

        private string[] GetGliderDependencies(string moduleName, string moduleVersion)
        {
            if (Dependencies is null)
                ReadDependencies();
            foreach (var module in Dependencies.module)
            {
                if (module.moduleName == moduleName)
                {
                    foreach (var version in module.versions)
                    {
                        if (version.version == moduleVersion)
                        {
                            return version.glider;
                        }
                    }
                }
            }

            return null;
        }

        private void SaveLocalPayload(LocalPayload payload)
        {
            File.WriteAllText(LocalPayloadPath,
                JsonUtility.ToJson(payload));
        }

        public static LocalPayload LoadLocalPayload()
        {
            var info = JsonUtility.FromJson<LocalPayload>(File.ReadAllText(LocalPayloadPath));
            return info;
        }

        public void RemoveFromLocalPayload(string moduleId)
        {
            var payload = LoadLocalPayload();
            Debug.Log("remove payload: " + moduleId);

            var unityPayload = payload.modules.ToList();
            unityPayload.Remove(moduleId);
            payload.modules = unityPayload.ToArray();

            List<string> gliderPayload = payload.modules.ToList();
            gliderPayload.Remove(moduleId);
            payload.gliders = gliderPayload.ToArray();

            SaveLocalPayload(payload);
        }

        public void RemoveAllFromLocalPayload()
        {
            var payload = new LocalPayload()
            {
                modules = new string[] { }
            };
            SaveLocalPayload(payload);
        }
    }
}

#endif