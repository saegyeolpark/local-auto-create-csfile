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
using System.Xml.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Serialization;
using VersionComparisonResult = GliderSdkUtils.VersionComparisonResult;


namespace GliderSdk.Scripts.SdkVersionManager.Editor
{
    [Serializable]
    public class ModuleWrapper
    {
        public Module[] list;
    }

    [Serializable]
    public class Module
    {
        //
        // Sample network data:
        //
        // {
        //   "name": "GliderCore",
        //   "downloadUrl": "https://duogame-bane-live.s3.ap-northeast-2.amazonaws.com/glider/module/Gameduo-GliderCore-1.0.0.unitypackage",
        //   "pluginFileName": "Gameduo-GliderCore-1.0.0.unitypackage",
        //   "dependenciesFilePath": "GliderSdk/Module/GliderCore/Editor/Dependencies.xml",
        //   "latestVersion" : "1.0.0"
        // }
        //

        public string moduleName;
        public string downloadUrl;
        public string dependenciesFilePath;
        public string[] pluginFilePaths;
        public string latestVersion;
        [NonSerialized] public string CurrentVersion;

        [NonSerialized]
        public VersionComparisonResult CurrentToLatestVersionComparisonResult = VersionComparisonResult.Lesser;

        [NonSerialized] public bool RequiresUpdate;
    }

    [Serializable]
    public class Dependency
    {
        public string version;
    }

    /// <summary>
    /// A manager class for Glider integration manager window.
    ///
    /// TODO: Decide if we should namespace these classes.
    /// </summary>
    public class GliderSdkVersionManager
    {
        /// <summary>
        /// Delegate to be called when downloading a plugin with the progress percentage. 
        /// </summary>
        /// <param name="pluginName">The name of the plugin being downloaded.</param>
        /// <param name="progress">Percentage downloaded.</param>
        /// <param name="done">Whether or not the download is complete.</param>
        public delegate void DownloadPluginProgressCallback(string pluginName, float progress, bool done);

        /// <summary>
        /// Delegate to be called when a plugin package is imported.
        /// </summary>
        /// <param name="module">The network data for which the package is imported.</param>
        public delegate void ImportPackageCompletedCallback(Module module);

        private static readonly GliderSdkVersionManager instance = new GliderSdkVersionManager();


        public const string NotInstalled = "not installed";

        public static readonly string GradleTemplatePath =
            Path.Combine("Assets/Plugins/Android", "mainTemplate.gradle");

        public static readonly string DefaultPluginExportPath = Path.Combine("Assets", "Glider");
        private const string GliderSdkAssetExportPath = "Glider/GliderSDK.cs";

        private const string GliderSdkModulesListApiFormat =
            "https://center.test-gameduo.com/glider/sdk/{0}/module/list";

        /// <summary>
        /// Some publishers might re-export our plugin via Unity Package Manager and the plugin will not be under the Assets folder. This means that the mediation adapters, settings files should not be moved to the packages folder,
        /// since they get overridden when the package is updated. These are the files that should not be moved, if the plugin is not under the Assets/ folder.
        /// 
        /// Note: When we distribute the plugin via Unity Package Manager, we need to distribute the adapters as separate packages, and the adapters won't be in the GliderSDK folder. So we need to take that into account.
        /// </summary>
        private static readonly List<string> PluginPathsToIgnoreMoveWhenPluginOutsideAssetsDirectory = new List<string>
        {
            "GliderSdk/Mediation",
            "GliderSdk/Mediation.meta",
            "GliderSdk/Resources.meta",
            GliderSettings.SettingsExportPath,
            GliderSettings.SettingsExportPath + ".meta"
        };

        private static string externalDependencyManagerVersion;

        public static DownloadPluginProgressCallback downloadPluginProgressCallback;
        public static ImportPackageCompletedCallback importPackageCompletedCallback;

        private UnityWebRequest webRequest;
        private Module _importingModule;

        /// <summary>
        /// An Instance of the Integration manager.
        /// </summary>
        public static GliderSdkVersionManager Instance
        {
            get { return instance; }
        }

