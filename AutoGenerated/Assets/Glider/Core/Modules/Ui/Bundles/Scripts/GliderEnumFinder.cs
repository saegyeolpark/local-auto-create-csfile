using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Glider.Core.Ui.Bundles
{
#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(GliderEnumFinder))]
    public class LocalizedTextEditor : PropertyDrawer
    {
        private static GUIContent findIconContent;
        private static GUIStyle onStyle;
        private static GUIStyle offStyle;


        public List<string> _matchList = new List<string>();

        private int selectedInSearch;
        private Vector2 scrollRect = Vector2.zero;

        public string search;

        float height;

        string name;
        bool cache = false;
        bool toggleOn = false;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (cache.Equals(false))
            {
                findIconContent = EditorGUIUtility.IconContent("d_Search Icon");
                name = property.displayName;
                onStyle = new GUIStyle()
                {
                    normal = new GUIStyleState()
                    {
                        background = Texture2D.whiteTexture
                    }
                };

                offStyle = new GUIStyle()
                {
                    normal = new GUIStyleState()
                    {
                        background = Texture2D.grayTexture
                    }
                };

                cache = true;
            }

            float heightValue = 20f;
            float margin = 5f;

            EditorGUI.BeginChangeCheck();

            Rect propertyRect = position;
            propertyRect.width -= 30f;
            propertyRect.height = heightValue;
            EditorGUI.PropertyField(propertyRect, property, label);

            propertyRect.x += propertyRect.width + margin;
            propertyRect.width = 30f - margin;
            if (GUI.Button(propertyRect, findIconContent, toggleOn ? onStyle : offStyle))
            {
                toggleOn = !toggleOn;
            }

            Rect labelRect = position;
            Rect contentRect = position;
            contentRect.x += EditorGUIUtility.labelWidth;
            contentRect.width -= EditorGUIUtility.labelWidth;

            height = heightValue;
            labelRect.height = heightValue;
            contentRect.height = heightValue;

            labelRect.x += 5f;
            contentRect.x += 5f;
            contentRect.width -= 10f;
            labelRect.width -= 10f;

            labelRect = EditorGUI.IndentedRect(labelRect);
            contentRect = EditorGUI.IndentedRect(contentRect);

            var enumNames = property.enumNames;
            // GUI.Label(contentRect, enumNames[property.enumValueIndex]);

            float calculateHeight = heightValue + margin;

            if (toggleOn)
            {
                height += calculateHeight;
                labelRect.y += calculateHeight;
                contentRect.y += calculateHeight;


                height += calculateHeight;
                labelRect.y += calculateHeight;
                contentRect.y += calculateHeight;

                search = GUI.TextField(labelRect, search);

                height += calculateHeight;
                labelRect.y += calculateHeight;
                contentRect.y += calculateHeight;

                if (GUI.Button(labelRect, "Search") && string.IsNullOrEmpty(search).Equals(false))
                {
                    _matchList.Clear();

                    for (int i = 0; i < enumNames.Length; i++)
                    {
                        string name = enumNames[i];

                        if (name == search)
                        {
                            property.enumValueIndex = i;
                            break;
                        }
                        else if (name.ToLower().Contains(search.ToLower()))
                        {
                            _matchList.Add(name);
                        }
                    }

                    search = string.Empty;
                }

                if (_matchList.Count > 0)
                {
                    height += calculateHeight + margin;
                    labelRect.y += calculateHeight + margin;
                    contentRect.y += calculateHeight + margin;

                    GUI.Label(labelRect, "Searching Result");
                    height += calculateHeight;
                    labelRect.y += calculateHeight;
                    contentRect.y += calculateHeight;


                    float scrollRectHeight = 400f;
                    Rect scrollRectSize = labelRect;
                    scrollRectSize.height = scrollRectHeight;

                    Rect scrollContentSize = Rect.zero;
                    scrollContentSize.width = scrollRectSize.width - 50f;
                    scrollContentSize.height = _matchList.Count * heightValue;

                    Rect gridSize = Rect.zero;
                    gridSize.width = labelRect.width;
                    gridSize.height = heightValue;

                    scrollRect = GUI.BeginScrollView(scrollRectSize, scrollRect, scrollContentSize);
                    selectedInSearch = GUI.SelectionGrid(gridSize, selectedInSearch, _matchList.ToArray(), 1,
                        EditorStyles.miniButtonMid);
                    GUI.EndScrollView();

                    height += scrollRectHeight + margin;
                    labelRect.y += scrollRectHeight + margin;
                    contentRect.y += scrollRectHeight + margin;

                    if (selectedInSearch >= 0)
                    {
                        for (int i = 0; i < enumNames.Length; i++)
                        {
                            string name = enumNames[i];

                            if (name == _matchList[selectedInSearch])
                            {
                                property.enumValueIndex = i;
                                break;
                            }
                        }
                    }
                }
                else
                {
                    selectedInSearch = -1;
                }

                height += margin;
                labelRect.y += margin;
                contentRect.y += margin;

                Rect boxRect = position;
                boxRect.height = height - calculateHeight;
                boxRect.y += calculateHeight;
                boxRect = EditorGUI.IndentedRect(boxRect);
                GUI.Box(boxRect, "Search Enums");

                height += margin;
                labelRect.y += margin;
                contentRect.y += margin;
            }

            if (EditorGUI.EndChangeCheck())
            {
                property.serializedObject.ApplyModifiedProperties();
            }
        }
    }
#endif
    public class GliderEnumFinder : PropertyAttribute
    {
        public GliderEnumFinder()
        {
        }
    }
}