using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// GameManager.match3Levels is the only thing that puts a level in the game, and its order is the
/// play order — Match3GameManager.SetNextLevel walks it by index. A level that is not in the array
/// is unreachable however well it validates, so the editor reads and writes the array directly.
/// </summary>
internal static class Match3LevelRegistry
{
    private const string PrefabPathPrefKey = "ElectroGrid.LevelEditor.GameManagerPrefab";
    private const string LevelsProperty = "match3Levels";
    private const string ScenePathProperty = "match3Scene.scenePath";

    private static GameManager _cached;
    private static bool _searched;

    /// <summary>The GameManager prefab that owns the level array. Scenes only hold instances of it.</summary>
    public static GameManager Prefab
    {
        get
        {
            if (_cached) return _cached;

            // The fallback scans every prefab in the project, and this is read from a repaint
            if (_searched) return null;
            _searched = true;

            string remembered = EditorPrefs.GetString(PrefabPathPrefKey, string.Empty);
            if (!string.IsNullOrEmpty(remembered))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(remembered);
                if (prefab && prefab.TryGetComponent(out GameManager manager)) return _cached = manager;
            }

            return _cached = Find("t:Prefab GameManager") ?? Find("t:Prefab");
        }
    }

    /// <summary>Lets the window retry after the prefab has been created or moved.</summary>
    public static void ClearCache()
    {
        _cached = null;
        _searched = false;
    }

    private static GameManager Find(string filter)
    {
        foreach (string guid in AssetDatabase.FindAssets(filter))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!prefab || !prefab.TryGetComponent(out GameManager manager)) continue;

            EditorPrefs.SetString(PrefabPathPrefKey, path);
            return manager;
        }

        return null;
    }

    public static List<SOMatch3Level> GetLevels()
    {
        var levels = new List<SOMatch3Level>();
        if (!Prefab) return levels;

        var arrayProp = new SerializedObject(Prefab).FindProperty(LevelsProperty);

        for (int i = 0; i < arrayProp.arraySize; i++)
        {
            levels.Add(arrayProp.GetArrayElementAtIndex(i).objectReferenceValue as SOMatch3Level);
        }

        return levels;
    }

    public static void SetLevels(IList<SOMatch3Level> levels)
    {
        if (!Prefab || levels == null) return;

        var serialized = new SerializedObject(Prefab);
        var arrayProp = serialized.FindProperty(LevelsProperty);

        arrayProp.arraySize = levels.Count;
        for (int i = 0; i < levels.Count; i++)
        {
            arrayProp.GetArrayElementAtIndex(i).objectReferenceValue = levels[i];
        }

        serialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(Prefab);
        AssetDatabase.SaveAssets();
    }

    /// <summary>Reads the field rather than SceneField.ScenePath, which logs a warning when the scene is not in the build settings.</summary>
    public static string GetMatch3ScenePath()
    {
        if (!Prefab) return string.Empty;

        return new SerializedObject(Prefab).FindProperty(ScenePathProperty)?.stringValue ?? string.Empty;
    }
}
