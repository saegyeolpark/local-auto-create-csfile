#if UNITY_EDITOR

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GliderSdk.Scripts.SdkVersionManager.Editor;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace GliderSdk.Scripts.DependencyManager.Editor
{
    public class GliderSdkDependencyManagerWindow : EditorWindow
    {
        private const string windowTitle = "Gameduo Dependency Manager";
        private static readonly Vector2 windowMinSize = new Vector2(400, 300);

        private static string[] _unity = new string[] { };
        private static string[] _glider = new string[] { };
        private bool _whileImporting = false;


        private const float actionFieldWidth = 60f;

        private GUIStyle titleLabelStyle;
        private GUIStyle headerLabelStyle;
        private GUIStyle linkLabelStyle;

        private static readonly GUILayoutOption fieldWidth = GUILayout.Width(actionFieldWidth);
        private static readonly GUILayoutOption doubleFieldWidth = GUILayout.Width(2 * actionFieldWidth);
        private static readonly GUILayoutOption quadFieldWidth = GUILayout.Width(4 * actionFieldWidth);

        private static readonly Color darkModeTextColor = new Color(0.29f, 0.6f, 0.8f);

        public static void ShowManager()
        {
            var manager = GetWindow<GliderSdkDependencyManagerWindow>(utility: true, title: windowTitle, focus: true);
            manager.minSize = windowMinSize;
        }


        #region Editor Window Lifecycle Methods

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
            linkLabelStyle = new GUIStyle(EditorStyles.label)
            {
                wordWrap = true,
                normal = { textColor = EditorGUIUtility.isProSkin ? darkModeTextColor : Color.blue }
            };
        }

        private void OnEnable()
        {
            Load();
        }

        private void OnDisable()
        {
        }

        private void OnGUI()
        {
            if (_whileImporting)
                GUI.enabled = false;
            else
                GUI.enabled = true;

            if (_unity is not null)
                if (_unity.Length > 0)
                {
                    DrawTitle("Unity Requirements");
                    EditorGUILayout.Space(4);
                    DrawUnity();
                }

            if (_glider is not null)
                if (_glider.Length > 0)
                {
                    DrawTitle("Glider Requirements");
                    EditorGUILayout.Space(4);
                    DrawGlider();
                }

            if (_unity.Length > 0 && _glider.Length > 0)
                DrawInstallAll();

            if (_unity.Length < 1 && _glider.Length < 1)
                Close();
        }

        void Load()
        {
            var localPayload = GliderSdkDependencyManager.LoadLocalPayload();
            _unity = localPayload.modules;
            _glider = localPayload.gliders;
        }

        #endregion

        #region UI Methods

        void DrawTitle(string name)
        {
            EditorGUILayout.LabelField(name, titleLabelStyle);
        }

        void DrawUnity()
        {
            using (new EditorGUILayout.HorizontalScope("box"))
            {
                using (new EditorGUILayout.VerticalScope())
                {
                    for (int i = 0; i < _unity.Length; i++)
                    {
                        var pack = _unity[i];
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.LabelField(pack, headerLabelStyle);

                            if (GUILayout.Button(new GUIContent("Install"), fieldWidth))
                            {
                                var moduleId = _unity[i];
                                _whileImporting = true;
                                GliderSdkEditorCoroutine.StartCoroutine(ImportUnityPackage(moduleId));
                            }
                        }
                    }
                }
            }

            if (_unity.Length > 1)
            {
                if (GUILayout.Button(new GUIContent("Install All Unity Dependencies"), quadFieldWidth))
                {
                    _whileImporting = true;
                    GliderSdkEditorCoroutine.StartCoroutine(
                        GliderSdkDependencyManager.Instance.InstallArrayDependencies(_unity,
                            isDone =>
                            {
                                _whileImporting = false;
                                if (isDone)
                                {
                                    GliderSdkDependencyManager.Instance.RemoveAllFromLocalPayload();
                                    Close();
                                }
                            }));
                }
            }
        }

        void DrawGlider()
        {
            using (new EditorGUILayout.HorizontalScope("box"))
            {
                using (new EditorGUILayout.VerticalScope())
                {
                    for (int i = 0; i < _glider.Length; i++)
                    {
                        var pack = _glider[i];
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.LabelField(pack, headerLabelStyle);

                            if (GUILayout.Button(new GUIContent("Install"), fieldWidth))
                            {
                                GliderSdkDependencyManager.Instance.gliderImportCallback.Invoke(pack);
                                Close();
                            }
                        }
                    }
                }
            }

            if (_glider.Length > 1)
            {
                if (GUILayout.Button(new GUIContent("Install All Glider Dependencies"), quadFieldWidth))
                {
                    GliderSdkDependencyManager.Instance.gliderImportAllCallback.Invoke(_glider);
                }
            }
        }

        void DrawInstallAll()
        {
            GUILayout.Space(20);
            if (GUILayout.Button(new GUIContent("INSTALL ALL"), GUILayout.Height(60)))
            {
                AssetDatabase.DisallowAutoRefresh();
                EditorApplication.LockReloadAssemblies();

                _whileImporting = true;
                GliderSdkEditorCoroutine.StartCoroutine(
                    GliderSdkDependencyManager.Instance.InstallArrayDependencies(_unity,
                        isDone =>
                        {
                            _whileImporting = false;
                            if (isDone)
                            {
                                GliderSdkDependencyManager.Instance.RemoveAllFromLocalPayload();
                                Close();
                            }
                        }));
                GliderSdkDependencyManager.Instance.gliderImportAllCallback.Invoke(_glider);

                EditorApplication.UnlockReloadAssemblies();
                AssetDatabase.AllowAutoRefresh();
                Load();
            }
        }

        #endregion

        #region Utility Methods

        IEnumerator ImportUnityPackage(string moduleId)
        {
            var addRequest = Client.Add(moduleId);
            while (!addRequest.IsCompleted)
                yield return null;
            _whileImporting = false;

            Debug.Log(addRequest.Result);
            if (addRequest.Result.errors.Length > 0)
            {
                foreach (var error in addRequest.Result.errors)
                {
                    Debug.Log(error);
                }
            }

            GliderSdkDependencyManager.Instance.RemoveFromLocalPayload(moduleId);

            Load();
            // if (_unity is null || _unity.Length < 1)
            // {
            //     Close();
            // }
        }

        #endregion
    }
}

#endif