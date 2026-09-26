using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using UnityEditor;

/// <summary>
/// The release keystore every Android release build is signed with. An installed APK only accepts an update
/// signed with the same key, so this key is what lets a newer build replace an older one.
/// </summary>
/// <remarks>
/// The keystore lives outside the repository, and the password never goes near it: it is held for the current
/// editor session, saved encrypted for this Windows user when <see cref="RememberPassword"/> is on, or read from
/// <see cref="PasswordEnvironmentVariable"/> for headless builds.
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

    /// <summary>
    /// Kept in SessionState, which survives script reloads but is cleared when the editor closes. With
    /// <see cref="RememberPassword"/> it is also saved for this Windows user, encrypted.
    /// </summary>
    public static string Password
    {
        get
        {
            string session = SessionState.GetString(PasswordSessionKey, string.Empty);
            if (!string.IsNullOrEmpty(session)) return session;

            string remembered = RememberedPassword;
            if (!string.IsNullOrEmpty(remembered))
            {
                SessionState.SetString(PasswordSessionKey, remembered);
                return remembered;
            }

            return Environment.GetEnvironmentVariable(PasswordEnvironmentVariable) ?? string.Empty;
        }
        set
        {
            SessionState.SetString(PasswordSessionKey, value ?? string.Empty);
            if (RememberPassword) RememberedPassword = value;
        }
    }

    /// <summary>
    /// Saves the password in EditorPrefs, encrypted with Windows DPAPI for the current Windows user, so only this
    /// user on this computer can read it back. It never touches the project folder.
    /// </summary>
    public static bool RememberPassword
    {
        get => EditorPrefs.HasKey(RememberedPasswordKey);
        set
        {
            if (value == RememberPassword) return;

            if (value) RememberedPassword = SessionState.GetString(PasswordSessionKey, string.Empty);
            else EditorPrefs.DeleteKey(RememberedPasswordKey);
        }
    }

    private const string RememberedPasswordKey = "ElectroGrid.Build.KeystorePasswordProtected";

    private static string RememberedPassword
    {
        get
        {
            string stored = EditorPrefs.GetString(RememberedPasswordKey, string.Empty);
            if (string.IsNullOrEmpty(stored)) return string.Empty;

            try
            {
                return Encoding.UTF8.GetString(WindowsDataProtection.Unprotect(Convert.FromBase64String(stored)));
            }
            catch (Exception)
            {
                // Saved by another Windows user or a reinstalled system, it cannot be decrypted here
                return string.Empty;
            }
        }
        set => EditorPrefs.SetString(RememberedPasswordKey, Convert.ToBase64String(WindowsDataProtection.Protect(Encoding.UTF8.GetBytes(value ?? string.Empty))));
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

/// <summary>Windows DPAPI for the current user, called directly since Unity's .NET profile has no ProtectedData.</summary>
internal static class WindowsDataProtection
{
    [StructLayout(LayoutKind.Sequential)]
    private struct DataBlob
    {
        public int Size;
        public IntPtr Data;
    }

    private const int UiForbidden = 0x1;

    [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CryptProtectData(ref DataBlob input, string description, IntPtr entropy, IntPtr reserved, IntPtr prompt, int flags, out DataBlob output);

    [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CryptUnprotectData(ref DataBlob input, IntPtr description, IntPtr entropy, IntPtr reserved, IntPtr prompt, int flags, out DataBlob output);

    [DllImport("kernel32.dll")]
    private static extern IntPtr LocalFree(IntPtr memory);

    public static byte[] Protect(byte[] data) => Transform(data, true);
    public static byte[] Unprotect(byte[] data) => Transform(data, false);

    private static byte[] Transform(byte[] data, bool protect)
    {
        var input = new DataBlob { Size = data.Length, Data = Marshal.AllocHGlobal(Math.Max(1, data.Length)) };
        var output = new DataBlob();

        try
        {
            Marshal.Copy(data, 0, input.Data, data.Length);

            bool ok = protect
                ? CryptProtectData(ref input, "ElectroGrid keystore", IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, UiForbidden, out output)
                : CryptUnprotectData(ref input, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, UiForbidden, out output);
            if (!ok) throw new InvalidOperationException($"DPAPI failed with error {Marshal.GetLastWin32Error()}");

            var result = new byte[output.Size];
            Marshal.Copy(output.Data, result, 0, output.Size);
            return result;
        }
        finally
        {
            Marshal.FreeHGlobal(input.Data);
            if (output.Data != IntPtr.Zero) LocalFree(output.Data);
        }
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
