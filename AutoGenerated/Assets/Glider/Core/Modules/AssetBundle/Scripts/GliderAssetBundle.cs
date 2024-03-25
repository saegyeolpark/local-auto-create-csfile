using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Glider.Core.AssetBundle
{
    public class GliderAssetBundle : MonoBehaviour
    {
        private static GliderAssetBundle instance;
        private static bool Initialized = false;
        public event UnityAction<float> OnProgressChange;
        private static readonly string[] Sizes = { "Bytes", "KB", "MB", "GB", "TB" };

        public static GliderAssetBundle Get()
        {
            if (Initialized && instance != null)
                return instance;
            if (instance != null)
            {
                Destroy(instance.gameObject);
            }

            var gameObject = new GameObject(nameof(GliderAssetBundle));
            instance = gameObject.AddComponent<GliderAssetBundle>();
            Initialized = true;
            DontDestroyOnLoad(gameObject);
            return instance;
        }

        private void OnDestroy()
        {
            Initialized = false;
            instance = null;
        }

        // download 전에 다운받을 용량을 표시
        public async UniTask<long> GetDownloadSizeAsync(string key, CancellationToken token)
        {
            var res = await Addressables.GetDownloadSizeAsync(key).ToUniTask(cancellationToken: token);
            Debug.Log($"[AssetBundleLoader.GetDownloadSizeAsync] getDownloadSize:{res}");
            return res;
        }

        public static string BytesToSize(long bytes)
        {
            int order = 0;
            double sizeInBytes = (double)bytes;

            while (sizeInBytes >= 1024 && order < Sizes.Length - 1)
            {
                order++;
                sizeInBytes /= 1024;
            }

            return $"{sizeInBytes:0.##} {Sizes[order]}";
        }

        // 타겟 번들 download
        public async UniTask<bool> DownloadAsync(string key, CancellationToken token)
        {
            var downloadHandle = Addressables.DownloadDependenciesAsync(key, false);

            float progress = 0;
            while (downloadHandle.Status == AsyncOperationStatus.None)
            {
                float percentageComplete = downloadHandle.GetDownloadStatus().Percent;
                if (percentageComplete > progress * 1.1)
                {
                    progress = percentageComplete;
                    OnProgressChange?.Invoke(progress);
                }

                await UniTask.Yield(token);
            }

            var isSuccess = downloadHandle.Status == AsyncOperationStatus.Succeeded;
            Addressables.Release(downloadHandle);
            return isSuccess;
        }
    }
}