using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using Glider.Core.Nav;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Glider.Core.Ui.Bundles
{
    public class GliderButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        [SerializeField] private float downSize = 0.95f;
        [SerializeField] private float upSize = 1.05f;


        [SerializeField] private UnityEvent onClick;

        public static event UnityAction OnClickAnyButton;
        private RectTransform _rect;
        [SerializeField] private bool isRepeatInvokeOnDown = false;

        private UnityAction<Vector2> onTouch;
        private Vector2 downPosition;
        private bool isDown = false;
        private int refeatClickCount = 0;
        private double lastClickTime;

        private Coroutine ieAnimCo;
        private Coroutine ieRepeatInvokeCo;


        private UnityAction _onceAction;

        [field: SerializeField] public bool UseNav { get; protected set; }
        [HideInInspector] public string navActivityId;
        [HideInInspector] public object[] navActivitiyParameters;

        public RectTransform Rect
        {
            get
            {
                if (_rect == null) _rect = GetComponent<RectTransform>();
                return _rect;
            }
        }


        public void SetOnClickOnceAction(UnityAction action)
        {
            _onceAction = action;
        }

        private void StartIeAnim(float size)
        {
            if (ieAnimCo != null)
            {
                StopCoroutine(ieAnimCo);
            }

            ieAnimCo = StartCoroutine(IeAnim(size));
        }


        public virtual void OnPointerDown(PointerEventData eventData)
        {
            if (isDown) return;
            downPosition = Input.mousePosition;
            isDown = true;
            StartIeAnim(downSize);
            if (isRepeatInvokeOnDown)
            {
                if (ieRepeatInvokeCo != null)
                    StopCoroutine(ieRepeatInvokeCo);
                ieRepeatInvokeCo = StartCoroutine(IeRepeatInvoke());
            }
        }


        public void OnPointerUp(PointerEventData eventData)
        {
            if (!isDown) return;
            isDown = false;
            StartIeAnim(upSize);
            if (ieRepeatInvokeCo != null)
                StopCoroutine(ieRepeatInvokeCo);
        }

        void TouchEffect(Vector2 pos)
        {
            onTouch?.Invoke(pos);
        }


        public void OnPointerClick(PointerEventData eventData)
        {
            InvokeOnClick();
            OnClickAnyButton?.Invoke();

            _onceAction?.Invoke();
            _onceAction = null;
            TouchEffect(eventData.position);
            if (isRepeatInvokeOnDown)
            {
                if (Time.unscaledTimeAsDouble - lastClickTime <= 0.35f)
                {
                    if (refeatClickCount >= 3)
                    {
                        //Toast.Open("터치하고 있으면 자동으로 클릭됩니다.");
                        refeatClickCount = 0;
                    }
                    else
                    {
                        refeatClickCount++;
                    }
                }
                else
                {
                    refeatClickCount = 0;
                }

                lastClickTime = Time.unscaledTimeAsDouble;
            }
        }

        void PointerExit()
        {
            if (!isDown) return;
            isDown = false;
            StartIeAnim(1f);
            if (ieRepeatInvokeCo != null)
                StopCoroutine(ieRepeatInvokeCo);
        }


        void OnDisable()
        {
            transform.localScale = Vector3.one;
            isDown = false;
            StopAllCoroutines();
        }


        private int cnt = 0;

        IEnumerator IeRepeatInvoke()
        {
            cnt = 0;

            var delay = new WaitForSecondsRealtime(0.08f);
            var delay2 = new WaitForSecondsRealtime(0.02f);

            yield return new WaitForSecondsRealtime(0.42f);

            double prevAnim = 0f;

            while (isDown)
            {
                TouchEffect(Input.mousePosition);
                InvokeOnClick();

                if (cnt > 10)
                {
                    if (prevAnim < Time.unscaledTimeAsDouble)
                    {
                        if (gameObject.activeInHierarchy)
                            StartIeAnim(upSize);
                        prevAnim = Time.unscaledTimeAsDouble + 0.02;
                    }

                    for (int i = 0; i < Mathf.Min(cnt - 10, 99); i++)
                    {
                        InvokeOnClick();
                    }

                    yield return null;
                }
                else
                {
                    if (gameObject.activeInHierarchy)
                        StartIeAnim(upSize);
                    if (cnt > 5)
                        yield return delay2;
                    else
                        yield return delay;
                }

                cnt++;
            }

            cnt = 0;
        }

        public void ResetRepeatInvoke()
        {
            cnt = 0;
        }


        IEnumerator IeAnim(float size)
        {
            while (Mathf.Abs(transform.localScale.x - size) > 0.01f)
            {
                transform.localScale = Vector3.one * Mathf.Lerp(transform.localScale.x, size, 0.45f);
                yield return null;
            }

            transform.localScale = Vector3.one * size;
            while (size > 1.01f)
            {
                transform.localScale = Vector3.one * Mathf.Lerp(transform.localScale.x, 1f, 0.45f);
                yield return null;
            }

            if (size > 1.01f)
                transform.localScale = Vector3.one;


            while (isDown)
            {
                if (Vector2.SqrMagnitude((Vector2)Input.mousePosition - downPosition) > 100000f)
                {
                    PointerExit();
                }

                yield return null;
            }
        }

        public void SetOnClick(UnityAction action)
        {
            onClick.RemoveAllListeners();
            onClick.AddListener(action);
        }

        public void AddOnClick(UnityAction action)
        {
            onClick.AddListener(action);
        }


        public bool InvokeOnClick()
        {
            onClick?.Invoke();
            if (UseNav)
            {
                if (navActivitiyParameters != null)
                {
                    GliderNav.Get().Show(navActivityId, navActivitiyParameters).Forget();
                }
                else
                {
                    GliderNav.Get().Show(navActivityId).Forget();
                }
            }

            return true;
        }

        public void SetVisible(bool b)
        {
            gameObject.SetActive(b);
        }
    }