        /// <summary>
        /// The parent directory path where the GliderSdk plugin directory is placed.
        /// </summary>
        public static string PluginParentDirectory
        {
            get
            {
                // Search for the asset with the default exported path first, In most cases, we should be able to find the asset.
                // In some cases where we don't, use the platform specific export path to search for the asset (in case of migrating a project from Windows to Mac or vice versa).
                var gliderSdkScriptAssetPath = GliderSdkUtils.GetAssetPathForExportPath(GliderSdkAssetExportPath);

                // gliderSdkScriptAssetPath will always have AltDirectorySeparatorChar (/) as the path separator. Convert to platform specific path.
                return gliderSdkScriptAssetPath.Replace(GliderSdkAssetExportPath, "")
                    .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            }
        }

        /// <summary>
        /// When the base plugin is outside the <c>Assets/</c> directory, the mediation plugin files are still imported to the default location under <c>Assets/</c>.
        /// Returns the parent directory where the mediation adapter plugins are imported.
        /// </summary>
        public static string ModuleSpecificPluginParentDirectory
        {
            get { return IsPluginOutsideAssetsDirectory ? "Assets" : PluginParentDirectory; }
        }

        /// <summary>
        /// Whether or not the plugin is under the Assets/ folder.
        /// </summary>
        public static bool IsPluginOutsideAssetsDirectory
        {
            get { return !PluginParentDirectory.StartsWith("Assets"); }
        }

        /// <summary>
        /// Whether or not gradle build system is enabled.
        /// </summary>
        public static bool GradleBuildEnabled
        {
            get { return GetEditorUserBuildSetting("androidBuildSystem", "").ToString().Equals("Gradle"); }
        }

        /// <summary>
        /// Whether or not Gradle template is enabled.
        /// </summary>
        public static bool GradleTemplateEnabled
        {
            get { return GradleBuildEnabled && File.Exists(GradleTemplatePath); }
        }

        /// <summary>
        /// Whether or not the Quality Service settings can be processed which requires Gradle template enabled or Unity IDE newer than version 2018_2.
        /// </summary>
        public static bool CanProcessAndroidQualityServiceSettings
        {
            get { return GradleTemplateEnabled || (GradleBuildEnabled && IsUnity2021_2OrNewer()); }
        }

        /// <summary>
        /// The External Dependency Manager version obtained dynamically.
        /// </summary>
        public static string ExternalDependencyManagerVersion
        {
            get
            {
                if (!string.IsNullOrEmpty(externalDependencyManagerVersion)) return externalDependencyManagerVersion;

                try
                {
                    var versionHandlerVersionNumberType =
                        Type.GetType("Google.VersionHandlerVersionNumber, Google.VersionHandlerImpl");
                    externalDependencyManagerVersion = versionHandlerVersionNumberType.GetProperty("Value")
                        .GetValue(null, null).ToString();
                }
#pragma warning disable 0168
                catch (Exception ignored)
#pragma warning restore 0168
                {
                    externalDependencyManagerVersion = "Failed to get version.";
                }

                return externalDependencyManagerVersion;
            }
        }

        private GliderSdkVersionManager()
        {
            // Add asset import callbacks.
            AssetDatabase.importPackageCompleted += packageName =>
            {
                if (!IsImportingNetwork(packageName)) return;

                var pluginParentDir = PluginParentDirectory;
                var isPluginOutsideAssetsDir = IsPluginOutsideAssetsDirectory;
                MovePluginFilesIfNeeded(pluginParentDir, isPluginOutsideAssetsDir);
                AddLabelsToAssetsIfNeeded(pluginParentDir, isPluginOutsideAssetsDir);
                AssetDatabase.Refresh();

                CallImportPackageCompletedCallback(_importingModule);
                _importingModule = null;
            };

            AssetDatabase.importPackageCancelled += packageName =>
            {
                if (!IsImportingNetwork(packageName)) return;

                GliderSdkLogger.UserDebug("Package import cancelled.");
                _importingModule = null;
            };

            AssetDatabase.importPackageFailed += (packageName, errorMessage) =>
            {
                if (!IsImportingNetwork(packageName)) return;

                GliderSdkLogger.UserError(errorMessage);
                _importingModule = null;
            };
        }

