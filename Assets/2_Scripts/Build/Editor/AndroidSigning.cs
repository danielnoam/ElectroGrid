using System;
using System.IO;
using UnityEditor;

/// <summary>
/// The release keystore every Android release build is signed with. An installed APK only accepts an update
/// signed with the same key, so this key is what lets a newer build replace an older one.
/// </summary>
/// <remarks>
/// The keystore lives outside the repository, and the password is never written to disk: it is held for the
/// current editor session only, or read from <see cref="PasswordEnvironmentVariable"/> for headless builds.
/// The path and alias are per machine, in EditorPrefs, so no absolute path lands in the project.
/// </remarks>
internal static class AndroidSigning
{
    public const string PasswordEnvironmentVariable = "ELECTROGRID_KEYSTORE_PASSWORD";

    private const string PathKey = "ElectroGrid.Build.KeystorePath";
    private const string AliasKey = "ElectroGrid.Build.KeyAlias";
    private const string PasswordSessionKey = "ElectroGrid.Build.KeystorePassword";
    private const string DefaultAlias = "electrogrid";

    public static string DefaultPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".android-keystores", "electrogrid.keystore");

    public static string KeystorePath
    {
        get => EditorPrefs.GetString(PathKey, DefaultPath);
        set => EditorPrefs.SetString(PathKey, value ?? string.Empty);
    }

    public static string Alias
    {
        get => EditorPrefs.GetString(AliasKey, DefaultAlias);
        set => EditorPrefs.SetString(AliasKey, value ?? string.Empty);
    }

    /// <summary>Kept in SessionState, which survives script reloads but is cleared when the editor closes.</summary>
    public static string Password
    {
        get
        {
            string session = SessionState.GetString(PasswordSessionKey, string.Empty);
            return !string.IsNullOrEmpty(session) ? session : Environment.GetEnvironmentVariable(PasswordEnvironmentVariable) ?? string.Empty;
        }
        set => SessionState.SetString(PasswordSessionKey, value ?? string.Empty);
    }

    public static bool KeystoreExists => File.Exists(KeystorePath);

    /// <summary>The keytool bundled with Unity's Android module, falling back to one on PATH.</summary>
    public static string KeytoolExecutable
    {
        get
        {
            string editorFolder = Path.GetDirectoryName(EditorApplication.applicationPath) ?? string.Empty;
            string bundled = Path.Combine(editorFolder, "Data", "PlaybackEngines", "AndroidPlayer", "OpenJDK", "bin", "keytool.exe");
            return File.Exists(bundled) ? bundled : "keytool";
        }
    }

    /// <summary>Opens the keystore with the current password and alias, which proves both before a build relies on them.</summary>
    public static bool Verify(out string message)
    {
        if (!KeystoreExists)
        {
            message = $"No keystore at {KeystorePath}.";
            return false;
        }

        if (string.IsNullOrEmpty(Password))
        {
            message = $"No keystore password. Enter it in the build window, or set {PasswordEnvironmentVariable} for headless builds.";
            return false;
        }

        var result = BuildProcess.Run(KeytoolExecutable,
            $"-list -keystore {BuildProcess.Quote(KeystorePath)} -alias {BuildProcess.Quote(Alias)} -storepass:env {PasswordEnvironmentVariable}", 30,
            (PasswordEnvironmentVariable, Password));

        message = result.Succeeded ? $"Keystore OK, alias '{Alias}'." : $"Keystore check failed: {FirstLine(result.Message)}";
        return result.Succeeded;
    }

    /// <summary>Points the Android player settings at the release key for one build. Always followed by <see cref="Clear"/>.</summary>
    public static void Apply()
    {
        PlayerSettings.Android.useCustomKeystore = true;
        PlayerSettings.Android.keystoreName = KeystorePath;
        PlayerSettings.Android.keystorePass = Password;
        PlayerSettings.Android.keyaliasName = Alias;
        PlayerSettings.Android.keyaliasPass = Password;
    }

    /// <summary>
    /// Leaves no keystore in the player settings. They are saved in ProjectSettings.asset, which is committed to a
    /// public repository, and the keystore path is machine-specific and personal, so it lives in EditorPrefs instead.
    /// </summary>
    public static void Clear()
    {
        PlayerSettings.Android.useCustomKeystore = false;
        PlayerSettings.Android.keystoreName = string.Empty;
        PlayerSettings.Android.keyaliasName = string.Empty;
        PlayerSettings.Android.keystorePass = string.Empty;
        PlayerSettings.Android.keyaliasPass = string.Empty;
    }

    private static string FirstLine(string text)
    {
        using var reader = new StringReader(text ?? string.Empty);
        return reader.ReadLine() ?? string.Empty;
    }
}

/// <summary>
/// Unity's Keystore Manager and Publishing Settings save the chosen keystore into ProjectSettings.asset. This moves
/// it into this machine's build window settings and clears it from the project, on every script reload.
/// </summary>
[InitializeOnLoad]
internal static class AndroidSigningMigration
{
    static AndroidSigningMigration()
    {
        // Deferred, player settings are not reliably writable while the domain is still loading
        EditorApplication.delayCall += Migrate;
    }

    private static void Migrate()
    {
        string keystore = PlayerSettings.Android.keystoreName;
        if (string.IsNullOrEmpty(keystore) && !PlayerSettings.Android.useCustomKeystore) return;

        if (!string.IsNullOrEmpty(keystore) && !AndroidSigning.KeystoreExists)
        {
            AndroidSigning.KeystorePath = keystore;
            if (!string.IsNullOrEmpty(PlayerSettings.Android.keyaliasName)) AndroidSigning.Alias = PlayerSettings.Android.keyaliasName;
        }

        AndroidSigning.Clear();
        AssetDatabase.SaveAssets();
        UnityEngine.Debug.Log("[Build] Moved the Android keystore out of Project Settings into this computer's build settings (ElectroGrid > Build).");
    }
}
