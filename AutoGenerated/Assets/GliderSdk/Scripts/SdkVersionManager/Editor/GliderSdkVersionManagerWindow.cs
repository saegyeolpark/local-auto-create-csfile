//
//  GliderSdkIntegrationManagerWindow.cs
//  Gameduo Glider Unity Plugin
//
//  Created by Seungjun on 12/26/23.
//  Copyright © 2023 Gameduo. All rights reserved.
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GliderSdk.Scripts.DependencyManager.Editor;
using UnityEditor;
using UnityEngine;
using VersionComparisonResult = GliderSdkUtils.VersionComparisonResult;

namespace GliderSdk.Scripts.SdkVersionManager.Editor
{
    public class GliderSdkVersionManagerWindow : EditorWindow
    {
        private const string windowTitle = "Gameduo SDK Version Manager";

        private const string gliderSdkKeyLink = "https://duocenter-admin.gameduo.net/sdk";
        private const string footerLink = "https://gameduo.net";
        private const string footerNote = "ⓒ gameduo Korea Corporation All Rights Reserved.";
        private const string uninstallIconExportPath = "GliderSdk/Resources/Images/uninstall_icon.png";
        private const string alertIconExportPath = "GliderSdk/Resources/Images/alert_icon.png";
        private const string warningIconExportPath = "GliderSdk/Resources/Images/warning_icon.png";
        private const string photonAppIdLink = "https://dashboard.photonengine.com/";

        private const string qualityServiceRequiresGradleBuildErrorMsg =
            "Gameduo Glider Quality Service integration via Gameduo Integration Manager requires Custom Gradle Template enabled or Unity 2018.2 or higher.\n" +
            "If you would like to continue using your existing setup, please add Quality Service Plugin to your build.gradle manually.";


        private readonly string[] envNames = new string[3] { "Live", "Sandbox", "Local" };
        private Vector2 scrollPosition;
        private static readonly Vector2 windowMinSize = new Vector2(800, 750);
        private const float actionFieldWidth = 60f;
        private const float upgradeAllButtonWidth = 80f;
        private const float networkFieldMinWidth = 180f;
        private const float versionFieldMinWidth = 140f;
        private const float privacySettingLabelWidth = 250f;
        private const float networkFieldWidthPercentage = 0.22f;

        private const float
            versionFieldWidthPercentage =
                0.36f; // There are two version fields. Each take 40% of the width, network field takes the remaining 20%.

        private static float previousWindowWidth = windowMinSize.x;
        private static GUILayoutOption networkWidthOption = GUILayout.Width(networkFieldMinWidth);
        private static GUILayoutOption versionWidthOption = GUILayout.Width(versionFieldMinWidth);

        private static GUILayoutOption settingValueFieldWidthOption = GUILayout.Width(360);
        private static readonly GUILayoutOption fieldWidth = GUILayout.Width(actionFieldWidth);
        private static readonly GUILayoutOption upgradeAllButtonFieldWidth = GUILayout.Width(upgradeAllButtonWidth);

        private static readonly Color darkModeTextColor = new Color(0.29f, 0.6f, 0.8f);

        private GUIStyle titleLabelStyle;
        private GUIStyle headerLabelStyle;
        private GUIStyle environmentValueStyle;
        private GUIStyle wrapTextLabelStyle;
        private GUIStyle linkLabelStyle;
        private GUIStyle iconStyle;

        private ModuleWrapper moduleWrapper;
        private string[] notInstalledGliders;
        private bool pluginDataLoadFailed;
        private bool isPluginMoved;
        private bool networkButtonsEnabled = true;
        private bool upgradeAllEnabled = true;

        private GliderSdkEditorCoroutine loadDataCoroutine;
        private Texture2D uninstallIcon;
        private Texture2D alertIcon;
        private Texture2D warningIcon;

