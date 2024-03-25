using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Glider.Core.Localization;
using Glider.Core.SerializableData;
using Glider.Core.Ui;
using Glider.Core.Web;
using UnityEngine;

namespace Glider.Core.Auth
{
    [System.Serializable]
    public class Character
    {
        public int userId;
        public int id;
        public int languageDivisionServerId;
        public string nickname;
        public string lastUpdateTime;
        public string createdTime;
    }

    internal class Dto
    {
        [System.Serializable]
        public class ResponsePostAuthLogin
        {
            public string accessToken;
            public int userId;
        }

        [System.Serializable]
        public class ResponseGetCharacterList
        {
            public Character[] list;
        }

        [System.Serializable]
        public class ResponsePostCharacter
        {
            public Glider409Exception error;
        }

        [System.Serializable]
        public class ResponsePostAuthProviderFind
        {
            public string userUuid;
            public string password;
        }

        [System.Serializable]
        public class ResponsePostAuthProviderLink
        {
        }

        [System.Serializable]
        public class ResponsePostUser
        {
            public string userUuid;
            public string password;
        }
    }

    public enum Platform
    {
        UnityEditor,
        Android,
        iOS,
        StandaloneWindows64
    }

    [Flags]
    public enum LoginProviderKey
    {
        Undefined = -1,
        Guest = 0,
        Google = 1 << 0,
        Apple = 1 << 1,
    }

    public class GliderLogin : MonoBehaviour
    {
        private static GliderLogin instance;
        private static bool Initialized = false;

        private readonly Dictionary<LoginProviderKey, string> _cachedProviderValues = new();
        private Req _unauthenticatedReq;
        private Req _authenticatedReq;

        private Character _loggedInCharacter;

        public Character LoggedInCharacter
        {
            get { return _loggedInCharacter; }
        }

        public static GliderLogin Get()
        {
            if (Initialized)
                return instance;
            if (instance != null)
            {
                Destroy(instance.gameObject);
            }

            var gameObject = new GameObject(nameof(GliderLogin));
            instance = gameObject.AddComponent<GliderLogin>();
            instance._unauthenticatedReq = new Req();
            Initialized = true;
            DontDestroyOnLoad(gameObject);
            return instance;
        }

        public Platform GetPlatform()
        {
            var platform = Platform.UnityEditor;

#if UNITY_EDITOR || UNITY_STANDALONE_WIN
#elif UNITY_ANDROID
			platform = Platform.Android;
#else
			platform = Platform.iOS;
#endif
            return platform;
        }

        public async UniTask CreateUserAsync(LoginProviderKey key, CancellationToken token)
        {
            var responseCreate = await RequestPostUserAsync(token);
            var providerValue = await GetProviderValueAsync(key);
            await RequestPostLinkAsync(key, providerValue, responseCreate.userUuid, responseCreate.password, token);
            if (key == LoginProviderKey.Guest)
            {
                LocalCaching.SaveGuestUuid();
            }
        }

        public async UniTask<string> LoginAsync(LoginProviderKey key, CancellationToken token)
        {
            var providerValue = await GetProviderValueAsync(key);
            Debug.Log($"[Provider Value] {providerValue}");
            var responseFind = await RequestPostFindAsync(key, providerValue, token);
            Debug.Log($"[Response Find] {key} / {providerValue}\n{responseFind.userUuid} / {responseFind.password}");

            if (!string.IsNullOrWhiteSpace(responseFind.userUuid) && !string.IsNullOrWhiteSpace(responseFind.password))
            {
                var responseLogin = await RequestLoginAsync(responseFind.userUuid, responseFind.password, token);
                var accessToken = responseLogin.accessToken;
                LocalCaching.SaveAccessToken(accessToken);
                _authenticatedReq = new Req(accessToken);
                return accessToken;
            }

            return null;
        }

        public async UniTask<Character[]> GetCharacterListAsync(ServerKey serverKey, CancellationToken token)
        {
            var res = await RequestGetCharacterListAsync(serverKey, token);
            return res.list;
        }

        public async UniTask CreateCharacterAsync(ServerKey serverKey, string nickname, CancellationToken token)
        {
            await RequestPostCharacterAsync(serverKey, nickname, token);
        }

        private async UniTask<string> GetProviderValueAsync(LoginProviderKey key)
        {
            if (_cachedProviderValues.ContainsKey(key))
                return _cachedProviderValues[key];
            string providerValue = null;
            switch (key)
            {
                case LoginProviderKey.Apple:
                    //TODO-glider: (업데이트 예정) apple provider value 가져오기  (com.lupidan.apple-signin-unity-src추천)
                    providerValue = "TEMP_APPLE_LOGIN";
                    break;
                case LoginProviderKey.Google:
                    //TODO-glider: (업데이트 예정) google provider value 가져오기(https://github.com/googlesamples/google-signin-unity/pull/205추천)
                    providerValue = "TEMP_GOOGLE_LOGIN";
                    break;
                case LoginProviderKey.Guest:
                    providerValue = LocalCaching.GetGuestToken();
                    break;
            }

            _cachedProviderValues[key] = providerValue;
            return providerValue;
        }

