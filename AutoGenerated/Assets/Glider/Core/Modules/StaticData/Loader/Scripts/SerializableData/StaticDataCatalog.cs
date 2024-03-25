using System.Collections.Generic;

namespace Glider.Core.StaticData.Loader.SerializableData
{
    [System.Serializable]
    public class StaticDataCatalog
    {
        public List<StaticDataCatalogData> collection = new List<StaticDataCatalogData>();
        public string lastUpdateTime;
    }

    [System.Serializable]
    public class StaticDataCatalogData
    {
        public string fileName;
        public string latestFileName;
        public string lastUpdatedTime;
        public string privateHash;
    }
}