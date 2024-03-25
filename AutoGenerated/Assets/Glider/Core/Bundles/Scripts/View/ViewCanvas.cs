using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Cysharp.Threading.Tasks;
using System.Threading;
using Glider.Util;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
using Debug = UnityEngine.Debug;

namespace Glider.Core.Bundles
{
    public abstract class ViewCanvas : MonoBehaviour
    {
        [SerializeField] private TMP_StyleSheet textStyleSheet;

        /// <summary>
        /// '이거보다 좁혀지면 스케일을 줄여야 한다'의 기준. 1920 높이와 비율을 계산하여 적용됨.
        /// </summary>
        [SerializeField] private float okWidth = 1080f;

        [SerializeField] private string sortingLayerName = "UI";
        protected Canvas canvas;
        protected GraphicRaycaster graphicRaycaster;
        public Canvas Canvas => canvas;

        protected CanvasScaler canvasScaler;
        private static Dictionary<Type, ViewCanvas> _viewCanvasPrefab = new Dictionary<Type, ViewCanvas>();
        private static Dictionary<Type, ViewCanvas> _viewCanvasMap = new Dictionary<Type, ViewCanvas>();
        private static HashSet<Type> _createdViewCanvases = new HashSet<Type>();
        public static bool IsLoaded { get; private set; }


        //private const string ViewCanvasAssetLabelName = "ViewCanvas";
        private const string BundleViewCanvasAssetLabelName = "BundleViewCanvas";

        private UnityAction<bool> _onChangeVisible;
        private UnityAction<bool> _onChangeInteractable;


        public bool IsVisible { get; private set; }
        public bool IsInteractable { get; private set; }

        public ViewCanvas BindOnChangeVisible(UnityAction<bool> action)
        {
            _onChangeVisible += action;
            return this;
        }

        public ViewCanvas SetVisibleForce(bool flag)
        {
            canvas.enabled = flag;
            IsVisible = flag;
            SetInteractable(flag);
            return this;
        }

        public ViewCanvas SetInteractable(bool flag)
        {
            /*
            if (IsInteractable == flag) return this;
            */

            IsInteractable = flag;
            _onChangeInteractable?.Invoke(flag);
            if (graphicRaycaster != null)
                graphicRaycaster.enabled = flag;

            return this;
        }


        public void SetVisible(bool flag)
        {
            SetInteractable(flag);

            if (IsVisible == flag) return;

            IsVisible = flag;
            canvas.enabled = flag;
            _onChangeVisible?.Invoke(flag);
        }

        public static async UniTask StartLoadAssets(CancellationToken token)
        {
            if (IsLoaded) return;
            _viewCanvasPrefab.Clear();

            // var a = await Addressables.LoadAssetAsync<BundleViewCanvas>(BundleViewCanvasAssetLabelName).WithCancellation(token);
            // OnLoadViewCanvas(a);
            // Addressables.Release(a);
            //
            IsLoaded = true;
        }

        public static void ClearPrefabs()
        {
            _viewCanvasPrefab.Clear();
        }

        public static T Get<T>() where T : ViewCanvas
        {
            var type = typeof(T);
            return _viewCanvasMap[type] as T;
        }

        public static bool Contain<T>() where T : ViewCanvas
        {
            var type = typeof(T);
            return _viewCanvasMap.ContainsKey(type);
        }

        public static T Create<T>(Transform parent, bool dontRemoveFromPrefabMap = false) where T : ViewCanvas
        {
            /*if (_createdViewCanvases.Contains(typeof(T)))
            {
                Debug.LogError($"{typeof(T)} is already created! Please use 'Get' method.");
                return Get<T>();
            }*/

            var view = Instantiate(_viewCanvasPrefab[typeof(T)] as T, parent);

            if (!dontRemoveFromPrefabMap)
            {
                _viewCanvasPrefab.Remove(typeof(T));
            }

            if (_createdViewCanvases.Contains(typeof(T)))
            {
                Debug.LogWarning($"Duplicate Creating: {typeof(T)}");
            }
            else
            {
                _createdViewCanvases.Add(typeof(T));
            }

            view.Setup();
            return view;
        }

        public static ViewCanvas Get(Type type)
        {
            return _viewCanvasMap[type];
        }

        /*private static void OnLoadViewCanvas(GameObject go)
        {
#if UNITY_EDITOR
            Debug.Log(go.name);
#endif
            if (go.TryGetComponent<ViewCanvas>(out var viewCanvas))
                _viewCanvasPrefab[viewCanvas.GetType()] = viewCanvas;
        }*/

