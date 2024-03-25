using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Glider.Util
{
    public static class GliderUtil
    {
        public enum RegexCondition
        {
            Number,
            English,
            Korean,
            NumberEnglish,
            NumberEnglishKorean,
            EnglishKorean,
            All
        }

        private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);


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
                        GameObject g = Object.Instantiate(newArray[0].gameObject, parent);
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
                        Object.Destroy(org[i].gameObject);
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
                // return null;
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
                        GameObject g = Object.Instantiate(newArray[0].gameObject, parent);
                        newArray[i] = g.GetComponent<T>();
                    }

                    org = newArray;
                }

                return org;
            }
        }


        public static void SetLocalPosZToZero(Transform t)
        {
            Vector2 org = t.localPosition;
            t.localPosition = new Vector3(org.x, org.y, 0f);
        }

        public static bool CheckNicknameCondition(RegexCondition type, string nickname)
        {
            Regex regex;
            switch (type)
            {
                case RegexCondition.English:
                    regex = new Regex(@"^[0-9]*$", RegexOptions.None);
                    break;
                case RegexCondition.Number:
                    regex = new Regex(@"^[a-zA-Z]*$", RegexOptions.None);
                    break;
                case RegexCondition.Korean:
                    //자음 하나로는 하지 않기에 ㄱ-ㅎ은 뺀다.
                    regex = new Regex(@"^[가-힣]*$", RegexOptions.None);
                    break;
                case RegexCondition.NumberEnglish:
                    regex = new Regex(@"^[a-zA-Z0-9]*$", RegexOptions.None);
                    break;
                case RegexCondition.NumberEnglishKorean:
                    regex = new Regex(@"^[a-zA-Z0-9가-힣]*$", RegexOptions.None);
                    break;
                case RegexCondition.EnglishKorean:
                    regex = new Regex(@"^[a-zA-Z가-힣]*$", RegexOptions.None);
                    break;
                case RegexCondition.All:
                    regex = new Regex(@"^[a-zA-Z0-9가-힣一-龥ぁ-ゔァ-ヴー々〆〤]*$", RegexOptions.None);
                    break;
                default:
                    return false;
            }

            return (string.IsNullOrEmpty(nickname)) ? false : regex.IsMatch(nickname);
        }

        public static bool CheckingSpecialText(string txt)
        {
            string str = @"[~!@\#$%^&*\()\=+|\\/:;?""<>']";
            System.Text.RegularExpressions.Regex rex = new System.Text.RegularExpressions.Regex(str);
            return rex.IsMatch(txt);
        }

        public static bool CheckingText(string text)
        {
            return new Regex(@"[a-zA-Z가-힇ㄱ-ㅎㅏ-ㅣぁ-ゔァ-ヴー々〆〤一-龥]").IsMatch(text);
        }


        public static string GetTimeSpanStringSimple(TimeSpan ts, string[] divs)
        {
            var second = divs[0];
            var minute = divs[1];
            var hour = divs[2];
            var day = divs[3];
            //24시간이 넘으면 일로 표시
            if (ts.TotalHours >= 24)
            {
                return string.Format(day, Mathf.FloorToInt((float)ts.TotalDays));
            }

            //50분이 넘으면 시간으로 표시
            if (ts.TotalMinutes >= 60)
            {
                return $"{string.Format(hour, Mathf.FloorToInt((float)ts.TotalHours))}";
            }

            //나머지 분으로 표시
            if (ts.TotalSeconds >= 60)
            {
                return $"{string.Format(minute, Mathf.FloorToInt((float)ts.TotalMinutes))}";
            }
            else
            {
                return string.Format(second, Mathf.FloorToInt((float)ts.TotalSeconds));
            }
        }

        public static string GetTimeSpanString(TimeSpan ts, string[] divs, bool isFull = false,
            bool containSecond = true)
        {
            var second = divs[0];
            var minute = divs[1];
            var hour = divs[2];
            var day = divs[3];
            if (isFull)
            {
                //24시간이 넘으면 일로 표시
                if (ts.TotalHours >= 24)
                {
                    if (containSecond)
                    {
                        return $"{string.Format(day, Mathf.FloorToInt((float)ts.TotalDays))} " +
                               $"{string.Format(hour, Mathf.FloorToInt((float)ts.Hours))} " +
                               $"{string.Format(minute, Mathf.FloorToInt((float)ts.Minutes))} " +
                               $"{string.Format(second, Mathf.FloorToInt((float)ts.Seconds))}";
                    }
                    else
                    {
                        return $"{string.Format(day, Mathf.FloorToInt((float)ts.TotalDays))} " +
                               $"{string.Format(hour, Mathf.FloorToInt((float)ts.Hours))} " +
                               $"{string.Format(minute, Mathf.FloorToInt((float)ts.Minutes))} ";
                    }
                }

                //50분이 넘으면 시간으로 표시
                if (ts.TotalMinutes >= 60)
                {
                    if (containSecond)
                    {
                        return $"{string.Format(hour, Mathf.FloorToInt((float)ts.Hours))} " +
                               $"{string.Format(minute, Mathf.FloorToInt((float)ts.Minutes))} " +
                               $"{string.Format(second, Mathf.FloorToInt((float)ts.Seconds))}";
                    }
                    else
                    {
                        return $"{string.Format(hour, Mathf.FloorToInt((float)ts.Hours))} " +
                               $"{string.Format(minute, Mathf.FloorToInt((float)ts.Minutes))} ";
                    }
                }

                //나머지 분으로 표시
                if (ts.TotalSeconds >= 60)
                {
                    if (containSecond)
                    {
                        return $"{string.Format(minute, Mathf.FloorToInt((float)ts.Minutes))} " +
                               $"{string.Format(second, Mathf.FloorToInt((float)ts.Seconds))}";
                    }
                    else
                    {
                        return $"{string.Format(minute, Mathf.FloorToInt((float)ts.Minutes))} ";
                    }
                }
                else
                {
                    if (containSecond)
                        return string.Format(second, Mathf.FloorToInt((float)ts.TotalSeconds));
                    else
                        return $"{string.Format(minute, Mathf.CeilToInt((float)ts.Minutes))} ";
                }
            }
            else
            {
                //24시간이 넘으면 일로 표시
                if (ts.TotalHours >= 24)
                {
                    return string.Format(day, Mathf.FloorToInt((float)ts.TotalDays));
                }

                //50분이 넘으면 시간으로 표시
                if (ts.TotalMinutes >= 60)
                {
                    return
                        $"{string.Format(hour, Mathf.FloorToInt((float)ts.TotalHours))} {string.Format(minute, Mathf.FloorToInt((float)ts.Minutes))}";
                }

                //나머지 분으로 표시
                if (ts.TotalSeconds >= 60)
                {
                    if (containSecond)
                    {
                        return
                            $"{string.Format(minute, Mathf.FloorToInt((float)ts.TotalMinutes))} {string.Format(second, Mathf.FloorToInt((float)ts.Seconds))}";
                    }
                    else
                    {
                        return $"{string.Format(minute, Mathf.FloorToInt((float)ts.TotalMinutes))}";
                    }
                }
                else
                {
                    if (containSecond)
                    {
                        return
                            $"{string.Format(minute, Mathf.FloorToInt((float)ts.TotalMinutes))} {string.Format(second, Mathf.FloorToInt((float)ts.Seconds))}";
                    }
                    else
                    {
                        return $"{string.Format(minute, Mathf.CeilToInt((float)ts.Minutes))} ";
                    }
                }
            }
        }


        public static DateTime AddDayTargetWeek(DateTime dateTime, DayOfWeek targetDayOfWeek, int nextCount = 0)
        {
            int currentDayOfWeek = (int)dateTime.DayOfWeek;
            int targetDay = (int)targetDayOfWeek;

            int daysDifference;
            if (nextCount >= 0)
            {
                daysDifference = targetDay - currentDayOfWeek + nextCount * 7;
                daysDifference += (daysDifference < 0 ? 7 : 0);
            }
            else
            {
                daysDifference = targetDay - currentDayOfWeek + nextCount * 7;
                daysDifference -= (daysDifference > 0 ? 7 : 0);
            }

            return dateTime.AddDays(daysDifference);
        }


        public static int GlobalWeekIndex(DateTime now, DayOfWeek targetWeek = DayOfWeek.Monday)
        {
            var cleanTime = new DateTime(now.Year, now.Month, now.Day);

            var unixEpoch = GliderUtil.UnixEpoch;
            var unixTargetTime = AddDayTargetWeek(unixEpoch, targetWeek);
            var globalIndex = (cleanTime - unixTargetTime).Days / 7;

            return globalIndex;
        }


        public static Sprite TextureToSprite(Texture2D texture, Rect rect, Vector2 pivot)
        {
            var sprite = Sprite.Create(texture, rect, pivot);
            return sprite;
        }

        public static int GetYmd(DateTime datetime) => datetime.Year * 10000 + datetime.Month * 100 + datetime.Day;

