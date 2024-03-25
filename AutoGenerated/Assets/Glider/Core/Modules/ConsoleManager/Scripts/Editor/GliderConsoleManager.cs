//
//  GliderSdkIntegrationManager.cs
//  Gameduo Glider Unity Plugin
//
//  Created by Seungjun on 26/12/23.
//  Copyright © 2023 Gameduo. All rights reserved.
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.Serialization;
using VersionComparisonResult = GliderSdkUtils.VersionComparisonResult;


namespace Glider.Core.ConsoleManager.Editor
{
    /// <summary>
    /// A manager class for Glider integration manager window.
    /// </summary>
    public class GliderConsoleManager
    {
        private static string externalDependencyManagerVersion;
        private static readonly GliderConsoleManager instance = new GliderConsoleManager();

        private const string GliderCenterAuthApi = "https://center.test-gameduo.com/auth/login";

        private const string GliderCenterGetSettingApiFormat =
            "https://center.test-gameduo.com/glider/setting?projectCode={0}";

        private const string GliderCenterAccessTokenFilePath = "UserSettings/GliderCenterAccessToken.json";

        private const string GliderCenterConvertApiFormat =
            "https://ready-center.gameduo.net/game/{0}/{1}/sheet/list/convert?target=CSharp";

        private const string GliderCenterSandboxConvertApiFormat =
            "https://center.test-gameduo.com/game/{0}/{1}/sheet/list/convert?target=CSharp";

        public static event UnityAction<float, bool> webRequestProgressCallback;
        public static event UnityAction onLoginComplete;

        private UnityWebRequest webRequest;
        private string accessToken;


        public bool HasAccessToken => !string.IsNullOrEmpty(accessToken);

        public bool IsLoginConsole { get; private set; }
        public bool IsLoadingCenterAccessToken { get; private set; }
        public bool IsConvertingSheet { get; private set; }

        private GliderConsoleManager()
        {
        }


        static GliderConsoleManager()
        {
        }

        public static GliderConsoleManager Get()
        {
            return instance;
        }

        public void Reset()
        {
            accessToken = null;
            IsLoginConsole = false;
            IsLoadingCenterAccessToken = false;
        }

        public void LogoutCenter()
        {
            File.Delete(Application.dataPath.Replace("Assets", "UserSettings/GliderCenterAccessToken.json"));
            Reset();
        }


        public void ReadCenterAccessTokenFromFile()
        {
            var projectRootPath = Path.GetDirectoryName(Application.dataPath);
            var tokenPath = Path.Combine(projectRootPath, GliderCenterAccessTokenFilePath);

            accessToken = null;

            if (File.Exists(tokenPath))
            {
                var text = File.ReadAllText(tokenPath);
                var def = new { accessToken = string.Empty };
                var res = JsonConvert.DeserializeAnonymousType(text, def);
                accessToken = res?.accessToken;
            }
        }

        public void SaveCenterAccessToken(string token)
        {
            accessToken = token;
            var settingsJson = JsonConvert.SerializeObject(new { accessToken = token });
            try
            {
                var projectRootPath = Path.GetDirectoryName(Application.dataPath);
                var tokenFilePath = Path.Combine(projectRootPath, GliderCenterAccessTokenFilePath);
                File.WriteAllText(tokenFilePath, settingsJson);
            }
            catch (Exception exception)
            {
                GliderSdkLogger.UserError("Failed to save access token.");
                Console.WriteLine(exception);
            }
        }

