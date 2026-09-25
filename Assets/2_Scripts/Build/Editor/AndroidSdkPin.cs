using UnityEditor;
using UnityEngine;

/// <summary>
/// Pins the Android target API level. Left on Automatic, Unity targets the highest SDK installed on whichever
/// machine builds, so the target can change silently between machines or after an SDK update, and Google Play
/// rejects uploads below its current minimum. Reapplied on every script reload, so switching it back to
/// Automatic in Player Settings does not stick; change <see cref="TargetSdk"/> here instead.
/// </summary>
[InitializeOnLoad]
internal static class AndroidSdkPin
{
    /// <summary>Android 16. Google Play requires new apps and updates to target the previous year's release each August.</summary>
    public const AndroidSdkVersions TargetSdk = (AndroidSdkVersions)36;

    static AndroidSdkPin()
    {
        // Deferred, player settings are not reliably writable while the domain is still loading
        EditorApplication.delayCall += Apply;
    }

    private static void Apply()
    {
        if (PlayerSettings.Android.targetSdkVersion == TargetSdk) return;

        var previous = PlayerSettings.Android.targetSdkVersion;
        PlayerSettings.Android.targetSdkVersion = TargetSdk;
        AssetDatabase.SaveAssets();

        Debug.Log($"[Build] Android target API pinned to {(int)TargetSdk} (was {(previous == AndroidSdkVersions.AndroidApiLevelAuto ? "Automatic" : ((int)previous).ToString())}).");
    }
}
