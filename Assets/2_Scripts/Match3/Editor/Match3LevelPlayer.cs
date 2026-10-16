using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Enters play mode straight into one level or Survival. Match3GameManager reads overrideLevel and overrideSurvival in Start, so the
/// value is written into the open scene before play mode begins and put back when it ends — the
/// scene is saved with its original value, so the file on disk is unchanged either way.
/// </summary>
[InitializeOnLoad]
internal static class Match3LevelPlayer
{
    private const string ScenePathKey = "ElectroGrid.LevelEditor.PlayScenePath";
    private const string RestorePathKey = "ElectroGrid.LevelEditor.PlayRestorePath";
    private const string PendingLevelKey = "ElectroGrid.LevelEditor.PendingLevel";
    private const string PropertyKey = "ElectroGrid.LevelEditor.PlayProperty";
    private const string LevelProperty = "overrideLevel";
    private const string SurvivalProperty = "overrideSurvival";

    static Match3LevelPlayer()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    /// <summary>Plays the level. While the game is running it stops play mode first and starts again in this level.</summary>
    public static void Play(SOMatch3Level level) => PlayTarget(level);

    /// <summary>Plays a Survival run on this mode, the same way as a level.</summary>
    public static void PlaySurvival(SOSurvivalMode mode) => PlayTarget(mode);

    private static void PlayTarget(Object level)
    {
        if (!level) return;

        if (EditorApplication.isPlaying)
        {
            SessionState.SetString(PendingLevelKey, AssetDatabase.GetAssetPath(level));
            EditorApplication.ExitPlaymode();
            return;
        }

        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        // Loading a scene tears down the GUI, so never do it inside an editor window's repaint
        RunNextUpdate(() => PlayNow(level));
    }

    private static void RunNextUpdate(System.Action action)
    {
        EditorApplication.CallbackFunction callback = null;
        callback = () =>
        {
            EditorApplication.update -= callback;
            action();
        };
        EditorApplication.update += callback;
    }

    private static void PlayNow(Object level)
    {
        if (!level || EditorApplication.isPlayingOrWillChangePlaymode) return;

        string scenePath = Match3LevelRegistry.GetMatch3ScenePath();
        if (string.IsNullOrEmpty(scenePath))
        {
            Debug.LogError("No Match3 scene set on the GameManager prefab, so there is nothing to play into.");
            return;
        }

        // Opening the scene discards unsaved work in whatever is open, so ask first
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        if (!scene.IsValid()) return;

        var gameManager = Object.FindAnyObjectByType<Match3GameManager>(FindObjectsInactive.Include);
        if (!gameManager)
        {
            Debug.LogError($"No {nameof(Match3GameManager)} in {scenePath}.");
            return;
        }

        var serialized = new SerializedObject(gameManager);
        string property = level is SOSurvivalMode ? SurvivalProperty : LevelProperty;
        var overrideProp = serialized.FindProperty(property);

        // Remember what was there, so playing a level is never a way to quietly change the scene
        var previous = overrideProp.objectReferenceValue;
        SessionState.SetString(RestorePathKey, previous ? AssetDatabase.GetAssetPath(previous) : string.Empty);
        SessionState.SetString(ScenePathKey, scenePath);
        SessionState.SetString(PropertyKey, property);

        overrideProp.objectReferenceValue = level;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        EditorApplication.EnterPlaymode();
    }

    private static void OnPlayModeChanged(PlayModeStateChange change)
    {
        if (change != PlayModeStateChange.EnteredEditMode) return;

        RestoreOverride();

        string pendingPath = SessionState.GetString(PendingLevelKey, string.Empty);
        SessionState.EraseString(PendingLevelKey);
        var pending = string.IsNullOrEmpty(pendingPath) ? null : AssetDatabase.LoadAssetAtPath<Object>(pendingPath);
        if (pending) RunNextUpdate(() => PlayNow(pending));
    }

    private static void RestoreOverride()
    {
        string scenePath = SessionState.GetString(ScenePathKey, string.Empty);
        if (string.IsNullOrEmpty(scenePath)) return;

        string restorePath = SessionState.GetString(RestorePathKey, string.Empty);
        string property = SessionState.GetString(PropertyKey, LevelProperty);
        SessionState.EraseString(ScenePathKey);
        SessionState.EraseString(PropertyKey);
        SessionState.EraseString(RestorePathKey);

        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != scenePath) return;

        var gameManager = Object.FindAnyObjectByType<Match3GameManager>(FindObjectsInactive.Include);
        if (!gameManager) return;

        var serialized = new SerializedObject(gameManager);
        serialized.FindProperty(property).objectReferenceValue = string.IsNullOrEmpty(restorePath)
            ? null
            : AssetDatabase.LoadAssetAtPath<Object>(restorePath);

        serialized.ApplyModifiedPropertiesWithoutUndo();

        // Saving the original value back leaves the file as it was and clears the dirty marker
        EditorSceneManager.SaveScene(scene);
    }
}
