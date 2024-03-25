using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Glider.Core.Localization;
using Glider.Core.SerializableData;
using Glider.Core.StaticData;
using Glider.Core.Ui;
using Glider.Core.Web;
using GliderSdk.Scripts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Purchasing.Security;
using UnityEngine.Serialization;

// Tangle 클래스 가져와야 해서 어셈블리 감싸지 않았음
namespace Glider.Core.IAP
{
    [RequireComponent(typeof(InitializeUGS))]
    public class GliderIAP : MonoBehaviour, IDetailedStoreListener
    {
        private static GliderIAP instance;

        public static GliderIAP Get()
        {
            if (instance == null)
            {
                GameObject go = new GameObject();
                go.name = nameof(GliderIAP);
                instance = go.AddComponent<GliderIAP>();

                DontDestroyOnLoad(instance.gameObject);
            }

            return instance;
        }

        private IStoreController _controller; // The Unity Purchasing system.
        private IExtensionProvider _extensions;
        private Dictionary<string, Action<string>> _productIds = new Dictionary<string, Action<string>>();
        public Dictionary<string, Action<string>> ProductTable => _productIds;
        private UnityAction<bool> _callback;

        private CrossPlatformValidator _validator;
        private bool _useAppleStoreKitTestCertificate;
        private int _initTriedCount = 0;

        private CancellationToken _token;
        private string _accessToken;
        private int _characterId;
        private Req _authenticatedReq;
        private Req _unauthenticatedReq;
        private const string PurchaseInAppUrl = "{0}/character/{1}/purchase";
        private const string PurchaseInAppRewardUrl = "{0}/character/{1}/purchase/reward";

        /// <summary>
        /// 첫 Init 포함하여 최대 몇 번까지 시도할 것인지
        /// </summary>
        private const int MaxInitTryCount = 2;

        public static bool isInitialized = false;

#if UNITY_EDITOR
        public string[] productKeysOnInspector;
#endif
        public void Init(string accessToken, int characterId)
        {
            _token = gameObject.GetCancellationTokenOnDestroy();
            _accessToken = accessToken;
            _characterId = characterId;
            _authenticatedReq = new Req(_accessToken);
            _unauthenticatedReq = new Req();

            var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

            // Add products
            var inAppProductContents = GliderStaticData.Get().Shared.InAppProductContents;
            var inAppProducts = GliderStaticData.Get().Shared.InAppProducts;
#if UNITY_EDITOR
            productKeysOnInspector = new string[inAppProducts.Length];
#endif
            for (int i = 0; i < inAppProducts.Length; i++)
            {
                var inAppProductId = inAppProductContents[i].InAppProductId;
                var productId = inAppProducts[inAppProductId].ProductId;
                if (_productIds.ContainsKey(productId))
                {
                    _productIds.Remove(productId); // 다시 설정
                }

                _productIds.Add(productId, data => { _controller.InitiatePurchase(data); });
#if UNITY_EDITOR
                productKeysOnInspector[i] = productId;
#endif
            }

            foreach (var product in _productIds)
            {
                builder.AddProduct(product.Key, ProductType.Consumable);
                Debug.Log($"[GliderIAP Init] added product : {product.Key}");
            }

            _initTriedCount++;

            // Initialize Unity IAP...
            UnityPurchasing.Initialize(this, builder);
        }


        #region implements of IDetailedStoreListener

        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            isInitialized = true;

            Debug.Log("In-App Purchasing successfully initialized");
            _controller = controller;
            _extensions = extensions;

            InitializeValidator();
        }

        public void OnInitializeFailed(InitializationFailureReason error)
        {
            OnInitializeFailed(error, null);
        }

