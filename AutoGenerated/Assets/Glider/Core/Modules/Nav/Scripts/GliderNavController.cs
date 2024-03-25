using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

// 데이터를 가공하고, View를 호출하기 위한 Controller 역할
namespace Glider.Core.Nav
{
    // public abstract class GliderNavController
    // {
    //     [field: SerializeField] public abstract GliderNavView View { get; protected set; }
    //
    //     public GliderNavController()
    //     {
    //         var nav = GliderNav.Get();
    //         if (nav != null)
    //         {
    //             nav.PushNavController(this);
    //         }
    //         else
    //         {
    //             Debug.LogError("[GliderNav] glider nav manager is null");
    //         }
    //     }
    //
    //     // 각종 데이터 초기화, 액션/이벤트 매핑
    //     public abstract UniTask SetupViewAsync(CancellationToken token);
    //
    //     // View 할당 해제
    //     public abstract void Release();
    // }
}