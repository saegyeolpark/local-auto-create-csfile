using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace GliderSdk.Scripts.SdkVersionManager.Editor
{
    public class GliderSdkAutoUpdater
    {
        public const string KeyAutoUpdateEnabled = "net.gameduo.auto_update_enabled";
#if !UNITY_2021_2_OR_NEWER
        private const string KeyOldUnityVersionWarningShown = "net.gameduo.old_unity_version_warning_shown";
#endif
        private const string
            KeyLastUpdateCheckTime =
                "net.gameduo.last_update_check_time_v2"; // Updated to v2 to force adapter version checks in plugin version 3.1.10.

        private static readonly DateTime EpochTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private static readonly int SecondsInADay = (int)TimeSpan.FromDays(1).TotalSeconds;

        // TODO: Make this list dynamic.
        public static readonly Dictionary<string, string> MinAdapterVersions = new Dictionary<string, string>()
        {
            { "TEST", "android_4.2.3.1_ios_4.3.1.1" },
        };

        /// <summary>
        /// Checks if a new version of the plugin is available and prompts the user to update if one is available.
        /// </summary>
        public static void Update()
        {
            var now = (int)(DateTime.UtcNow - EpochTime).TotalSeconds;
            if (EditorPrefs.HasKey(KeyLastUpdateCheckTime))
            {
                var elapsedTime = now - EditorPrefs.GetInt(KeyLastUpdateCheckTime);

                // Check if we have checked for a new version in the last 24 hrs and skip update if we have.
                if (elapsedTime < SecondsInADay) return;
            }

            // Update last checked time.
            EditorPrefs.SetInt(KeyLastUpdateCheckTime, now);

#if !UNITY_2021_2_OR_NEWER
            ShowNotSupportingOldUnityVersionsIfNeeded();
#endif

            // Load the plugin data
            GliderSdkEditorCoroutine.StartCoroutine(GliderSdkVersionManager.Instance.LoadPluginData(data =>
            {
                if (data == null) return;

                ShowPluginUpdateDialogIfNeeded(data);
                //ShowNetworkAdaptersUpdateDialogIfNeeded(data.list);
            }));
        }

        [Serializable]
        internal class MoudleVersionRead
        {
            public string version;
        }

        private static void ShowPluginUpdateDialogIfNeeded(ModuleWrapper wrapper)
        {
            // Check if publisher has disabled auto update.
            if (!EditorPrefs.GetBool(KeyAutoUpdateEnabled, true)) return;

            // Check if the current and latest version are the same or if the publisher is on a newer version (on beta). If so, skip update.
            var gliderCore = wrapper.list[1];
            // find GliderCore
            foreach (var value in wrapper.list)
            {
                if (value.moduleName == "GliderCore")
                {
                    gliderCore = value;
                    break;
                }
            }

            gliderCore.CurrentVersion = Newtonsoft.Json.JsonConvert
                .DeserializeObject<MoudleVersionRead>(System.IO.File.ReadAllText(gliderCore.dependenciesFilePath))
                .version;

            var comparison = gliderCore.CurrentToLatestVersionComparisonResult;
            if (comparison == GliderSdkUtils.VersionComparisonResult.Equal ||
                comparison == GliderSdkUtils.VersionComparisonResult.Greater) return;

            // A new version of the plugin is available. Show a dialog to the publisher.
            var option = EditorUtility.DisplayDialogComplex(
                "Gameduo GliderCore Plugin Update",
                $"A new version of Gameduo GliderCore({gliderCore.CurrentVersion} -> {gliderCore.latestVersion}) plugin is available for download. Update now?",
                "Download",
                "Not Now",
                "Don't Ask Again");

            if (option == 0) // Download
            {
                GliderSdkLogger.UserDebug("Downloading GliderCore...");
                GliderSdkVersionManager.downloadPluginProgressCallback =
                    GliderSdkVersionManagerWindow.OnDownloadPluginProgress;
                // 이전 GliderCore 제거
                GliderSdkVersionManager.Instance.DeleteModule(
                    gliderCore.dependenciesFilePath.Replace("/Editor/Dependencies.json", ""));
                // 새로운 GliderCore 설치
                GliderSdkEditorCoroutine.StartCoroutine(
                    GliderSdkVersionManager.Instance.DownloadPlugin(gliderCore, isDone => { }));
            }
            else if (option == 1) // Not Now
            {
                // Do nothing
                GliderSdkLogger.UserDebug("Update postponed.");
            }
            else if (option == 2) // Don't Ask Again
            {
                GliderSdkLogger.UserDebug(
                    "Auto Update disabled. You can enable it again from the Gameduo SDK Version Manager");
                EditorPrefs.SetBool(KeyAutoUpdateEnabled, false);
            }
        }

        private static void ShowNetworkAdaptersUpdateDialogIfNeeded(Module[] networks)
        {
            var networksToUpdate = networks.Where(network => network.RequiresUpdate).ToList();

            // If all networks are above the required version, do nothing.
            if (networksToUpdate.Count <= 0) return;

            // We found a few adapters that are not compatible with the current SDK, show alert.
            var message =
                "The following network adapters are not compatible with the current version of Gameduo Glider Plugin:\n";
            foreach (var networkName in networksToUpdate)
            {
                message += "\n- ";
                message += networkName.moduleName + " (Requires " + MinAdapterVersions[networkName.moduleName] +
                           " or newer)";
            }

            message += "\n\nPlease update them to the latest versions to avoid any issues.";

            GliderSdkVersionManager.ShowBuildFailureDialog(message);
        }


#if !UNITY_2021_2_OR_NEWER
        private static void ShowNotSupportingOldUnityVersionsIfNeeded()
        {
            // Check if publisher has seen the warning before
            if (EditorPrefs.GetBool(KeyOldUnityVersionWarningShown, false)) return;

            // Show a dialog if they haven't seen the warning yet.
            var option = EditorUtility.DisplayDialog(
                "WARNING: Old Unity Version Detected",
                "Gameduo Glider Unity plugin will soon require Unity 2021.2 or newer to function. Please upgrade to a newer Unity version.",
                "Ok",
                "Don't Ask Again"
            );

            if (!option) // 'false' means `Don't Ask Again` was clicked.
            {
                EditorPrefs.SetBool(KeyOldUnityVersionWarningShown, true);
            }
        }
#endif

        private static bool GoogleNetworkAdaptersCompatible(string googleVersion, string googleAdManagerVersion,
            string breakingVersion)
        {
            var googleResult = GliderSdkUtils.CompareVersions(googleVersion, breakingVersion);
            var googleAdManagerResult = GliderSdkUtils.CompareVersions(googleAdManagerVersion, breakingVersion);

            // If one is less than the breaking version and the other is not, they are not compatible.
            if (googleResult == GliderSdkUtils.VersionComparisonResult.Lesser &&
                googleAdManagerResult != GliderSdkUtils.VersionComparisonResult.Lesser) return false;

            if (googleAdManagerResult == GliderSdkUtils.VersionComparisonResult.Lesser &&
                googleResult != GliderSdkUtils.VersionComparisonResult.Lesser) return false;

            return true;
        }
    }
}