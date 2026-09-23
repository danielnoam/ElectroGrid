#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Enters play mode straight into one level. Match3GameManager reads overrideLevel in Start, so the
/// value is written into the open scene before play mode begins and put back when it ends — the
/// scene is saved with its original value, so the file on disk is unchanged either way.
/// </summary>
[InitializeOnLoad]
internal static class Match3LevelPlayer
{
    private const string ScenePathKey = "ElectroGrid.LevelEditor.PlayScenePath";
    private const string RestorePathKey = "ElectroGrid.LevelEditor.PlayRestorePath";
    private const string OverrideProperty = "overrideLevel";

    static Match3LevelPlayer()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    public static void Play(SOMatch3Level level)
    {
        if (!level || EditorApplication.isPlayingOrWillChangePlaymode) return;

        // Loading a scene tears down the GUI, so never do it inside an editor window's repaint
        EditorApplication.delayCall += () => PlayNow(level);
    }

    private static void PlayNow(SOMatch3Level level)
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
        var overrideProp = serialized.FindProperty(OverrideProperty);

        // Remember what was there, so playing a level is never a way to quietly change the scene
        var previous = overrideProp.objectReferenceValue;
        SessionState.SetString(RestorePathKey, previous ? AssetDatabase.GetAssetPath(previous) : string.Empty);
        SessionState.SetString(ScenePathKey, scenePath);

        overrideProp.objectReferenceValue = level;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        EditorApplication.EnterPlaymode();
    }

    private static void OnPlayModeChanged(PlayModeStateChange change)
    {
        if (change != PlayModeStateChange.EnteredEditMode) return;

        string scenePath = SessionState.GetString(ScenePathKey, string.Empty);
        if (string.IsNullOrEmpty(scenePath)) return;

        string restorePath = SessionState.GetString(RestorePathKey, string.Empty);
        SessionState.EraseString(ScenePathKey);
        SessionState.EraseString(RestorePathKey);

        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != scenePath) return;

        var gameManager = Object.FindAnyObjectByType<Match3GameManager>(FindObjectsInactive.Include);
        if (!gameManager) return;

        var serialized = new SerializedObject(gameManager);
        serialized.FindProperty(OverrideProperty).objectReferenceValue = string.IsNullOrEmpty(restorePath)
            ? null
            : AssetDatabase.LoadAssetAtPath<SOMatch3Level>(restorePath);

        serialized.ApplyModifiedPropertiesWithoutUndo();

        // Saving the original value back leaves the file as it was and clears the dirty marker
        EditorSceneManager.SaveScene(scene);
    }
}
#endif
