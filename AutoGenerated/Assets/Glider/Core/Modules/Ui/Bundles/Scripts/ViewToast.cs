using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Glider.Core.Ui.Bundles
{
    public class ViewToast : MonoBehaviour
    {
        public TextMeshProUGUI textMessage;
        public RectTransform rect;

        public void StartMessage(string message, float time)
        {
            gameObject.SetActive(true);
            textMessage.text = message;
            StopAllCoroutines();
            StartCoroutine(IeMessage(time));
            transform.SetAsFirstSibling();
        }

        IEnumerator IeMessage(float time)
        {
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, 60f);
            rect.localScale = Vector3.one * 1.4f;
            textMessage.color = new Color(1f, 1f, 1f, 0f);
            float t = 0f;
            float duration = 0.1f;
            while (t < duration)
            {
                float r = t / duration;
                rect.localScale = Vector3.one * (1.4f - (1f - r) * 0.4f);
                t += Time.unscaledDeltaTime;
                textMessage.color = new Color(1f, 1f, 1f, r);
                yield return null;
            }

            rect.localScale = Vector3.one;
            textMessage.color = new Color(1f, 1f, 1f, 1f);
            yield return new WaitForSecondsRealtime(time);
            duration = 0.25f;
            t = 0f;
            while (t < duration)
            {
                float r = t / duration;
                rect.sizeDelta = new Vector2(rect.sizeDelta.x, 60f - r * 60f);
                textMessage.color = new Color(1f, 1f, 1f, 1f - r);
                t += Time.unscaledDeltaTime;
                yield return null;
            }

            gameObject.SetActive(false);
        }
    }
}