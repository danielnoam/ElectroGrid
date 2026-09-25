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

    /// <summary>The PowerShell command that creates the keystore. keytool asks for the password itself, so it is never typed into a file.</summary>
    public static string CreateCommand =>
        $"New-Item -ItemType Directory -Force \"{Path.GetDirectoryName(KeystorePath)}\" | Out-Null; & \"{KeytoolExecutable}\" -genkeypair -v -keystore \"{KeystorePath}\" -alias {Alias} " +
        "-keyalg RSA -keysize 2048 -validity 10000 -dname \"CN=Daniel Noam, O=Daniel's Games\"";

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

    public readonly struct Snapshot
    {
        public readonly bool UseCustomKeystore;
        public readonly string KeystoreName;
        public readonly string KeyaliasName;

        public Snapshot(bool useCustomKeystore, string keystoreName, string keyaliasName)
        {
            UseCustomKeystore = useCustomKeystore;
            KeystoreName = keystoreName;
            KeyaliasName = keyaliasName;
        }
    }

    /// <summary>Points the Android player settings at the release key for one build. Returns what to restore afterwards.</summary>
    public static Snapshot Apply()
    {
        var snapshot = new Snapshot(PlayerSettings.Android.useCustomKeystore, PlayerSettings.Android.keystoreName, PlayerSettings.Android.keyaliasName);

        PlayerSettings.Android.useCustomKeystore = true;
        PlayerSettings.Android.keystoreName = KeystorePath;
        PlayerSettings.Android.keystorePass = Password;
        PlayerSettings.Android.keyaliasName = Alias;
        PlayerSettings.Android.keyaliasPass = Password;

        return snapshot;
    }

    /// <summary>Puts the settings back, so the machine-specific keystore path never ends up committed in ProjectSettings.</summary>
    public static void Restore(Snapshot snapshot)
    {
        PlayerSettings.Android.useCustomKeystore = snapshot.UseCustomKeystore;
        PlayerSettings.Android.keystoreName = snapshot.KeystoreName;
        PlayerSettings.Android.keyaliasName = snapshot.KeyaliasName;
        PlayerSettings.Android.keystorePass = string.Empty;
        PlayerSettings.Android.keyaliasPass = string.Empty;
    }

    private static string FirstLine(string text)
    {
        using var reader = new StringReader(text ?? string.Empty);
        return reader.ReadLine() ?? string.Empty;
    }
}