        public bool HasLocalAccessToken => !string.IsNullOrWhiteSpace(LocalCaching.AccessToken);

        public string LoginAsLocalAccessToken()
        {
            var token = LocalCaching.AccessToken;
            _authenticatedReq = new Req(token);
            _authenticatedReq.On409Exception += On409Exception;
            return token;
        }

        void On409Exception(string message)
        {
            try
            {
                Debug.Log("[GliderLogin] 409 Exception event");
                var exception = JsonUtility.FromJson<Glider409Exception>(message);
                GliderUi.Get().Toast(exception.error.localizedMessage.GetMessage(Application.systemLanguage));
            }
            catch (Exception e)
            {
                Debug.LogWarning(e.Message);
            }
        }

        public static void ClearLocalAccessToken()
        {
            LocalCaching.SaveAccessToken(null);
        }

        public void Release()
        {
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            _unauthenticatedReq = null;
            _cachedProviderValues.Clear();
            Initialized = false;
            instance = null;
        }


        #region Request

        private async UniTask<Dto.ResponsePostAuthLogin> RequestLoginAsync(string userUuid, string password,
            CancellationToken token)
        {
            var glider = GliderManager.Get();
            var url = $"{glider.AuthWebServer.webServerBaseUrl}/auth/login";

            await Web.RequestHandler.Enqueue(token, url);
            var res = await _unauthenticatedReq.PostHmac<Dto.ResponsePostAuthLogin>(token, url, new
            {
                userUuid = userUuid,
                password = password
            });
            Web.RequestHandler.Dequeue();
            return res;
        }

        private async UniTask<Dto.ResponseGetCharacterList> RequestGetCharacterListAsync(ServerKey serverKey,
            CancellationToken token)
        {
            var glider = GliderManager.Get();
            var url = $"{glider.GameWebServices[(int)serverKey].webServerBaseUrl}/character/list";
            // 리팩토링 테스트
            var res = await Web.RequestHandler.Enqueue<Dto.ResponseGetCharacterList>(token, url,
                _authenticatedReq);

            // await Web.RequestHandler.Enqueue(token, url);
            // var res = await _authenticatedReq.Get<Dto.ResponseGetCharacterList>(token, url);
            // Web.RequestHandler.Dequeue();

            return res;
        }

        private async UniTask<Dto.ResponsePostCharacter> RequestPostCharacterAsync(ServerKey serverKey, string nickname,
            CancellationToken token)
        {
            var glider = GliderManager.Get();
            var url = $"{glider.GameWebServices[(int)serverKey].webServerBaseUrl}/character";
            await Web.RequestHandler.Enqueue(token, url);
            var res = await _authenticatedReq.PostHmac<Dto.ResponsePostCharacter>(token, url, new
            {
                nickname
            });
            Web.RequestHandler.Dequeue();
            return res;
        }

        private async UniTask<Dto.ResponsePostAuthProviderFind> RequestPostFindAsync(LoginProviderKey loginProviderKey,
            string providerValue, CancellationToken token)
        {
            var glider = GliderManager.Get();
            var url = $"{glider.AuthWebServer.webServerBaseUrl}/auth/provider/find";
            await Web.RequestHandler.Enqueue(token, url);
            var res = await _unauthenticatedReq.PostHmac<Dto.ResponsePostAuthProviderFind>(token, url, new
            {
                providerKey = loginProviderKey.ToString(),
                providerValue
            });
            Web.RequestHandler.Dequeue();
            return res;
        }

        private async UniTask<Dto.ResponsePostAuthProviderLink> RequestPostLinkAsync(LoginProviderKey loginProviderKey,
            string providerValue, string userUuid, string password, CancellationToken token)
        {
            var glider = GliderManager.Get();
            var url = $"{glider.AuthWebServer.webServerBaseUrl}/auth/provider/link";
            await Web.RequestHandler.Enqueue(token, url);
            var res = await _unauthenticatedReq.PostHmac<Dto.ResponsePostAuthProviderLink>(token, url, new
            {
                providerKey = loginProviderKey.ToString(),
                providerValue,
                userUuid,
                password
            });
            Web.RequestHandler.Dequeue();
            return res;
        }

        private async UniTask<Dto.ResponsePostUser> RequestPostUserAsync(CancellationToken token)
        {
            var glider = GliderManager.Get();
            var url = $"{glider.AuthWebServer.webServerBaseUrl}/user";
            await Web.RequestHandler.Enqueue(token, url);
            var res = await _unauthenticatedReq.PostHmac<Dto.ResponsePostUser>(token, url, new
            {
                platform = GetPlatform().ToString(),
                version = Application.version
            });
            Web.RequestHandler.Dequeue();
            return res;
        }

        #endregion
    }
}