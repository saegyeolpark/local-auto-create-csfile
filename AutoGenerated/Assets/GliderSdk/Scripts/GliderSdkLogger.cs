using UnityEngine;
#if UNITY_EDITOR
public class GliderSdkLogger
{
	private const string SdkTag = "Gameduo Glider";
	public const string KeyVerboseLoggingEnabled = "net.gameduo.verbose_logging_enabled";
    
	/// <summary>
	/// Log debug messages.
	/// </summary>
	public static void UserDebug(string message)
	{
		Debug.Log("Debug [" + SdkTag + "] " + message);
	}
    
	/// <summary>
	/// Log debug messages when verbose logging is enabled.
	///
	/// Verbose logging can be enabled by calling <see cref="GliderSdk.SetVerboseLogging"/> or via the Integration Manager for build time logs.
	/// </summary>
	public static void D(string message)
	{
		if (GliderSDK.IsVerboseLoggingEnabled())
		{
			Debug.Log("Debug [" + SdkTag + "] " + message);
		}
	}
    
	/// <summary>
	/// Log warning messages.
	/// </summary>
	public static void UserWarning(string message)
	{
		Debug.LogWarning("Warning [" + SdkTag + "] " + message);
	}
    
	/// <summary>
	/// Log warning messages when verbose logging is enabled.
	///
	/// Verbose logging can be enabled by calling <see cref="GliderSdk.SetVerboseLogging"/> or via the Integration Manager for build time logs.
	/// </summary>
	public static void W(string message)
	{
		if (GliderSDK.IsVerboseLoggingEnabled())
		{
			Debug.LogWarning("Warning [" + SdkTag + "] " + message);
		}
	}
    
	/// <summary>
	/// Log error messages.
	/// </summary>
	public static void UserError(string message)
	{
		Debug.LogError("Error [" + SdkTag + "] " + message);
	}
    
	/// <summary>
	/// Log error messages when verbose logging is enabled.
	///
	/// Verbose logging can be enabled by calling <see cref="GliderSdk.SetVerboseLogging"/> or via the Integration Manager for build time logs.
	/// </summary>
	public static void E(string message)
	{
		if (GliderSDK.IsVerboseLoggingEnabled())
		{
			Debug.LogError("Error [" + SdkTag + "] " + message);
		}
	}
    
}
#endif