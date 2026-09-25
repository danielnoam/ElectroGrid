using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
/// Builds every enabled target in <see cref="SOBuildConfig"/> one after another, packages the results and uploads
/// them. The window is a front end for this; <see cref="BuildFromCommandLine"/> runs the same thing headless.
/// </summary>
internal static class ElectroGridBuild
{
    private const string ExecutableName = "ElectroGrid";

    public enum Severity
    {
        Error,
        Warning
    }

    public readonly struct Issue
    {
        public readonly Severity Severity;
        public readonly string Message;

        public Issue(Severity severity, string message)
        {
            Severity = severity;
            Message = message;
        }
    }

    public class StepResult
    {
        public string Name;
        public bool Succeeded;
        public string Message;
        public string ArtifactPath;
        public double Seconds;
    }

    public class RunResult
    {
        public string Version;
        public string OutputFolder;
        public readonly List<StepResult> Steps = new List<StepResult>();
        public bool Succeeded => Steps.Count > 0 && Steps.All(step => step.Succeeded);
    }

    public static string Version => PlayerSettings.bundleVersion;

    public enum VersionPart
    {
        Major,
        Minor,
        Patch
    }

    public static bool TryParseVersion(string version, out int major, out int minor, out int patch)
    {
        major = minor = patch = 0;
        var parts = (version ?? string.Empty).Split('.');
        return parts.Length == 3
               && int.TryParse(parts[0], out major)
               && int.TryParse(parts[1], out minor)
               && int.TryParse(parts[2], out patch)
               && major >= 0 && minor is >= 0 and < 100 && patch is >= 0 and < 100;
    }

    /// <summary>
    /// Derived from the version rather than counted, 1.2.3 becomes 10203, so it always rises with the version and
    /// never needs remembering or committing separately. Minor and patch are capped at 99 to keep that ordering.
    /// </summary>
    public static int AndroidVersionCode(string version)
    {
        return TryParseVersion(version, out int major, out int minor, out int patch) ? major * 10000 + minor * 100 + patch : 0;
    }

    public static void BumpVersion(VersionPart part)
    {
        if (!TryParseVersion(Version, out int major, out int minor, out int patch)) return;

        switch (part)
        {
            case VersionPart.Major: major++; minor = 0; patch = 0; break;
            case VersionPart.Minor: minor++; patch = 0; break;
            case VersionPart.Patch: patch++; break;
        }

        SetVersion($"{major}.{minor}.{patch}");
    }

    public static void SetVersion(string version)
    {
        if (!TryParseVersion(version, out _, out _, out _)) return;

        PlayerSettings.bundleVersion = version;
        PlayerSettings.Android.bundleVersionCode = AndroidVersionCode(version);
        AssetDatabase.SaveAssets();
    }

    /// <summary>Everything that would make a run fail or produce the wrong thing, found before anything is built.</summary>
    public static List<Issue> Preflight(SOBuildConfig config, bool includeUploads)
    {
        var issues = new List<Issue>();
        var enabled = config.targets.Where(target => target.enabled).ToList();

        if (!TryParseVersion(Version, out _, out _, out _))
        {
            issues.Add(new Issue(Severity.Error, $"Version '{Version}' is not major.minor.patch with minor and patch under 100."));
        }

        if (enabled.Count == 0) issues.Add(new Issue(Severity.Error, "No targets are enabled."));

        foreach (var target in enabled)
        {
            if (!target.profile) issues.Add(new Issue(Severity.Error, $"{target.kind} has no Build Profile assigned."));
        }

        AddSigningIssues(issues, enabled);

        if (config.requireValidLevels) AddLevelIssues(issues);

        if (!includeUploads) return issues;

        if (config.uploadToGitHub) AddGitHubIssues(issues, config);

        if (config.copyToFolder && string.IsNullOrWhiteSpace(BuildMachineSettings.CopyFolder))
        {
            issues.Add(new Issue(Severity.Error, "Copy To Folder is on but no folder is set."));
        }

        return issues;
    }

