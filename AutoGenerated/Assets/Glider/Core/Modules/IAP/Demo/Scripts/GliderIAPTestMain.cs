using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Glider.Core;
using Glider.Core.Auth;
using Glider.Core.IAP;
using Glider.Core.SerializableData;
using Glider.Core.StaticData;
using Glider.Core.Ui.Bundles;
using Glider.Util;
using TMPro;
using UnityEngine;

public class GliderIAPTestMain : MonoBehaviour
{
    private CancellationToken _token;

    public GliderButton[] _buttonLists;


    private async UniTask Start()
    {
        _token = gameObject.GetCancellationTokenOnDestroy();

        var manager = GliderManager.Get();
        if (!manager.IsLoadedConfig) await manager.LoadConfigAsync(_token);
        Debug.Log("GliderManager Initiated");

        var data = GliderStaticData.Get();
        if (!data.IsLoadedSharedStaticData)
            await data.LoadSharedStaticDataAsync(_token,
                $"{manager.SharedStorage.cdnBaseUrl}/shared/static-data/{Application.version}");
        Debug.Log("GliderStaticData Initiated");
        Debug.Log("test fix");
        var _login = GliderLogin.Get();

        string accessToken = null;
        // if (!_login.HasLocalAccessToken)
        if (_login.HasLocalAccessToken)
        {
            Debug.Log("Has Local Login Access Token");
            accessToken = _login.LoginAsLocalAccessToken();
        }
        else
        {
            await _login.CreateUserAsync(LoginProviderKey.Guest, _token);
        }

        accessToken = await _login.LoginAsync(LoginProviderKey.Guest, _token);
        Debug.Log("GliderLogin Initiated");
        await UniTask.Delay(500);

        var characterLists = await _login.GetCharacterListAsync(ServerKey.ko0, _token);
        Debug.Log($"chara counts {characterLists.Length}");
        if (characterLists.Length < 1)
        {
            await _login.CreateCharacterAsync(ServerKey.ko0, "G" + Time.time.ToString().Replace(".", "0"), _token);
            Debug.Log("chara create");
            characterLists = await _login.GetCharacterListAsync(ServerKey.ko0, _token);
        }

        var characterId = characterLists[0].id;
        Debug.Log($"CharacterId: {characterId}");

        var iap = GliderIAP.Get();
        iap.Init(accessToken, characterId);
        Debug.Log("GliderIAP Initiated");


        await UniTask.WaitUntil(() => GliderIAP.isInitialized);
        Debug.Log("iap initialized");
        // _buttonLists = new GliderButton[1];
        GliderUtil.UpdateList(ref _buttonLists, iap.ProductTable.Count);
        int idx = 0;
        foreach (var pair in iap.ProductTable)
        {
            var text = _buttonLists[idx].GetComponentInChildren<TMP_Text>();
            text.text = pair.Key;

            _buttonLists[idx].SetOnClick(() => { iap.TryPurchase(pair.Key, succeed => { Debug.Log(succeed); }); });

            idx++;
        }
    }
}