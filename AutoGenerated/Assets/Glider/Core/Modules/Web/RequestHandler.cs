using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using GliderSdk.Scripts;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Events;

namespace Glider.Core.Web
{
    public class Req
    {
        private const string HeaderHmacKey = "hmackey";
        private const string HeaderAuthorizationKey = "Authorization";
        private const string HeaderContentTypeKey = "Content-Type";
        private const string HeaderContentTypeValue = "application/json";
        private readonly string _bearerToken;
        public event UnityAction<string> On409Exception;

        public Req(string bearerToken)
        {
            _bearerToken = $"Bearer {bearerToken}";
            // Debug.Log($"[Req] bearer: {bearerToken}");
        }

        public Req()
        {
        }

        public async UniTask<T> PostHmac<T>(CancellationToken token, string url, object body) where T : class
        {
            using UnityWebRequest req = new UnityWebRequest();
#if UNITY_EDITOR
            Debug.Log($"[Glider.Core.Web.Req.PostHmac] url:{url}");
#endif
            req.url = url;
            req.method = UnityWebRequest.kHttpVerbPOST;

            return await RequestHmac<T>(token, req, body);
        }

        public async UniTask<T> PutHmac<T>(CancellationToken token, string url, object body) where T : class
        {
            using UnityWebRequest req = new UnityWebRequest();
#if UNITY_EDITOR
            Debug.Log($"[Glider.Core.Web.Req.PutHmac] url:{url}");
#endif
            req.url = url;
            req.method = UnityWebRequest.kHttpVerbPUT;

            return await RequestHmac<T>(token, req, body);
        }

        private async UniTask<T> RequestHmac<T>(CancellationToken token, UnityWebRequest req, object body)
            where T : class
        {
            var payloadString = JsonConvert.SerializeObject(body);
#if UNITY_EDITOR
            Debug.Log($"payloadString:{payloadString}");
#endif
            var buffer = new System.Text.UTF8Encoding().GetBytes(payloadString);
            var hMacBuffer = new System.Text.UTF8Encoding().GetBytes(payloadString + GliderSettings.Get().Salt);


            if (_bearerToken != null)
                req.SetRequestHeader(HeaderAuthorizationKey, _bearerToken);
            req.SetRequestHeader(HeaderHmacKey, Cryptography.SHA256Hash(hMacBuffer));
#if UNITY_EDITOR
            Debug.Log($"hmac:{Cryptography.SHA256Hash(hMacBuffer)}, salt:{GliderSettings.Get().Salt}");
#endif
            req.SetRequestHeader(HeaderContentTypeKey, HeaderContentTypeValue);
            req.uploadHandler = new UploadHandlerRaw(buffer);
            req.downloadHandler = new DownloadHandlerBuffer();
            try
            {
                await req.SendWebRequest().WithCancellation(token);
            }
            catch (Exception e)
            {
                Debug.LogWarning(e);
            }

            if (req.result == UnityWebRequest.Result.Success)
            {
            }
            else if (req.result == UnityWebRequest.Result.ProtocolError)
            {
                if (req.responseCode == 409)
                {
                    On409Exception?.Invoke(req.downloadHandler.text);
                }
                else
                {
                    throw new Exception(
                        $"[RequestHmac.ProtocolError] url:{req.url}, payloadString:{payloadString}, res:{req.downloadHandler.text}");
                }
            }
            else
            {
                throw new Exception(
                    $"[RequestHmac.UnknownError] url:{req.url}, payloadString:{payloadString}, res:{req.downloadHandler.text}");
            }
#if UNITY_EDITOR
            Debug.Log($"[Glider.Core.Web.Req.PostBuffer] res:{req.downloadHandler.text}");
#endif
            return JsonUtility.FromJson<T>(req.downloadHandler.text);
        }

        public async UniTask<T> Post<T>(CancellationToken token, string url, object body) where T : class
        {
            using UnityWebRequest req = new UnityWebRequest();
#if UNITY_EDITOR
            Debug.Log($"[Glider.Core.Web.Req.PostBuffer] url:{url}");
#endif
            req.url = url;
            req.method = UnityWebRequest.kHttpVerbPOST;
            var payloadString = JsonConvert.SerializeObject(body);
            var buffer = new System.Text.UTF8Encoding().GetBytes(payloadString);

            if (_bearerToken != null)
                req.SetRequestHeader(HeaderAuthorizationKey, _bearerToken);
            req.SetRequestHeader("Content-Type", "application/json");
            req.uploadHandler = new UploadHandlerRaw(buffer);
            req.downloadHandler = new DownloadHandlerBuffer();
            await req.SendWebRequest().WithCancellation(token);

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
            Debug.Log($"[Glider.Core.Web.Req.PostBuffer] result:{req.downloadHandler.text}");
#endif
            return JsonUtility.FromJson<T>(req.downloadHandler.text);
        }

