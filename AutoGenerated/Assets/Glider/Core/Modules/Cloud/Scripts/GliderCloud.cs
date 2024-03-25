using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Glider.Core.SerializableData;
using Glider.Core.Ui;
using Glider.Core.Web;
using JetBrains.Annotations;
using UnityEngine;

namespace Glider.Core.Cloud
{
    public class GliderCloud : MonoBehaviour
    {
        private static GliderCloud instance;
        private static bool Initialized = false;

        private ServerKey _serverKey;
        private string _lastSaveToken;
        private int _integratedSaveCount = 0;
        private int _characterId;
        private Req _req;
        private CloudDataWrapper _data;


        public static GliderCloud Get(string accessToken, ServerKey serverKey, int characterId)
        {
            if (Initialized)
                return instance;
            if (instance != null)
            {
                Destroy(instance.gameObject);
            }

            var gameObject = new GameObject(nameof(GliderCloud));
            instance = gameObject.AddComponent<GliderCloud>();
            instance._serverKey = serverKey;
            instance._req = new Req(accessToken);
            instance._characterId = characterId;
            Initialized = true;

            DontDestroyOnLoad(gameObject);
            return instance;
        }

        [CanBeNull]
        public async UniTask<CloudDataWrapper> LoadDataAsync(CancellationToken token)
        {
            var res = await RequestGetCharacterDataCommonAsync(token);
            _lastSaveToken = res.saveToken;
            _data = res.data;
            _data.UpdateCrc();
            return _data;
        }

        public async UniTask SaveDataAsync(CancellationToken token)
        {
            var keys = new List<string>();
            var values = new List<string>();
            _data.SetPayload(ref keys, ref values);
            var res = await RequestPutCharacterDataCommonAsync(keys, values, token);
        }

        private async UniTask<Dto.ResponseGetData> RequestGetCharacterDataCommonAsync(CancellationToken token)
        {
            var glider = GliderManager.Get();
            var url =
                $"{glider.GameWebServices[(int)_serverKey].webServerBaseUrl}/character/{_characterId}/data/common";
            await Web.RequestHandler.Enqueue(token, url);
            var res = await _req.Get<Dto.ResponseGetData>(token, url);
            Web.RequestHandler.Dequeue();
            return res;
        }

        private async UniTask<Dto.ResponsePutData> RequestPutCharacterDataCommonAsync(List<string> keys,
            List<string> values, CancellationToken token)
        {
            var glider = GliderManager.Get();
            var url =
                $"{glider.GameWebServices[(int)_serverKey].webServerBaseUrl}/character/{_characterId}/data/common";
            await Web.RequestHandler.Enqueue(token, url);
            if (string.IsNullOrWhiteSpace(_lastSaveToken))
            {
                _lastSaveToken = Guid.NewGuid().ToString();
            }

            var res = await _req.PutHmac<Dto.ResponsePutData>(token, url, new
            {
                saveToken = _lastSaveToken,
                integratedSaveCount = ++_integratedSaveCount,
                keys = keys,
                values = values
            });
            _lastSaveToken = res.saveToken;
            Web.RequestHandler.Dequeue();
            return res;
        }

        internal class Dto
        {
            [System.Serializable]
            public class ResponseGetData
            {
                public string saveToken;
                public CloudDataWrapper data;
            }

            [System.Serializable]
            public class ResponsePutData
            {
                public string saveToken;
                public int integratedSaveCount;
            }
        }
    }
}