public class GliderSDK :
#if UNITY_EDITOR
    // Check for Unity Editor first since the editor also responds to the currently selected platform.
    GliderUnityEditor
#elif UNITY_ANDROID
    GliderAndroid
#elif UNITY_IPHONE || UNITY_IOS
    GlideriOS
#else
    GliderUnityEditor
#endif
{
    private const string _version = "1.0.1";

    /// <summary>
    /// Returns the current plugin version.
    /// </summary>
    public static string Version
    {
        get { return _version; }
    }
}