        static GliderSdkVersionManager()
        {
        }

        /// <summary>
        /// Loads the plugin data to be display by integration manager window.
        /// </summary>
        /// <param name="callback">Callback to be called once the plugin data download completes.</param>
        public IEnumerator LoadPluginData(Action<ModuleWrapper> callback)
        {
            var url = string.Format(GliderSdkModulesListApiFormat, GliderSDK.Version);
            Debug.Log($"[LoadPluginData.Request] {url}");
            using (var www = UnityWebRequest.Get(url))
            {
#if UNITY_2017_2_OR_NEWER
                var operation = www.SendWebRequest();
#else
                var operation = www.Send();
#endif

                while (!operation.isDone)
                    yield return
                        new WaitForSeconds(0.1f); // Just wait till www is done. Our coroutine is pretty rudimentary.

#if UNITY_2020_1_OR_NEWER
                if (www.result != UnityWebRequest.Result.Success)
#elif UNITY_2017_2_OR_NEWER
                if (www.isNetworkError || www.isHttpError)
#else
                if (www.isError)
#endif
                {
                    callback(null);
                }
                else
                {
                    ModuleWrapper moduleWrapper;
                    try
                    {
                        // Debug.Log($"[LoadPluginData.Response] {www.downloadHandler.text}");
                        GliderSdkLogger.D(www.downloadHandler.text);
                        moduleWrapper = JsonUtility.FromJson<ModuleWrapper>(www.downloadHandler.text);
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine(exception);
                        moduleWrapper = null;
                    }

                    if (moduleWrapper != null)
                    {
                        // Get current version of the plugin
                        var core = moduleWrapper.list[1];
                        UpdateCurrentVersions(core, PluginParentDirectory);

                        // Get current versions for all the mediation networks.
                        var modulePluginParentDirectory = ModuleSpecificPluginParentDirectory;
                        foreach (var network in moduleWrapper.list)
                        {
                            UpdateCurrentVersions(network, modulePluginParentDirectory);
                        }
                    }

                    callback(moduleWrapper);
                }
            }
        }

        /// <summary>
        /// Updates the CurrentVersion fields for a given network data object.
        /// </summary>
        /// <param name="module">Network for which to update the current versions.</param>
        /// <param name="mediationPluginParentDirectory">The parent directory of where the mediation adapter plugins are imported to.</param>
        public static void UpdateCurrentVersions(Module module, string mediationPluginParentDirectory)
        {
            var dependencyFilePath = Path.Combine(mediationPluginParentDirectory, module.dependenciesFilePath);
            var currentVersion = GetCurrentVersion(dependencyFilePath);

            module.CurrentVersion = currentVersion;

            // If adapter is indeed installed, compare the current (installed) and the latest (from db) versions, so that we can determine if the publisher is on an older, current or a newer version of the adapter.
            // If the publisher is on a newer version of the adapter than the db version, that means they are on a beta version.
            if (!string.IsNullOrEmpty(currentVersion))
            {
                module.CurrentToLatestVersionComparisonResult =
                    GliderSdkUtils.CompareUnityMediationVersions(currentVersion, module.latestVersion);
            }

            if (!string.IsNullOrEmpty(module.CurrentVersion) &&
                GliderSdkAutoUpdater.MinAdapterVersions.ContainsKey(module.moduleName))
            {
                var comparisonResult = GliderSdkUtils.CompareUnityMediationVersions(module.CurrentVersion,
                    GliderSdkAutoUpdater.MinAdapterVersions[module.moduleName]);
                // Requires update if current version is lower than the min required version.
                module.RequiresUpdate = comparisonResult < 0;
            }
            else
            {
                // Reset value so that the Integration manager can hide the alert icon once adapter is updated.
                module.RequiresUpdate = false;
            }
        }

