//
//  GliderSdkIntegrationManagerWindow.cs
//  Gameduo Glider Unity Plugin
//
//  Created by Seungjun on 12/26/23.
//  Copyright © 2023 Gameduo. All rights reserved.
//

using UnityEditor;
using UnityEngine;

namespace Glider.Core.ConsoleManager.Editor
{
    public class GliderConsoleManagerMenuItems
    {
        /**
         * The special characters at the end represent a shortcut for this action.
         *
         * % - ctrl on Windows, cmd on macOS
         * # - shift
         * & - alt
         *
         * So, (shift + cmd/ctrl + c) will launch the integration manager
         */
        [MenuItem("Gameduo/Console Manager %#c", false, 2)]
        private static void ConsoleManager()
        {
            ShowConsoleManager();
        }

        private static void ShowConsoleManager()
        {
            GliderConsoleManagerWindow.ShowManager();
        }
    }
}