        /// <summary>
        /// Loads the plugin data to be display by integration manager window.
        /// </summary>
        /// <param name="callback">Callback to be called once the plugin data download completes.</param>
        public async UniTask LoadCenterAccessToken(CancellationToken token)
        {
            IsLoadingCenterAccessToken = true;
            await UniTask.Delay(1000, true, PlayerLoopTiming.Update, token);
            IsLoadingCenterAccessToken = false;
            // 			var projectRootPath = Path.GetDirectoryName(Application.dataPath);
            // 			var tokenPath = Path.Combine(projectRootPath, GliderCenterAccessTokenFilePath);
            //
            // 			string tokenJson = null;
            // 			if (File.Exists(tokenPath))
            // 			{
            // 				tokenJson = File.ReadAllText(tokenPath);
            // 			}
            //
            // 			if (string.IsNullOrEmpty(tokenJson))
            // 			{
            // 				var url = string.Format(GliderCenterAuthApi, GliderSDK.Version);
            // 				Debug.Log($"[LoadPluginData.Request] {url}");
            // 				using (var www = UnityWebRequest.Get(url))
            // 				{
            // #if UNITY_2017_2_OR_NEWER
            // 					var operation = www.SendWebRequest();
            // #else
            //                 var operation = www.Send();
            // #endif
            //
            // 					while (!operation.isDone) yield return new WaitForSeconds(0.1f); // Just wait till www is done. Our coroutine is pretty rudimentary.
            //
            // #if UNITY_2020_1_OR_NEWER
            // 					if (www.result != UnityWebRequest.Result.Success)
            // #elif UNITY_2017_2_OR_NEWER
            //                 if (www.isNetworkError || www.isHttpError)
            // #else
            //                 if (www.isError)
            // #endif
            // 					{
            // 						callback(null);
            // 					}
            // 					else
            // 					{
            // 						ModuleWrapper moduleWrapper;
            // 						try
            // 						{
            // 							Debug.Log($"[LoadPluginData.Response] {www.downloadHandler.text}");
            // 							GliderSdkLogger.D(www.downloadHandler.text);
            // 							moduleWrapper = JsonUtility.FromJson<ModuleWrapper>(www.downloadHandler.text);
            // 						}
            // 						catch (Exception exception)
            // 						{
            // 							Console.WriteLine(exception);
            // 							moduleWrapper = null;
            // 						}
            //
            // 						if (moduleWrapper != null)
            // 						{
            // 							// Get current version of the plugin
            // 							var core = moduleWrapper.list[0];
            // 							UpdateCurrentVersions(core, PluginParentDirectory);
            //
            // 							// Get current versions for all the mediation networks.
            // 							var modulePluginParentDirectory = ModuleSpecificPluginParentDirectory;
            // 							foreach (var network in moduleWrapper.list)
            // 							{
            // 								UpdateCurrentVersions(network, modulePluginParentDirectory);
            // 							}
            // 						}
            //
            // 						callback(moduleWrapper);
            // 					}
            // 				}
            // 			}
        }

        /// <summary>
        /// Cancels the plugin download if one is in progress.
        /// </summary>
        public void CancelDownload()
        {
            if (webRequest == null) return;

            webRequest.Abort();
        }

        /// <summary>
        /// Shows a dialog to the user with the given message and logs the error message to console.
        /// </summary>
        /// <param name="message">The failure message to be shown to the user.</param>
        public static void ShowBuildFailureDialog(string message)
        {
            var openIntegrationManager =
                EditorUtility.DisplayDialog("Gameduo Glider", message, "Open Integration Manager", "Dismiss");
            if (openIntegrationManager)
            {
                GliderConsoleManagerWindow.ShowManager();
            }

            GliderSdkLogger.UserError(message);
        }


        public IEnumerator GetProjectSetting(string projectCode)
        {
            var url = string.Format(GliderCenterGetSettingApiFormat, projectCode);
            webRequest = new UnityWebRequest(url)
            {
                method = UnityWebRequest.kHttpVerbGET,
                downloadHandler = new DownloadHandlerBuffer()
            };
#if UNITY_2017_2_OR_NEWER
            var operation = webRequest.SendWebRequest();
#else
            var operation = webRequest.Send();
#endif
            while (!operation.isDone)
            {
                yield return
                    new WaitForSeconds(
                        0.1f); // Just wait till webRequest is completed. Our coroutine is pretty rudimentary.
                webRequestProgressCallback?.Invoke(operation.progress, operation.isDone);
            }

#if UNITY_2020_1_OR_NEWER
            if (webRequest.result != UnityWebRequest.Result.Success)
#elif UNITY_2017_2_OR_NEWER
            if (webRequest.isNetworkError || webRequest.isHttpError)
#else
            if (webRequest.isError)
#endif
            {
                Debug.Log($"[Console Manager]\n{url}\n{accessToken}");
                GliderSdkLogger.UserError(webRequest.error);
            }
            else
            {
                var def = new { accessToken = "" };
                var res = JsonConvert.DeserializeAnonymousType(webRequest.downloadHandler.text, def);
                Debug.Log(webRequest.downloadHandler.text);
                if (!string.IsNullOrWhiteSpace(res.accessToken))
                {
                    SaveCenterAccessToken(res.accessToken);
                }
                else
                {
                    EditorUtility.DisplayDialog("Glider Console Manager", "Login failed!", "Ok");
                }
            }

            webRequest.Dispose();
            webRequest = null;
        }

