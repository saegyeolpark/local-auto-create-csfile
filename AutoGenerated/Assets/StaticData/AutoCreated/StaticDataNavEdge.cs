// This file was automatically generated by Gameduo Center Manager.
// Do not modify it manually!

using System;
using UnityEngine;

namespace Glider.Core.SerializableData
{
	[Serializable]
	public class StaticDataNavEdge
	{
        [SerializeField] private NavButtonKey buttonKey;
        public NavButtonKey ButtonKey => buttonKey;

        [SerializeField] private NavPanelKey destPanel;
        public NavPanelKey DestPanel => destPanel;

        /// <summary>
        /// 동적으로 할당되는 버튼들: 그 index를 지정 (존재가 보장되지 않음)
        /// </summary>
        [SerializeField] private int flexibleButtonIndex;
        public int FlexibleButtonIndex => flexibleButtonIndex;

        [SerializeField] private NavPanelKey srcPanel;
        public NavPanelKey SrcPanel => srcPanel;

        /// <summary>
        /// edge만으로 경로를 검색하다 보니 삭제했을 때 순서가 밀려서 따로 설정 (edge 검색용임)
        /// </summary>
        [SerializeField] private int uuid;
        public int Uuid => uuid;

	}
}