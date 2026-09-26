using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Lets a tool outside Unity start a build in the open editor, which a batchmode build cannot do while the
/// project is open. It uses this editor session's keystore password, so the password never leaves Unity.
/// </summary>
/// <remarks>
/// Write <see cref="RequestFile"/> (JSON, every field optional) to start a build, then read <see cref="StatusFile"/>,
/// whose state goes running, then succeeded or failed. Both live in Temp, which git ignores and Unity clears on close.
/// The request overrides the saved config for that run only, the config asset is not changed.
/// </remarks>
[InitializeOnLoad]
internal static class BuildRequestWatcher
{
    [Serializable]
    private class Request
    {
        public bool upload = true;
        // Empty keeps the config's value
        public string releaseType = string.Empty;
        public string replaceExistingRelease = string.Empty;
    }

    [Serializable]
    private class Status
    {
        public string state;
        public string version;
        public string requestedAt;
        public string finishedAt;
        public string log;
    }

    private const double PollSeconds = 1;

    private static string Folder => Path.Combine(BuildProcess.ProjectFolder, "Temp", "ElectroGridBuild");
    public static string RequestFile => Path.Combine(Folder, "request.json");
    public static string StatusFile => Path.Combine(Folder, "status.json");

    private static double _nextPoll;
    private static bool _queued;

    static BuildRequestWatcher()
    {
        EditorApplication.update += Poll;
    }

    private static void Poll()
    {
        if (EditorApplication.timeSinceStartup < _nextPoll) return;
        _nextPoll = EditorApplication.timeSinceStartup + PollSeconds;

        if (!File.Exists(RequestFile)) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;

        // Unity only notices changed files when its window gets focus, so a request made while it sits in the
        // background would build stale code. The request stays until the scripts it may need have reloaded
        AssetDatabase.Refresh();
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;

        if (_queued) return;
        _queued = true;

        // Deferred so the build does not run inside the update callback that found the request. The file is only
        // consumed there, so a script reload in between drops the queued call but not the request
        EditorApplication.delayCall += RunRequest;
    }

    private static void RunRequest()
    {
        _queued = false;
        if (!File.Exists(RequestFile) || EditorApplication.isCompiling) return;

        string json = File.ReadAllText(RequestFile);
        File.Delete(RequestFile);

        var status = new Status { state = "running", version = ElectroGridBuild.Version, requestedAt = DateTime.Now.ToString("s") };
        WriteStatus(status);

        var log = new StringBuilder();
        bool succeeded = false;
        SOBuildConfig config = null;

        try
        {
            var request = string.IsNullOrWhiteSpace(json) ? new Request() : JsonUtility.FromJson<Request>(json) ?? new Request();

            config = UnityEngine.Object.Instantiate(SOBuildConfig.LoadOrCreate());
            if (Enum.TryParse(request.releaseType, true, out SOBuildConfig.ReleaseType releaseType)) config.githubReleaseType = releaseType;
            if (bool.TryParse(request.replaceExistingRelease, out bool replace)) config.replaceExistingRelease = replace;

            log.AppendLine($"Version {ElectroGridBuild.Version}, upload {request.upload}, GitHub {config.uploadToGitHub} as {config.githubReleaseType}, replace {config.replaceExistingRelease}, copy {config.copyToFolder}");

            var issues = ElectroGridBuild.Preflight(config, request.upload);
            foreach (var issue in issues) log.AppendLine($"{issue.Severity}: {issue.Message}");

            if (issues.All(issue => issue.Severity != ElectroGridBuild.Severity.Error))
            {
                var result = ElectroGridBuild.Run(config, request.upload);
                foreach (var step in result.Steps)
                {
                    log.AppendLine($"{(step.Succeeded ? "OK" : "FAILED")} {step.Name} ({step.Seconds:0.0}s): {step.Message}");
                }

                succeeded = result.Succeeded;
            }
        }
        catch (Exception exception)
        {
            log.AppendLine($"Exception: {exception}");
        }
        finally
        {
            if (config) UnityEngine.Object.DestroyImmediate(config);
        }

        status.state = succeeded ? "succeeded" : "failed";
        status.finishedAt = DateTime.Now.ToString("s");
        status.log = log.ToString();
        WriteStatus(status);
        Debug.Log($"[Build] Build requested from outside the editor {status.state}.\n{status.log}");
    }

    private static void WriteStatus(Status status)
    {
        Directory.CreateDirectory(Folder);
        File.WriteAllText(StatusFile, JsonUtility.ToJson(status, true));
    }
}
