#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Read-only summary of a level. All editing happens in the Level Editor window,
/// so there is one place where a level can be changed rather than two that can disagree.
/// </summary>
[CustomEditor(typeof(SOMatch3Level))]
public class SOMatch3LevelEditor : UnityEditor.Editor
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

        DrawObjectives(level);
        DrawLoseConditions(level);
        DrawGridPreview(level);
        DrawValidation(level);
    }

    private void DrawValidation(SOMatch3Level level)
    {
        var issues = Match3LevelValidation.Validate(level);
        if (issues.Count == 0) return;

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);

        foreach (var issue in issues)
        {
            var type = issue.Severity == Match3LevelValidation.Severity.Error ? MessageType.Error : MessageType.Warning;
            EditorGUILayout.HelpBox(issue.Message, type);
        }
    }

    private void DrawObjectives(SOMatch3Level level)
    {
        EditorGUILayout.LabelField("Objectives", EditorStyles.boldLabel);

        if (level.Objectives == null || level.Objectives.Count == 0)
        {
            EditorGUILayout.LabelField("   None", EditorStyles.miniLabel);
            return;
        }

        foreach (var objective in level.Objectives)
        {
            EditorGUILayout.LabelField(objective == null ? "   (empty)" : $"   • {objective.GetDescription()}");
        }
    }

    private void DrawLoseConditions(SOMatch3Level level)
    {
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Lose Conditions", EditorStyles.boldLabel);

        if (level.LoseConditions == null || level.LoseConditions.Count == 0)
        {
            EditorGUILayout.LabelField("   None", EditorStyles.miniLabel);
            return;
        }

        foreach (var condition in level.LoseConditions)
        {
            EditorGUILayout.LabelField(condition == null ? "   (empty)" : $"   • {condition.GetDescription()}");
        }
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

        float gridWidth = grid.Width * PreviewCellSize;
        Rect gridRect = GUILayoutUtility.GetRect(gridWidth, grid.Height * PreviewCellSize, GUILayout.ExpandWidth(true));
        gridRect.x += Mathf.Max(0f, (gridRect.width - gridWidth) / 2f);
        gridRect.width = gridWidth;

        Match3LevelGridGUI.DrawCells(gridRect, grid, tileObjectsProp, PreviewCellSize, false);
    }
}
#endif