        /// <summary>
        /// Downloads the plugin file for a given network.
        /// </summary>
        /// <param name="module">Network for which to download the current version.</param>
        /// <param name="showImport">Whether or not to show the import window when downloading. Defaults to <c>true</c>.</param>
        /// <returns></returns>
        public IEnumerator DownloadPlugin(Module module, Action<bool> callback, bool downloadLatest = false,
            bool showImport = true)
        {
            var currentVersion = GliderSDK.Version;
            if (downloadLatest)
            {
                DeleteModule(module);

                module.downloadUrl = module.downloadUrl.Replace(currentVersion, module.latestVersion);
                Debug.Log($"modifiy downloadUrl: {currentVersion} to {module.latestVersion}");
                // Debug.Log(module.downloadUrl);
            }

            var path = Path.Combine(Application.temporaryCachePath,
                GetPluginFileName(module)); // TODO: Maybe delete plugin file after finishing import.
            var downloadHandler = new DownloadHandlerFile(path);

            webRequest = new UnityWebRequest(module.downloadUrl)
            {
                method = UnityWebRequest.kHttpVerbGET,
                downloadHandler = downloadHandler
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
                CallDownloadPluginProgressCallback(module.moduleName, operation.progress, operation.isDone);
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
                callback(false);
            }
            else
            {
                _importingModule = module;
                AssetDatabase.ImportPackage(path, showImport);
            }

            webRequest.Dispose();
            webRequest = null;
            callback(true);
        }

        /// <summary>
        /// Cancels the plugin download if one is in progress.
        /// </summary>
        public void CancelDownload()
        {
            if (webRequest == null) return;

            webRequest.Abort();
        }

        public void DeleteModule(Module module)
        {
            // TODO-Glider 모듈 삭제
            var pluginRoot = module.dependenciesFilePath;
            pluginRoot = pluginRoot.Substring(0, pluginRoot.IndexOf("/Editor"));
            pluginRoot = Path.Combine(Application.dataPath, pluginRoot);

            FileUtil.DeleteFileOrDirectory(pluginRoot);

            GliderSdkVersionManager.UpdateCurrentVersions(module, pluginRoot);

            // Refresh UI
            AssetDatabase.Refresh();
        }

        public void DeleteModule(string path)
        {
            FileUtil.DeleteFileOrDirectory(path);
            AssetDatabase.Refresh();
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
                GliderSdkVersionManagerWindow.ShowManager();
            }

            GliderSdkLogger.UserError(message);
        }

        /// <summary>
        /// Checks whether or not an module with the given version or newer exists.
        /// </summary>
        /// <param name="moduleName">The name of the network (the root module folder name in "Glider/Modules/" folder.</param>
        /// <param name="version">The min module version to check for. Can be <c>null</c> if we want to check for any version.</param>
        /// <returns><c>true</c> if an module with the min version is installed.</returns>
        public static bool IsModuleInstalled(string moduleName, string version = null)
        {
            var dependencyFilePath = moduleName == "GliderCore"
                ? GliderSdkUtils.GetAssetPathForExportPath("Glider/Editor/Dependencies.json")
                : GliderSdkUtils.GetAssetPathForExportPath("Glider/Modules/" + moduleName +
                                                           "/Editor/Dependencies.json");
            if (!File.Exists(dependencyFilePath)) return false;

            // If version is null, we just need the adapter installed. We don't have to check for a specific version.
            if (version == null) return true;

            var currentVersion = GetCurrentVersion(dependencyFilePath);
            Debug.Log($"[Compare Version] {currentVersion} {version}");
            var versionComparison = GliderSdkUtils.CompareVersions(currentVersion, version);
            return versionComparison != VersionComparisonResult.Lesser;
        }

        #region Utility Methods

        /// <summary>
        /// Gets the current versions for a given network's dependency file path.
        /// </summary>
        /// <param name="dependencyPath">A dependency file path that from which to extract current versions.</param>
        /// <returns>Current versions of a given network's dependency file.</returns>
        public static string GetCurrentVersion(string dependencyPath)
        {
            Dependency data;
            try
            {
                var content = File.ReadAllText(dependencyPath);
                data = JsonUtility.FromJson<Dependency>(content);
            }
            catch (IOException e)
            {
                // Debug.Log(e.ToString());
                return NotInstalled;
            }

            return data.version;
        }