        public async UniTask<T> Get<T>(CancellationToken token, string url) where T : class
        {
            using var req = UnityWebRequest.Get(url);
#if UNITY_EDITOR
            Debug.Log($"[Glider.Core.Web.Req.Get] url:{url}");
#endif

            if (_bearerToken != null)
                req.SetRequestHeader(HeaderAuthorizationKey, _bearerToken);

            var result = (await req.SendWebRequest().WithCancellation(token)).downloadHandler.text;

            // var operation = req.SendWebRequest();
            // while (operation.isDone == false)
            //     await UniTask.Yield(PlayerLoopTiming.Update);
            // var result = operation.webRequest.downloadHandler.text;
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
            Debug.Log($"[Glider.Core.Web.Req.Get] result:{result}");
#endif
            return JsonUtility.FromJson<T>(result);
        }

        public async UniTask<byte[]> GetBuffer(CancellationToken token, string url)
        {
#if ENABLE_LOG||UNITY_EDITOR
            Debug.Log($"[GdWAS.GetBuffer] {url}");
#endif
            using var req = UnityWebRequest.Get(url);
            if (_bearerToken != null)
                req.SetRequestHeader(HeaderAuthorizationKey, _bearerToken);
            var result = (await req.SendWebRequest().WithCancellation(token)).downloadHandler.data;
            return result;
        }
    }

    /// <summary>
    /// 웹서버에 요청하는 리퀘스트가 한번에 하나씩, 선입선출 될 수 있도록 큐로 관리 
    /// </summary>
    public static class RequestHandler
    {
        private struct RequestRecord
        {
            public string uuid;
            public string url;
            public float time;
        }

        private static Queue<RequestRecord> _queue = new Queue<RequestRecord>();
        private const int LoadingLimit = 10; //TODO-Glider:설정에서 변경할 수 있도록
        public static int LoadingQueueCount => _queue.Count;
        public static event UnityAction OnExceedQueueLimit;
        public static event UnityAction OnResolveQueueLimit;

        public static async UniTask Wait(CancellationToken token)
        {
            await UniTask.WaitUntil(() => _queue.Count == 0,
                PlayerLoopTiming.Update, token);
        }

        public static async UniTask Enqueue(CancellationToken token, string url)
        {
            var uuid = Guid.NewGuid().ToString();
            _queue.Enqueue(new RequestRecord { uuid = uuid, url = url, time = UnityEngine.Time.unscaledTime });

            if (_queue.Count > 1)
            {
                Debug.Log($"[RequestHandler] _queue count is {_queue.Count}");
                if (_queue.Count >= LoadingLimit) OnExceedQueueLimit?.Invoke();
                await UniTask.WaitUntil(
                    () => _queue.Count <= 1 || _queue.Peek().uuid == uuid,
                    PlayerLoopTiming.Update, token);
            }
        }

        public static async UniTask<T> Enqueue<T>(CancellationToken token, string url, Req req) where T : class
        {
            var uuid = Guid.NewGuid().ToString();
            _queue.Enqueue(new RequestRecord { uuid = uuid, url = url, time = UnityEngine.Time.unscaledTime });

            if (_queue.Count > 1)
            {
                Debug.Log($"[RequestHandler] _queue count is {_queue.Count}");
                if (_queue.Count >= LoadingLimit) OnExceedQueueLimit?.Invoke();
                await UniTask.WaitUntil(
                    () => _queue.Count <= 1 || _queue.Peek().uuid == uuid,
                    PlayerLoopTiming.Update, token);
            }

            var res = await req.Get<T>(token, url);
            Dequeue();
            return res;
        }

        public static void Dequeue()
        {
            if (_queue.Count < LoadingLimit) OnResolveQueueLimit?.Invoke();
            _queue.Dequeue();
        }
    }
}