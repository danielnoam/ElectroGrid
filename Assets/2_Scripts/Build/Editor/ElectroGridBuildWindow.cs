using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEngine;

internal class ElectroGridBuildWindow : EditorWindow
{
    private SOBuildConfig _config;
    private SerializedObject _serializedConfig;
    private Vector2 _scroll;
    private List<ElectroGridBuild.Issue> _issues;
    private ElectroGridBuild.RunResult _lastResult;
    private string _ghStatus;
    private bool _ghReady;
    private string _notesPreview;
    private bool _notesFoldout;
    private string _signingStatus;
    private bool _signingOk;

    [MenuItem("ElectroGrid/Build")]
    public static void Open()
    {
        var window = GetWindow<ElectroGridBuildWindow>("ElectroGrid Build");
        window.minSize = new Vector2(460f, 520f);
        window.Show();
    }

    private void OnEnable()
    {
        _config = SOBuildConfig.LoadOrCreate();
        BuildMachineSettings.MigrateFromConfig(_config);
        _serializedConfig = new SerializedObject(_config);
        RefreshGitHubStatus();
    }

    private void OnGUI()
    {
        if (!_config)
        {
            OnEnable();
            return;
        }

        _serializedConfig.Update();
        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        DrawVersion();
        DrawTargets();
        DrawOutput();
        DrawSigning();
        DrawChecks();
        DrawUploads();
        DrawIssues();
        DrawActions();
        DrawLastResult();

        EditorGUILayout.EndScrollView();
        _serializedConfig.ApplyModifiedProperties();
    }

    private void DrawVersion()
    {
        Header("Version");

        EditorGUILayout.BeginHorizontal();
        EditorGUI.BeginChangeCheck();
        string version = EditorGUILayout.DelayedTextField(ElectroGridBuild.Version, GUILayout.Width(90f));
        if (EditorGUI.EndChangeCheck()) ElectroGridBuild.SetVersion(version);

        if (GUILayout.Button("+ Patch", EditorStyles.miniButtonLeft)) ElectroGridBuild.BumpVersion(ElectroGridBuild.VersionPart.Patch);
        if (GUILayout.Button("+ Minor", EditorStyles.miniButtonMid)) ElectroGridBuild.BumpVersion(ElectroGridBuild.VersionPart.Minor);
        if (GUILayout.Button("+ Major", EditorStyles.miniButtonRight)) ElectroGridBuild.BumpVersion(ElectroGridBuild.VersionPart.Major);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.LabelField($"Android version code {ElectroGridBuild.AndroidVersionCode(ElectroGridBuild.Version)}, release tag v{ElectroGridBuild.Version}", EditorStyles.miniLabel);
    }

