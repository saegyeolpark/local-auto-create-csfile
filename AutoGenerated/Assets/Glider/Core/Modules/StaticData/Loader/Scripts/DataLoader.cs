using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Glider.Core.SerializableData;
using Glider.Core.StaticData.Loader.SerializableData;
using Glider.Util;
using GliderSdk.Scripts;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace Glider.StaticData.Loader
{
    public class DataLoader
    {
        private StaticDataCatalog _catalog;

        private const string KeyStaticDataLocalBody = "net.gameduo.sdl_body_{0}";
        private const string KeyStaticDataLocalUpdatedTime = "net.gameduo.sdl_updated_time_{0}";

        public DataLoader()
        {
        }

        private StaticDataCatalogData GetCatalogData(string filename)
        {
            return _catalog?.collection?.Find(e => e.fileName == filename);
        }

        private void SaveLocalCache(string loadUrl, string body, string lastUpdatedTime)
        {
            Debug.Log($"[Save Local Cache] {loadUrl} + {lastUpdatedTime}");
            PlayerPrefs.SetString(string.Format(KeyStaticDataLocalBody, loadUrl), body);
            PlayerPrefs.SetString(string.Format(KeyStaticDataLocalUpdatedTime, loadUrl), lastUpdatedTime);
            PlayerPrefs.Save();
        }

        private string GetLocalBody(string loadUrl)
        {
            var res = PlayerPrefs.GetString(string.Format(KeyStaticDataLocalBody, loadUrl), null);
            return res;
        }

        private DateTime GetLocalLastUpdatedTime(string loadUrl)
        {
            var res = PlayerPrefs.GetString(string.Format(KeyStaticDataLocalUpdatedTime, loadUrl), null);
            return DataUtil.GetDateTimeByIsoString(res);
        }

        [System.Serializable]
        public class LocalCachedStaticData
        {
            public string body;
            public string lastUpdatedTime;
        }

        private async UniTask LoadCatalog(string catalogUrl, CancellationToken token)
        {
            _catalog = await Get<StaticDataCatalog>(token, catalogUrl);
        }

        public async UniTask<T> LoadAllAsync<T>(string baseUrl, CancellationToken token)
        {
            // 카탈로그 로드
            if (_catalog == null)
                await LoadCatalog($"{baseUrl}/_catalog.json", token);

            // 카탈로그의 각 데이터들 
            var dataCommonType = typeof(T);
            var dataCommonFields = dataCommonType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
            CancellationToken loadingToken = cts.Token;

            ConcurrentDictionary<string, string> result = new ConcurrentDictionary<string, string>();
            for (int i = 0; i < dataCommonFields.Length; i++)
            {
                Debug.Log($"dataCommonFields[i].Name:{dataCommonFields[i].Name}\n{baseUrl}");
                LoadDataAsync(baseUrl, dataCommonFields[i].Name, loadingToken, result).Forget();
            }

            const float timeOutTime = 30f;
            for (float t = 0f;
                 t < timeOutTime && result.Count < dataCommonFields.Length;
                 t += UnityEngine.Time.unscaledDeltaTime)
            {
                await UniTask.Yield(token);
            }

            if (result.Count != dataCommonFields.Length)
            {
                cts.CancelAndDispose();
                throw new Exception($"[StaticData.Load] timeout({timeOutTime}s");
            }

            StringBuilder sb = new StringBuilder(1000);
            sb.Append("{");
            foreach (var kv in result)
            {
                sb.Append('\"');
                sb.Append(kv.Key);
                sb.Append('\"');
                sb.Append(':');
                sb.Append(kv.Value);
                sb.Append(',');
            }

            sb.Remove(sb.Length - 1, 1);
            sb.Append("}");
            Debug.Log(sb.ToString());
            return JsonUtility.FromJson<T>(sb.ToString());
        }

// 		public static async UniTask LoadAbTestStaticDataVariations(ServerKey serverKey, CancellationToken token, Dictionary<string, string> abTestMap)
// 		{
// 			var serverData = WebServerManager.Config.servers[(int)serverKey];
// 			var s3BaseUrl = serverData.BaseUrl;
// 			var projectCode = WebServerManager.ProjectCode;
// 			var serverCode = serverData.serverCode;
// 			var version = Application.version;
//
// 			var dataCommonType = typeof(StaticDataWrapperForPlayer);
// 			var dataCommonFields = dataCommonType.GetFields();
// 			StringBuilder sb = new StringBuilder(1000);
// 			sb.Append("{");
//
// 			CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(token);
// 			CancellationToken loadingToken = cts.Token;
// 			StaticDataLoading staticDataLoading = new StaticDataLoading(
// 				serverData,
// 				s3BaseUrl,
// 				projectCode,
// 				serverCode,
// 				version,
// 				dataCommonFields.Length);
//
// 			ConcurrentDictionary<string, int> concurrentDictionary = new ConcurrentDictionary<string, int>();
// 			int length = 0;
// 			List<int> fieldIndexes = new List<int>();
// 			for (int i = 0; i < dataCommonFields.Length; i++)
// 			{
// 				string orgFieldName = dataCommonFields[i].Name;
// 				string fieldName = orgFieldName;
// 				if (!abTestMap.ContainsKey(fieldName)) continue;
//
// 				fieldName = abTestMap[fieldName];
// #if UNITY_EDITOR || ENABLE_LOG
// 				Debug.Log($"[LoadAbTestStaticDataVariations] Load field '{orgFieldName}' from '{fieldName}'");
// #endif
// 				concurrentDictionary.TryAdd(orgFieldName, 1);
// 				LoadDataAsync(staticDataLoading, length++, fieldName, orgFieldName, loadingToken, concurrentDictionary).Forget();
// 				fieldIndexes.Add(i);
// 			}
//
// 			const float timeOutTime = 30f;
// 			for (float t = 0f; t < timeOutTime && concurrentDictionary.Count > 0; t += UnityEngine.Time.deltaTime)
// 			{
// 				await UniTask.Yield(token);
// 			}
//
// 			if (concurrentDictionary.Count != 0)
// 			{
// 				cts.CancelAndDispose();
// 				//복구해야됨
// 				//SceneError.Open("Please check your Internet connection.", "Please reconnect.");
// 				throw new Exception($"[StaticData.Load] timeout2 ({timeOutTime}s");
// 			}
//
// 			// if (!StaticDataLoader.CheckHash())
// 			// {
// 			// 	//복구해야됨
// 			// 	// LogReporter.ReportError(
// 			// 	//  "",
// 			// 	//  "invalidStaticDataHash").Forget();
// 			// 	// SceneError.Open("Abnormal manipulation",
// 			// 	//  Player.AccountUuidWithVersion);
// 			// 	return;
// 			// }
//
// 			for (int i = 0; i < length; i++)
// 			{
// #if UNITY_EDITOR || ENABLE_LOG
// 				Debug.Log($"[LoadAbTestStaticDataVariations] SetValue -----------------------------");
// 				Debug.Log($"[LoadAbTestStaticDataVariations] field: '{dataCommonFields[fieldIndexes[i]].Name}', type: '{dataCommonFields[fieldIndexes[i]].FieldType}'");
// 				Debug.Log($"[LoadAbTestStaticDataVariations] jsonResult: '{staticDataLoading.results[i]}'");
// #endif
// 				dataCommonFields[fieldIndexes[i]].SetValue(
// 					Wrapper,
// 					JsonConvert.DeserializeObject(staticDataLoading.results[i], dataCommonFields[fieldIndexes[i]].FieldType));
// 			}
//
// 		}

        public async UniTask<string> LoadDataAsync(
            string baseUrl, string fieldName, CancellationToken token,
            ConcurrentDictionary<string, string> concurrentDictionary = null)
        {
            // 서버에서 받아온 catalog에서 참조하려는 데이터세트만 가져옴
            var catalogData = GetCatalogData(fieldName);
            if (catalogData == null)
                throw new Exception($"[StaticData.LoadData] catalog is null (fieldName:{fieldName})");

            string finalData;
            var filename = catalogData.latestFileName;
            var url = $"{baseUrl}/{filename}";
            var cacheUrl = $"{baseUrl}/{filename.Substring(0, filename.IndexOf("_"))}";
            // 마지막으로 로컬에 캐싱된 데이터세트의 저장 시간
            var localLastUpdatedTime = GetLocalLastUpdatedTime(cacheUrl);

            // Debug.Log($"Check Remote Load\n{DataUtil.GetDateTimeByIsoString(catalogData.lastUpdatedTime)}\n{localLastUpdatedTime}");

            // 갱신 필요한지 확인
            if (DataUtil.GetDateTimeByIsoString(catalogData.lastUpdatedTime) > localLastUpdatedTime)
            {
#if UNITY_EDITOR || ENABLE_LOG
                Debug.Log(
                    $"[StaticData.LoadData] 리모트 로드 : ${fieldName}, url:{url} ({catalogData?.lastUpdatedTime} > {localLastUpdatedTime})");
#endif
                using var req = UnityWebRequest.Get(url);
                var res = await GetBuffer(token, url);
                var unzipped = Cryptography.Unzip(res);
                Debug.LogWarning($"unzipped:{unzipped}");
                finalData = Cryptography.DecryptAesToBytes(unzipped, GliderSettings.Get().Salt);
                //finalData = Cryptography.Unzip(Encoding.UTF8.GetBytes(decrypted));
                SaveLocalCache(cacheUrl, finalData, catalogData?.lastUpdatedTime);
            }
            // 로컬과 서버에 차이 없으면 로컬에 저장된 데이터세트 로드
            else
            {
                finalData = GetLocalBody(cacheUrl);
#if UNITY_EDITOR || ENABLE_LOG
                Debug.Log($"[StaticData.LoadData] 로컬로드 : {fieldName} \n{finalData}");
#endif
            }


            // catalog의 privateHash와 가져온 데이터에서 hash 비교하여 무결성 체크
            // salt값은 데이터 올릴 때 썼던 값과 동일해야 함 
            var hash = Cryptography.SHA256Hash(Encoding.UTF8.GetBytes(finalData));
            var hashWithSalt = Convert.ToBase64String(Cryptography.EncryptAes(hash, GliderSettings.Get().Salt));
            if (string.IsNullOrWhiteSpace(catalogData.privateHash) || hashWithSalt != catalogData.privateHash)
            {
#if UNITY_EDITOR
                //Debug.LogError($"[StaticData.LoadData] Error while loading field: {fieldName}");
#endif
                //throw new Exception("[StaticData.LoadData] invalid hash");
            }


            if (concurrentDictionary != null)
                concurrentDictionary[fieldName] = finalData;
            return finalData;
        }

        public static async UniTask<byte[]> GetBuffer(CancellationToken token, string url)
        {
#if UNITY_EDITOR
            Debug.Log($"[DataLoader.GetBuffer] {url}");
#endif
            using var req = UnityWebRequest.Get(url);
            var result = (await req.SendWebRequest().WithCancellation(token)).downloadHandler.data;
            return result;
        }

        public static async UniTask<T> Get<T>(CancellationToken token, string url) where T : class
        {
            using var req = UnityWebRequest.Get(url);
#if UNITY_EDITOR
            Debug.Log($"[DataLoader.Get] url:{url}");
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
            Debug.Log($"[DataLoader.Get] result:{result}");
#endif
            return JsonConvert.DeserializeObject<T>(result);
        }
    }
}