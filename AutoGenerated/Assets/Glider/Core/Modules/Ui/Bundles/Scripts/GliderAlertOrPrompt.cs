using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Glider.Core.Ui.Bundles
{
    public class GliderAlertOrPrompt : MonoBehaviour
    {
        [SerializeField] private Canvas canvas;
        [SerializeField] private TMP_Text textTitle;
        [SerializeField] private TMP_Text textDesc;
        [SerializeField] private GameObject panelPrompt;
        [SerializeField] private GameObject panelButtons;
        [field: SerializeField] public TMP_InputField InputFieldForPrompt { get; private set; }
        [field: SerializeField] public GliderButton[] Buttons { get; private set; }

        public void SetVisible(bool b)
        {
            canvas.enabled = b;
        }

        public void SetTitle(string title)
        {
            textTitle.text = title;
        }

        public void SetDesc(string title)
        {
            textDesc.text = title;
        }

        public void SetVisibleButtons(bool b)
        {
            panelButtons.SetActive(b);
        }

        public void SetVisibleInputField(bool b)
        {
            panelPrompt.SetActive(b);
        }
    }
}