using UnityEngine;

/// <summary>
/// Provides cross-platform haptic feedback (vibration) for mobile devices.
/// Supports Android with duration control and iOS with system haptics.
/// No GameObject required - initializes automatically on first use.
/// </summary>
public static class MobileHaptics
{
    private static bool _isInitialized;
    
#if UNITY_ANDROID && !UNITY_EDITOR
    private static AndroidJavaObject vibrator;
#endif

    /// <summary>
    /// Initializes the haptic system. Called automatically on first use.
    /// </summary>
    private static void Initialize()
    {
        if (_isInitialized) return;
        
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[MobileHaptics] Failed to initialize: {e.Message}");
        }
#endif
        
        _isInitialized = true;
    }

    /// <summary>
    /// Triggers haptic feedback.
    /// </summary>
    /// <param name="milliseconds">Duration in milliseconds (Android only, iOS uses system default)</param>
    public static void Vibrate(long milliseconds = 50)
    {
        if (!_isInitialized) Initialize();

#if UNITY_ANDROID && !UNITY_EDITOR
        if (vibrator != null)
        {
            vibrator.Call("vibrate", milliseconds);
        }
#elif UNITY_IOS && !UNITY_EDITOR
        Handheld.Vibrate();
#endif
    }

    /// <summary>
    /// Checks if the device supports haptic feedback.
    /// </summary>
    public static bool IsSupported()
    {
        if (!_isInitialized) Initialize();
        
#if UNITY_ANDROID && !UNITY_EDITOR
        if (vibrator != null)
        {
            return vibrator.Call<bool>("hasVibrator");
        }
        return false;
#elif UNITY_IOS
        return true;
#else
        return false;
#endif
    }

    /// <summary>
    /// Cancels any ongoing haptic feedback (Android only).
    /// </summary>
    public static void Cancel()
    {
        if (!_isInitialized) Initialize();
        
#if UNITY_ANDROID && !UNITY_EDITOR
        if (vibrator != null)
        {
            vibrator.Call("cancel");
        }
#endif
    }
}