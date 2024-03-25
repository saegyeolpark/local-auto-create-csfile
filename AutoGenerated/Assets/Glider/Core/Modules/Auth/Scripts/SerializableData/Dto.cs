using System.Collections.Generic;

namespace Glider.Core.Auth.SerializableData
{
    [System.Serializable]
    public class ResponseAuth
    {
        public string fileName;
        public string lastUpdatedTime;
        public string privateHash;
    }
}