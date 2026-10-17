using System.Collections.Generic;
using UnityEngine;

/// <summary>A game moment with its own feel: a phone vibration length and a controller rumble.</summary>
public enum Haptic { Tick, Pop, Hit, Collect, ComboStep, ComboFull, LineBreak, SurgeMade, Surge, FuseBlown, Win, Lose }

/// <summary>
/// Provides cross-platform haptic feedback: phone vibration, or controller rumble through the DNExtensions
/// ControllerRumble system while a gamepad is the active input. No GameObject required.
/// </summary>
public static class MobileHaptics
{
    private static bool _isInitialized;
    private static DNExtensions.Systems.ControllerRumble.ControllerRumbleSource _rumbleSource;
    private static readonly Dictionary<Haptic, float> LastPlayed = new Dictionary<Haptic, float>();
    private const float RepeatInterval = 0.05f;

    private readonly struct Feel
    {
        public readonly long PhoneMilliseconds;
        public readonly float Low;
        public readonly float High;
        public readonly float Duration;
        public readonly bool FadeOut;

        public Feel(long phoneMilliseconds, float low, float high, float duration, bool fadeOut = false)
        {
            PhoneMilliseconds = phoneMilliseconds;
            Low = low;
            High = high;
            Duration = duration;
            FadeOut = fadeOut;
        }
    }

    private static Feel FeelOf(Haptic haptic) => haptic switch
    {
        Haptic.Tick => new Feel(0, 0f, 0.25f, 0.05f),
        Haptic.Pop => new Feel(50, 0.12f, 0.3f, 0.07f),
        Haptic.Hit => new Feel(30, 0.25f, 0.25f, 0.08f),
        Haptic.Collect => new Feel(50, 0.35f, 0.5f, 0.14f, true),
        Haptic.ComboStep => new Feel(0, 0.1f, 0.35f, 0.06f),
        Haptic.ComboFull => new Feel(0, 0.45f, 0.9f, 0.3f, true),
        Haptic.LineBreak => new Feel(0, 0.75f, 0.55f, 0.3f, true),
        Haptic.SurgeMade => new Feel(0, 0.35f, 0.65f, 0.2f, true),
        Haptic.Surge => new Feel(0, 0.85f, 0.75f, 0.35f, true),
        Haptic.FuseBlown => new Feel(0, 1f, 0.5f, 0.45f, true),
        Haptic.Win => new Feel(0, 0.3f, 0.6f, 0.6f, true),
        Haptic.Lose => new Feel(0, 0.7f, 0.15f, 0.8f, true),
        _ => new Feel(50, 0.2f, 0.2f, 0.1f)
    };
    
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
        
        if (!Allowed()) return;

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

    /// <summary>
    /// Plays the feel of a game moment: a rumble tuned to it with a gamepad, or the phone vibration. Moments that only
    /// have a rumble do nothing on a phone. The same moment repeated within a few frames plays once.
    /// </summary>
    public static void Play(Haptic haptic, float strength = 1f)
    {
        if (!_isInitialized) Initialize();

        if (!Allowed()) return;

        float now = Time.unscaledTime;
        if (LastPlayed.TryGetValue(haptic, out float last) && now - last < RepeatInterval) return;
        LastPlayed[haptic] = now;

        var feel = FeelOf(haptic);
        if (ControllerFocus.GamepadActive)
        {
            if (!_rumbleSource) _rumbleSource = Object.FindAnyObjectByType<DNExtensions.Systems.ControllerRumble.ControllerRumbleSource>();
            if (!_rumbleSource) return;

            float low = Mathf.Clamp01(feel.Low * strength);
            float high = Mathf.Clamp01(feel.High * strength);
            if (feel.FadeOut) _rumbleSource.RumbleFadeOut(low, high, feel.Duration);
            else _rumbleSource.Rumble(low, high, feel.Duration);
            return;
        }

        if (feel.PhoneMilliseconds > 0) Vibrate(feel.PhoneMilliseconds);
    }

    /// <summary>Phone vibration follows the Haptics setting, controller rumble the Rumble setting.</summary>
    private static bool Allowed()
    {
        if (FirebaseManager.Instance && !FirebaseManager.Instance.HapticsEnabled) return false;
        if (!SaveManager.Instance) return true;
        return ControllerFocus.GamepadActive ? SaveManager.Instance.RumbleEnabled : SaveManager.Instance.HapticsEnabled;
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