        /// <summary>
        /// Checks whether or not the given package name is the currently importing package.
        /// </summary>
        /// <param name="packageName">The name of the package that needs to be checked.</param>
        /// <returns>true if the importing package matches the given package name.</returns>
        private bool IsImportingNetwork(string packageName)
        {
            // Note: The pluginName doesn't have the '.unitypacakge' extension included in its name but the pluginFileName does. So using Contains instead of Equals.
            return _importingModule != null && GetPluginFileName(_importingModule).Contains(packageName);
        }

        /// <summary>
        /// Returns a URL friendly version string by replacing periods with underscores.
        /// </summary>
        private static string GetPluginVersionForUrl()
        {
            var version = GliderSDK.Version;
            var versionsSplit = version.Split('.');
            return string.Join("_", versionsSplit);
        }

        /// <summary>
        /// Adds labels to assets so that they can be easily found.
        /// </summary>
        /// <param name="pluginParentDir">The Glider Unity plugin's parent directory.</param>
        /// <param name="isPluginOutsideAssetsDirectory">Whether or not the plugin is outside the Assets directory.</param>
        public static void AddLabelsToAssetsIfNeeded(string pluginParentDir, bool isPluginOutsideAssetsDirectory)
        {
            if (isPluginOutsideAssetsDirectory)
            {
                var defaultPluginLocation = Path.Combine("Assets", "Glider");
                if (Directory.Exists(defaultPluginLocation))
                {
                    AddLabelsToAssets(defaultPluginLocation, "Assets");
                }
            }

            var pluginDir = Path.Combine(pluginParentDir, "Glider");
            AddLabelsToAssets(pluginDir, pluginParentDir);
        }

        private static void AddLabelsToAssets(string directoryPath, string pluginParentDir)
        {
            var files = Directory.GetFiles(directoryPath);
            foreach (var file in files)
            {
                if (file.EndsWith(".meta")) continue;

                UpdateAssetLabelsIfNeeded(file, pluginParentDir);
            }

            var directories = Directory.GetDirectories(directoryPath);
            foreach (var directory in directories)
            {
                // Add labels to this directory asset.
                UpdateAssetLabelsIfNeeded(directory, pluginParentDir);

                // Recursively add labels to all files under this directory.
                AddLabelsToAssets(directory, pluginParentDir);
            }
        }

        private static void UpdateAssetLabelsIfNeeded(string assetPath, string pluginParentDir)
        {
            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
            var labels = AssetDatabase.GetLabels(asset);

            var labelsToAdd = labels.ToList();
            var didAddLabels = false;
            if (!labels.Contains("gd_glider"))
            {
                labelsToAdd.Add("gd_glider");
                didAddLabels = true;
            }

            var exportPathLabel = "gd_glider_export_path-" + assetPath.Replace(pluginParentDir, "")
                .Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (!labels.Contains(exportPathLabel))
            {
                labelsToAdd.Add(exportPathLabel);
                didAddLabels = true;
            }

            // We only need to set the labels if they changed.
            if (!didAddLabels) return;

            AssetDatabase.SetLabels(asset, labelsToAdd.ToArray());
        }

        /// <summary>
        /// Moves the imported plugin files to the GliderSdk directory if the publisher has moved the plugin to a different directory. This is a failsafe for when some plugin files are not imported to the new location.
        /// </summary>
        /// <returns>True if the adapters have been moved.</returns>
        public static bool MovePluginFilesIfNeeded(string pluginParentDirectory, bool isPluginOutsideAssetsDirectory)
        {
            var pluginDir = Path.Combine(pluginParentDirectory, "Glider");

            // Check if the user has moved the Plugin and if new assets have been imported to the default directory.
            if (DefaultPluginExportPath.Equals(pluginDir) || !Directory.Exists(DefaultPluginExportPath)) return false;

            MovePluginFiles(DefaultPluginExportPath, pluginDir, isPluginOutsideAssetsDirectory);
            if (!isPluginOutsideAssetsDirectory)
            {
                FileUtil.DeleteFileOrDirectory(DefaultPluginExportPath + ".meta");
            }

            AssetDatabase.Refresh();
            return true;
        }

