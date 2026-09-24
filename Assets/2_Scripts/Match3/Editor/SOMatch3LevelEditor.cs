using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Read-only summary of a level. All editing happens in the Level Editor window,
/// so there is one place where a level can be changed rather than two that can disagree.
/// </summary>
[CustomEditor(typeof(SOMatch3Level))]
internal class SOMatch3LevelEditor : UnityEditor.Editor
{
    private const float PreviewCellSize = 14f;

    public override void OnInspectorGUI()
    {
        var level = (SOMatch3Level)target;

        if (GUILayout.Button("Open in Level Editor", GUILayout.Height(30)))
        {
            Match3LevelEditorWindow.Open(level);
        }

        EditorGUILayout.Space(8);

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.LabelField("Level Name", level.LevelName);
            EditorGUILayout.ObjectField("Grid Shape", level.GridShape, typeof(SOGridShape), false);
        }

        EditorGUILayout.Space(8);

        DrawEntries("Objectives", level.Objectives, objective => objective.GetDescription());
        EditorGUILayout.Space(4);
        DrawEntries("Lose Conditions", level.LoseConditions, condition => condition.GetDescription());
        DrawGridPreview(level);
        DrawValidation(level);
    }

    private static void DrawEntries<T>(string header, List<T> entries, Func<T, string> describe) where T : class
    {
        EditorGUILayout.LabelField(header, EditorStyles.boldLabel);

        if (entries == null || entries.Count == 0)
        {
            EditorGUILayout.LabelField("   None", EditorStyles.miniLabel);
            return;
        }

        foreach (var entry in entries)
        {
            EditorGUILayout.LabelField(entry == null ? "   (empty)" : $"   • {describe(entry)}");
        }
    }

    private void DrawValidation(SOMatch3Level level)
    {
        var issues = Match3LevelValidation.Validate(level);
        if (issues.Count == 0) return;

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);
        Match3LevelGridGUI.DrawIssues(issues);
    }

    private void DrawGridPreview(SOMatch3Level level)
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Grid", EditorStyles.boldLabel);

        if (!level.GridShape || level.GridShape.Grid == null)
        {
            EditorGUILayout.HelpBox("No Grid Shape assigned.", MessageType.Info);
            return;
        }

        Grid grid = level.GridShape.Grid;
        var tileObjectsProp = serializedObject.FindProperty("tileObjects");

        // A level whose grid shape changed since it was last opened has a stale array, and the
        // preview must not resize it — that is the editor window's job, on an explicit edit
        if (tileObjectsProp.arraySize != grid.Width * grid.Height)
        {
            EditorGUILayout.HelpBox("Tile data does not match the grid shape. Open the Level Editor to rebuild it.", MessageType.Warning);
            return;
        }

        EditorGUILayout.LabelField(Match3LevelGridGUI.BuildCountsLabel(level), Match3LevelGridGUI.RichLabel);
        EditorGUILayout.Space(4);

        Rect gridRect = Match3LevelGridGUI.GetCenteredGridRect(grid, PreviewCellSize);
        Match3LevelGridGUI.DrawCells(gridRect, grid, tileObjectsProp, PreviewCellSize, false);
    }
}
