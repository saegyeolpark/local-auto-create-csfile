using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Glider.Core.Ui.Bundles
{
    [ExecuteInEditMode]
    public class GliderToggle : MonoBehaviour
    {
        [field: SerializeField] public bool IsOn { get; private set; }
        [SerializeField] private GameObject on;
        [SerializeField] private GameObject off;

        public void Toggle()
        {
            Set(!IsOn);
        }

        private void OnEnable()
        {
            UpdateView();
        }

        public void Set(bool flag)
        {
            IsOn = flag;
            UpdateView();
        }

        void UpdateView()
        {
            if (on != null)
                on.SetActive(IsOn);
            if (off != null)
                off.SetActive(!IsOn);
        }

        // private void OnDrawGizmosSelected()
        // {
        //     UpdateView();
        // }

        private void OnValidate()
        {
            UpdateView();
        }
    }
}