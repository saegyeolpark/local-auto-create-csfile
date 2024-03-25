using UnityEditor;

public class GliderUnityEditor : GliderBase
{
	/// <summary>
	/// Whether or not verbose logging is enabled.
	/// </summary>
	/// <returns><c>true</c> if verbose logging is enabled.</returns>
	public static bool IsVerboseLoggingEnabled()
	{
#if UNITY_EDITOR
		return EditorPrefs.GetBool(GliderSdkLogger.KeyVerboseLoggingEnabled, false);
#else
        return false;
#endif
	}
    
}