#if UNITY_EDITOR
        public static void ReAssignShader(GameObject obj)
        {
            var renderers = obj.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                ReplaceShaderForEditor(renderer.sharedMaterials);
            }

            var tmps = obj.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var tmp in tmps)
            {
                ReplaceShaderForEditor(tmp.material);
                ReplaceShaderForEditor(tmp.materialForRendering);
            }

            var spritesRenderers = obj.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var spriteRenderer in spritesRenderers)
            {
                ReplaceShaderForEditor(spriteRenderer.sharedMaterials);
            }

            var images = obj.GetComponentsInChildren<Image>(true);
            foreach (var image in images)
            {
                ReplaceShaderForEditor(image.material);
            }

            var particleSystemRenderers = obj.GetComponentsInChildren<ParticleSystemRenderer>(true);
            foreach (var particleSystemRenderer in particleSystemRenderers)
            {
                ReplaceShaderForEditor(particleSystemRenderer.sharedMaterials);
            }

            var particles = obj.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var particle in particles)
            {
                var renderer = particle.GetComponent<Renderer>();
                if (renderer != null) ReplaceShaderForEditor(renderer.sharedMaterials);
            }
        }

        static void ReplaceShaderForEditor(Material[] materials)
        {
            for (int i = 0; i < materials.Length; i++)
            {
                ReplaceShaderForEditor(materials[i]);
            }
        }

        static void ReplaceShaderForEditor(Material material)
        {
            if (material == null) return;

            var shaderName = material.shader.name;
            var shader = Shader.Find(shaderName);

            if (shader != null) material.shader = shader;
        }
