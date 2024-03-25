using System;
using System.IO;
using UnityEngine;

namespace GliderSdk.Scripts.SdkVersionManager.Editor
{
    /// <summary>
    /// A <see cref="ScriptableObject"/> representing the Glider internal settings that can be set in the Integration Manager Window.
    ///
    /// The scriptable object asset is saved under ProjectSettings as <c>GliderSdkInternalSettings.json</c>.
    /// </summary>
    public class GliderSdkInternalSettings : ScriptableObject
    {
        private static GliderSdkInternalSettings instance;


        [SerializeField] private bool consentFlowEnabled;
        [SerializeField] private string consentFlowPrivacyPolicyUrl = string.Empty;
        [SerializeField] private string consentFlowTermsOfServiceUrl = string.Empty;
        private const string SettingsFilePath = "ProjectSettings/GliderInternalSettings.json";

        public static GliderSdkInternalSettings Instance
        {
            get
            {
                if (instance != null) return instance;

                instance = CreateInstance<GliderSdkInternalSettings>();

                var projectRootPath = Path.GetDirectoryName(Application.dataPath);
                var settingsFilePath = Path.Combine(projectRootPath, SettingsFilePath);
                if (!File.Exists(settingsFilePath))
                {
                    instance.Save();
                    return instance;
                }

                var settingsJson = File.ReadAllText(settingsFilePath);
                if (string.IsNullOrEmpty(settingsJson))
                {
                    instance.Save();
                    return instance;
                }

                JsonUtility.FromJsonOverwrite(settingsJson, instance);
                return instance;
            }
        }

        public void Save()
        {
            var settingsJson = JsonUtility.ToJson(instance);
            try
            {
                var projectRootPath = Path.GetDirectoryName(Application.dataPath);
                var settingsFilePath = Path.Combine(projectRootPath, SettingsFilePath);
                File.WriteAllText(settingsFilePath, settingsJson);
            }
            catch (Exception exception)
            {
                GliderSdkLogger.UserError("Failed to save internal settings.");
                Console.WriteLine(exception);
            }
        }

        /// <summary>
        /// Whether or not Glider Consent Flow is enabled.
        /// </summary>
        public bool ConsentFlowEnabled
        {
            get { return consentFlowEnabled; }
            set
            {
                var previousValue = consentFlowEnabled;
                consentFlowEnabled = value;

                if (value)
                {
                    // If the value didn't change, we don't need to update anything.
                    if (previousValue) return;
                }
                else
                {
                    ConsentFlowPrivacyPolicyUrl = string.Empty;
                    ConsentFlowTermsOfServiceUrl = string.Empty;
                }
            }
        }

        /// <summary>
        /// A URL pointing to the Privacy Policy for the app to be shown when prompting the user for consent.
        /// </summary>
        public string ConsentFlowPrivacyPolicyUrl
        {
            get { return consentFlowPrivacyPolicyUrl; }
            set { consentFlowPrivacyPolicyUrl = value; }
        }

        /// <summary>
        /// An optional URL pointing to the Terms of Service for the app to be shown when prompting the user for consent. 
        /// </summary>
        public string ConsentFlowTermsOfServiceUrl
        {
            get { return consentFlowTermsOfServiceUrl; }
            set { consentFlowTermsOfServiceUrl = value; }
        }
    }
}