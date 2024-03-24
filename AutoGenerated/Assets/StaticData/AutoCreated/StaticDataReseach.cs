// This file was automatically generated by Gameduo Center Manager.
// Do not modify it manually!

using System;
using UnityEngine;

namespace Glider.Core.SerializableData
{
	[Serializable]
	public class StaticDataReseach
	{
        [SerializeField] private double cost;
        public double Cost => cost;

        [SerializeField] private CurrencyKey currencyType;
        public CurrencyKey CurrencyType => currencyType;

        [SerializeField] private LocalizedMessageKey currencyTypeLmk;
        public LocalizedMessageKey CurrencyTypeLmk => currencyTypeLmk;

        [SerializeField] private int id;
        public int Id => id;

        [SerializeField] private LocalizedMessageKey lmk;
        public LocalizedMessageKey Lmk => lmk;

        [SerializeField] private ResearchType researchType;
        public ResearchType ResearchType => researchType;

	}
}