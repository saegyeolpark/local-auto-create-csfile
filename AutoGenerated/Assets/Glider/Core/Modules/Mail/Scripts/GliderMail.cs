using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Glider.Core.Localization;
using Glider.Core.Web;
using Glider.Core.SerializableData;
using Glider.Core.StaticData;
using Glider.Core.Ui;
using UnityEngine;

namespace Glider.Core.Mail
{
    [System.Serializable]
    public class Mail
    {
        public int id;
        public int mailFormId;
        public int characterId;
        public string minClientVersion;
        public string expiredTime;
        public string receivedTime;
        public string createdTime;
        public string updatedTime;
        public MailForm mailForm;

        [System.Serializable]
        public class MailForm
        {
            public int id;
            public MailBundle bundle;
            public string tag;
            public string createdTime;
            public string updatedTime;

            [System.Serializable]
            public class MailBundle
            {
                public LocalizedMessage localizedMessage; // 메세지
                public StaticDataAcquirableAsset acquirableAsset; // custom 가능한 보상
            }
        }

        public string GetTitle()
        {
            return mailForm.bundle.localizedMessage.lmk.ToLocalizedString();
        }

        public string GetContent()
        {
            return mailForm.tag;
        }
    }

    public class GliderMail : MonoBehaviour
    {
        private static GliderMail instance;

        private ServerKey _serverKey;
        private int _characterId;
        private Req _req;

#if UNITY_EDITOR
        public Mail[] Mails { get; private set; }
#endif

        public static GliderMail Get(string accessToken, ServerKey serverKey, int characterId)
        {
            if (instance != null)
                return instance;

            var gameObject = new GameObject(nameof(GliderMail));
            instance = gameObject.AddComponent<GliderMail>();
            instance._req = new Req(accessToken);
            instance._serverKey = serverKey;
            instance._characterId = characterId;

            DontDestroyOnLoad(gameObject);
            return instance;
        }

        public static GliderMail Get()
        {
            if (instance != null)
                return instance;

            var gameObject = new GameObject(nameof(GliderMail));
            instance = gameObject.AddComponent<GliderMail>();

            DontDestroyOnLoad(gameObject);
            return instance;
        }

        public void Init(string accessToken, ServerKey serverKey, int characterId)
        {
            _req = new Req(accessToken);
            _serverKey = serverKey;
            _characterId = characterId;
        }

        /// <summary>
        /// 메일 리스트 로드
        /// </summary>
        /// <returns></returns>
        public async UniTask<Mail[]> GetMailListAsync(CancellationToken token)
        {
            var res = await RequestGetMailListAsync(token);

#if UNITY_EDITOR
            Mails = res.mails;
#endif

            return res.mails;
        }

        /// <summary>
        /// 하나의 메일 수령
        /// </summary>
        public async UniTask PostReceiveMailAsync(int mailId, CancellationToken token)
        {
            await RequestPostMailAsync(mailId, token);
        }

        /// <summary>
        /// 모든 메일 수령
        /// </summary>
        public async UniTask PostReceiveMailBatchAsync(Mail[] mails, CancellationToken token)
        {
            await RequestPostMailBatchAsync(mails, token);
        }

        // 메일 리스트 로드 요청
        private async UniTask<Dto.ResponseGetMailList> RequestGetMailListAsync(CancellationToken token)
        {
            var glider = GliderManager.Get();
            var url =
                $"{glider.GameWebServices[(int)_serverKey].webServerBaseUrl}/character/{_characterId}/mail/receivable/list";
            Debug.Log($"[GliderMail] url: {url}");
            // await Web.RequestHandler.Enqueue(token, url);
            // Debug.Log($"[GliderMail] Enqueue Over;");
            // var res = await _req.Get<Dto.ResponseGetMailList>(token, url);
            // Web.RequestHandler.Dequeue();

            var res = await RequestHandler.Enqueue<Dto.ResponseGetMailList>(token, url, _req);
            Debug.Log($"[GliderMail] res: {res.mails.Length}");
            return res;
        }

        // 메일 하나 수령 요청
        private async UniTask RequestPostMailAsync(int mailId, CancellationToken token)
        {
            var glider = GliderManager.Get();
            var url =
                $"{glider.GameWebServices[(int)_serverKey].webServerBaseUrl}/character/{_characterId}/mail/receive/list";
            var reqPost = new Dto.RequestPostMail
            {
                mailIds = new int[] { mailId }
            };
            await _req.PostHmac<object>(token, url, reqPost);
        }

        // 모든 메일 수령 요청
        private async UniTask RequestPostMailBatchAsync(Mail[] mails, CancellationToken token)
        {
            var staticData = GliderStaticData.Get();
            if (mails.Length < 1)
            {
                GliderUi.Get().Toast("[Fix] no mails");
                return;
            }

            var glider = GliderManager.Get();
            var url =
                $"{glider.GameWebServices[(int)_serverKey].webServerBaseUrl}/character/{_characterId}/mail/receive/list";

            // 수령할 Mails id 복사
            var reqPost = new Dto.RequestPostMail();
            reqPost.mailIds = new int[mails.Length];
            for (int i = 0; i < mails.Length; i++)
                reqPost.mailIds[i] = mails[i].id;

            await _req.PostHmac<object>(token, url, reqPost);
        }

        internal class Dto
        {
            [System.Serializable]
            public class ResponseGetMailList
            {
                public Mail[] mails;
            }

            [System.Serializable]
            public class RequestPostMail
            {
                public int[] mailIds;
            }
        }
    }
}