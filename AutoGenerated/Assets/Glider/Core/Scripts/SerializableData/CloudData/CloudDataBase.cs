using System;
using System.Collections.Generic;
using UnityEngine;

namespace Glider.Core.SerializableData
{
    public class CloudDataBase
    {
        public bool IsDirty { get; protected set; }
        protected List<int> _crcCodes = new();

        public void ClearDirty()
        {
            IsDirty = false;
        }
    }
}