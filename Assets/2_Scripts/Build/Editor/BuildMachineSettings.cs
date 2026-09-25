using System.IO;
using UnityEditor;

/// <summary>
/// Build paths that belong to one computer rather than the project, such as a Google Drive folder, which differs by
/// user name and drive. Kept in EditorPrefs, so each machine has its own and none of them land in git.
/// </summary>
internal static class BuildMachineSettings
{
    private const string OutputFolderKey = "ElectroGrid.Build.OutputFolder";
    private const string CopyFolderKey = "ElectroGrid.Build.CopyFolder";

    /// <summary>Where builds go on this machine. Empty means the project default in <see cref="SOBuildConfig.outputRoot"/>.</summary>
    public static string OutputFolderOverride
    {
        get => EditorPrefs.GetString(OutputFolderKey, string.Empty);
        set => EditorPrefs.SetString(OutputFolderKey, value ?? string.Empty);
    }

    public static string CopyFolder
    {
        get => EditorPrefs.GetString(CopyFolderKey, string.Empty);
        set => EditorPrefs.SetString(CopyFolderKey, value ?? string.Empty);
    }

    /// <summary>The build folder in effect: this machine's choice, or the project default, relative to the project folder.</summary>
    public static string ResolveOutputRoot(SOBuildConfig config)
    {
        string folder = string.IsNullOrWhiteSpace(OutputFolderOverride) ? config.outputRoot : OutputFolderOverride;
        return Path.GetFullPath(Path.Combine(BuildProcess.ProjectFolder, string.IsNullOrWhiteSpace(folder) ? "Builds" : folder));
    }

    /// <summary>
    /// An absolute build folder saved in the shared config before this setting existed only works on the machine that
    /// picked it, so it becomes this machine's choice and the config goes back to the project default.
    /// </summary>
    public static void MigrateFromConfig(SOBuildConfig config)
    {
        if (!Path.IsPathRooted(config.outputRoot ?? string.Empty)) return;

        if (string.IsNullOrWhiteSpace(OutputFolderOverride)) OutputFolderOverride = config.outputRoot;
        config.outputRoot = "Builds";
        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssetIfDirty(config);
    }
}
