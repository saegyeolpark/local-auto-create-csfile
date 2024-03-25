using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
using Debug = UnityEngine.Debug;

namespace Glider.Core.Bundles
{
    public static class ViewHelper
    {
        public static event UnityAction<StackTrace, string> OnReportError;

        public static string MaxTextString { get; private set; }
        public static string MaxUppercaseTextString { get; private set; }


        public static void SetMaxTextString(string text)
        {
            MaxTextString = text;
        }

        public static void SetMaxUppercaseTextString(string text)
        {
            MaxUppercaseTextString = text;
        }


        public static void LocalizeFont(GameObject gameObject)
        {
            var allTexts = gameObject.GetComponentsInChildren<TMP_Text>(true);

            int normalHash = TMP_Style.NormalStyle.hashCode;
            int nowHash;
            for (int i = 0; i < allTexts.Length; i++)
            {
                // nowHash = allTexts[i].textStyle.hashCode;
                // if (nowHash == normalHash)
                // {
                // 	allTexts[i].font = BundleLocalized.Resource.GetFont(0);
                // }
                // else if (nowHash == 2425)
                // {
                // 	allTexts[i].font = BundleLocalized.Resource.GetFont(1);
                // } else if (nowHash == 2959547)
                // {
                // 	allTexts[i].font = BundleLocalized.Resource.GetFont(2);
                // } else
                // {
                // 	ReportError(new StackTrace(), $"Unknown Text Style! : {allTexts[i].textStyle}, {allTexts[i].textStyle.hashCode}");
                // }
            }
        }

        public static void ReportError(StackTrace stackTrace, string log)
        {
            OnReportError?.Invoke(stackTrace, log);
        }
    }
}