        public async UniTask LoginCenterAsync(CancellationToken token, string email, string password)
        {
            IsLoginConsole = true;
            var payloadString = JsonConvert.SerializeObject(new
            {
                email = email,
                password = password
            });
            var buffer = new System.Text.UTF8Encoding().GetBytes(payloadString);
            webRequest = new UnityWebRequest(GliderCenterAuthApi)
            {
                method = UnityWebRequest.kHttpVerbPOST,
                uploadHandler = new UploadHandlerRaw(buffer),
                downloadHandler = new DownloadHandlerBuffer()
            };

#if UNITY_2017_2_OR_NEWER
            var operation = webRequest.SendWebRequest();
#else
            var operation = webRequest.Send();
#endif
            while (!operation.isDone)
            {
                await UniTask.Delay(100, true, PlayerLoopTiming.Update, token);
                webRequestProgressCallback?.Invoke(operation.progress, operation.isDone);
            }

#if UNITY_2020_1_OR_NEWER
            if (webRequest.result != UnityWebRequest.Result.Success)
#elif UNITY_2017_2_OR_NEWER
            if (webRequest.isNetworkError || webRequest.isHttpError)
#else
            if (webRequest.isError)
#endif
            {
                GliderSdkLogger.UserError(webRequest.error);
            }
            else
            {
                var def = new { accessToken = "" };
                var res = JsonConvert.DeserializeAnonymousType(webRequest.downloadHandler.text, def);
                Debug.Log(webRequest.downloadHandler.text);
                if (!string.IsNullOrWhiteSpace(res.accessToken))
                {
                    SaveCenterAccessToken(res.accessToken);
                    onLoginComplete?.Invoke();
                }
                else
                {
                    EditorUtility.DisplayDialog("Glider Console Manager", "Login failed!", "Ok");
                }
            }

            webRequest.Dispose();
            webRequest = null;


            IsLoginConsole = false;
        }

        public async UniTask SheetConvertAsync(CancellationToken token)
        {
            IsConvertingSheet = true;
            Debug.Log("???");

            try
            {
                var files = await RequestSheetConvertAsync(token);

                var tasks = new List<UniTask>();
                foreach (var file in files)
                {
                    tasks.Add(WriteFileAsync(file, token));
                }

                await UniTask.WhenAll(tasks);
            }
            catch
            {
            }

            AssetDatabase.Refresh();
            IsConvertingSheet = false;
        }

        async UniTask WriteFileAsync(ConvertedFile file, CancellationToken token)
        {
            await File.WriteAllTextAsync(Path.Combine(file.filePath, $"{file.fileName}.cs"), file.content, token);
        }

        private async UniTask<ConvertedFile[]> RequestSheetConvertAsync(CancellationToken token)
        {
            string url = "";
            if (GliderSettings.Get().Env == Env.Live)
            {
                url = string.Format(GliderCenterConvertApiFormat, GliderSettings.Get().ProjectCode, "shared");
            }
            else if (GliderSettings.Get().Env == Env.Sandbox)
            {
                url = string.Format(GliderCenterSandboxConvertApiFormat, GliderSettings.Get().ProjectCode, "shared");
            }
            else
            {
            }

            webRequest = new UnityWebRequest(url)
            {
                method = UnityWebRequest.kHttpVerbGET,
                downloadHandler = new DownloadHandlerBuffer()
            };
            webRequest.SetRequestHeader("Authorization", $"Bearer {accessToken}");

#if UNITY_2017_2_OR_NEWER
            var operation = webRequest.SendWebRequest();
#else
            var operation = webRequest.Send();
#endif
            while (!operation.isDone)
            {
                Debug.Log(operation.progress);
                await UniTask.Delay(100, true, PlayerLoopTiming.Update, token);
                webRequestProgressCallback?.Invoke(operation.progress, operation.isDone);
            }

            ConvertedFile[] result = null;
#if UNITY_2020_1_OR_NEWER
            if (webRequest.result != UnityWebRequest.Result.Success)
#elif UNITY_2017_2_OR_NEWER
            if (webRequest.isNetworkError || webRequest.isHttpError)
#else
            if (webRequest.isError)
#endif
            {
                GliderSdkLogger.UserError(webRequest.error);
            }
            else
            {
                // System.IO.File.WriteAllText(Application.dataPath + "/output.json", webRequest.downloadHandler.text);

                var res = JsonUtility.FromJson<ResponseConvert>(webRequest.downloadHandler.text);
                result = res.files;
            }

            webRequest.Dispose();
            webRequest = null;


            return result;
        }

        [System.Serializable]
        private class ResponseConvert
        {
            public ConvertedFile[] files;
        }

        [System.Serializable]
        private class ConvertedFile
        {
            public string content;
            public string filePath;
            public string fileName;
        }
    }
}