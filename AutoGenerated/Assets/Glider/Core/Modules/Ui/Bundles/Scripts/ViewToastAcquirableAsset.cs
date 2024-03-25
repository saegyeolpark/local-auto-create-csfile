using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Glider.Core.Ui.Bundles
{
    public class ViewToastAcquirableAsset : MonoBehaviour
    {
        [SerializeField] private RectTransform rect;
        [SerializeField] private Image imageIcon;
        [SerializeField] private TextMeshProUGUI textName;

        public void StartMessage(Sprite sprite, string message, float time)
        {
            gameObject.SetActive(true);

            imageIcon.sprite = sprite;
            textName.text = message;

            StopAllCoroutines();
            StartCoroutine(IeMessage(time));
            transform.SetAsFirstSibling();
        }

        IEnumerator IeMessage(float time)
        {
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, 40f);
            rect.localScale = Vector3.one * 1.4f;
            textName.color = new Color(1f, 1f, 1f, 0f);
            imageIcon.color = new Color(1f, 1f, 1f, 0f);
            float t = 0f;
            float duration = 0.1f;
            while (t < duration)
            {
                float r = t / duration;
                rect.localScale = Vector3.one * (1.4f - (1f - r) * 0.4f);
                t += Time.unscaledDeltaTime;
                textName.color = new Color(1f, 1f, 1f, r);
                imageIcon.color = new Color(1f, 1f, 1f, r);
                yield return null;
            }

            rect.localScale = Vector3.one;
            textName.color = Color.white;
            imageIcon.color = Color.white;
            yield return new WaitForSecondsRealtime(time);
            duration = 0.2f;
            t = 0f;
            while (t < duration)
            {
                float r = t / duration;
                //rect.sizeDelta = new Vector2(rect.sizeDelta.x, 36f - r * 36f);
                textName.color = new Color(1f, 1f, 1f, 1f - r);
                imageIcon.color = new Color(1f, 1f, 1f, 1f - r);
                t += Time.unscaledDeltaTime;
                yield return null;
            }

            gameObject.SetActive(false);
        }
    }
}