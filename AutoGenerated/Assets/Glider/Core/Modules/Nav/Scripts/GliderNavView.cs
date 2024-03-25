using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UI;

// Controller로부터 호출되어, GliderNav에 등록하여 우선순위 관리
namespace Glider.Core.Nav
{
    public enum ViewType
    {
        Root, // 가장 기본적인 UI
        Panel,
        Tab, // 탭이면 수평이동이 가능
        Popup,
    }

    public class GliderNavView : MonoBehaviour
    {
        [field: SerializeField] protected ViewType _viewType = Nav.ViewType.Panel;
        public ViewType ViewType => _viewType;

        [HideInInspector] public UnityEvent EventActivate;
        [HideInInspector] public UnityEvent EventDeactivate;

        // 활성화
        public virtual async UniTaskVoid Activate()
        {
            SetVisible(true);

            // Nav 스택에 추가
            GliderNav.Get().PushNavView(this);

            EventActivate?.Invoke();
            Debug.Log($"[NavView] Activate {this.GetType()}");
        }

        // 비활성화
        public virtual async UniTaskVoid DeActivate()
        {
            SetVisible(false);

            GliderNav.Get().TryRemoveTargetViewInHistory(this);

            EventDeactivate?.Invoke();
            Debug.Log($"[NavView] DeActivate {this.GetType()}");
        }

        protected virtual void SetVisible(bool isVisible)
        {
            gameObject.SetActive(isVisible);
        }
    }
}