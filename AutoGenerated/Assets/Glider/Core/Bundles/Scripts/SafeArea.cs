using UnityEngine;

namespace Glider.Core.Bundles
{
    public class SafeArea : MonoBehaviour
    {
        public bool top = true;
        public bool left = true;
        public bool bottom = true;
        public bool right = true;

        private RectTransform _rect;
        private Vector2 _defaultAnchoredPosition;
        private Vector2 _defaultSizeDelta;

        public float fixedArea => -_rect.sizeDelta.y;

        private static readonly float _minLeft = 0;
        private static readonly float _minRight = 0;
        private static readonly float _minTop = 0;
        private static readonly float _minBottom = 0;

        void OnEnable()
        {
            Set();
        }


        void Set()
        {
            if (_rect == null)
            {
                _rect = GetComponent<RectTransform>();
                _defaultAnchoredPosition = _rect.anchoredPosition;
                _defaultSizeDelta = _rect.sizeDelta;
            }

            var l = left ? Mathf.Max(_minLeft, Screen.safeArea.xMin) : 0;
            var r = right ? Mathf.Max(_minRight, (Screen.width - Screen.safeArea.xMax)) : 0;
            var b = bottom ? Mathf.Max(_minBottom, Screen.safeArea.yMin) : 0;
            var t = top ? Mathf.Max(_minTop, (Screen.height - Screen.safeArea.yMax)) : 0;

            _rect.offsetMin = new Vector2(l, b);
            _rect.offsetMax = new Vector2(-r, -t);
        }
    }
}