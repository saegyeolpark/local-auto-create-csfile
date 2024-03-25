using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Glider.Core.SerializableData
{
    [Serializable]
    public class SharedStorageData
    {
        /// <summary> S3 버킷 이름 </summary>
        public string storageName;

        /// <summary> S3 Base Url </summary>
        public string storageBaseUrl;

        /// <summary> CloudFront Url </summary>
        public string cdnBaseUrl;
    }

    [Serializable]
    public class GameWebServiceData
    {
        //게임서버 구분 코드 (ex.ko0)
        public string gameServerCode;
        public string webServerBaseUrl;

        /// <summary> S3 버킷 이름 </summary>
        public string storageName;

        /// <summary> S3 Base Url </summary>
        public string storageBaseUrl;

        /// <summary> CloudFront Url </summary>
        public string cdnBaseUrl;

        public override string ToString()
        {
            return JsonUtility.ToJson(this);
        }
    }

    [Serializable]
    public class AuthWebServerData
    {
        public string webServerBaseUrl;
    }

    [Serializable]
    public class PurchaseWebServerData
    {
        public string webServerBaseUrl;
    }
}