        public static void ShowManager()
        {
            var manager =
                GetWindow<GliderSdkVersionManagerWindow>(utility: true, title: windowTitle, focus: true);
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

            iconStyle = new GUIStyle(EditorStyles.miniButton)
            {
                fixedWidth = 18,
                fixedHeight = 18,
                padding = new RectOffset(1, 1, 1, 1)
            };

            // Load uninstall icon texture.
            var uninstallIconData =
                File.ReadAllBytes(GliderSdkUtils.GetAssetPathForExportPath(uninstallIconExportPath));
            uninstallIcon =
                new Texture2D(0, 0, TextureFormat.RGBA32,
                    false); // 1. Initial size doesn't matter here, will be automatically resized once the image asset is loaded. 2. Set mipChain to false, else the texture has a weird blurry effect.
            uninstallIcon.LoadImage(uninstallIconData);

            // Load alert icon texture.
            var alertIconData = File.ReadAllBytes(GliderSdkUtils.GetAssetPathForExportPath(alertIconExportPath));
            alertIcon = new Texture2D(0, 0, TextureFormat.RGBA32, false);
            alertIcon.LoadImage(alertIconData);

            // Load warning icon texture.
            var warningIconData = File.ReadAllBytes(GliderSdkUtils.GetAssetPathForExportPath(warningIconExportPath));
            warningIcon = new Texture2D(0, 0, TextureFormat.RGBA32, false);
            warningIcon.LoadImage(warningIconData);
        }

        private void OnEnable()
        {
            GliderSdkVersionManager.downloadPluginProgressCallback = OnDownloadPluginProgress;

            // Plugin downloaded and imported. Update current versions for the imported package.
            GliderSdkVersionManager.importPackageCompletedCallback = OnImportPackageCompleted;

            GliderSdkDependencyManager.Instance.gliderImportCallback = null;
            GliderSdkDependencyManager.Instance.gliderImportAllCallback = null;
            GliderSdkDependencyManager.Instance.gliderImportCallback += OnInstallGliderModuleDependencies;
            GliderSdkDependencyManager.Instance.gliderImportAllCallback += OnInstallAllGliderModuleDependencies;
            Load();
        }

        private void OnDisable()
        {
            if (loadDataCoroutine != null)
            {
                loadDataCoroutine.Stop();
                loadDataCoroutine = null;
            }

            GliderSdkVersionManager.Instance.CancelDownload();
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

                GUI.enabled = upgradeAllEnabled;
                // Draw Core module details
                EditorGUILayout.LabelField("GliderSDK", titleLabelStyle);
                DrawSdkDetails();

                // Draw mediated networks
                using (new EditorGUILayout.HorizontalScope(GUILayout.ExpandHeight(false)))
                {
                    EditorGUILayout.LabelField("Modules", titleLabelStyle);
                    DrawUpgradeAllButton();
                }

                DrawModules();
                GUI.enabled = true;

                // Draw Glider Quality Service settings
                EditorGUILayout.LabelField("SDK Settings", titleLabelStyle);
                DrawSDKSettings();

                EditorGUILayout.LabelField("Env Settings", titleLabelStyle);
                DrawEnvSettings();

                EditorGUILayout.LabelField("Privacy Settings", titleLabelStyle);
                DrawPrivacySettings();

                EditorGUILayout.LabelField("Other Settings", titleLabelStyle);
                DrawOtherSettings();

                // Draw Unity environment details
                EditorGUILayout.LabelField("Unity Environment Details", titleLabelStyle);
                DrawUnityEnvironmentDetails();

                // Draw documentation notes
                GUILayout.Space(15);
                EditorGUILayout.LabelField(new GUIContent(footerNote), wrapTextLabelStyle);
                if (GUILayout.Button(new GUIContent(footerLink), linkLabelStyle))
                {
                    Application.OpenURL(footerLink);
                }
            }

            if (GUI.changed)
            {
                GliderSettings.Get().SaveAsync();
                //GliderInternalSettings.Instance.Save();
            }
        }

        #endregion

        #region UI Methods