    /// <summary>
    /// A release APK signed with any other key can never be installed over the previous one, so releases refuse
    /// to build without the release key. Development builds fall back to the debug key with a warning.
    /// </summary>
    private static void AddSigningIssues(List<Issue> issues, List<SOBuildConfig.Target> enabled)
    {
        var android = enabled.Where(target => target.kind == SOBuildConfig.TargetKind.AndroidApk).ToList();
        if (android.Count == 0) return;

        if (AndroidSigning.Verify(out string message)) return;

        if (android.Any(target => !target.developmentBuild))
        {
            issues.Add(new Issue(Severity.Error, $"Android release builds need the release keystore. {message}"));
        }
        else
        {
            issues.Add(new Issue(Severity.Warning, $"Development APK will use the debug key and cannot be installed over a release. {message}"));
        }
    }

    private static void AddLevelIssues(List<Issue> issues)
    {
        foreach (var level in Match3LevelRegistry.GetLevels())
        {
            if (!level)
            {
                issues.Add(new Issue(Severity.Error, "The play order has an empty slot."));
                continue;
            }

            if (Match3LevelValidation.WorstSeverity(Match3LevelValidation.Validate(level)) == Match3LevelValidation.Severity.Error)
            {
                issues.Add(new Issue(Severity.Error, $"Level {level.name} fails validation. Open it in the Level Editor."));
            }
        }
    }

    private static void AddGitHubIssues(List<Issue> issues, SOBuildConfig config)
    {
        var version = BuildProcess.Gh("--version");
        if (!version.Succeeded)
        {
            issues.Add(new Issue(Severity.Error, "GitHub CLI (gh) not found. Install it with: winget install --id GitHub.cli"));
            return;
        }

        var auth = BuildProcess.Gh("auth status");
        if (!auth.Succeeded)
        {
            issues.Add(new Issue(Severity.Error, "GitHub CLI is not signed in. Run: gh auth login"));
            return;
        }

        var existing = FindExistingRelease();
        if (existing.HasValue)
        {
            string state = existing.Value ? "draft" : "published";
            issues.Add(config.replaceExistingRelease
                ? new Issue(Severity.Warning, $"Replacing the files of the existing {state} release {ReleaseTag}. Players already on {Version} will not be offered the new build.")
                : new Issue(Severity.Error, $"A {state} GitHub release {ReleaseTag} already exists. Bump the version, or turn on Replace Existing Release."));
        }

        // The release tag is created on GitHub at this commit, so it has to be there already
        BuildProcess.Git("fetch --quiet", 120);
        var ahead = BuildProcess.Git("rev-list --count @{u}..HEAD");
        if (!ahead.Succeeded)
        {
            issues.Add(new Issue(Severity.Error, $"Could not compare with the remote branch: {ahead.Message}"));
        }
        else if (int.TryParse(ahead.Output.Trim(), out int count) && count > 0)
        {
            issues.Add(new Issue(Severity.Error, $"{count} local commit(s) are not pushed. Push before releasing, the tag is created on GitHub."));
        }
    }

    private static string ReleaseTag => "v" + Version;

