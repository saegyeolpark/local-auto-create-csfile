// This file was automatically generated by Gameduo Center Manager.
// Do not modify it manually!

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Glider.Core.SerializableData
{
	[Serializable]
	public class CharacterEquipmentWrapper : CloudDataBase
	{
        [SerializeField] private List<CharacterEquipment> list = new();
        private Dictionary<int, UnityAction<int>> _changeCallback=new();
        public event UnityAction OnAnyChange;
        public int Size => list?.Count ?? 0;
        public CharacterEquipment Get(int index)
        {
			if (_crcCodes[index] != list[index].CreateCrdCode())
				throw new InvalidCloudDataHashException("Failed to retrieve cloud data due to invalid hash.", "equipment", index, JsonUtility.ToJson(list[index]));
			var res = new CharacterEquipment();
			res = list[index];
			return res;
        }
        public void Set(int index, CharacterEquipment value)
        {
			var crdCode = list[index].CreateCrdCode();
			if (_crcCodes[index] != crdCode)
				throw new InvalidCloudDataHashException("Failed to set cloud data due to invalid hash.", "equipment", index, JsonUtility.ToJson(list[index]), JsonUtility.ToJson(value));
			list[index]=value;
			_crcCodes[index]=value.CreateCrdCode();
			IsDirty = true;
			if(_changeCallback.ContainsKey(index))
				_changeCallback[index]?.Invoke(index);
			OnAnyChange?.Invoke();
        }
        public void AddChangeListener(UnityAction action)
        {
        	OnAnyChange += action;
        }
        public void RemoveChangeListener(UnityAction action)
        {
        	OnAnyChange -= action;
        }
        public void AddChangeListener(int index, UnityAction<int> action)
        {
        	if (!_changeCallback.ContainsKey(index))
        		_changeCallback.Add(index, action);
        	else
        		_changeCallback[index] += action;
        }
        public void RemoveChangeListener(int index, UnityAction<int> action)
        {
        	if (_changeCallback.ContainsKey(index))
        		_changeCallback[index] -= action;
        }
		public void UpdateCrcCode()
		{
			for (int i = 0; i < list.Count; i++)
			{
				var code=list[i].CreateCrdCode();
				if (i<_crcCodes.Count)
					_crcCodes[i] = code ;
				else
					_crcCodes.Add(code);
			}
		}
		public void Add(CharacterEquipment e)
		{
			list.Add(e);
			_crcCodes.Add(e.CreateCrdCode());
			IsDirty = true;
		}
		public void SetPayload(ref List<string> keys, ref List<string> values)
		{
			if (IsDirty)
			{
				keys.Add("equipment");
				values.Add(JsonUtility.ToJson(this));
				IsDirty = false;
			}
		}
	}
}