        // private static void OnLoadViewCanvas(BundleViewCanvas bundle)
        // {
        // 	var allCanvases = bundle.GetAllCanvases();
        // 	for (int i = 0; i < allCanvases.Length; i++)
        // 	{
        // 		_viewCanvasPrefab[allCanvases[i].GetType()] = allCanvases[i];
        // 	}
        // }

        private ViewCanvas Setup()
        {
#if UNITY_EDITOR
            GliderUtil.ReAssignShader(gameObject);
#endif
            LocalizeFont();

            if (canvas == null)
            {
                canvas = GetComponent<Canvas>();
            }

            if (graphicRaycaster == null)
            {
                graphicRaycaster = GetComponent<GraphicRaycaster>();
            }

            if (canvas.worldCamera == null)
                canvas.worldCamera = Camera.main;

            canvas.sortingLayerName = sortingLayerName;
            canvas.enabled = false;
            if (canvasScaler == null)
            {
                canvasScaler = GetComponent<CanvasScaler>();
            }

            canvasScaler.referenceResolution = new Vector2(okWidth, 1920);
            canvasScaler.matchWidthOrHeight = GetMatchWidthOrHeight(okWidth);


            var type = GetType();
            if (!_viewCanvasMap.ContainsKey(type))
            {
                _viewCanvasMap.Add(type, this);
            }
            else
            {
                _viewCanvasMap[type] = this;
            }

            return this;
        }


        public void LocalizeFont()
        {
#if GD_CHEAT
			ViewCanvas myCanvas = this;
			if (myCanvas is ViewCanvasDebug)
			{
				return;
			}
#endif

            ViewHelper.LocalizeFont(gameObject);
        }

#if UNITY_EDITOR
        [ContextMenu("CheckTextSetting")]
        public void CheckTextSetting()
        {
            if (textStyleSheet == null)
            {
                Debug.LogError("textStyleSheet is NULL! Please put textStyleSheet");
                return;
            }

            var allTexts = gameObject.GetComponentsInChildren<TMP_Text>(true);

            int normalHash = TMP_Style.NormalStyle.hashCode;
            int nowHash;
            for (int i = 0; i < allTexts.Length; i++)
            {
                nowHash = allTexts[i].textStyle.hashCode;
                string fontName = allTexts[i].font.name;
                string gName = allTexts[i].gameObject.name;
                if (fontName == "NEXON Lv2 Gothic OTF Bold SDF")
                {
                    if (nowHash != normalHash)
                    {
                        Debug.LogError($"font is normal but style not. : {gName}");
                    }
                }
                else if (fontName == "BlackHanSans-Regular-NumberUp SDF" || fontName == "JalnanOTF SDF")
                {
                    if (fontName == "JalnanOTF SDF")
                    {
                        Debug.LogError($"Deprecated font. Please Change. : {allTexts[i].gameObject.name}");
                    }
                    else
                    {
                        if (nowHash != 2425)
                        {
                            Debug.LogError($"font is Title but style not. : {gName}");
                        }
                    }
                }
                else if (fontName == "NEXON Lv2 Gothic OTF SDF")
                {
                    if (nowHash != 2959547)
                    {
                        Debug.LogError($"font is Thin but style not. : {gName}");
                    }
                }
                else
                {
                    Debug.LogError($"Unknown Font! : {fontName}");
                }
            }
        }

        public void TextStyleSetting()
        {
            if (textStyleSheet == null)
            {
                Debug.LogError("textStyleSheet is NULL! Please put textStyleSheet");
                return;
            }

            var allTexts = gameObject.GetComponentsInChildren<TMP_Text>(true);

            int normalHash = TMP_Style.NormalStyle.hashCode;
            int nowHash;
            for (int i = 0; i < allTexts.Length; i++)
            {
                nowHash = normalHash;
                string fontName = allTexts[i].font.name;
                if (fontName == "NEXON Lv2 Gothic OTF Bold SDF")
                {
                    nowHash = normalHash;
                }
                else if (fontName == "BlackHanSans-Regular-NumberUp SDF" || fontName == "JalnanOTF SDF")
                {
                    if (fontName == "JalnanOTF SDF")
                    {
                        Debug.LogError($"Deprecated font. Please Change. : {allTexts[i].gameObject.name}");
                    }

                    nowHash = 2425;
                }
                else if (fontName == "NEXON Lv2 Gothic OTF SDF")
                {
                    nowHash = 2959547;
                }
                else
                {
                    Debug.LogError($"Unknown Font! : {fontName}");
                }

                allTexts[i].textStyle = textStyleSheet.GetStyle(nowHash);
            }
        }