#endif

        public static int GetRandomIndex(float[] randomArray)
        {
            var max = randomArray[randomArray.Length - 1];
            float r = UnityEngine.Random.value * max;
            for (int i = 0; i < randomArray.Length; i++)
            {
                if (r <= randomArray[i])
                {
                    return i;
                }
            }

            return GetRandomIndex(randomArray); //randomArray.Length - 1;
        }

        public static string ToArrayString<T>(this T[] array)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append('[');
            int lengthM1 = array.Length - 1;
            for (int i = 0; i < lengthM1; i++)
            {
                sb.Append(array[i]);
                sb.Append(", ");
            }

            sb.Append(array[array.Length - 1]);
            sb.Append(']');
            return sb.ToString();
        }

        public async static UniTaskVoid PushAfter(int delayMilliSeconds, UnityAction callback,
            CancellationTokenSource cts)
        {
            await UniTask.Delay((int)(delayMilliSeconds), DelayType.DeltaTime, PlayerLoopTiming.Update, cts.Token);
            callback?.Invoke();
        }

        public static bool IsPointInScreen(Vector2 point) =>
            0 <= point.x && point.x < Screen.width &&
            0 <= point.y && point.y < Screen.height;

        public static bool IsPointerOverUIObject(Vector2 pos)
        {
            PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);

            eventDataCurrentPosition.position = pos;
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventDataCurrentPosition, results);

            return results.Count > 0;
        }

        public static T GetOrAddComponent<T>(GameObject go) where T : UnityEngine.Component
        {
            T component = go.GetComponent<T>();
            if (component == null)
                component = go.AddComponent<T>();
            return component;
        }


        public static GameObject FindChild(GameObject go, string name = null, bool recursive = false)
        {
            Transform transform = FindChild<Transform>(go, name, recursive);
            if (transform == null)
                return null;

            return transform.gameObject;
        }

        public static T FindChild<T>(GameObject go, string name = null, bool recursive = false)
            where T : UnityEngine.Object
        {
            if (go == null)
                return null;

            if (recursive == false)
            {
                for (int i = 0; i < go.transform.childCount; i++)
                {
                    Transform transform = go.transform.GetChild(i);
                    if (string.IsNullOrEmpty(name) || transform.name == name)
                    {
                        T component = transform.GetComponent<T>();
                        if (component != null)
                            return component;
                    }
                }
            }
            else
            {
                foreach (T component in go.GetComponentsInChildren<T>())
                {
                    if (string.IsNullOrEmpty(name) || component.name == name)
                        return component;
                }
            }

            return null;
        }


        public static Vector2 Rotate(this Vector2 v, float degrees)
        {
            return Quaternion.Euler(0, 0, degrees) * v;
        }

        public static float GetDistanceSqr(Vector2 dir)
        {
            //2
            return dir.x * dir.x + (dir.y * dir.y * 2 * 2);
        }

        public static double Lerp(double a, double b, float t)
        {
            return a + (b - a) * Mathf.Clamp01(t);
        }

        public static Vector2 GetIntersectPoint(Vector2 pos, Transform[] tfs)
        {
            float min = float.MaxValue;
            Vector2 res = pos;
            for (int i = 0; i < tfs.Length; i++)
            {
                Vector2 v1 = tfs[i].transform.position;
                Vector2 v2 = tfs[(i + 1) % tfs.Length].transform.position;
                var next = GetIntersectPoint(v1, v2, pos);
                float sqr = (next - pos).sqrMagnitude;
                if (sqr < min)
                {
                    min = sqr;
                    res = next;
                }
            }

            return res;
        }

        private static Vector2 GetIntersectPoint(Vector2 l1, Vector2 l2, Vector2 pos)
        {
            var dir = l2 - l1;
            var amount = Vector2.Dot((dir).normalized, (pos - l1));
            if (amount < 0)
            {
                amount = 0;
            }
            else if (amount * amount > (dir).sqrMagnitude)
            {
                amount = (dir).magnitude;
            }

            return l1 +
                   amount * (dir).normalized;
        }

        public static bool IsInside(Transform[] points, Vector2 v)
        {
            var polyCorners = points.Length;
            int i, j = polyCorners - 1;
            bool oddNodes = false;

            for (i = 0; i < polyCorners; i++)
            {
                if ((points[i].position.y < v.y && points[j].position.y >= v.y ||
                     points[j].position.y < v.y && points[i].position.y >= v.y) &&
                    (points[i].position.x <= v.x || points[j].position.x <= v.x))
                {
                    oddNodes ^= (points[i].position.x +
                                 (v.y - points[i].position.y) /
                                 (points[j].position.y - points[i].position.y) *
                                 (points[j].position.x - points[i].position.x) <
                                 v.x);
                }

                j = i;
            }

            return oddNodes;
        }

        public static bool IsInside(Vector2[] points, Vector2 v)
        {
            var polyCorners = points.Length;
            int i, j = polyCorners - 1;
            bool oddNodes = false;

            for (i = 0; i < polyCorners; i++)
            {
                if ((points[i].y < v.y && points[j].y >= v.y || points[j].y < v.y && points[i].y >= v.y) &&
                    (points[i].x <= v.x || points[j].x <= v.x))
                {
                    oddNodes ^= (points[i].x +
                        (v.y - points[i].y) / (points[j].y - points[i].y) * (points[j].x - points[i].x) < v.x);
                }

                j = i;
            }

            return oddNodes;
        }

        public static String ColorToHex(Color color)
        {
            return $"#{ColorUtility.ToHtmlStringRGBA(color)}";
        }

        public static string ColorString(string text, Color color)
        {
            return $"<color={ColorToHex(color)}>{text}</color>";
        }
    }
}