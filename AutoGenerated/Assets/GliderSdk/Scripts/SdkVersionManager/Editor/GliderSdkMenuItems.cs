//
//  GliderSdkIntegrationManagerWindow.cs
//  Gameduo Glider Unity Plugin
//
//  Created by Seungjun on 12/26/23.
//  Copyright © 2023 Gameduo. All rights reserved.
//

using UnityEditor;
using UnityEngine;

namespace GliderSdk.Scripts.SdkVersionManager.Editor
{
    public class GliderSdkMenuItems
    {
        /**
         * The special characters at the end represent a shortcut for this action.
         *
         * % - ctrl on Windows, cmd on macOS
         * # - shift
         * & - alt
         *
         * So, (shift + cmd/ctrl + i) will launch the integration manager
         */
        [MenuItem("Gameduo/SDK Version Manager %#i", false, 1)]
        private static void IntegrationManager()
        {
            ShowSdkVersionManager();
        }

        [MenuItem("Gameduo/Documentation", false, 10)]
        private static void Documentation()
        {
            Application.OpenURL("https://glider.gameduo.net/documentation");
        }

        [MenuItem("Gameduo/Contact Us", false, 11)]
        private static void ContactUs()
        {
            Application.OpenURL("https://glider.gameduo.net/contact/");
        }

        [MenuItem("Gameduo/About", false, 12)]
        private static void About()
        {
            Application.OpenURL("https://glider.gameduo.net/about/");
        }

        [MenuItem("Edit/Clear All EditorPrefs", false, 279)]
        private static void ClearAllEditorPrefs()
        {
            EditorPrefs.DeleteAll();
            Debug.Log("Cleared All EditorPrefs");
        }

        private static void ShowSdkVersionManager()
        {
            GliderSdkVersionManagerWindow.ShowManager();
        }
    }
}