using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Glider.Core.SerializableData;
using UnityEngine;
using UnityEngine.Networking;

namespace Glider.Core
{
    [Serializable]
    internal class ResponseConfig
    {
        public GameWebServiceData[] gameWebServices;
        public AuthWebServerData authWebServer;
        public PurchaseWebServerData purchaseWebServer;
        public SharedStorageData sharedStorage;
    }

    public class GliderManager : MonoBehaviour
    {
        private static GliderManager instance;
        private static bool Initialized = false;
        public bool IsLoadedConfig { get; private set; }

        public GameWebServiceData[] GameWebServices { get; private set; }
        public AuthWebServerData AuthWebServer { get; private set; }
        public PurchaseWebServerData PurchaseWebServer { get; private set; }
        public SharedStorageData SharedStorage { get; private set; }

        public string GetStaticDataBaseUrl
        {
            get
            {
                if (!Initialized)
                    return null;
                return $"{SharedStorage.cdnBaseUrl}/shared/static-data/{Application.version}";
            }
        }

        void Awake()
        {
            if (instance)
                return;
        }

        private void OnDestroy()
        {
            Initialized = false;
            instance = null;
        }

        public static GliderManager Get()
        {
            Debug.Log("[TEST] Get");
            if (Initialized)
                return instance;
            if (instance != null)
            {
                Destroy(instance.gameObject);
            }

            var gameObject = new GameObject(nameof(GliderManager));
            instance = gameObject.AddComponent<GliderManager>();
#if UNITY_EDITOR
            Debug.Log($"[GliderManager.InitializeGlider] SdkKey={GliderSettings.Get().SdkKey}");
#endif
            DontDestroyOnLoad(gameObject);
            Initialized = true;
            return instance;
        }


        public async UniTask LoadConfigAsync(CancellationToken token)
        {
            var url = GliderSettings.Get().CurrentEnvConfigUrl;
            using var req = UnityWebRequest.Get(url);
#if UNITY_EDITOR
            Debug.Log($"[GliderManager.LoadConfigAsync] url:{url}");
#endif
            var result = (await req.SendWebRequest().WithCancellation(token)).downloadHandler.text;
            if (req.result == UnityWebRequest.Result.Success)
            {
            }
            else if (req.result == UnityWebRequest.Result.ProtocolError)
            {
            }
            else
            {
            }
#if UNITY_EDITOR
            Debug.Log(result);
#endif
            var response = JsonUtility.FromJson<ResponseConfig>(result);

#if UNITY_EDITOR
            Debug.Log($"[GliderManager.LoadConfigAsync] {JsonUtility.ToJson(response)}");
#endif
            GameWebServices = response.gameWebServices;
            AuthWebServer = response.authWebServer;
            PurchaseWebServer = response.purchaseWebServer;
            SharedStorage = response.sharedStorage;
            IsLoadedConfig = true;
        }
    }
}