using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Glider.Core.SerializableData
{
    public static class DataUtil
    {
        public static T[] Expand<T>(this T[] array, int size, T defaultValue)
        {
            var newArray = new T[size];
            var prevLength = 0;
            if (array == null)
            {
                newArray = new T[size];
            }
            else
            {
                prevLength = array.Length;
                Array.Copy(array, newArray, size);
            }

            for (int i = prevLength; i < size; i++)
            {
                newArray[i] = defaultValue;
            }

            return newArray;
        }

        public static DateTime GetDateTimeByIsoString(string isoString)
        {
            if (string.IsNullOrWhiteSpace(isoString)) return DateTime.MinValue;
            //return DateTime.Parse(isoString, CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind);
            return DateTime.Parse(isoString, null, DateTimeStyles.RoundtripKind).ToLocalTime();
        }

        public static int GetLocalHour(int utc0Hour)
        {
#if UNITY_EDITOR
            // var curTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");
            var curTimeZone = TimeZoneInfo.Local;
#else
            var curTimeZone = TimeZoneInfo.Local;
#endif
            TimeSpan currentOffset = curTimeZone.GetUtcOffset(DateTime.Now);
            var offsetHours = (utc0Hour + (int)currentOffset.TotalHours);
            var localHour = offsetHours >= 0 ? offsetHours % 24 : (offsetHours % 24 + 24) % 24;

            return localHour;
        }

        public static string GetIsoStringTagConvertedContent(string content, string formatWeekDay)
        {
            if (string.IsNullOrEmpty(content)) return String.Empty;

            string result = content;

            try
            {
                string pattern = "<isoTime.*?>(.*?)<\\/isoTime>";

                MatchCollection matches = Regex.Matches(content, pattern);
                var replaces = new List<KeyValuePair<string, string>>();
                foreach (Match match in matches)
                {
                    //0_XX
                    //1_X_XX
                    //2_XXXXXXXXXX
                    var str = match.Groups[1].Value;
                    var div = str.Split('_');
#if UNITY_EDITOR
                    Debug.LogWarning($"str:{str} / match.Name:{match.Value}");
#endif
                    switch (str[0])
                    {
                        case '0':
                        {
                            var hour = int.Parse(div[1]);
                            var localHour = GetLocalHour(hour);
                            replaces.Add(new KeyValuePair<string, string>(match.Value, localHour.ToString()));
                        }
                            break;
                        case '1':
                        {
                            var dayOfWeek = (DayOfWeek)int.Parse(div[1]);
                            var hour = int.Parse(div[2]);
                            var localWeekHour = GetLocalWeekHour(dayOfWeek, hour);
#if UNITY_EDITOR
                            CultureInfo culture = CultureInfo.GetCultureInfo("en-US");
#else
                                CultureInfo culture = CultureInfo.CurrentCulture;
#endif
                            DayOfWeek day = localWeekHour.Key;
                            string dayName = culture.DateTimeFormat.GetDayName(day);
                            var res = string.Format(formatWeekDay, dayName, localWeekHour.Value);
                            replaces.Add(new KeyValuePair<string, string>(match.Value, res));
                        }
                            break;
                        case '2':
                        {
                            var iso = GetDateTimeByIsoString(div[1]);
                            var localTime = iso.ToLocalTime().ToString(
#if UNITY_EDITOR
                                // CultureInfo.GetCultureInfo("en-US")
                                CultureInfo.CurrentCulture
#else
                                    CultureInfo.CurrentCulture
#endif
                            );
                            replaces.Add(new KeyValuePair<string, string>(match.Value, localTime));
                        }
                            break;
                    }
                }

                foreach (var kv in replaces)
                {
                    result = result.Replace(kv.Key, kv.Value);
                }
            }
            catch (Exception exception)
            {
                Debug.LogError(exception);
            }

            return result;
        }

        public static KeyValuePair<DayOfWeek, int> GetLocalWeekHour(DayOfWeek dayOfWeek, int utc0Hour)
        {
#if UNITY_EDITOR
            // var curTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");
            var curTimeZone = TimeZoneInfo.Local;
#else
            var curTimeZone = TimeZoneInfo.Local;
#endif
            TimeSpan currentOffset = curTimeZone.GetUtcOffset(DateTime.Now);

            var localHour = (utc0Hour + (int)currentOffset.TotalHours);
            if (localHour < 0)
            {
                dayOfWeek--;
            }
            else if (localHour >= 24)
            {
                dayOfWeek++;
            }

            if (dayOfWeek < DayOfWeek.Sunday) dayOfWeek = DayOfWeek.Saturday;
            if (dayOfWeek > DayOfWeek.Saturday) dayOfWeek = DayOfWeek.Sunday;
            localHour %= 24;
            return new KeyValuePair<DayOfWeek, int>(dayOfWeek, localHour);
        }
    }
}