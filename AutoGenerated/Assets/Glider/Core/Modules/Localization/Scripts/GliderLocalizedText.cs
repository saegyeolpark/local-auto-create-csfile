using System;
using System.Collections.Generic;
using Glider.Core.SerializableData;
using Glider.Core.Ui.Bundles;
using UnityEngine;
using TMPro;
using UnityEditor;
using UnityEngine.TextCore.Text;
using UnityEngine.UI;

namespace Glider.Core.Localization
{
    public enum TextStyleName
    {
        Title = 97690656,
        Outlined = -222257628,
        Normal = -1183493901
    }

    public class GliderLocalizedText : MonoBehaviour
    {
        [GliderEnumFinder] [SerializeField] LocalizedMessageKey localizedMessageKey;
        public bool hasIsoTime;

        private void Start()
        {
            var t = GetComponent<Text>();
            if (t != null)
            {
                var msg = localizedMessageKey.ToLocalizedString();
                if (hasIsoTime)
                    msg = DataUtil.GetIsoStringTagConvertedContent(msg,
                        LocalizedMessageKey.FormatWeekDay.ToLocalizedString());
                t.text = msg;
            }
            else
            {
                var tmp = GetComponent<TMP_Text>();
                if (tmp != null)
                {
                    var msg = localizedMessageKey.ToLocalizedString();
                    if (hasIsoTime)
                        msg = DataUtil.GetIsoStringTagConvertedContent(msg,
                            LocalizedMessageKey.FormatWeekDay.ToLocalizedString());
                    tmp.text = msg;
                }
            }
        }

#if UNITY_EDITOR
        public void SetLocalizedMessageKey(LocalizedMessageKey key)
        {
            localizedMessageKey = key;
        }
#endif
        // private void OnDrawGizmosSelected()
        // {
        //     var tmp = GetComponent<TMP_Text>();
        //     if (tmp != null)
        //     {
        //         if (tmp.textStyle.hashCode != (int)textStyleName)
        //         {
        //             tmp.textStyle = TMP_Settings.defaultStyleSheet.GetStyle((int)textStyleName);
        //             tmp.ForceMeshUpdate();
        //         }
        //     }
        //    
        // }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(GliderLocalizedText))]
    public class GliderLocalizedTextEditor : Editor
    {
        private string _textInput;
        private LocalizedMessageKey _selectedKey;

        private string _searchText;
        private LocalizedMessageKey[] _totalArray;
        private List<LocalizedMessageKey> _searchedList = new List<LocalizedMessageKey>();


        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            using (new EditorGUILayout.VerticalScope())
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label("Search Key: ", GUILayout.Width(100));
                    _textInput = GUILayout.TextField(_textInput);
                }

                if (!string.IsNullOrWhiteSpace(_textInput))
                {
                    // 검색
                    if (_searchText != _textInput)
                    {
                        _searchText = _textInput;
                        if (_totalArray == null)
                            _totalArray = (LocalizedMessageKey[])Enum.GetValues(typeof(LocalizedMessageKey));

                        _searchedList.Clear();
                        foreach (var key in _totalArray)
                        {
                            if (key.ToString().ToLower().Contains(_searchText.ToLower()))
                            {
                                _searchedList.Add(key);
                            }
                        }
                    }

                    // 표시
                    using (new EditorGUILayout.VerticalScope("box"))
                    {
                        foreach (var element in _searchedList)
                        {
                            DrawSearchedKey(element);
                        }
                    }
                }
            }
        }

        private void DrawSearchedKey(LocalizedMessageKey key)
        {
            if (GUILayout.Button(key.ToString()))
            {
                var glt = (GliderLocalizedText)target;
                glt.SetLocalizedMessageKey(key);

                // GameObject prefabRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(target);
                // 변경 사항을 저장
                EditorUtility.SetDirty(target);
                // 프리팹 저장
                // PrefabUtility.SaveAsPrefabAsset(prefabRoot,
                //     PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(prefabRoot));
            }
            // EditorGUILayout.LabelField(key.ToString());
            // // 클릭 이벤트 처리
            // if (Event.current.type == EventType.MouseDown &&
            //     GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
            // {
            //     var glt = (GliderLocalizedText)target;
            //     glt.SetLocalizedMessageKey(key);
            //     PrefabUtility.RecordPrefabInstancePropertyModifications(target);
            //     Repaint();
            // }
        }
    }
#endif
}