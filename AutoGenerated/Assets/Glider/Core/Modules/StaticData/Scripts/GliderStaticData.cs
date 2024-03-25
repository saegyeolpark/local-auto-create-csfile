using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Glider.Core.Localization;
using Glider.Core.SerializableData;
using UnityEngine;
using Glider.StaticData.Loader;
using GliderDemo.SerializableData;
using Newtonsoft.Json;
using UnityEngine.Serialization;


namespace Glider.Core.StaticData
{
    public class GliderStaticData : MonoBehaviour
    {
        private static GliderStaticData instance;
        private static bool Initialized = false;

        public GameServerStaticDataWrapper GameServer { get; private set; }
        public SharedStaticDataWrapper Shared { get; private set; }
#if UNITY_EDITOR
        public DataLocalizedMessage[] LocalizedMessagesForInspector;
#endif
        private DataLoader _dataLoader;
        public bool IsLoadedSharedStaticData { get; private set; }

        private static int _gameServerHash;
        private static int _gameServerHashForOptimized;
        private static int _sharedHash;
        private static int _sharedHashForOptimized;
        public static string _lastBaseUrl;


        public static GliderStaticData Get()
        {
            if (Initialized)
            {
                if (instance == null)
                {
                }
                else
                {
                    return instance;
                }
            }

            if (instance != null)
            {
                Destroy(instance.gameObject);
            }

            var gameObject = new GameObject(nameof(GliderStaticData));
            instance = gameObject.AddComponent<GliderStaticData>();
            instance._dataLoader = new DataLoader();
            Initialized = true;
            DontDestroyOnLoad(gameObject);
            return instance;
        }

        private void OnDestroy()
        {
            Initialized = false;
            instance = null;
        }

        public async UniTask LoadSharedStaticDataAsync(CancellationToken token, string baseUrl)
        {
            _lastBaseUrl = baseUrl;
            Shared = await _dataLoader.LoadAllAsync<SharedStaticDataWrapper>(baseUrl, token);
            var res = await _dataLoader.LoadDataAsync(baseUrl, LocalizedMessageCache.GetLanguageFullString(), token);
            Debug.Log($"[LocalizedMessage] {res}");
            var rows = JsonConvert.DeserializeObject<DataLocalizedMessage[]>(res);
#if UNITY_EDITOR
            LocalizedMessagesForInspector = rows;
#endif
            LocalizedMessageCache.Clear();
            foreach (var row in rows)
            {
                if (!LocalizedMessageCache.TryAdd(row.key, row.message))
                {
                    Debug.LogError($"[LoadSharedStaticData] key is already exist: {row.key}");
                }
            }

            UpdateSharedStaticDataHash();
            IsLoadedSharedStaticData = true;
        }

        public async UniTask ReloadGameStatus(CancellationToken token)
        {
            if (_lastBaseUrl == null) throw new Exception("[ReloadGameStatus] 캐싱된 baseUrl이 없음");
            var res = await _dataLoader.LoadDataAsync(_lastBaseUrl, "gameStatus", token);
            Shared.ReloadGameStatus(JsonUtility.FromJson<StaticDataGameStatus>(res));
        }

        public async UniTask LoadGameServerStaticDataAsync(CancellationToken token, string cdnBaseUrl)
        {
            GameServer = await _dataLoader.LoadAllAsync<GameServerStaticDataWrapper>(cdnBaseUrl, token);
            UpdateGameServerStaticDataHash();
        }

        private void UpdateSharedStaticDataHash()
        {
            _sharedHash = JsonUtility.ToJson(Shared).GetHashCode();
            _sharedHashForOptimized = Shared.GetHashCode();
        }

        private void UpdateGameServerStaticDataHash()
        {
            _gameServerHash = JsonUtility.ToJson(GameServer).GetHashCode();
            _gameServerHashForOptimized = GameServer.GetHashCode();
        }

        public bool CheckGameServerHash()
        {
            var isValid = false;
            var hash = JsonUtility.ToJson(GameServer).GetHashCode();
            if (_gameServerHash == hash)
            {
                isValid = true;
            }

            return isValid;
        }

        public bool CheckSharedHash()
        {
            var isValid = false;
            var hash = JsonUtility.ToJson(Shared).GetHashCode();
            if (_sharedHash == hash)
            {
                isValid = true;
            }

            return isValid;
        }

        public bool CheckGameServerHashOptimized()
        {
            var isValid = false;

//#if UNITY_EDITOR
            /*Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();*/
//#endif

            var hash = GameServer.GetHashCode();

//#if UNITY_EDITOR
            /*stopwatch.Stop();
            Debug.Log($"Elapsed Time: {stopwatch.ElapsedMilliseconds} ms");*/
//#endif

            if (_gameServerHashForOptimized == hash)
            {
                isValid = true;
            }

            return isValid;
        }

        public bool CheckSharedHashOptimized()
        {
            var isValid = false;

//#if UNITY_EDITOR
            /*Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();*/
//#endif

            var hash = Shared.GetHashCode();

//#if UNITY_EDITOR
            /*stopwatch.Stop();
            Debug.Log($"Elapsed Time: {stopwatch.ElapsedMilliseconds} ms");*/
//#endif

            if (_sharedHashForOptimized == hash)
            {
                isValid = true;
            }

            return isValid;
        }
    }
}