        public void OnInitializeFailed(InitializationFailureReason error, string message)
        {
            Debug.Log(
                $"message: {message}\ntime: {UnityEngine.Time.unscaledTime}, realtimeSinceStartUp: {UnityEngine.Time.realtimeSinceStartup}" +
                $"[IAPManager] Initialize Failed : {error}"
            );

            if (_initTriedCount < MaxInitTryCount)
            {
                Init(_accessToken, _characterId);
            }
        }

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs purchaseEvent)
        {
            //Retrieve the purchased product
            var product = purchaseEvent.purchasedProduct;
            var isPurchaseValid = IsPurchaseValid(product);
            if (isPurchaseValid)
            {
                Debug.Log($"[GliderIAP] IsValid Success {purchaseEvent.purchasedProduct.definition.id}");
                // 구매 진행
                PurchaseAsync(purchaseEvent.purchasedProduct).Forget();
            }
            else
            {
                // 유효성 검사 실패
                Debug.LogError($"[ProcessPurchase] IsValid Failed {purchaseEvent.purchasedProduct.definition.id}");
            }

            //We return Complete, informing IAP that the processing on our side is done and the transaction can be closed.
            return PurchaseProcessingResult.Pending;
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
        {
            Debug.Log($"Purchase failed - Product: '{product.definition.id}', PurchaseFailureReason: {failureReason}");
            _callback?.Invoke(false);
            _callback = null;
        }


        public void OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription)
        {
            Debug.Log($"Purchase failed - Product: '{product.definition.id}'," +
                      $" Purchase failure reason: {failureDescription.reason}," +
                      $" Purchase failure details: {failureDescription.message}");
            _callback?.Invoke(false);
            _callback = null;
        }

        #endregion


        void InitializeValidator()
        {
            if (IsCurrentStoreSupportedByValidator())
            {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
                var appleTangleData =
                    _useAppleStoreKitTestCertificate ? AppleStoreKitTestTangle.Data() : AppleTangle.Data();
                _validator =
                    new CrossPlatformValidator(GooglePlayTangle.Data(), appleTangleData, Application.identifier);
#endif
            }
        }

        public void TryPurchase(string productId, UnityAction<bool> callback)
        {
            // Purchase 과정 후 콜백
            _callback = callback;

            Debug.Log($"[GliderIAP] try purchase {productId}");

            if (_productIds.ContainsKey(productId))
            {
                _controller.InitiatePurchase(productId);
                // _productIds[productId](productId); // InitiatePurchase
            }
        }

        bool IsPurchaseValid(Product product)
        {
            //If we the validator doesn't support the current store, we assume the purchase is valid
            if (IsCurrentStoreSupportedByValidator())
            {
                try
                {
                    var result = _validator.Validate(product.receipt);

                    //The validator returns parsed receipts.
                    LogReceipts(result);
                }

                //If the purchase is deemed invalid, the validator throws an IAPSecurityException.
                catch (IAPSecurityException reason)
                {
                    Debug.Log($"Invalid receipt: {reason}");
                    return false;
                }
            }

            return true;
        }

        static bool IsCurrentStoreSupportedByValidator()
        {
            //The CrossPlatform validator only supports the GooglePlayStore and Apple's App Stores.
            return IsGooglePlayStoreSelected() || IsAppleAppStoreSelected();
        }

        static bool IsGooglePlayStoreSelected()
        {
            var currentAppStore = StandardPurchasingModule.Instance().appStore;
            return currentAppStore == AppStore.GooglePlay;
        }

        static bool IsAppleAppStoreSelected()
        {
            var currentAppStore = StandardPurchasingModule.Instance().appStore;
            return currentAppStore == AppStore.AppleAppStore ||
                   currentAppStore == AppStore.MacAppStore;
        }

        static void LogReceipts(IEnumerable<IPurchaseReceipt> receipts)
        {
            Debug.Log("Receipt is valid. Contents:");
            foreach (var receipt in receipts)
            {
                LogReceipt(receipt);
            }
        }

        static void LogReceipt(IPurchaseReceipt receipt)
        {
            Debug.Log($"Product ID: {receipt.productID}\n" +
                      $"Purchase Date: {receipt.purchaseDate}\n" +
                      $"Transaction ID: {receipt.transactionID}");

            if (receipt is GooglePlayReceipt googleReceipt)
            {
                Debug.Log($"Purchase State: {googleReceipt.purchaseState}\n" +
                          $"Purchase Token: {googleReceipt.purchaseToken}");
            }

            if (receipt is AppleInAppPurchaseReceipt appleReceipt)
            {
                Debug.Log($"Original Transaction ID: {appleReceipt.originalTransactionIdentifier}\n" +
                          $"Subscription Expiration Date: {appleReceipt.subscriptionExpirationDate}\n" +
                          $"Cancellation Date: {appleReceipt.cancellationDate}\n" +
                          $"Quantity: {appleReceipt.quantity}");
            }
        }

        private async UniTask PurchaseAsync(Product product)
        {
            StoreKey storeKey = StoreKey.None;
#if UNITY_EDITOR
            storeKey = StoreKey.FakeStore;
#elif UNITY_ANDROID
            storeKey = StoreKey.GooglePlay;
#elif UNITY_IOS
            storeKey = StoreKey.AppleAppStore;
#endif
            if (storeKey == StoreKey.None) return;

            Debug.LogWarning($"[PurchaseAsync] req PurchaseInApp start : {product.definition.id}");
            // 주문서 내용 API 전송 (fakeStore 여부 반환)
            // Debug.LogWarning("receipt\n" + (string)JsonConvert.DeserializeObject(product.receipt));
            var isFakeStore = await RequestPurchaseInApp(product.definition.id, storeKey, product.receipt);
            Debug.LogWarning($"[PurchaseAsync] req PurchaseInAppReward start");
            // 주문서 보상 내용 수령
            var rewardResult = await RequestPurchaseInAppReward();

            if (!rewardResult)
            {
                _callback?.Invoke(false);
                return;
            }

            var productId = product.definition.id;
            //TODO Player.Product.EnqueueLazyPurchase(productId);

            ConfirmPendingPurchase(product);
            var lmkToast = LocalizedMessageKey.ToastCheckMailBox.ToLocalizedString();
            GliderUi.Get().Toast(lmkToast);

// #if GD_BUILD
            //TODO GdSingularLinker.Instance.SendInAppPurchase(product);
// #endif

            // 인게임 메일 리로드   
            // InGameMail.TryRefresh();


            //Shop.InvokePurchaseAny();
            _callback?.Invoke(true);
        }

        private async UniTask<bool> RequestPurchaseInApp(string productId, StoreKey storeType,
            string receiptBinary)
        {
            var url = string.Format(PurchaseInAppUrl, GliderManager.Get().PurchaseWebServer.webServerBaseUrl,
                _characterId);
            // var jobj = (JObject)JsonConvert.DeserializeObject(receiptBinary);
            // jobj["Payload"] = (JObject)JsonConvert.DeserializeObject(jobj["Payload"].ToString());
            // jobj["Payload"] = (JObject)JsonConvert.DeserializeObject(jobj["Payload"].ToString());
            UnityWebRequest req = new UnityWebRequest();
#if UNITY_EDITOR
            Debug.Log($"[Glider.Core.Web.Req.PostHmac] url:{url}");
#endif
            req.url = url;
            req.method = UnityWebRequest.kHttpVerbPOST;

            // TODO body 직렬화 로직 변경 필요...
            // var result = await RequestHmac<Dto.ResponseInAppPurchase>(_token, req, new
            // {
            //     productIAPID = productId,
            //     store = storeType.ToString(),
            //     receipt = receiptBinary,
            // });


            var result = await _authenticatedReq.PostHmac<Dto.ResponseInAppPurchase>(_token, url, new
            {
                productIAPID = productId,
                store = storeType.ToString(),
                receipt = receiptBinary,
            });


            Debug.Log($"[GliderIAP] receiptBinary: {receiptBinary}");
            return result.isTest;
        }

        private async UniTask<bool> RequestPurchaseInAppReward()
        {
            var url = string.Format(PurchaseInAppRewardUrl, GliderManager.Get().PurchaseWebServer.webServerBaseUrl,
                _characterId);
            var result = await _authenticatedReq.PostHmac<Dto.ResponseInAppPurchaseReward>(_token, url, new
            {
            });
            if (result.error.code == 101)
            {
                Debug.LogError(result.error.debugMessage.results);
                GliderUi.Get().Toast(result.error.debugMessage.results);
                return false;
            }

            return true;
        }

        private void ConfirmPendingPurchase(Product product)
        {
            Debug.Log($"[GliderIAP] confirm product: {product.definition.id}");
            _controller.ConfirmPendingPurchase(product);
        }


        private const string HeaderHmacKey = "hmackey";
        private const string HeaderAuthorizationKey = "Authorization";
        private const string HeaderContentTypeKey = "Content-Type";
        private const string HeaderContentTypeValue = "application/json";
        public event UnityAction<string> On409Exception;

        private async UniTask<T> RequestHmac<T>(CancellationToken token, UnityWebRequest req, object body)
            where T : class
        {
            var payloadString = JsonConvert.SerializeObject(body);
#if UNITY_EDITOR
            Debug.Log($"payloadString:{payloadString}");
#endif
            var buffer = new System.Text.UTF8Encoding().GetBytes(payloadString);
            var hMacBuffer = new System.Text.UTF8Encoding().GetBytes(payloadString + GliderSettings.Get().Salt);


            // if (_accessToken != null)
            {
                req.SetRequestHeader(HeaderAuthorizationKey, _accessToken);
#if UNITY_EDITOR
                Debug.LogWarning($"Authorized: {_accessToken}");
#endif
            }

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

        [Serializable]
        internal class Dto
        {
            [Serializable]
            public class ResponseBase
            {
            }

            [Serializable]
            public class ResponseInAppPurchase
            {
                public bool isTest = false;
            }

            [Serializable]
            public class ResponseInAppPurchaseReward
            {
                // 정상 응답
                public string rewardedOrderIds;

                // 예외 응답
                public RewardError error;

                [Serializable]
                internal class RewardError
                {
                    public int code;

                    public RewardDebugMessage debugMessage;

                    public LocalizedMessage localizedMessage;

                    public string traceId;

                    [Serializable]
                    internal class RewardDebugMessage
                    {
                        public string results;
                    }
                }
            }
        }
    }

    public enum StoreKey
    {
        None = -1,
        GooglePlay,
        AppleAppStore,
        FakeStore
    }
}