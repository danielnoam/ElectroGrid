using System;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Appends uncaught exceptions to crash.log next to the save on Windows builds, where Crashlytics does not report.
/// Unlike Player.log it survives relaunches, so a player can send it in after the fact. The file is only created
/// once something goes wrong, rotates to crash.old.log once it grows past MaxFileBytes, and stops taking entries
/// after MaxEntriesPerSession so an exception thrown every frame cannot fill the disk.
/// </summary>
internal static class CrashLog
{
    private const string FileName = "crash.log";
    private const string OldFileName = "crash.old.log";
    private const long MaxFileBytes = 512 * 1024;
    private const int MaxEntriesPerSession = 100;

    private static readonly object WriteLock = new object();
    private static string _path;
    private static string _sessionHeader;
    private static bool _headerWritten;
    private static int _entries;
    private static volatile string _scene = "(no scene)";

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    private static void Bootstrap() => Initialize(Application.persistentDataPath);
#endif

    private static void Initialize(string directory)
    {
        _path = Path.Combine(directory, FileName);
        RotateIfLarge(Path.Combine(directory, OldFileName));

        // Read on the main thread now, since exceptions can arrive on any thread and these APIs are main thread only
        _sessionHeader = $"=== ElectroGrid {Application.version} | session started {DateTime.Now:yyyy-MM-dd HH:mm:ss} | " +
                         $"{SystemInfo.operatingSystem} | {SystemInfo.graphicsDeviceName} | {SystemInfo.systemMemorySize} MB RAM";

        SceneManager.activeSceneChanged += (_, scene) => _scene = scene.name;
        Application.logMessageReceivedThreaded += OnLogMessage;
    }

    private static void RotateIfLarge(string oldPath)
    {
        try
        {
            if (!File.Exists(_path) || new FileInfo(_path).Length <= MaxFileBytes) return;

            if (File.Exists(oldPath)) File.Delete(oldPath);
            File.Move(_path, oldPath);
        }
        catch (Exception)
        {
            // A locked or read-only file just means the log keeps growing, which is not worth failing startup over
        }
    }

    private static void OnLogMessage(string condition, string stackTrace, LogType type)
    {
        if (type != LogType.Exception && type != LogType.Assert) return;

        lock (WriteLock)
        {
            if (_entries >= MaxEntriesPerSession) return;
            _entries++;

            var entry = new StringBuilder();
            if (!_headerWritten)
            {
                entry.AppendLine().AppendLine(_sessionHeader);
                _headerWritten = true;
            }

            entry.Append($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {_scene} | {type}: ").AppendLine(condition);
            if (!string.IsNullOrEmpty(stackTrace)) entry.AppendLine(stackTrace.TrimEnd());
            if (_entries == MaxEntriesPerSession) entry.AppendLine($"(Stopped logging this session after {MaxEntriesPerSession} entries.)");

            try
            {
                File.AppendAllText(_path, entry.ToString());
            }
            catch (Exception)
            {
                // Nothing safe to do here: logging the failure would come straight back into this handler
            }
        }
    }
}
