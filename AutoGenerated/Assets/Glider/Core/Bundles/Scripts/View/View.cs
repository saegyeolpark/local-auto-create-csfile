using System;
using System.Collections.Generic;
using UnityEngine;

namespace Glider.Core.Bundles
{
    public abstract class View : Poolable
    {
        public static T GetOriginal<T>(bool doLocalize = true) where T : class
        {
            if (doLocalize)
            {
                if (!LocalizedViews.Contains(typeof(T)))
                {
                    ViewHelper.LocalizeFont(ViewMap[typeof(T)].gameObject);
                    LocalizedViews.Add(typeof(T));
                }
            }

            return ViewMap[typeof(T)] as T;
        }

        private static readonly Dictionary<Type, View> ViewMap = new Dictionary<Type, View>();
        private static readonly HashSet<Type> LocalizedViews = new HashSet<Type>();
        private const string ViewAssetLabelName = "View";

        private static void OnLoadView(GameObject go)
        {
#if UNITY_EDITOR
            Debug.Log(go.name);
#endif
            var view = go.GetComponent<View>();
            var type = view.GetType();
            if (!ViewMap.ContainsKey(type))
                ViewMap.Add(type, view);
        }
    }
}