        /// <summary>
        /// Shows failure or loading screen based on whether or not plugin data failed to load.
        /// </summary>
        private void DrawEmptyPluginData()
        {
            GUILayout.Space(5);

            // Plugin data failed to load. Show error and retry button.
            if (pluginDataLoadFailed)
            {
                GUILayout.Space(10);
                GUILayout.BeginHorizontal();
                GUILayout.Space(5);
                EditorGUILayout.LabelField(
                    "Failed to load plugin data. Please click retry or restart the integration manager.",
                    titleLabelStyle);
                if (GUILayout.Button("Retry", fieldWidth))
                {
                    pluginDataLoadFailed = false;
                    Load();
                }

                GUILayout.Space(5);
                GUILayout.EndHorizontal();
                GUILayout.Space(10);
            }
            // Still loading, show loading label.
            else
            {
                GUILayout.Space(10);
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField("Loading data...", titleLabelStyle);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.Space(10);
            }

            GUILayout.Space(5);
        }


        /// <summary>
        /// Draws Gameduo Glider plugin details.
        /// </summary>
        private void DrawSdkDetails()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                // Draw plugin version details
                DrawHeaders("Name", false);

                // Immediately after downloading and importing a plugin the entire IDE reloads and current versions can be null in that case. Will just show loading text in that case.
                if (moduleWrapper == null || moduleWrapper.list?.Length == 0)
                {
                    DrawEmptyPluginData();
                }
                else
                {
                    var core = moduleWrapper.list[1];
                    foreach (var module in moduleWrapper.list)
                    {
                        if (module.moduleName == "GliderCore")
                        {
                            core = module;
                            break;
                        }
                    }

                    // Check if a newer version is available to enable the upgrade button.
                    var upgradeButtonEnabled =
                        core.CurrentToLatestVersionComparisonResult == VersionComparisonResult.Lesser;
                    var buttonDesc = core.CurrentVersion == GliderSdkVersionManager.NotInstalled
                        ? "Install"
                        : "Upgrade";
                    var comparison = core.CurrentToLatestVersionComparisonResult;
                    if (comparison == VersionComparisonResult.Lesser == false &&
                        comparison == VersionComparisonResult.Greater == false)
                    {
                        buttonDesc = "Installed";
                    }

                    DrawPluginDetailRow("GliderCore", core.CurrentVersion, core.latestVersion);

                    // BeginHorizontal combined with FlexibleSpace makes sure that the button is centered horizontally.
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();

                    GUI.enabled = upgradeButtonEnabled && upgradeAllEnabled;
                    if (GUILayout.Button(new GUIContent(buttonDesc), fieldWidth))
                    {
                        // TODO dependencies check
                        GliderSdkEditorCoroutine.StartCoroutine(GliderSdkDependencyManager.Instance.CheckDependencies(
                            core.moduleName, core.latestVersion, notInstalledGliders,
                            data =>
                            {
                                if (data == true)
                                {
                                    GliderSdkVersionManager.Instance.DeleteModule(Path.Combine(Application.dataPath,
                                        "Glider/Editor"));
                                    GliderSdkVersionManager.Instance.DeleteModule(Path.Combine(Application.dataPath,
                                        "Glider/Fonts"));
                                    GliderSdkEditorCoroutine.StartCoroutine(
                                        GliderSdkVersionManager.Instance.DownloadPlugin(core, isDone => { Load(); },
                                            true));
                                }
                            }));
                    }

                    GUI.enabled = upgradeAllEnabled;
                    GUILayout.Space(5);
                    GUILayout.EndHorizontal();

                    GUILayout.Space(5);
                }

#if !UNITY_2021_2_OR_NEWER
                EditorGUILayout.HelpBox("Gameduo Glider Unity plugin will soon require Unity 2021.2 or newer to function. Please upgrade to a newer Unity version.", MessageType.Warning);
#endif
            }

            GUILayout.Space(5);
            GUILayout.EndHorizontal();
        }


        /// <summary>
        /// Draws the headers for a table.
        /// </summary>
        private void DrawHeaders(string firstColumnTitle, bool drawAction)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(5);
                EditorGUILayout.LabelField(firstColumnTitle, headerLabelStyle, networkWidthOption);
                EditorGUILayout.LabelField("Current Version", headerLabelStyle, versionWidthOption);
                GUILayout.Space(3);
                EditorGUILayout.LabelField("Latest Version", headerLabelStyle, versionWidthOption);
                GUILayout.Space(3);
                if (drawAction)
                {
                    GUILayout.FlexibleSpace();
                    GUILayout.Button("Actions", headerLabelStyle, fieldWidth);
                    GUILayout.Space(5);
                }
            }

            GUILayout.Space(4);
        }

        /// <summary>
        /// Draws the platform specific version details for Gameduo Glider plugin.
        /// </summary>
        private void DrawPluginDetailRow(string platform, string currentVersion, string latestVersion)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(5);
                EditorGUILayout.LabelField(new GUIContent(platform), networkWidthOption);
                EditorGUILayout.LabelField(new GUIContent(currentVersion), versionWidthOption);
                GUILayout.Space(3);
                EditorGUILayout.LabelField(new GUIContent(latestVersion), versionWidthOption);
                GUILayout.Space(3);
            }

            GUILayout.Space(4);
        }

        /// <summary>
        /// Draws mediated network details table.
        /// </summary>
        private void DrawModules()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                DrawHeaders("Module", true);

                // Immediately after downloading and importing a plugin the entire IDE reloads and current versions can be null in that case. Will just show loading text in that case.
                if (moduleWrapper == null || moduleWrapper.list?.Length < 2)
                {
                    DrawEmptyPluginData();
                }
                else
                {
                    var modules = moduleWrapper.list;
                    for (int i = 0; i < modules.Length; i++)
                    {
                        var module = modules[i];
                        if (module.moduleName == "GliderCore" ||
                            module.moduleName == "GliderSdk")
                        {
                            continue;
                        }

                        DrawModuleDetailRow(module);
                    }

                    GUILayout.Space(10);
                }
            }

            GUILayout.Space(5);
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// Draws the network specific details for a given network.
        /// </summary>
        private void DrawModuleDetailRow(Module module)
        {
            string action;
            var currentVersion = module.CurrentVersion;
            var latestVersion = module.latestVersion;
            bool isActionEnabled;
            bool isInstalled;


            // GliderSdkIntegrationManager의 UpdateCurrentVersions으로 currentVersion이 채워짐
            // Debug.Log(currentVersion);
            if (string.IsNullOrEmpty(currentVersion) || currentVersion.CompareTo("not installed") == 0)
            {
                action = "Install";
                currentVersion = "Not Installed";
                isActionEnabled = true;
                isInstalled = false;
                // Debug.Log(module.moduleName + " " + action);
            }
            else
            {
                isInstalled = true;

                var comparison = module.CurrentToLatestVersionComparisonResult;
                // A newer version is available
                if (comparison == VersionComparisonResult.Lesser)
                {
                    action = "Upgrade";
                    isActionEnabled = true;
                }
                // Current installed version is newer than latest version from DB (beta version)
                else if (comparison == VersionComparisonResult.Greater)
                {
                    action = "Installed";
                    isActionEnabled = false;
                }
                // Already on the latest version
                else
                {
                    action = "Installed";
                    isActionEnabled = false;
                }

                // Debug.Log(module.moduleName + " " + action);
            }

            GUILayout.Space(4);
            using (new EditorGUILayout.HorizontalScope(GUILayout.ExpandHeight(false)))
            {
                GUILayout.Space(5);
                EditorGUILayout.LabelField(new GUIContent(module.moduleName), networkWidthOption);
                EditorGUILayout.LabelField(new GUIContent(currentVersion), versionWidthOption);
                GUILayout.Space(3);
                EditorGUILayout.LabelField(new GUIContent(latestVersion), versionWidthOption);
                GUILayout.Space(3);
                GUILayout.FlexibleSpace();

                if (module.RequiresUpdate)
                {
                    GUILayout.Label(
                        new GUIContent
                        {
                            image = alertIcon, tooltip = "Adapter not compatible, please update to the latest version."
                        }, iconStyle);
                }
                // else if (module.name.Equals("GliderPhotonChat") )
                // {
                //     GUILayout.Label(new GUIContent {image = warningIcon, tooltip = "Adapter not compatible, please update to the latest version."}, iconStyle);
                // }

                GUI.enabled = networkButtonsEnabled && isActionEnabled && upgradeAllEnabled;
                if (GUILayout.Button(
                        new GUIContent
                        {
                            text = action,
                            tooltip =
                                (action == "Upgrade" ? "Delete previous version, and then import new version" : "")
                        }, fieldWidth))
                {
                    // TODO 의존성 체크
                    // Download the plugin.
                    GliderSdkEditorCoroutine.StartCoroutine(GliderSdkDependencyManager.Instance.CheckDependencies(
                        module.moduleName, module.latestVersion, notInstalledGliders,
                        data =>
                        {
                            if (data == true)
                            {
                                GliderSdkEditorCoroutine.StartCoroutine(
                                    GliderSdkVersionManager.Instance.DownloadPlugin(module, isDone => { Load(); },
                                        true));
                            }
                        }));
                }

                GUI.enabled = upgradeAllEnabled;
                GUILayout.Space(2);

                GUI.enabled = networkButtonsEnabled && isInstalled && upgradeAllEnabled;
                if (GUILayout.Button(new GUIContent { image = uninstallIcon, tooltip = "Uninstall" }, iconStyle))
                {
                    EditorUtility.DisplayProgressBar("Integration Manager", "Deleting " + module.moduleName + "...",
                        0.5f);

                    GliderSdkVersionManager.Instance.DeleteModule(module);
                    Load();
                    EditorUtility.ClearProgressBar();
                }

                GUI.enabled = upgradeAllEnabled;
                GUILayout.Space(5);
            }

            if (isInstalled)
            {
                DrawGoogleAppIdTextBoxIfNeeded(module);
            }
        }


        private void DrawGoogleAppIdTextBoxIfNeeded(Module module)
        {
            // Custom integration for AdMob where the user can enter the Android and iOS App IDs.
            if (module.moduleName.Equals("GliderPhotonChat"))
            {
                // Show only one set of text boxes if both ADMOB and GAM are installed
                if (GliderSdkVersionManager.IsModuleInstalled("GliderPhotonChat")) return;
                DrawPhotonKeyTextBox();
            }

            // Custom integration for GAM where the user can enter the Android and iOS App IDs.
            else if (module.moduleName.Equals("GOOGLE_AD_MANAGER_NETWORK"))
            {
                DrawPhotonKeyTextBox();
            }
        }

        /// <summary>
        /// Draws the text box for GAM or ADMOB to input the App ID
        /// </summary>
        private void DrawPhotonKeyTextBox()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                GUILayout.Space(2);
                GliderSettings.Get().PhotonAppId = DrawTextField("Photon App ID", GliderSettings.Get().PhotonAppId,
                    networkWidthOption);

                GUILayout.BeginHorizontal();
                GUILayout.Space(4);
                GUILayout.Button("You can find your Photon App ID here: ", wrapTextLabelStyle, GUILayout.Width(220));
                if (GUILayout.Button(new GUIContent(photonAppIdLink), linkLabelStyle))
                {
                    Application.OpenURL(photonAppIdLink);
                }

                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }

            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// Draws the upgrade all button
        /// </summary>
        private void DrawUpgradeAllButton()
        {
            AssetDatabase.DisallowAutoRefresh();
            EditorApplication.LockReloadAssemblies();

            GUI.enabled = NetworksRequireUpgrade() && upgradeAllEnabled;
            if (GUILayout.Button(new GUIContent("Upgrade All"), upgradeAllButtonFieldWidth))
            {
                upgradeAllEnabled = false;
                GliderSdkEditorCoroutine.StartCoroutine(UpgradeAllNetworks(isDone => { upgradeAllEnabled = true; }));
            }

            GUI.enabled = upgradeAllEnabled;
            GUILayout.Space(10);

            EditorApplication.UnlockReloadAssemblies();
            AssetDatabase.AllowAutoRefresh();
        }

        private void DrawSDKSettings()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                GUILayout.Space(4);
                if (!GliderSdkVersionManager.CanProcessAndroidQualityServiceSettings)
                {
                    GUILayout.Space(4);
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(4);
                    EditorGUILayout.HelpBox(qualityServiceRequiresGradleBuildErrorMsg, MessageType.Warning);
                    GUILayout.Space(4);
                    GUILayout.EndHorizontal();

                    GUILayout.Space(4);
                }

                GliderSettings.Get().ProjectCode = DrawTextField("Project Code", GliderSettings.Get().ProjectCode,
                    GUILayout.Width(privacySettingLabelWidth), settingValueFieldWidthOption);
                GliderSettings.Get().SdkKey = DrawTextField("Glider SDK Key", GliderSettings.Get().SdkKey,
                    GUILayout.Width(privacySettingLabelWidth), settingValueFieldWidthOption);
                // GliderSettings.Instance.Salt = DrawTextField("Glider Salt", GliderSettings.Instance.Salt, GUILayout.Width(privacySettingLabelWidth), settingValueFieldWidthOption);


                GUILayout.BeginHorizontal();
                GUILayout.Space(4);
                GUILayout.Button("You can find your SDK key here: ", wrapTextLabelStyle,
                    GUILayout.Width(
                        200)); // Setting a fixed width since Unity adds arbitrary padding at the end leaving a space between link and text.
                if (GUILayout.Button(new GUIContent(gliderSdkKeyLink), linkLabelStyle))
                {
                    Application.OpenURL(gliderSdkKeyLink);
                }


                GUILayout.FlexibleSpace();

                GUILayout.EndHorizontal();


                // GUILayout.Space(4);
                // GUILayout.BeginHorizontal();
                // GUILayout.Space(4);
                // GameduoSettings.Instance.QualityServiceEnabled = GUILayout.Toggle(GameduoSettings.Instance.QualityServiceEnabled, "  Enable Glider Ad Review");
                // GUILayout.EndHorizontal();
                // GUILayout.Space(4);

                GUILayout.Space(4);
            }

            GUILayout.Space(5);
            GUILayout.EndHorizontal();
        }

        private void DrawEnvSettings()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                //Env
                {
                    GUILayout.Space(4);
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(4);
                    GliderSettings.Get().Env =
                        (Env)EditorGUILayout.Popup("Env", (int)GliderSettings.Get().Env, envNames);
                    GUILayout.Space(4);
                    GUILayout.EndHorizontal();
                    GUILayout.Space(4);

                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Space(4);
                        using (new EditorGUILayout.VerticalScope("box"))
                        {
                            GUILayout.Space(2);
                            GliderSettings.Get().LiveConfigUrl = DrawTextField("Config Url (Live)",
                                GliderSettings.Get().LiveConfigUrl, networkWidthOption);
                            GliderSettings.Get().SandboxConfigUrl = DrawTextField("Config Url (Sandbox)",
                                GliderSettings.Get().SandboxConfigUrl, networkWidthOption);
                            GliderSettings.Get().LocalConfigUrl = DrawTextField("Config Url (Local)",
                                GliderSettings.Get().LocalConfigUrl, networkWidthOption);
                        }

                        GUILayout.EndHorizontal();
                    }
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

        private void DrawPrivacySettings()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                DrawConsentFlowSettings();
            }

            GUILayout.Space(5);
            GUILayout.EndHorizontal();
        }


        private void DrawConsentFlowSettings()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(4);
            GliderSdkInternalSettings.Instance.ConsentFlowEnabled =
                GUILayout.Toggle(GliderSdkInternalSettings.Instance.ConsentFlowEnabled, "  Enable Consent Flow");
            GUILayout.EndHorizontal();
            GUILayout.Space(4);
            GUI.enabled = true;

            if (!GliderSdkInternalSettings.Instance.ConsentFlowEnabled) return;

            GliderSdkInternalSettings.Instance.ConsentFlowPrivacyPolicyUrl = DrawTextField("Privacy Policy URL",
                GliderSdkInternalSettings.Instance.ConsentFlowPrivacyPolicyUrl,
                GUILayout.Width(privacySettingLabelWidth), settingValueFieldWidthOption);
            GliderSdkInternalSettings.Instance.ConsentFlowTermsOfServiceUrl = DrawTextField(
                "Terms of Service URL (optional)", GliderSdkInternalSettings.Instance.ConsentFlowTermsOfServiceUrl,
                GUILayout.Width(privacySettingLabelWidth), settingValueFieldWidthOption);

            GUILayout.Space(4);
            //
            // GUILayout.Space(4);
            // GUILayout.EndHorizontal();
            // GUILayout.Space(4);
        }

        private void DrawOtherSettings()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                GUILayout.Space(5);
                var autoUpdateEnabled = DrawOtherSettingsToggle(
                    EditorPrefs.GetBool(GliderSdkAutoUpdater.KeyAutoUpdateEnabled, true), "  Enable Auto Update");
                EditorPrefs.SetBool(GliderSdkAutoUpdater.KeyAutoUpdateEnabled, autoUpdateEnabled);
                GUILayout.Space(5);
                var verboseLoggingEnabled = DrawOtherSettingsToggle(
                    EditorPrefs.GetBool(GliderSdkLogger.KeyVerboseLoggingEnabled, false),
                    "  Enable Verbose Logging"
                );
                EditorPrefs.SetBool(GliderSdkLogger.KeyVerboseLoggingEnabled, verboseLoggingEnabled);
                GUILayout.Space(5);
            }

            GUILayout.Space(5);
            GUILayout.EndHorizontal();
        }

        private bool DrawOtherSettingsToggle(bool value, string text)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(4);
                var toggleValue = GUILayout.Toggle(value, text);
                GUILayout.Space(4);

                return toggleValue;
            }
        }

        private void DrawUnityEnvironmentDetails()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                DrawUnityEnvironmentDetailRow("Unity Version", Application.unityVersion);
                GUILayout.Space(5);
                DrawUnityEnvironmentDetailRow("Platform", Application.platform.ToString());
                GUILayout.Space(5);
                DrawUnityEnvironmentDetailRow("External Dependency Manager Version",
                    GliderSdkVersionManager.ExternalDependencyManagerVersion);
                GUILayout.Space(5);
                DrawUnityEnvironmentDetailRow("Gradle Template Enabled",
                    GliderSdkVersionManager.GradleTemplateEnabled.ToString());
            }

            GUILayout.Space(5);
            GUILayout.EndHorizontal();
        }

        private void DrawUnityEnvironmentDetailRow(string key, string value)
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
            settingValueFieldWidthOption = GUILayout.Width(availableUserDescriptionTextFieldWidth);
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Loads the plugin data to be displayed by this window.
        /// </summary>
        private void Load()
        {
            loadDataCoroutine = GliderSdkEditorCoroutine.StartCoroutine(
                GliderSdkVersionManager.Instance.LoadPluginData(data =>
                {
                    if (data == null)
                    {
                        pluginDataLoadFailed = true;
                    }
                    else
                    {
                        moduleWrapper = data;
                        pluginDataLoadFailed = false;
                    }

                    var list = new List<string>();
                    foreach (var module in moduleWrapper.list)
                    {
                        if (module.CurrentVersion == "not installed")
                        {
                            list.Add(module.moduleName);
                        }
                    }

                    notInstalledGliders = list.ToArray();

                    CalculateFieldWidth();
                    Repaint();
                }));

            GliderSdkEditorCoroutine.StartCoroutine(GliderSdkDependencyManager.Instance.DownloadDependenciesFile(
                isDone => { AssetDatabase.Refresh(); }));
        }

        /// <summary>
        /// Callback method that will be called with progress updates when the plugin is being downloaded.
        /// </summary>
        public static void OnDownloadPluginProgress(string pluginName, float progress, bool done)
        {
            // Download is complete. Clear progress bar.
            if (done)
            {
                EditorUtility.ClearProgressBar();
            }
            // Download is in progress, update progress bar.
            else
            {
                if (EditorUtility.DisplayCancelableProgressBar(windowTitle,
                        string.Format("Downloading {0} plugin...", pluginName), progress))
                {
                    GliderSdkVersionManager.Instance.CancelDownload();
                    EditorUtility.ClearProgressBar();
                }
            }
        }

        private void OnImportPackageCompleted(Module module)
        {
            var parentDirectory = module.moduleName.Equals("GliderCore")
                ? GliderSdkVersionManager.PluginParentDirectory
                : GliderSdkVersionManager.ModuleSpecificPluginParentDirectory;


            GliderSdkVersionManager.UpdateCurrentVersions(module, parentDirectory);
        }

        private void OnInstallGliderModuleDependencies(string gliderModule)
        {
            Debug.Log($"install {gliderModule}");
            Module module = null;
            foreach (var _module in moduleWrapper.list)
            {
                if (_module.moduleName == gliderModule)
                {
                    module = _module;
                    break;
                }
            }

            GliderSdkEditorCoroutine.StartCoroutine(
                GliderSdkVersionManager.Instance.DownloadPlugin(module,
                    isDone => { GliderSdkDependencyManager.Instance.RemoveFromLocalPayload(gliderModule); }, true,
                    false));
        }

        private void OnInstallAllGliderModuleDependencies(string[] gliderModules)
        {
            Debug.Log("start");

            GliderSdkEditorCoroutine.StartCoroutine(OnInstallAllGliderModuleDependenciesCoroutine(gliderModules));
        }

        IEnumerator OnInstallAllGliderModuleDependenciesCoroutine(string[] gliderModules)
        {
            AssetDatabase.DisallowAutoRefresh();
            EditorApplication.LockReloadAssemblies();

            foreach (var gliderModule in gliderModules)
            {
                Debug.Log($"search {gliderModule}");
                Module module = null;
                foreach (var _module in moduleWrapper.list)
                {
                    if (_module.moduleName == gliderModule)
                    {
                        module = _module;
                        break;
                    }
                }

                var isDone = false;
                GliderSdkEditorCoroutine.StartCoroutine(
                    GliderSdkVersionManager.Instance.DownloadPlugin(module,
                        done =>
                        {
                            isDone = true;
                            GliderSdkDependencyManager.Instance.RemoveFromLocalPayload(gliderModule);
                        },
                        true,
                        false));
                while (isDone == false) yield return null;
            }

            Debug.Log("finish all");
            Load();
            EditorApplication.UnlockReloadAssemblies();
            AssetDatabase.AllowAutoRefresh();
            GliderSdkDependencyManager.LoadLocalPayload();
        }

        /// <summary>
        /// Upgrades all outdated networks
        /// </summary>
        private IEnumerator UpgradeAllNetworks(Action<bool> callback)
        {
            networkButtonsEnabled = false;
            EditorApplication.LockReloadAssemblies();
            var unityDone = false;
            GliderSdkEditorCoroutine.StartCoroutine(GliderSdkDependencyManager.Instance.InstallAllDependencies(isDone =>
            {
                if (isDone)
                {
                    unityDone = true;
                }
            }));
            while (unityDone == false) yield return null;

            Debug.Log("start glider");
            var networks = moduleWrapper.list;
            // foreach (var network in networks)
            for (int i = 0; i < networks.Length; i++) // ignore GliderSdk
            {
                var network = networks[i];
                if (network.moduleName == "GliderSdk")
                    continue;
                Debug.Log($"check upgrade {network.moduleName}");

                var comparison = network.CurrentToLatestVersionComparisonResult;
                // A newer version is available
                if (!string.IsNullOrEmpty(network.CurrentVersion) &&
                    comparison == VersionComparisonResult.Lesser)
                {
                    Debug.Log($"upgrade {network.moduleName}...");
                    yield return GliderSdkVersionManager.Instance.DownloadPlugin(network, isDone => { Load(); }, true,
                        false);
                }
            }

            EditorApplication.UnlockReloadAssemblies();
            networkButtonsEnabled = true;

            // The pluginData becomes stale after the networks have been updated, and we should re-load it.
            Load();

            callback(true);
        }

        /// <summary>
        /// Returns whether any network adapter needs to be upgraded
        /// </summary>
        private bool NetworksRequireUpgrade()
        {
            if (moduleWrapper == null || moduleWrapper.list?.Length == 0) return false;

            var networks = moduleWrapper.list;
            return networks.Any(network =>
                !string.IsNullOrEmpty(network.CurrentVersion) && network.CurrentToLatestVersionComparisonResult ==
                VersionComparisonResult.Lesser);
        }

        #endregion
    }
}