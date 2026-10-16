using UnityEngine;

/// <summary>
/// Provides cross-platform haptic feedback: phone vibration, or controller rumble through the DNExtensions
/// ControllerRumble system while a gamepad is the active input. No GameObject required.
/// </summary>
public static class MobileHaptics
{
    private static bool _isInitialized;
    private static DNExtensions.Systems.ControllerRumble.ControllerRumbleSource _rumbleSource;
    
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
        
        if (FirebaseManager.Instance && !FirebaseManager.Instance.HapticsEnabled) return;
        if (SaveManager.Instance && !SaveManager.Instance.HapticsEnabled) return;

        if (ControllerFocus.GamepadActive)
        {
            Rumble(milliseconds);
            return;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        if (vibrator != null)
        {
            vibrator.Call("vibrate", milliseconds);
        }
#elif UNITY_IOS && !UNITY_EDITOR
        Handheld.Vibrate();
#endif
    }

    /// <summary>A short controller rumble, stronger and longer for longer vibrations.</summary>
    private static void Rumble(long milliseconds)
    {
        if (!_rumbleSource) _rumbleSource = Object.FindAnyObjectByType<DNExtensions.Systems.ControllerRumble.ControllerRumbleSource>();
        if (!_rumbleSource) return;

        float strength = Mathf.Clamp(milliseconds / 100f, 0.15f, 0.7f);
        float duration = Mathf.Clamp(milliseconds / 1000f * 2f, 0.08f, 0.4f);
        _rumbleSource.Rumble(strength, strength * 0.8f, duration);
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