#if UNITY_EDITOR
    [CustomEditor(typeof(GliderButton))]
    public class GliderButtonEditor : Editor
    {
        private void OnEnable()
        {
            Initialized = false;
            if (((GliderButton)target).UseNav)
            {
                FindInGliderNavMap();
            }
        }

        private void OnDisable()
        {
            Initialized = false;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            var button = ((GliderButton)target);
            if (button.UseNav)
            {
                if (Initialized)
                {
                    if (_activityIds.Length > 0)
                    {
                        _navIndex = EditorGUILayout.Popup("NavMap", _navIndex, _activityIds);
                        button.navActivityId = _activityIds[_navIndex];
                    }
                }
                else
                {
                    FindInGliderNavMap();
                }
            }

            if (GUI.changed)
            {
                EditorUtility.SetDirty(button);
            }
        }

        private bool Initialized = false;
        private int _navIndex;
        private GliderNavActivity[] _activities;
        private string[] _activityIds;

        private void FindInGliderNavMap()
        {
            var button = (GliderButton)target;

            var assets = AssetDatabase.FindAssets("GliderNavMap");
            if (assets.Length > 0)
            {
                Debug.LogWarning("NavMap count > 0");
            }

            var path = AssetDatabase.GUIDToAssetPath(assets[0]);
            var navMap = AssetDatabase.LoadAssetAtPath<GliderNavMap>(path);

            _activities = navMap.activities.ToArray();
            _activityIds = new string[_activities.Length];
            for (int i = 0; i < _activities.Length; i++)
            {
                _activityIds[i] = _activities[i].id;
                if (!string.IsNullOrWhiteSpace(button.navActivityId))
                {
                    if (button.navActivityId == _activityIds[i])
                    {
                        _navIndex = i;
                    }
                }
            }

            Initialized = true;
        }
    }
#endif
}