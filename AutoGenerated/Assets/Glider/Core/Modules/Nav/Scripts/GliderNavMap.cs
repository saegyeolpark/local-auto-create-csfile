using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Glider.Core.Nav
{
    [Serializable]
    public class GliderNavActivity
    {
        public string id; // activity는 이 고유 id로 식별됩니다
        public string controller; // 실행할 method가 위치한 class 이름
        public string method; // 실행할 method 이름
    }

    [CreateAssetMenu(menuName = "Glider/NavMap", fileName = "GliderNavMap")]
    public class GliderNavMap : ScriptableObject
    {
        public List<GliderNavActivity> activities = new List<GliderNavActivity>();

        public GliderNavActivity FindActivity(string id)
        {
            foreach (var activity in activities)
            {
                if (activity.id == id)
                {
                    return activity;
                }
            }

            return null;
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(GliderNavMap))]
    public class GliderNavMapEditor : Editor
    {
        private static List<bool> foldout = new List<bool>();

        private const float buttonWidth = 100f;
        private const float labelDescWidth = 400f;
        private GUILayoutOption ButtonStyle = GUILayout.Width(buttonWidth);
        private GUILayoutOption LabelDescStyle = GUILayout.Width(labelDescWidth);

        private string searchText;
        private const string noneString = "-";

        private void OnEnable()
        {
            FindClassesInheritingFrom();
        }

        public override void OnInspectorGUI()
        {
            // base.OnInspectorGUI();

            CustomOnInspectorGUI();
        }


        #region FindClassesInheritingFrom

        private List<string> _classNameList = new List<string>();
        private List<List<string>> _methodNameList = new List<List<string>>();

        private void ClearReflections()
        {
            _classNameList.Clear();
            _methodNameList.Clear();
        }

        private bool CheckClassInList(string className)
        {
            foreach (var item in _classNameList)
            {
                if (className == item)
                    return true;
            }

            return false;
        }

        private void AddClassName(string className)
        {
            _classNameList.Add(className);
            _methodNameList.Add(new List<string>());
        }

        private bool TryAddClassName(string className)
        {
            if (CheckClassInList(className))
            {
                return false;
            }
            else
            {
                AddClassName(className);
                return true;
            }
        }

        private int FindClassNameIndex(string className)
        {
            for (int i = 0; i < _classNameList.Count; i++)
            {
                var index = i;
                if (_classNameList[index] == className)
                {
                    return index;
                }
            }

            return -1;
        }

        private void TryAddMethodName(string className, string methodName)
        {
            var index = FindClassNameIndex(className);
            if (index == -1)
            {
                return;
            }

            _methodNameList[index].Add(methodName);
        }

        private void FindClassesInheritingFrom()
        {
            ClearReflections();

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly assembly in assemblies)
            {
                Type[] classes = assembly.GetTypes();
                foreach (Type classType in classes)
                {
                    var controllerAttribute =
                        classType.GetCustomAttribute(typeof(NavControllerAttribute)) as NavControllerAttribute;
                    if (controllerAttribute != null)
                    {
                        TryAddClassName(classType.Name);

                        var methods =
                            classType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        foreach (var method in methods)
                        {
                            var methodAttribute =
                                method.GetCustomAttribute(
                                    typeof(NavMethodAttribute)) as NavMethodAttribute;
                            if (methodAttribute != null)
                            {
                                TryAddMethodName(classType.Name, method.Name);
                            }
                        }
                    }
                }
            }
        }

        #endregion

        private void CustomOnInspectorGUI()
        {
            var navMap = (GliderNavMap)target;

            // 배열 요소 표시
            EditorGUILayout.LabelField("Activities", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label($"Counts : {navMap.activities.Count}", ButtonStyle);
                if (GUILayout.Button("Add", ButtonStyle))
                {
                    navMap.activities.Add(new GliderNavActivity());
                }

                if (GUILayout.Button("Clear All", ButtonStyle))
                {
                    navMap.activities.Clear();
                }

                // GUILayout.Space(200f);
                GUILayout.Label("Search: ", GUILayout.Width(0.5f * buttonWidth));
                searchText = GUILayout.TextField(searchText);
            }

            // if (foldout.Count < navMap.activities.Count)
            //     foldout.Add(false);
            // else if (foldout.Count > navMap.activities.Count)
            //     foldout.RemoveRange(foldout.Count - navMap.activities.Count, navMap.activities.Count);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                // 배열 요소 순회 및 표시
                for (int i = 0; i < navMap.activities.Count; i++)
                {
                    var element = navMap.activities[i];
                    if (!string.IsNullOrWhiteSpace(searchText))
                    {
                        var targetText = searchText.ToLower();
                        var checkContains = element.id.ToLower().Contains(targetText) ||
                                            element.controller.ToLower().Contains(targetText) ||
                                            element.method.ToLower().Contains(targetText);
                        if (!checkContains)
                            continue;
                    }

                    DrawActivity(i, element);
                }
            }

            if (GUI.changed)
            {
                EditorUtility.SetDirty(navMap);
            }
        }

        private void DrawActivity(int index, GliderNavActivity activity)
        {
            var navMap = (GliderNavMap)target;


            string[] classArray = _classNameList.ToArray();
            int classIndex = -1;
            for (int i = 0; i < _classNameList.Count; i++)
            {
                if (_classNameList[i] == activity.controller)
                {
                    classIndex = i;
                    break;
                }
            }

            string[] methodArray = new string[] { "-" };
            int methodIndex = -1;
            if (classIndex != -1)
            {
                methodArray = _methodNameList[classIndex].ToArray();
                for (int i = 0; i < _methodNameList[classIndex].Count; i++)
                {
                    if (_methodNameList[classIndex][i] == activity.method)
                    {
                        methodArray = _methodNameList[classIndex].ToArray();
                        methodIndex = i;
                        break;
                    }
                }
            }

            using (new EditorGUILayout.VerticalScope("box"))
            {
                // 폴드아웃 그룹 시작
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Space(10);
                    Rect foldoutRect = EditorGUILayout.BeginVertical();


                    // using (new EditorGUILayout.VerticalScope())
                    while (foldout.Count <= index)
                    {
                        foldout.Add(true);
                    }

                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            foldout[index] =
                                EditorGUILayout.Foldout(foldout[index],
                                    $"{index.ToString()} : " +
                                    (string.IsNullOrWhiteSpace(activity.id) ? noneString : activity.id));
                            if (GUILayout.Button("Add below", ButtonStyle))
                            {
                                navMap.activities.Insert(index + 1, new GliderNavActivity());
                                FindClassesInheritingFrom();
                            }

                            if (GUILayout.Button("Remove", ButtonStyle))
                            {
                                navMap.activities.RemoveAt(index);
                                FindClassesInheritingFrom();
                            }
                        }

                        if (foldout[index])
                        {
                            // id
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                EditorGUILayout.BeginHorizontal();
                                GUILayout.Space(10);

                                activity.id = EditorGUILayout.TextField("Id", activity.id);
                                EditorGUILayout.EndHorizontal();
                            }

                            // className
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                EditorGUILayout.BeginHorizontal();

                                GUILayout.Space(10);

                                // activity.className = EditorGUILayout.TextField("ClassName",
                                //     activity.className);
                                classIndex = EditorGUILayout.Popup("NavController", classIndex, classArray);
                                if (classIndex != -1)
                                    activity.controller = _classNameList[classIndex];

                                // Debug.Log(Type.GetType("GliderDemoV3.InGame." + activity.className +
                                //                        ", GliderDemoV3.InGame"));
                                EditorGUILayout.EndHorizontal();
                            }

                            // targetMethodName
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                GUILayout.Space(10);

                                // activity.targetMethodName = EditorGUILayout.TextField(
                                //     "TargetMethodName",
                                //     activity.targetMethodName);

                                methodIndex = EditorGUILayout.Popup("NavMethod", methodIndex, methodArray);
                                if (classIndex != -1 && methodIndex != -1)

                                    if (_methodNameList[classIndex].Count > 0)
                                        activity.method = _methodNameList[classIndex][methodIndex];
                            }
                        }
                    }
                    EditorGUILayout.EndVertical();

                    // 터치 범위 확장
                    Rect expandedRect = new Rect(foldoutRect.x - 10f, foldoutRect.y, foldoutRect.width + 20f,
                        20);
                    EditorGUIUtility.AddCursorRect(expandedRect, MouseCursor.Link);
                    // 클릭 이벤트 처리
                    Event e = Event.current;
                    if (e.type == EventType.MouseDown && expandedRect.Contains(e.mousePosition))
                    {
                        // 여기에 클릭 시 실행할 동작을 정의
                        // Debug.Log("폴드아웃을 클릭했습니다!");
                        foldout[index] = !foldout[index];
                    }
                }
            }

            // 변경 사항 저장
            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}