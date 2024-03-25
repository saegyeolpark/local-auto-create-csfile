using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Glider.Core.Auth
{
    internal class LocalCaching
    {
        private const string LocalCachingAccessToken = "net.gameduo.glider.access_token";
        private const string LocalCachingGuestToken = "net.gameduo.glider.guest_token";
        public static bool HasAccessToken => !string.IsNullOrWhiteSpace(AccessToken);
        public static string AccessToken => PlayerPrefs.GetString(LocalCachingAccessToken, string.Empty);
        private static string GuestToken => PlayerPrefs.GetString(LocalCachingGuestToken, string.Empty);
        private static string tempGuestToken = null;

        public static void SaveGuestUuid()
        {
            PlayerPrefs.SetString(LocalCachingGuestToken, tempGuestToken);
            PlayerPrefs.Save();
        }

        public static void SaveAccessToken(string accessToken)
        {
            PlayerPrefs.SetString(LocalCachingAccessToken, accessToken);
            PlayerPrefs.Save();
        }

        public static string GetGuestToken()
        {
            if (string.IsNullOrWhiteSpace(GuestToken))
            {
                if (tempGuestToken == null)
                {
                    tempGuestToken = Guid.NewGuid().ToString();
                }

                return tempGuestToken;
            }
            else
            {
                return GuestToken;
            }
        }
    }
}