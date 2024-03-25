using System;
using System.Collections.Generic;
using Glider.Core.SerializableData;
using UnityEngine;

namespace Glider.Core.Localization
{
    public static class GliderStaticDataExtension
    {
        public static string GetMessage(this LocalizedMessage msg, SystemLanguage language)
        {
            string message;
            if (msg.lmk > 0)
            {
                message = msg.lmk.ToLocalizedString();
            }
            else
            {
                message = msg.Find(language);

                //없으면 영어
                if (message == null && language != SystemLanguage.English)
                {
                    message = msg.Find(SystemLanguage.English);
                }
            }

            message = DataUtil.GetIsoStringTagConvertedContent(message,
                LocalizedMessageKey.FormatWeekDay.ToLocalizedString());
            if (msg.bundles is { Length: > 0 })
            {
                message = string.Format(message, msg.bundles);
            }

            return message;
        }
    }

    public static class LocalizedMessageCache
    {
        private static readonly Dictionary<LocalizedMessageKey, string> _localizedMessageMap = new();
        private static readonly Dictionary<SystemLanguage, string> PossibleLanguages = new();

        public static string GetLanguageFullString()
        {
            return PossibleLanguages[GetLanguageCode()];
        }

        public static SystemLanguage GetLanguageCode()
        {
            SystemLanguage language;
            if (PossibleLanguages.Count == 0)
            {
                PossibleLanguages.Add(SystemLanguage.Korean, "korean");
                PossibleLanguages.Add(SystemLanguage.English, "english");
                PossibleLanguages.Add(SystemLanguage.Chinese, "chineseSimplified");
                PossibleLanguages.Add(SystemLanguage.ChineseSimplified, "chineseSimplified");
                PossibleLanguages.Add(SystemLanguage.ChineseTraditional, "chineseTraditional");
                PossibleLanguages.Add(SystemLanguage.Japanese, "japanese");
            }

            language = PossibleLanguages.ContainsKey(Application.systemLanguage)
                ? Application.systemLanguage
                : SystemLanguage.English;

#if UNITY_EDITOR
            //language = SystemLanguage.English;
            //language = SystemLanguage.Japanese;
            //language = SystemLanguage.ChineseSimplified;
            //language = SystemLanguage.ChineseTraditional;
#endif


            return language;
        }


        public static bool TryAdd(LocalizedMessageKey key, string message)
        {
            return _localizedMessageMap.TryAdd(key, message);
        }

        public static string ToLocalizedString(this LocalizedMessageKey key)
        {
            if (_localizedMessageMap.TryGetValue(key, out var s)) return s;
            NotFound(key);
            return String.Empty;
        }

        public static string ToLocalizedString(this LocalizedMessageKey key, double param)
        {
            if (_localizedMessageMap.TryGetValue(key, out var value)) return String.Format(value, param);
            NotFound(key);
            return String.Empty;
        }

        public static string ToLocalizedString(this LocalizedMessageKey key, int param)
        {
            if (_localizedMessageMap.TryGetValue(key, out var value)) return String.Format(value, param);
            NotFound(key);
            return String.Empty;
        }

        public static string ToLocalizedString(this LocalizedMessageKey key, int param1, int param2)
        {
            if (_localizedMessageMap.TryGetValue(key, out var value)) return String.Format(value, param1, param2);
            NotFound(key);
            return String.Empty;
        }

        public static string ToLocalizedString(this LocalizedMessageKey key, int param1, int param2, int param3)
        {
            if (_localizedMessageMap.TryGetValue(key, out var value))
                return String.Format(value, param1, param2, param3);
            NotFound(key);
            return String.Empty;
        }

        public static string ToLocalizedString(this LocalizedMessageKey key, string param)
        {
            if (_localizedMessageMap.TryGetValue(key, out var value)) return String.Format(value, param);
            NotFound(key);
            return String.Empty;
        }

        public static string ToLocalizedString(this LocalizedMessageKey key, string param1, string param2)
        {
            if (_localizedMessageMap.TryGetValue(key, out var value)) return String.Format(value, param1, param2);
            NotFound(key);
            return String.Empty;
        }

        public static string ToLocalizedString(this LocalizedMessageKey key, string param1, string param2,
            string param3)
        {
            if (_localizedMessageMap.TryGetValue(key, out var value))
                return String.Format(value, param1, param2, param3);
            NotFound(key);
            return String.Empty;
        }

        public static string ToLocalizedString(this LocalizedMessageKey key, string param1, int param2)
        {
            if (_localizedMessageMap.TryGetValue(key, out var value)) return String.Format(value, param1, param2);
            NotFound(key);
            return String.Empty;
        }

        public static string ToLocalizedString(this LocalizedMessageKey key, string param1, double param2)
        {
            if (_localizedMessageMap.TryGetValue(key, out var value)) return String.Format(value, param1, param2);
            NotFound(key);
            return String.Empty;
        }

        public static string ToLocalizedString(this LocalizedMessageKey key, params object[] param)
        {
            if (_localizedMessageMap.TryGetValue(key, out var value))
            {
                if (param == null || param.Length == 0)
                    return value;
                return String.Format(value, param);
            }

            NotFound(key);
            return String.Empty;
        }

        private static void NotFound(LocalizedMessageKey key)
        {
            if (_localizedMessageMap.Count == 0)
            {
                throw new Exception("[LocalizedMessageCache] 캐싱되지 않은 상태에서 호출됨");
            }
#if UNITY_EDITOR
            Debug.LogError($"<color='red'>{key.ToString()} is not found in localized message map.</color>");
            return;
#endif
            //복구해야됨
            // LogReporter.ReportError(
            //   null, $"<color='red'>{key.ToString()} is not found in localized message map.</color>")
            //  .Forget();
        }


        public static void Clear()
        {
            _localizedMessageMap.Clear();
        }
    }
}