        [ContextMenu("FindJalnanFont")]
        public void FindJalnanFont()
        {
            var allTexts = gameObject.GetComponentsInChildren<TMP_Text>(true);

            for (int i = 0; i < allTexts.Length; i++)
            {
                string fontName = allTexts[i].font.name;
                if (fontName == "JalnanOTF SDF")
                {
                    Debug.LogError($"Deprecated font. Please Change. : {allTexts[i].gameObject.name}");
                }
            }
        }
#endif

        protected virtual void OnDestroy()
        {
            var type = GetType();
            _createdViewCanvases.Remove(GetType());
        }
#if UNITY_EDITOR
        private void Update()
        {
            if (canvasScaler != null)
            {
                canvasScaler.referenceResolution = new Vector2(okWidth, 1920);
                canvasScaler.matchWidthOrHeight = GetMatchWidthOrHeight(okWidth);
            }
        }
#endif
        /*float GetMatchWidthOrHeight()
        {
            var minAspect = 1920f / 1080f; //  1.77
            var maxAspect = 2640/1080f; //2.44
            var aspect = (float)Screen.height / Screen.width;
            return Mathf.Clamp01(1-(aspect - minAspect) / (maxAspect - minAspect));
        }*/

        public static float GetMatchWidthOrHeight(float okWidth)
        {
            var minAspect = 1920f / okWidth;
            var aspect = (float)Screen.height / Screen.width;
            return aspect > minAspect ? 0f : 1f;
        }

        public void SetCanvasOrder(int order)
        {
            canvas.sortingOrder = order;
        }

        public int GetCanvasOrder()
        {
            return canvas.sortingOrder;
        }


        /// <summary>
        /// 리스트 요소 개수를 length개로 맞추기. 부족하면 생성해서 채웁니다.
        /// 주의: 메모리 최적화를 위해, 현재 요소 개수가 length보다 길다면 남는 것들을 다 지워버립니다.
        /// 참고: SetActive는 해주지 않습니다.
        /// </summary>
        public static T[] UpdateList<T>(ref T[] org, int length) where T : MonoBehaviour
        {
            if (org.Length == 0)
            {
                throw new Exception("Array is EMPTY! : UpdateList");
            }

            if (org.Length == length)
            {
                return org;
            }
            else
            {
                var newArray = new T[length];

                if (org.Length < length)
                {
                    // 부족한 만큼 채우기
                    for (int i = 0; i < org.Length; i++)
                    {
                        newArray[i] = org[i];
                    }

                    Transform parent = newArray[0].transform.parent;
                    for (int i = org.Length; i < length; i++)
                    {
                        GameObject g = Instantiate(newArray[0].gameObject, parent);
                        newArray[i] = g.GetComponent<T>();
                    }
                }
                else
                {
                    // 남는 것들 지우기
                    for (int i = 0; i < length; i++)
                    {
                        newArray[i] = org[i];
                    }

                    for (int i = length; i < org.Length; i++)
                    {
                        Destroy(org[i].gameObject);
                    }
                }

                org = newArray;
                return org;
            }
        }

        /// <summary>
        /// 리스트 요소 개수를 length개로 맞추기. 부족하면 생성해서 채웁니다.
        /// 현재 요소 개수가 length보다 길다면, 남는 것들은 내버려둡니다.
        /// 참고: SetActive는 해주지 않습니다.
        /// </summary>
        public static T[] UpdateListDontDestroy<T>(ref T[] org, int minLength) where T : MonoBehaviour
        {
            if (org.Length == 0)
            {
                Debug.LogError("Array is EMPTY! : UpdateListDontDestroy");
                throw new Exception("Array is EMPTY! : UpdateListDontDestroy");
                return null;
            }

            if (org.Length == minLength)
            {
                return org;
            }
            else
            {
                // 부족한 만큼 채우기
                if (org.Length < minLength)
                {
                    var newArray = new T[minLength];

                    for (int i = 0; i < org.Length; i++)
                    {
                        newArray[i] = org[i];
                    }

                    Transform parent = newArray[0].transform.parent;
                    for (int i = org.Length; i < minLength; i++)
                    {
                        GameObject g = Instantiate(newArray[0].gameObject, parent);
                        newArray[i] = g.GetComponent<T>();
                    }

                    org = newArray;
                }

                return org;
            }
        }
    }
}