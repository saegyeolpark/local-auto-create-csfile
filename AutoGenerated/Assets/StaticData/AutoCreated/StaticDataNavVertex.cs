// This file was automatically generated by Gameduo Center Manager.
// Do not modify it manually!

using System;
using UnityEngine;

namespace Glider.Core.SerializableData
{
	[Serializable]
	public class StaticDataNavVertex
	{
        /// <summary>
        /// 끌 수 있는 UI인가?
        /// </summary>
        [SerializeField] private bool isClosable;
        public bool IsClosable => isClosable;

        /// <summary>
        /// 해당 패널의 타입
        /// </summary>
        [SerializeField] private NavPanelKey panelKey;
        public NavPanelKey PanelKey => panelKey;

        /// <summary>
        /// 탭에 속하는 UI들을 같은 그룹으로 묶어서 교차 on/off가 가능하게끔
        /// </summary>
        [SerializeField] private int tabGroup;
        public int TabGroup => tabGroup;

        /// <summary>
        /// 고유id
        /// </summary>
        [SerializeField] private int uuid;
        public int Uuid => uuid;

	}
}
