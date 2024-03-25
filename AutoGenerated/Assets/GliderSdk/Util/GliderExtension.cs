using System;
using UnityEngine;

namespace Glider.Util
{
	public static class GliderExtension
	{
		public static T GetOrAddComponent<T>(this GameObject go) where T : UnityEngine.Component
		{
			return GliderUtil.GetOrAddComponent<T>(go);
		}
		public static string ToIsoString(this DateTime dateTime)
		{
			return dateTime.ToString("O");
		}
		public static string ToShortCode(this SystemLanguage systemLanguage)
		{
			string language = "en";
			switch (systemLanguage)
			{
				case SystemLanguage.English:
					language = "en";
					break;
				case SystemLanguage.Korean:
					language = "ko";
					break;
				case SystemLanguage.ChineseSimplified:
				case SystemLanguage.Chinese:
					language = "zh-cn";
					break;
				case SystemLanguage.ChineseTraditional:
					language = "zh-tw";
					break;
				case SystemLanguage.Japanese:
					language = "ja";
					break;
			}
			return language;
		}
	}
}