    public static RunResult Run(SOBuildConfig config, bool upload)
    {
        var result = new RunResult
        {
            Version = Version,
            OutputFolder = Path.Combine(BuildMachineSettings.ResolveOutputRoot(config), Version)
        };

        PlayerSettings.Android.bundleVersionCode = AndroidVersionCode(Version);

        var previousProfile = BuildProfile.GetActiveBuildProfile();
        var previousTarget = EditorUserBuildSettings.activeBuildTarget;

        // Whichever platform is already active goes first, which saves one asset reimport
        var targets = config.targets
            .Where(target => target.enabled && target.profile)
            .OrderBy(target => ToBuildTarget(target.kind) == previousTarget ? 0 : 1)
            .ToList();

        var artifacts = new List<string>();

        try
        {
            for (int i = 0; i < targets.Count; i++)
            {
                var target = targets[i];
                EditorUtility.DisplayProgressBar("ElectroGrid Build", $"Building {target.kind} ({i + 1}/{targets.Count})", (float)i / targets.Count);

                var step = BuildOne(config, target, result.OutputFolder);
                result.Steps.Add(step);
                if (step.Succeeded && !string.IsNullOrEmpty(step.ArtifactPath)) artifacts.Add(step.ArtifactPath);
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            RestorePlatform(previousProfile, previousTarget);
        }

        bool allBuilt = result.Steps.Count > 0 && result.Steps.All(step => step.Succeeded);

        if (upload && allBuilt && artifacts.Count > 0)
        {
            if (config.copyToFolder) result.Steps.Add(CopyToFolder(config, artifacts));
            if (config.uploadToGitHub) result.Steps.Add(UploadToGitHub(config, artifacts));
        }
        else if (upload && !allBuilt && (config.copyToFolder || config.uploadToGitHub))
        {
            result.Steps.Add(new StepResult { Name = "Upload", Succeeded = false, Message = "Skipped, not every target built." });
        }

        LogSummary(result);
        return result;
    }

    private static BuildTarget ToBuildTarget(SOBuildConfig.TargetKind kind)
    {
        return kind == SOBuildConfig.TargetKind.AndroidApk ? BuildTarget.Android : BuildTarget.StandaloneWindows64;
    }

    private static StepResult BuildOne(SOBuildConfig config, SOBuildConfig.Target target, string outputFolder)
    {
        var step = new StepResult { Name = $"Build {target.kind}{(target.developmentBuild ? " (development)" : "")}" };
        var timer = Stopwatch.StartNew();
        AndroidSigning.Snapshot? signing = null;

        try
        {
            string playerPath;
            string platformFolder;

            if (target.kind == SOBuildConfig.TargetKind.AndroidApk)
            {
                platformFolder = Path.Combine(outputFolder, "Android");
                playerPath = Path.Combine(platformFolder, $"{ExecutableName}-{Version}.apk");
                EditorUserBuildSettings.buildAppBundle = false;

                // Release builds were already refused without the key; a development build uses it when it is available
                if (AndroidSigning.Verify(out _)) signing = AndroidSigning.Apply();
            }
            else
            {
                platformFolder = Path.Combine(outputFolder, "Windows");
                playerPath = Path.Combine(platformFolder, ExecutableName, ExecutableName + ".exe");
            }

            // A rebuild of the same version must not leave files from the previous one behind
            if (Directory.Exists(platformFolder)) Directory.Delete(platformFolder, true);
            Directory.CreateDirectory(Path.GetDirectoryName(playerPath) ?? platformFolder);

            var options = new BuildPlayerWithProfileOptions
            {
                buildProfile = target.profile,
                locationPathName = playerPath,
                options = target.developmentBuild ? BuildOptions.Development : BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;

            if (summary.result != BuildResult.Succeeded)
            {
                step.Succeeded = false;
                step.Message = $"{summary.result}, {summary.totalErrors} error(s). See the Console.";
                return step;
            }

            step.ArtifactPath = playerPath;
            DeleteDoNotShipFolders(platformFolder);

            if (target.kind == SOBuildConfig.TargetKind.Windows && config.zipWindowsBuild)
            {
                step.ArtifactPath = ZipWindowsBuild(Path.GetDirectoryName(playerPath), outputFolder);

                // The zip is what gets uploaded and copied, the loose folder would only be a second copy
                Directory.Delete(platformFolder, true);
            }

            step.Succeeded = true;
            step.Message = $"{FormatSize(SizeOf(step.ArtifactPath))}{(signing.HasValue ? ", release-signed" : "")} → {step.ArtifactPath}";
            return step;
        }
        catch (Exception e)
        {
            step.Succeeded = false;
            step.Message = e.Message;
            Debug.LogException(e);
            return step;
        }
        finally
        {
            if (signing.HasValue) AndroidSigning.Restore(signing.Value);
            step.Seconds = timer.Elapsed.TotalSeconds;
        }
    }

    private static string ZipWindowsBuild(string playerFolder, string outputFolder)
    {
        string zipPath = Path.Combine(outputFolder, $"{ExecutableName}-{Version}-Windows.zip");
        if (File.Exists(zipPath)) File.Delete(zipPath);

        using var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create);

        foreach (string file in Directory.GetFiles(playerFolder, "*", SearchOption.AllDirectories))
        {
            string relative = Path.GetRelativePath(playerFolder, file).Replace('\\', '/');
            archive.CreateEntryFromFile(file, $"{ExecutableName}/{relative}", System.IO.Compression.CompressionLevel.Optimal);
        }

        return zipPath;
    }

    /// <summary>
    /// Burst's debug symbols, which Unity writes next to the player and names so they are not shipped. They only
    /// help decode a native crash's call stack, so they are removed rather than left to be copied or uploaded.
    /// </summary>
    private static void DeleteDoNotShipFolders(string folder)
    {
        if (!Directory.Exists(folder)) return;

        foreach (string directory in Directory.GetDirectories(folder, "*_DoNotShip", SearchOption.AllDirectories))
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    private static void RestorePlatform(BuildProfile previousProfile, BuildTarget previousTarget)
    {
        if (previousProfile)
        {
            if (BuildProfile.GetActiveBuildProfile() != previousProfile) BuildProfile.SetActiveBuildProfile(previousProfile);
            return;
        }

        if (EditorUserBuildSettings.activeBuildTarget == previousTarget) return;

        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildPipeline.GetBuildTargetGroup(previousTarget), previousTarget);
    }

    private static StepResult CopyToFolder(SOBuildConfig config, List<string> artifacts)
    {
        var step = new StepResult { Name = "Copy To Folder" };
        var timer = Stopwatch.StartNew();

        try
        {
            string destination = Path.Combine(BuildMachineSettings.CopyFolder, Version);
            Directory.CreateDirectory(destination);

            foreach (string artifact in artifacts)
            {
                File.Copy(artifact, Path.Combine(destination, Path.GetFileName(artifact)), true);
            }

            step.Succeeded = true;
            step.Message = $"{artifacts.Count} file(s) → {destination}";
        }
        catch (Exception e)
        {
            step.Succeeded = false;
            step.Message = e.Message;
        }

        step.Seconds = timer.Elapsed.TotalSeconds;
        return step;
    }

    private static StepResult UploadToGitHub(SOBuildConfig config, List<string> artifacts)
    {
        var step = new StepResult { Name = "GitHub Release" };
        var timer = Stopwatch.StartNew();

        string notesPath = Path.Combine(Path.GetTempPath(), $"ElectroGrid-{Version}-notes.md");
        File.WriteAllText(notesPath, BuildReleaseNotes());

        var head = BuildProcess.Git("rev-parse HEAD");
        string commit = head.Succeeded ? head.Output.Trim() : string.Empty;
        var existing = FindExistingRelease();

        EditorUtility.DisplayProgressBar("ElectroGrid Build", $"Uploading {ReleaseTag} to GitHub", 1f);
        var release = existing.HasValue && config.replaceExistingRelease
            ? ReplaceRelease(existing.Value, artifacts, notesPath, commit)
            : CreateRelease(config, artifacts, notesPath, commit);
        EditorUtility.ClearProgressBar();

        step.Name = existing.HasValue ? "GitHub Release (replaced)" : "GitHub Release";
        step.Succeeded = release.Succeeded;
        step.Message = release.Succeeded ? release.Output.Trim() : release.Message;
        step.Seconds = timer.Elapsed.TotalSeconds;
        return step;
    }

    /// <summary>Whether a release already exists for this version: null if not, otherwise whether it is a draft.</summary>
    private static bool? FindExistingRelease()
    {
        var view = BuildProcess.Gh($"release view {ReleaseTag} --json isDraft --jq .isDraft");
        if (!view.Succeeded) return null;

        return view.Output.Trim() == "true";
    }

    private static BuildProcess.Result CreateRelease(SOBuildConfig config, List<string> artifacts, string notesPath, string commit)
    {
        var arguments = new StringBuilder($"release create {ReleaseTag}");
        foreach (string artifact in artifacts) arguments.Append(' ').Append(BuildProcess.Quote(artifact));
        arguments.Append($" --title {BuildProcess.Quote($"ElectroGrid {Version}")}");
        arguments.Append($" --notes-file {BuildProcess.Quote(notesPath)}");
        if (!string.IsNullOrEmpty(commit)) arguments.Append($" --target {commit}");
        if (config.githubReleaseType == SOBuildConfig.ReleaseType.Draft) arguments.Append(" --draft");
        if (config.githubReleaseType == SOBuildConfig.ReleaseType.Prerelease) arguments.Append(" --prerelease");

        return BuildProcess.Gh(arguments.ToString(), 30 * 60);
    }

    /// <summary>
    /// Swaps the files and notes of an existing release and points it at the commit just built, leaving it draft or
    /// published as it was. A draft has no git tag until it is published, only a target, so the two are moved differently.
    /// </summary>
    private static BuildProcess.Result ReplaceRelease(bool isDraft, List<string> artifacts, string notesPath, string commit)
    {
        if (!string.IsNullOrEmpty(commit))
        {
            var retarget = isDraft
                ? BuildProcess.Gh($"release edit {ReleaseTag} --target {commit}")
                : BuildProcess.Gh($"api -X PATCH repos/{{owner}}/{{repo}}/git/refs/tags/{ReleaseTag} -f sha={commit} -F force=true");
            if (!retarget.Succeeded) return retarget;

            // Keeps the local tag in step, so the next release's notes start from the right commit
            if (!isDraft) BuildProcess.Git("fetch --tags --force --quiet", 120);
        }

        var upload = new StringBuilder($"release upload {ReleaseTag} --clobber");
        foreach (string artifact in artifacts) upload.Append(' ').Append(BuildProcess.Quote(artifact));
        var uploaded = BuildProcess.Gh(upload.ToString(), 30 * 60);
        if (!uploaded.Succeeded) return uploaded;

        var edit = BuildProcess.Gh($"release edit {ReleaseTag} --title {BuildProcess.Quote($"ElectroGrid {Version}")} --notes-file {BuildProcess.Quote(notesPath)}");
        if (!edit.Succeeded) return edit;

        var url = BuildProcess.Gh($"release view {ReleaseTag} --json url --jq .url");
        return new BuildProcess.Result(0, url.Succeeded ? url.Output : $"Replaced {ReleaseTag}", string.Empty);
    }

    /// <summary>
    /// Commit subjects since the previous release tag. GitHub's generated notes list pull requests, and this
    /// project commits straight to main, so they would come out empty.
    /// </summary>
    public static string BuildReleaseNotes()
    {
        // The tag being released or replaced is skipped, so the notes start from the release before it
        var previousTag = BuildProcess.Git($"describe --tags --abbrev=0 --exclude {ReleaseTag}");
        string range = previousTag.Succeeded ? $"{previousTag.Output.Trim()}..HEAD" : "-n 30 HEAD";

        var log = BuildProcess.Git($"log --no-merges --pretty=format:\"- %s\" {range}");

        var notes = new StringBuilder();
        notes.AppendLine(previousTag.Succeeded ? $"Changes since {previousTag.Output.Trim()}:" : "Recent changes:");
        notes.AppendLine();
        notes.AppendLine(log.Succeeded && !string.IsNullOrWhiteSpace(log.Output) ? log.Output.Trim() : "- No commit messages found.");
        return notes.ToString();
    }

    /// <summary>
    /// Headless entry point: Unity.exe -batchmode -quit -projectPath &lt;path&gt; -executeMethod ElectroGridBuild.BuildFromCommandLine
    /// Uses the targets and uploads saved in the config. Add -noUpload to only build. Exits non-zero on any failure.
    /// </summary>
    public static void BuildFromCommandLine()
    {
        var config = SOBuildConfig.LoadOrCreate();
        bool upload = !Environment.GetCommandLineArgs().Contains("-noUpload");

        var issues = Preflight(config, upload);
        foreach (var issue in issues)
        {
            if (issue.Severity == Severity.Error) Debug.LogError($"[Build] {issue.Message}");
            else Debug.LogWarning($"[Build] {issue.Message}");
        }

        if (issues.Any(issue => issue.Severity == Severity.Error))
        {
            EditorApplication.Exit(1);
            return;
        }

        var result = Run(config, upload);
        EditorApplication.Exit(result.Succeeded ? 0 : 1);
    }

    private static void LogSummary(RunResult result)
    {
        var summary = new StringBuilder($"[Build] ElectroGrid {result.Version}: {(result.Succeeded ? "succeeded" : "FAILED")}\n");
        foreach (var step in result.Steps)
        {
            summary.AppendLine($"  {(step.Succeeded ? "✓" : "✗")} {step.Name} ({step.Seconds:0.0}s): {step.Message}");
        }

        if (result.Succeeded) Debug.Log(summary.ToString());
        else Debug.LogError(summary.ToString());
    }

    private static long SizeOf(string path) => File.Exists(path) ? new FileInfo(path).Length : 0;

    public static string FormatSize(long bytes)
    {
        return bytes >= 1024 * 1024 ? $"{bytes / (1024f * 1024f):0.0} MB" : $"{bytes / 1024f:0} KB";
    }
}
