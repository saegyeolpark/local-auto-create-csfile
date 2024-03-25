//
//  GliderInitialize.cs
//  Gameduo Glider Unity Plugin
//
//  Created by Seungjun on 12/26/23.
//  Copyright © 2023 Gameduo. All rights reserved.
//


using GliderSdk.Scripts.SdkVersionManager.Editor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;

namespace GliderSdk.Scripts.Editor
{
    [InitializeOnLoad]
    public class GliderInitialize
    {
        private static readonly List<string> ObsoleteNetworks = new List<string>
        {
            "Test",
        };

#if UNITY_2021_2_OR_NEWER
        private static readonly List<string> ObsoleteFileExportPathsToDelete = new List<string>
        {
            // The `EventSystemChecker` has been renamed to `GliderEventSystemChecker`.
            "GliderSdk/Scripts/EventSystemChecker.cs",
            "GliderSdk/Scripts/EventSystemChecker.cs.meta",
        };
#endif

        static GliderInitialize()
        {
#if UNITY_IOS
            // Check that the publisher is targeting iOS 9.0+
            if (!PlayerSettings.iOS.targetOSVersionString.StartsWith("9.") && !PlayerSettings.iOS.targetOSVersionString.StartsWith("1"))
            {
                GliderSdkLogger.UserError("Detected iOS project version less than iOS 9 - The Glider SDK WILL NOT WORK ON < iOS9!!!");
            }
#endif

            var pluginParentDir = GliderSdkVersionManager.PluginParentDirectory;
            var isPluginOutsideAssetsDir = GliderSdkVersionManager.IsPluginOutsideAssetsDirectory;
            var changesMade =
                GliderSdkVersionManager.MovePluginFilesIfNeeded(pluginParentDir, isPluginOutsideAssetsDir);
            if (isPluginOutsideAssetsDir)
            {
                // If the plugin is not under the assets folder, delete the GliderSdk/Mediation folder in the plugin, so that the adapters are not imported at that location and imported to the default location.
                var mediationDir = Path.Combine(pluginParentDir, "GliderSdk/Mediation");
                if (Directory.Exists(mediationDir))
                {
                    FileUtil.DeleteFileOrDirectory(mediationDir);
                    FileUtil.DeleteFileOrDirectory(mediationDir + ".meta");
                    changesMade = true;
                }
            }

            GliderSettings.Get();
            GliderSdkVersionManager.AddLabelsToAssetsIfNeeded(pluginParentDir, isPluginOutsideAssetsDir);

#if UNITY_2021_2_OR_NEWER
            foreach (var obsoleteFileExportPathToDelete in ObsoleteFileExportPathsToDelete)
            {
                var pathToDelete = GliderSdkUtils.GetAssetPathForExportPath(obsoleteFileExportPathToDelete);
                if (CheckExistence(pathToDelete))
                {
                    GliderSdkLogger.UserDebug(
                        "Deleting obsolete file '" + pathToDelete + "' that are no longer needed.");
                    FileUtil.DeleteFileOrDirectory(pathToDelete);
                    changesMade = true;
                }
            }
#endif

            // Check if any obsolete networks are installed
            foreach (var obsoleteNetwork in ObsoleteNetworks)
            {
                var networkDir = Path.Combine(pluginParentDir, "Glider/Modules/" + obsoleteNetwork);
                if (CheckExistence(networkDir))
                {
                    GliderSdkLogger.UserDebug("Deleting obsolete network " + obsoleteNetwork + " from path " +
                                              networkDir + "...");
                    FileUtil.DeleteFileOrDirectory(networkDir);
                    changesMade = true;
                }
            }

            // Refresh UI
            if (changesMade)
            {
                AssetDatabase.Refresh();
                GliderSdkLogger.UserDebug("Gameduo Glider Migration completed");
            }

            GliderSdkAutoUpdater.Update();
        }

        private static bool CheckExistence(string location)
        {
            return File.Exists(location) ||
                   Directory.Exists(location) ||
                   (location.EndsWith("/*") && Directory.Exists(Path.GetDirectoryName(location)));
        }
    }
}