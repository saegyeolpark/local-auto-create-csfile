using System;
using System.IO;
using System.Text;
using GliderSdk.Scripts;
using GliderSdk.Scripts.SdkVersionManager.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;


namespace GliderSdk.Scripts.SdkVersionManager.Editor
{
    public enum Platform
    {
        All,
        Android,
        iOS
    }
}

public enum Env
{
    Live,
    Sandbox,
    Local
}

public class GliderSettings : ScriptableObject
{
    public const string SettingsExportPath = "Glider/Resources/GliderSettings.asset";


    /// <summary>
    /// A placeholder constant to be replaced with the actual default localization or an empty string based on whether or not localization is enabled when when the getter is called.
    /// </summary>
    protected const string DefaultLocalization = "default_localization";

    private static GliderSettings instance;

    [SerializeField] private string projectCode;

    [SerializeField] private string sdkKey;

    // [SerializeField] private string salt;
    [SerializeField] private bool initializeOnAwake;

    [SerializeField] private Env env;
    [SerializeField] private string liveConfigUrl;
    [SerializeField] private string sandboxConfigUrl;
    [SerializeField] private string localConfigUrl;

    [SerializeField] private string photonAppId = string.Empty;

    public static GliderSettings Get()
    {
        if (instance == null)
        {
#if UNITY_EDITOR
            // Check for an existing GliderSettings somewhere in the project
            var guids = AssetDatabase.FindAssets("GliderSettings t:ScriptableObject");
            if (guids.Length > 1)
            {
                GliderSdkLogger.UserWarning("Multiple GliderSettings found. This may cause unexpected results.");
            }

            if (guids.Length != 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                instance = AssetDatabase.LoadAssetAtPath<GliderSettings>(path);
                return instance;
            }

            string settingsFilePath = Path.Combine("Assets", SettingsExportPath);
            var gliderDir = Path.Combine(Application.dataPath, "Glider");
            if (!Directory.Exists(gliderDir))
            {
                Directory.CreateDirectory(gliderDir);
            }

            var settingsDir = Path.GetDirectoryName(settingsFilePath);
            if (!Directory.Exists(settingsDir))
            {
                Directory.CreateDirectory(settingsDir);
            }

            instance = CreateInstance<GliderSettings>();
            AssetDatabase.CreateAsset(instance, settingsFilePath);
            instance.liveConfigUrl = "https://[LIVE_CDN_NAME].cloudfront.net/config.json";
            instance.sandboxConfigUrl = "https://[SANDBOX_CDN_NAME].cloudfront.net/config.json";
            instance.localConfigUrl = "https://[SANDBOX_CDN_NAME].cloudfront.net/config.json";
            GliderSdkLogger.D("Creating new GliderSettings asset at path: " + settingsFilePath);
#else
            instance = Resources.Load<GliderSettings>("GliderSettings");
#endif
        }


        return instance;
    }

    /// <summary>
    /// Glider SDK Key.
    /// </summary>
    public string SdkKey
    {
        get => Get().sdkKey;
        set => Get().sdkKey = value;
    }

    //public string Salt => SdkKey[..16];
    public string Salt
    {
        get
        {
            if (string.IsNullOrWhiteSpace(_salt))
            {
                _salt = Cryptography.SHA256Hash(SdkKey)[..16];
            }

            return _salt;
        }
    }

    private static string _salt = null;

    public string ProjectCode
    {
        get => Get().projectCode;
        set => Get().projectCode = value;
    }

    public Env Env
    {
        get => Get().env;
        set => Get().env = value;
    }

    public string CurrentEnvConfigUrl
    {
        get
        {
            switch (Env)
            {
                case Env.Live:
                    return LiveConfigUrl;
                case Env.Sandbox:
                    return SandboxConfigUrl;
                case Env.Local:
                    return LocalConfigUrl;
                default:
                    throw new Exception("[GliderSetting] Invalid Env");
            }
        }
    }

    public string LiveConfigUrl
    {
        get => Get().liveConfigUrl;
        set => Get().liveConfigUrl = value;
    }

    public string SandboxConfigUrl
    {
        get => Get().sandboxConfigUrl;
        set => Get().sandboxConfigUrl = value;
    }

    public string LocalConfigUrl
    {
        get => Get().localConfigUrl;
        set => Get().localConfigUrl = value;
    }


    /// <summary>
    /// Photon App ID.
    /// </summary>
    public string PhotonAppId
    {
        get { return Get().photonAppId; }
        set { Get().photonAppId = value; }
    }


    /// <summary>
    /// Saves the instance of the settings.
    /// </summary>
    public void SaveAsync()
    {
#if UNITY_EDITOR
        EditorUtility.SetDirty(instance);
#endif
    }
}