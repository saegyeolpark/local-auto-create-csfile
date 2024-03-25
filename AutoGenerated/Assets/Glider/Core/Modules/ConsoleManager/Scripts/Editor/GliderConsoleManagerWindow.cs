//
//  GliderConsoleManagerWindow.cs
//  Gameduo Glider Unity Plugin
//
//  Created by Seungjun on 12/26/23.
//  Copyright © 2023 Gameduo. All rights reserved.
//

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Glider.Core.ConsoleManager.Editor
{
    public class GliderConsoleManagerWindow : EditorWindow
    {
        private const string windowTitle = "Gameduo Center Manager";
        private const string gliderSdkKeyLink = "https://duocenter-admin.gameduo.net/sdk";
        private const string footerLink = "https://gameduo.net";
        private const string footerNote = "ⓒ gameduo Korea Corporation All Rights Reserved.";

        private readonly string[] staticDataTypes = new string[] { "Shared", "GameServer" };

        private enum StaticDataCategory
        {
            Shared,
            GameServer
        }

        private Vector2 scrollPosition;
        private static readonly Vector2 windowMinSize = new Vector2(800, 750);
        private const float actionFieldWidth = 60f;
        private const float upgradeAllButtonWidth = 80f;
        private const float networkFieldMinWidth = 200;
        private const float versionFieldMinWidth = 140f;
        private const float privacySettingLabelWidth = 250f;
        private const float networkFieldWidthPercentage = 0.22f;

        private const float
            versionFieldWidthPercentage =
                0.36f; // There are two version fields. Each take 40% of the width, network field takes the remaining 20%.

        private static float previousWindowWidth = windowMinSize.x;
        private static GUILayoutOption networkWidthOption = GUILayout.Width(networkFieldMinWidth);
        private static GUILayoutOption versionWidthOption = GUILayout.Width(versionFieldMinWidth);

        private static GUILayoutOption privacySettingFieldWidthOption = GUILayout.Width(400);
        private static readonly GUILayoutOption fieldWidth = GUILayout.Width(actionFieldWidth);
        private static readonly GUILayoutOption upgradeAllButtonFieldWidth = GUILayout.Width(upgradeAllButtonWidth);

        private static readonly Color darkModeTextColor = new Color(0.29f, 0.6f, 0.8f);

        private GUIStyle titleLabelStyle;
        private GUIStyle headerLabelStyle;
        private GUIStyle environmentValueStyle;
        private GUIStyle wrapTextLabelStyle;
        private GUIStyle linkLabelStyle;

        private string centerAuthEmail = string.Empty;
        private string centerAuthPassword = string.Empty;
        private StaticDataCategory staticDataCategory;


        private CancellationTokenSource cts;
        private CancellationToken token;

        public static void ShowManager()
        {
            var manager = GetWindow<GliderConsoleManagerWindow>(utility: true, title: windowTitle, focus: true);
            manager.minSize = windowMinSize;
        }

        #region Editor Window Lifecyle Methods

        private void Awake()
        {
            titleLabelStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                fixedHeight = 20
            };

            headerLabelStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                fixedHeight = 18
            };

            environmentValueStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleRight
            };

            linkLabelStyle = new GUIStyle(EditorStyles.label)
            {
                wordWrap = true,
                normal = { textColor = EditorGUIUtility.isProSkin ? darkModeTextColor : Color.blue }
            };

            wrapTextLabelStyle = new GUIStyle(EditorStyles.label)
            {
                wordWrap = true
            };
        }

        private void OnEnable()
        {
            cts?.Cancel();
            cts?.Dispose();
            cts = new CancellationTokenSource();
            token = cts.Token;

            LoadAsync().Forget();
        }

        private void OnDisable()
        {
            cts?.Cancel();
            cts?.Dispose();
            GliderConsoleManager.Get().Reset();
            EditorUtility.ClearProgressBar();

            // Saves the GliderSettings object if it has been changed.
            AssetDatabase.SaveAssets();
        }

        private void OnGUI()
        {
            // OnGUI is called on each frame draw, so we don't want to do any unnecessary calculation if we can avoid it. So only calculate it when the width actually changed.
            if (Math.Abs(previousWindowWidth - position.width) > 1)
            {
                previousWindowWidth = position.width;
                CalculateFieldWidth();
            }

            using (var scrollView = new EditorGUILayout.ScrollViewScope(scrollPosition, false, false))
            {
                scrollPosition = scrollView.scrollPosition;

                GUILayout.Space(5);


                if (!GliderConsoleManager.Get().HasAccessToken)
                {
                    // Draw Auth
                    EditorGUILayout.LabelField("Auth", titleLabelStyle);
                    DrawAuthSettings();
                }
                else
                {
                    // Draw SDK Settings
                    EditorGUILayout.LabelField("StaticData Manage", titleLabelStyle);
                    if (GliderConsoleManager.Get().IsConvertingSheet)
                    {
                        DrawInLoading();
                    }
                    else
                    {
                        DrawStaticDataManage();
                    }

                    GUILayout.Space(15);
                    if (GUILayout.Button("Logout", GUILayout.Width(80)))
                    {
                        GliderConsoleManager.Get().LogoutCenter();
                    }
                }

                // Draw documentation notes
                EditorGUILayout.LabelField(new GUIContent(footerNote), wrapTextLabelStyle);
                if (GUILayout.Button(new GUIContent(footerLink), linkLabelStyle))
                {
                    Application.OpenURL(footerLink);
                }
            }
        }

        #endregion

        #region UI Methods

        private void DrawAuthSettings()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                GUILayout.Space(4);
                centerAuthEmail = DrawTextField("email", centerAuthEmail, GUILayout.Width(privacySettingLabelWidth),
                    privacySettingFieldWidthOption);
                centerAuthPassword = DrawPasswordField("password", centerAuthPassword,
                    GUILayout.Width(privacySettingLabelWidth), privacySettingFieldWidthOption);


                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUI.enabled = centerAuthEmail?.Length > 0 && centerAuthPassword?.Length > 0;
                if (GUILayout.Button(new GUIContent("Login"), GUILayout.Width(200)))
                {
                    //로그인
                    GliderConsoleManager.Get().LoginCenterAsync(token, centerAuthEmail, centerAuthPassword).Forget();
                }

                GUI.enabled = true;
                GUILayout.Space(5);
                GUILayout.EndHorizontal();

                GUILayout.Space(5);


                GUILayout.Space(4);
            }

            GUILayout.Space(5);
            GUILayout.EndHorizontal();
        }


        private void DrawInLoading()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField("Loading···", GUILayout.Width(60));
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(5);
            GUILayout.EndHorizontal();
        }

        private void DrawStaticDataManage()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                GUILayout.Space(4);


                {
                    GUILayout.Space(4);
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(4);
                    EditorGUILayout.LabelField("Sheet to C# File", GUILayout.Width(250));
                    GUILayout.Space(4);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Space(4);
                    using (new EditorGUILayout.VerticalScope("box"))
                    {
                        GUILayout.Space(2);

                        GUILayout.BeginHorizontal();

                        if (GUILayout.Button(new GUIContent("Update"), GUILayout.Width(80)))
                        {
                            if (!GliderConsoleManager.Get().IsConvertingSheet)
                            {
                                GliderConsoleManager.Get().SheetConvertAsync(token).Forget();
                            }
                        }
                        // if (GUILayout.Button(new GUIContent("Static Data"), GUILayout.Width(200)))
                        // {
                        // 	if (!GliderConsoleManager.Instance.GoogleSheet.IsProcessing)
                        // 	{
                        // 		GliderConsoleManager.Instance.GoogleSheet.WriteSheetStaticDataClassesToCsFile(staticDataCategory).Forget();
                        // 	}
                        // }
                        // if(GUILayout.Button(new GUIContent("Cloud Data"), GUILayout.Width(200)))
                        // {
                        // 	if (!GliderConsoleManager.Instance.GoogleSheet.IsProcessing)
                        // 	{
                        // 		GliderConsoleManager.Instance.GoogleSheet.WriteSheetCloudDataClassesToCsFile(staticDataCategory).Forget();
                        // 	}
                        // }
                        // if (GUILayout.Button(new GUIContent("Wrapper"), GUILayout.Width(120)))
                        // {
                        // 	if (!GliderConsoleManager.Instance.GoogleSheet.IsProcessing)
                        // 		GliderConsoleManager.Instance.GoogleSheet.WriteSheetWrapperToCsFile(staticDataCategory).Forget();
                        // }

                        GUILayout.Space(5);
                        GUILayout.FlexibleSpace();
                        GUILayout.EndHorizontal();
                    }

                    GUILayout.EndHorizontal();
                }
            }

            GUILayout.Space(5);
            GUILayout.EndHorizontal();
        }

        private string DrawTextField(string fieldTitle, string text, GUILayoutOption labelWidth,
            GUILayoutOption textFieldWidthOption = null, bool isTextFieldEditable = true)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(4);
            EditorGUILayout.LabelField(new GUIContent(fieldTitle), labelWidth);
            GUILayout.Space(4);
            if (isTextFieldEditable)
            {
                text = (textFieldWidthOption == null)
                    ? GUILayout.TextField(text)
                    : GUILayout.TextField(text, textFieldWidthOption);
            }
            else
            {
                if (textFieldWidthOption == null)
                {
                    GUILayout.Label(text);
                }
                else
                {
                    GUILayout.Label(text, textFieldWidthOption);
                }
            }

            GUILayout.Space(4);
            GUILayout.EndHorizontal();
            GUILayout.Space(4);

            return text;
        }

        private string DrawPasswordField(string fieldTitle, string text, GUILayoutOption labelWidth,
            GUILayoutOption textFieldWidthOption = null)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(4);
            EditorGUILayout.LabelField(new GUIContent(fieldTitle), labelWidth);
            GUILayout.Space(4);
            text = (textFieldWidthOption == null)
                ? GUILayout.PasswordField(text, '*')
                : GUILayout.PasswordField(text, '*', textFieldWidthOption);

            GUILayout.Space(4);
            GUILayout.EndHorizontal();
            GUILayout.Space(4);

            return text;
        }

        private bool DrawToggle(bool value, string text)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(4);
                var toggleValue = GUILayout.Toggle(value, text);
                GUILayout.Space(4);

                return toggleValue;
            }
        }


        private void DrawKeyValueRow(string key, string value)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(5);
                EditorGUILayout.LabelField(key, GUILayout.Width(250));
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField(value, environmentValueStyle);
                GUILayout.Space(5);
            }
        }

        /// <summary>
        /// Calculates the fields width based on the width of the window.
        /// </summary>
        private void CalculateFieldWidth()
        {
            var currentWidth = position.width;
            var availableWidth =
                currentWidth - actionFieldWidth -
                80; // NOTE: Magic number alert. This is the sum of all the spacing the fields and other UI elements.
            var networkLabelWidth = Math.Max(networkFieldMinWidth, availableWidth * networkFieldWidthPercentage);
            networkWidthOption = GUILayout.Width(networkLabelWidth);

            var versionLabelWidth = Math.Max(versionFieldMinWidth, availableWidth * versionFieldWidthPercentage);
            versionWidthOption = GUILayout.Width(versionLabelWidth);

            const int
                textFieldOtherUiElementsWidth =
                    45; // NOTE: Magic number alert. This is the sum of all the spacing the fields and other UI elements.
            var availableUserDescriptionTextFieldWidth =
                currentWidth - privacySettingLabelWidth - textFieldOtherUiElementsWidth;
            privacySettingFieldWidthOption = GUILayout.Width(availableUserDescriptionTextFieldWidth);
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Loads the plugin data to be displayed by this window.
        /// </summary>
        private async UniTask LoadAsync()
        {
            GliderConsoleManager.Get().ReadCenterAccessTokenFromFile();
            if (!GliderConsoleManager.Get().HasAccessToken)
            {
                await GliderConsoleManager.Get().LoadCenterAccessToken(token);
            }

            CalculateFieldWidth();
            Repaint();
        }

        #endregion
    }
}