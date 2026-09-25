using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEngine;

/// <summary>
/// Everything the ElectroGrid build window runs: which Build Profiles to build, where the output goes, what to
/// check first and where to upload. Lives in the project so the setup is shared, but holds no secrets; GitHub
/// authentication comes from the gh CLI on each machine.
/// </summary>
internal class SOBuildConfig : ScriptableObject
{
    public const string AssetPath = "Assets/Settings/Build/BuildConfig.asset";

    public enum TargetKind
    {
        AndroidApk,
        Windows
    }

    [Serializable]
    public class Target
    {
        public bool enabled = true;
        public TargetKind kind;
        public BuildProfile profile;
        public bool developmentBuild;
    }

    public List<Target> targets = new List<Target>
    {
        new Target { kind = TargetKind.AndroidApk },
        new Target { kind = TargetKind.Windows }
    };

    [Tooltip("The project default, relative to the project folder. Each machine can pick its own folder in the build window. Each run goes into <folder>/<version>/")]
    public string outputRoot = "Builds";
    [Tooltip("Zip the Windows player folder into a single file, which is what gets uploaded")]
    public bool zipWindowsBuild = true;

    [Tooltip("Refuse to build with uncommitted changes, so a build always matches a commit")]
    public bool requireCleanWorkingTree = true;
    [Tooltip("Refuse to build if any level in the play order fails validation")]
    public bool requireValidLevels = true;

    public bool uploadToGitHub;
    [Tooltip("Create the release as a draft, to review before it goes public")]
    public bool githubDraft = true;
    public bool githubPrerelease;
    [Tooltip("If this version was already released, replace its files and move its tag to the current commit instead of stopping. " +
             "It stays draft or published as it was. Players already on this version are not offered the new build.")]
    public bool replaceExistingRelease;

    public bool copyToFolder;

    public static SOBuildConfig LoadOrCreate()
    {
        var config = AssetDatabase.LoadAssetAtPath<SOBuildConfig>(AssetPath);
        if (config) return config;

        EnsureFolder("Assets/Settings/Build");
        config = CreateInstance<SOBuildConfig>();
        AssetDatabase.CreateAsset(config, AssetPath);
        AssetDatabase.SaveAssets();
        return config;
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;

        string parent = System.IO.Path.GetDirectoryName(folder)?.Replace('\\', '/');
        if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(folder));
    }
}
