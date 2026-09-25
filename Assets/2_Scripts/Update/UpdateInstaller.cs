using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using UnityEngine;

/// <summary>
/// Hands a downloaded build over to be installed. Android passes the APK to the system installer, which shows its
/// own confirmation. Windows cannot overwrite the running executable, so a small script waits for the game to exit,
/// copies the new files over the install folder and relaunches it.
/// </summary>
public static class UpdateInstaller
{
    public enum Outcome
    {
        Started,
        NeedsPermission,
        Failed
    }

    public readonly struct Result
    {
        public readonly Outcome Outcome;
        public readonly string Message;

        private Result(Outcome outcome, string message)
        {
            Outcome = outcome;
            Message = message;
        }

        public static Result Started(string message) => new Result(Outcome.Started, message);
        public static Result NeedsPermission(string message) => new Result(Outcome.NeedsPermission, message);
        public static Result Failed(string message) => new Result(Outcome.Failed, message);
    }

    public static Result Install(string path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return Result.Failed("The downloaded update is missing.");

#if UNITY_ANDROID && !UNITY_EDITOR
        return InstallAndroid(path);
#elif UNITY_STANDALONE_WIN && !UNITY_EDITOR
        return InstallWindows(path);
#else
        return Result.Failed("Updates install from a build, not the editor.");
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    /// <summary>Must match the provider authority in ElectroGridUpdater.androidlib/AndroidManifest.xml.</summary>
    private static string ProviderAuthority => Application.identifier + ".electrogrid.updates";

    private static Result InstallAndroid(string path)
    {
        try
        {
            using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            using var packageManager = activity.Call<AndroidJavaObject>("getPackageManager");

            // Android asks once per app whether it may install other apps; the player has to allow it in settings
            if (!packageManager.Call<bool>("canRequestPackageInstalls"))
            {
                using var uriClass = new AndroidJavaClass("android.net.Uri");
                using var packageUri = uriClass.CallStatic<AndroidJavaObject>("parse", "package:" + Application.identifier);
                using var settings = new AndroidJavaObject("android.content.Intent", "android.settings.MANAGE_UNKNOWN_APP_SOURCES", packageUri);
                activity.Call("startActivity", settings);
                return Result.NeedsPermission("Allow ElectroGrid to install apps, then come back and tap Install.");
            }

            using var context = activity.Call<AndroidJavaObject>("getApplicationContext");
            using var file = new AndroidJavaObject("java.io.File", path);
            using var fileProvider = new AndroidJavaClass("androidx.core.content.FileProvider");
            using var contentUri = fileProvider.CallStatic<AndroidJavaObject>("getUriForFile", context, ProviderAuthority, file);

            const int grantReadUriPermission = 0x00000001;
            const int newTask = 0x10000000;

            using var intent = new AndroidJavaObject("android.content.Intent", "android.intent.action.VIEW");
            intent.Call<AndroidJavaObject>("setDataAndType", contentUri, "application/vnd.android.package-archive").Dispose();
            intent.Call<AndroidJavaObject>("addFlags", grantReadUriPermission | newTask).Dispose();
            activity.Call("startActivity", intent);

            return Result.Started("Follow the installer to finish the update.");
        }
        catch (Exception e)
        {
            return Result.Failed($"Could not open the installer: {e.Message}");
        }
    }
#endif

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
    private static Result InstallWindows(string zipPath)
    {
        try
        {
            string executable = Process.GetCurrentProcess().MainModule?.FileName;
            string installFolder = Path.GetDirectoryName(executable);
            if (string.IsNullOrEmpty(executable) || string.IsNullOrEmpty(installFolder)) return Result.Failed("Could not find the game's install folder.");

            if (!CanWriteTo(installFolder))
            {
                return Result.Failed("The game's folder is read-only, for example under Program Files. Move the game to another folder, or update from the release page.");
            }

            string updatesFolder = Path.GetDirectoryName(zipPath) ?? Application.persistentDataPath;
            string staging = Path.Combine(updatesFolder, "staging");
            if (Directory.Exists(staging)) Directory.Delete(staging, true);
            ZipFile.ExtractToDirectory(zipPath, staging);

            // The build window zips the player under an ElectroGrid folder
            string source = Directory.Exists(Path.Combine(staging, "ElectroGrid")) ? Path.Combine(staging, "ElectroGrid") : staging;

            string script = Path.Combine(updatesFolder, "apply-update.cmd");
            File.WriteAllText(script, BuildScript(Process.GetCurrentProcess().Id, source, installFolder, executable, staging));

            Process.Start(new ProcessStartInfo("cmd.exe", $"/c \"{script}\"")
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden
            });

            Application.Quit();
            return Result.Started("Restarting to finish the update.");
        }
        catch (Exception e)
        {
            return Result.Failed($"Could not apply the update: {e.Message}");
        }
    }

    /// <summary>Waits for the game to exit, copies the new build over it, relaunches it and cleans up after itself.</summary>
    private static string BuildScript(int processId, string source, string installFolder, string executable, string staging)
    {
        return "@echo off\r\n" +
               ":wait\r\n" +
               $"tasklist /FI \"PID eq {processId}\" 2>NUL | find \"{processId}\" >NUL && (timeout /t 1 /nobreak >NUL & goto wait)\r\n" +
               $"robocopy \"{source}\" \"{installFolder}\" /E /R:5 /W:1 /NFL /NDL /NJH /NJS >NUL\r\n" +
               $"start \"\" \"{executable}\"\r\n" +
               $"rmdir /S /Q \"{staging}\"\r\n" +
               "del \"%~f0\"\r\n";
    }

    private static bool CanWriteTo(string folder)
    {
        try
        {
            string probe = Path.Combine(folder, $".update-probe-{Guid.NewGuid():N}");
            File.WriteAllText(probe, string.Empty);
            File.Delete(probe);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
#endif
}