    private void DrawTargets()
    {
        Header("Targets");

        var targets = _serializedConfig.FindProperty("targets");

        for (int i = 0; i < targets.arraySize; i++)
        {
            var target = targets.GetArrayElementAtIndex(i);
            var kind = (SOBuildConfig.TargetKind)target.FindPropertyRelative("kind").enumValueIndex;
            var profile = target.FindPropertyRelative("profile");

            EditorGUILayout.BeginHorizontal();

            var enabled = target.FindPropertyRelative("enabled");
            enabled.boolValue = EditorGUILayout.ToggleLeft(Nicify(kind), enabled.boolValue, EditorStyles.boldLabel, GUILayout.Width(110f));

            using (new EditorGUI.DisabledScope(!enabled.boolValue))
            {
                EditorGUILayout.PropertyField(profile, GUIContent.none);

                var development = target.FindPropertyRelative("developmentBuild");
                development.boolValue = GUILayout.Toggle(development.boolValue, "Dev", EditorStyles.miniButton, GUILayout.Width(40f));
            }

            EditorGUILayout.EndHorizontal();

            if (profile.objectReferenceValue) continue;

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(114f);
            if (GUILayout.Button($"Create {Nicify(kind)} Profile", EditorStyles.miniButton)) CreateProfile(kind, profile);
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.LabelField("Targets build one after another; the active platform goes first and is restored after.", EditorStyles.wordWrappedMiniLabel);
    }

    private void DrawOutput()
    {
        Header("Output");

        string folder = BuildMachineSettings.ResolveOutputRoot(_config);
        bool overridden = !string.IsNullOrWhiteSpace(BuildMachineSettings.OutputFolderOverride);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(new GUIContent("Build Folder", "Saved for this computer only, so each machine can use its own folder"), GUILayout.Width(EditorGUIUtility.labelWidth - 4f));
        EditorGUILayout.SelectableLabel(folder, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
        if (GUILayout.Button("Browse", GUILayout.Width(60f)))
        {
            string picked = EditorUtility.OpenFolderPanel("Build into", Directory.Exists(folder) ? folder : BuildProcess.ProjectFolder, string.Empty);
            if (!string.IsNullOrEmpty(picked)) BuildMachineSettings.OutputFolderOverride = picked;
        }
        using (new EditorGUI.DisabledScope(!overridden))
        {
            if (GUILayout.Button(new GUIContent("Reset", $"Back to the project default ({_config.outputRoot})"), GUILayout.Width(50f)))
            {
                BuildMachineSettings.OutputFolderOverride = string.Empty;
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.PropertyField(_serializedConfig.FindProperty("zipWindowsBuild"));

        using (new EditorGUI.DisabledScope(!Directory.Exists(folder)))
        {
            if (GUILayout.Button("Open Build Folder", EditorStyles.miniButton)) EditorUtility.RevealInFinder(folder);
        }
    }

    private void DrawSigning()
    {
        if (!_config.targets.Any(target => target.enabled && target.kind == SOBuildConfig.TargetKind.AndroidApk)) return;

        Header("Android Signing");

        EditorGUILayout.BeginHorizontal();
        EditorGUI.BeginChangeCheck();
        string path = EditorGUILayout.TextField("Keystore", AndroidSigning.KeystorePath);
        if (EditorGUI.EndChangeCheck()) AndroidSigning.KeystorePath = path;
        if (GUILayout.Button("Browse", GUILayout.Width(60f)))
        {
            string picked = EditorUtility.OpenFilePanel("Release keystore", Path.GetDirectoryName(AndroidSigning.KeystorePath), "keystore,jks");
            if (!string.IsNullOrEmpty(picked)) AndroidSigning.KeystorePath = picked;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUI.BeginChangeCheck();
        string alias = EditorGUILayout.TextField("Alias", AndroidSigning.Alias);
        if (EditorGUI.EndChangeCheck()) AndroidSigning.Alias = alias;

        EditorGUILayout.BeginHorizontal();
        EditorGUI.BeginChangeCheck();
        string password = EditorGUILayout.PasswordField(new GUIContent("Password", "Kept for this editor session only, never saved"), AndroidSigning.Password);
        if (EditorGUI.EndChangeCheck())
        {
            AndroidSigning.Password = password;
            _signingStatus = null;
        }
        if (GUILayout.Button("Test", GUILayout.Width(60f)))
        {
            _signingOk = AndroidSigning.Verify(out _signingStatus);
        }
        EditorGUILayout.EndHorizontal();

        if (!string.IsNullOrEmpty(_signingStatus))
        {
            EditorGUILayout.HelpBox(_signingStatus, _signingOk ? MessageType.Info : MessageType.Error);
        }

        if (AndroidSigning.KeystoreExists) return;

        EditorGUILayout.HelpBox("No keystore at this path. Pick your keystore file with Browse (create one in Player Settings > Publishing Settings > Keystore Manager).", MessageType.Warning);
    }

    private void DrawChecks()
    {
        Header("Checks");

        EditorGUILayout.PropertyField(_serializedConfig.FindProperty("requireValidLevels"));
    }

    private void DrawUploads()
    {
        Header("Uploads");

        DrawCopyToFolder();
        DrawGitHubRelease();
    }

    private void DrawCopyToFolder()
    {
        var copy = _serializedConfig.FindProperty("copyToFolder");
        EditorGUILayout.PropertyField(copy, new GUIContent("Copy To Folder"));

        if (!copy.boolValue) return;

        EditorGUILayout.BeginHorizontal();
        EditorGUI.indentLevel++;
        EditorGUI.BeginChangeCheck();
        string copyFolder = EditorGUILayout.TextField(new GUIContent(" ", "Saved for this computer only"), BuildMachineSettings.CopyFolder);
        if (EditorGUI.EndChangeCheck()) BuildMachineSettings.CopyFolder = copyFolder;
        EditorGUI.indentLevel--;
        if (GUILayout.Button("Browse", GUILayout.Width(60f)))
        {
            string picked = EditorUtility.OpenFolderPanel("Copy builds to", BuildMachineSettings.CopyFolder, string.Empty);
            if (!string.IsNullOrEmpty(picked)) BuildMachineSettings.CopyFolder = picked;
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawGitHubRelease()
    {
        var github = _serializedConfig.FindProperty("uploadToGitHub");
        EditorGUILayout.PropertyField(github, new GUIContent("GitHub Release"));

        if (!github.boolValue) return;

        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(_serializedConfig.FindProperty("githubReleaseType"), new GUIContent("Release As"));
        EditorGUILayout.PropertyField(_serializedConfig.FindProperty("replaceExistingRelease"), new GUIContent("Replace Existing Release"));

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.HelpBox(_ghStatus, _ghReady ? MessageType.Info : MessageType.Warning);
        if (GUILayout.Button("Refresh", GUILayout.Width(60f), GUILayout.Height(38f))) RefreshGitHubStatus();
        EditorGUILayout.EndHorizontal();

        _notesFoldout = EditorGUILayout.Foldout(_notesFoldout, "Release Notes Preview", true);
        if (_notesFoldout)
        {
            if (_notesPreview == null || GUILayout.Button("Regenerate", EditorStyles.miniButton)) _notesPreview = ElectroGridBuild.BuildReleaseNotes();
            EditorGUILayout.HelpBox(_notesPreview, MessageType.None);
        }

        EditorGUI.indentLevel--;
    }

    private void DrawIssues()
    {
        if (_issues == null) return;

        Header("Check Results");

        if (_issues.Count == 0)
        {
            EditorGUILayout.HelpBox("All checks passed.", MessageType.Info);
            return;
        }

        foreach (var issue in _issues)
        {
            EditorGUILayout.HelpBox(issue.Message, issue.Severity == ElectroGridBuild.Severity.Error ? MessageType.Error : MessageType.Warning);
        }
    }

    private void DrawActions()
    {
        EditorGUILayout.Space(10);

        bool hasUploads = _config.uploadToGitHub || _config.copyToFolder;

        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling))
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Run Checks", GUILayout.Height(30f))) _issues = ElectroGridBuild.Preflight(_config, hasUploads);
            if (GUILayout.Button("Build", GUILayout.Height(30f))) StartRun(false);

            using (new EditorGUI.DisabledScope(!hasUploads))
            {
                if (GUILayout.Button("Build & Upload", GUILayout.Height(30f))) StartRun(true);
            }

            EditorGUILayout.EndHorizontal();
        }
    }

    private void DrawLastResult()
    {
        if (_lastResult == null) return;

        Header($"Last Run: {_lastResult.Version}");

        foreach (var step in _lastResult.Steps)
        {
            string text = $"{step.Name} ({step.Seconds:0.0}s)\n{step.Message}";
            EditorGUILayout.HelpBox(text, step.Succeeded ? MessageType.Info : MessageType.Error);
        }

        if (Directory.Exists(_lastResult.OutputFolder) && GUILayout.Button("Open Build Folder", EditorStyles.miniButton))
        {
            EditorUtility.RevealInFinder(_lastResult.OutputFolder);
        }
    }

    /// <summary>
    /// Deferred past the GUI pass: a build switches platforms and reloads assets, which must not happen while the
    /// window is halfway through laying itself out.
    /// </summary>
    private void StartRun(bool upload)
    {
        _serializedConfig.ApplyModifiedProperties();
        AssetDatabase.SaveAssetIfDirty(_config);

        EditorApplication.delayCall += () =>
        {
            _issues = ElectroGridBuild.Preflight(_config, upload);
            if (_issues.Any(issue => issue.Severity == ElectroGridBuild.Severity.Error))
            {
                Repaint();
                return;
            }

            string destinations = upload ? string.Join(" and ", UploadNames()) : "nowhere (build only)";
            var enabledTargets = _config.targets.Where(target => target.enabled).Select(target => Nicify(target.kind));
            if (!EditorUtility.DisplayDialog("ElectroGrid Build", $"Build {ElectroGridBuild.Version} for {string.Join(", ", enabledTargets)} and upload to {destinations}?", "Build", "Cancel")) return;

            _lastResult = ElectroGridBuild.Run(_config, upload);
            Repaint();
        };
    }

    private IEnumerable<string> UploadNames()
    {
        if (_config.copyToFolder) yield return "the copy folder";
        if (_config.uploadToGitHub) yield return $"GitHub ({_config.githubReleaseType.ToString().ToLowerInvariant()} release)";
    }

    private void RefreshGitHubStatus()
    {
        var version = BuildProcess.Gh("--version", 10);
        if (!version.Succeeded)
        {
            _ghReady = false;
            _ghStatus = "GitHub CLI not found. Install: winget install --id GitHub.cli, then run: gh auth login";
            return;
        }

        var auth = BuildProcess.Gh("auth status", 15);
        _ghReady = auth.Succeeded;
        _ghStatus = auth.Succeeded ? "GitHub CLI ready. " + FirstLine(auth.Message) : "GitHub CLI not signed in. Run: gh auth login";
    }

    private void CreateProfile(SOBuildConfig.TargetKind kind, SerializedProperty profileProperty)
    {
        string wanted = kind == SOBuildConfig.TargetKind.AndroidApk ? "Android" : "Windows";
        var matches = BuildProfile.GetInstalledPlatformModules()
            .Where(module => module.displayName != null && module.displayName.Contains(wanted))
            .ToList();

        if (matches.Count == 0)
        {
            EditorUtility.DisplayDialog("ElectroGrid Build", $"The {wanted} build support module is not installed. Add it from Unity Hub.", "OK");
            return;
        }

        var profile = BuildProfile.CreateBuildProfile(matches[0].platformGuid, $"ElectroGrid {wanted}");
        profileProperty.objectReferenceValue = profile;
        _serializedConfig.ApplyModifiedProperties();
    }

    private static string FirstLine(string text)
    {
        return (text ?? string.Empty).Split('\n').Select(line => line.Trim()).FirstOrDefault(line => line.Contains("Logged in")) ?? string.Empty;
    }

    private static string Nicify(SOBuildConfig.TargetKind kind) => kind == SOBuildConfig.TargetKind.AndroidApk ? "Android APK" : "Windows";

    private static void Header(string text)
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField(text, EditorStyles.boldLabel);
    }
}