        /// <summary>
        /// A helper function to move all the files recursively from the default plugin dir to a custom location the publisher moved the plugin to.
        /// </summary>
        private static void MovePluginFiles(string fromDirectory, string pluginRoot,
            bool isPluginOutsideAssetsDirectory)
        {
            var files = Directory.GetFiles(fromDirectory);
            foreach (var file in files)
            {
                // We have to ignore some files, if the plugin is outside the Assets/ directory.
                if (isPluginOutsideAssetsDirectory &&
                    PluginPathsToIgnoreMoveWhenPluginOutsideAssetsDirectory.Any(pluginPathsToIgnore =>
                        file.Contains(pluginPathsToIgnore))) continue;

                // Check if the destination folder exists and create it if it doesn't exist
                var parentDirectory = Path.GetDirectoryName(file);
                var destinationDirectoryPath = parentDirectory.Replace(DefaultPluginExportPath, pluginRoot);
                if (!Directory.Exists(destinationDirectoryPath))
                {
                    Directory.CreateDirectory(destinationDirectoryPath);
                }

                // If the meta file is of a folder asset and doesn't have labels (it is auto generated by Unity), just delete it.
                if (IsAutoGeneratedFolderMetaFile(file))
                {
                    FileUtil.DeleteFileOrDirectory(file);
                    continue;
                }

                var destinationPath = file.Replace(DefaultPluginExportPath, pluginRoot);

                // Check if the file is already present at the destination path and delete it.
                if (File.Exists(destinationPath))
                {
                    FileUtil.DeleteFileOrDirectory(destinationPath);
                }

                FileUtil.MoveFileOrDirectory(file, destinationPath);
            }

            var directories = Directory.GetDirectories(fromDirectory);
            foreach (var directory in directories)
            {
                // We might have to ignore some directories, if the plugin is outside the Assets/ directory.
                if (isPluginOutsideAssetsDirectory &&
                    PluginPathsToIgnoreMoveWhenPluginOutsideAssetsDirectory.Any(pluginPathsToIgnore =>
                        directory.Contains(pluginPathsToIgnore))) continue;

                MovePluginFiles(directory, pluginRoot, isPluginOutsideAssetsDirectory);
            }

            if (!isPluginOutsideAssetsDirectory)
            {
                FileUtil.DeleteFileOrDirectory(fromDirectory);
            }
        }

        private static bool IsAutoGeneratedFolderMetaFile(string assetPath)
        {
            // Check if it is a meta file.
            if (!assetPath.EndsWith(".meta")) return false;

            var lines = File.ReadAllLines(assetPath);
            var isFolderAsset = false;
            var hasLabels = false;
            foreach (var line in lines)
            {
                if (line.Contains("folderAsset: yes"))
                {
                    isFolderAsset = true;
                }

                if (line.Contains("labels:"))
                {
                    hasLabels = true;
                }
            }

            // If it is a folder asset and doesn't have a label, the meta file is auto generated by 
            return isFolderAsset && !hasLabels;
        }

        private static void CallDownloadPluginProgressCallback(string pluginName, float progress, bool isDone)
        {
            if (downloadPluginProgressCallback == null) return;

            downloadPluginProgressCallback(pluginName, progress, isDone);
        }

        private static void CallImportPackageCompletedCallback(Module module)
        {
            if (importPackageCompletedCallback == null) return;
            importPackageCompletedCallback(module);
        }

        private static object GetEditorUserBuildSetting(string name, object defaultValue)
        {
            var editorUserBuildSettingsType = typeof(EditorUserBuildSettings);
            var property = editorUserBuildSettingsType.GetProperty(name);
            if (property != null)
            {
                var value = property.GetValue(null, null);
                if (value != null) return value;
            }

            return defaultValue;
        }

        private static bool IsUnity2021_2OrNewer()
        {
#if UNITY_2021_2_OR_NEWER
            return true;
#else
            return false;
#endif
        }

        private static string GetPluginFileName(Module module)
        {
            return module.moduleName.ToLowerInvariant() + "_" + module.latestVersion + ".unitypackage";
        }

        #endregion
    }
}