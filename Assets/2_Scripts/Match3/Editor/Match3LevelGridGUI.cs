using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Grid and validation drawing shared by the level editor window and the read-only inspector
/// preview, so the two can never drift apart.
/// </summary>
internal static class Match3LevelGridGUI
{
    public const float DefaultCellSize = 20f;

    private const float CellBorder = 1f;

    private static readonly Color BackgroundColor = new Color(0.2f, 0.2f, 0.2f);
    private static readonly Color ActiveTileColor = new Color(0.3f, 0.7f, 0.3f);
    private static readonly Color InactiveTileColor = new Color(0.4f, 0.4f, 0.4f);
    private static readonly Color ObstacleTileColor = new Color(0.8f, 0.3f, 0.3f);
    private static readonly Color BottomObjectTileColor = new Color(0.3f, 0.5f, 0.9f);
    private static readonly Color GridLineColor = new Color(0.1f, 0.1f, 0.1f);
    private static readonly Color HoverColor = new Color(1f, 1f, 1f, 0.3f);

    public static void DrawCells(Rect gridRect, Grid grid, SerializedProperty tileObjectsProp, float cellSize, bool drawHover)
    {
        int width = grid.Width;
        int height = grid.Height;

        EditorGUI.DrawRect(new Rect(gridRect.x, gridRect.y, width * cellSize, height * cellSize), BackgroundColor);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                if (index >= tileObjectsProp.arraySize) continue;

                bool isActive = grid.IsCellActive(x, y);
                var tileObjectType = (Match3TileObjectType)tileObjectsProp.GetArrayElementAtIndex(index).enumValueIndex;

                // Row 0 is the bottom of the board but the top of a GUI rect, so the row is flipped for drawing
                int visualY = height - 1 - y;
                var cellRect = new Rect(
                    gridRect.x + x * cellSize,
                    gridRect.y + visualY * cellSize,
                    cellSize - CellBorder,
                    cellSize - CellBorder
                );

                Color cellColor;
                if (!isActive)
                {
                    cellColor = InactiveTileColor;
                }
                else
                {
                    cellColor = tileObjectType switch
                    {
                        Match3TileObjectType.Obstacle => ObstacleTileColor,
                        Match3TileObjectType.Bottom => BottomObjectTileColor,
                        _ => ActiveTileColor
                    };
                }

                EditorGUI.DrawRect(cellRect, cellColor);

                if (drawHover && cellRect.Contains(Event.current.mousePosition))
                {
                    EditorGUI.DrawRect(cellRect, HoverColor);
                }
            }
        }

        Handles.color = GridLineColor;
        float endX = gridRect.x + width * cellSize;
        float endY = gridRect.y + height * cellSize;

        for (int x = 0; x <= width; x++)
        {
            float xPos = gridRect.x + x * cellSize;
            Handles.DrawLine(new Vector3(xPos, gridRect.y), new Vector3(xPos, endY));
        }

        for (int y = 0; y <= height; y++)
        {
            float yPos = gridRect.y + y * cellSize;
            Handles.DrawLine(new Vector3(gridRect.x, yPos), new Vector3(endX, yPos));
        }
    }

    /// <summary>Maps a mouse position onto a paintable cell index, false if it is outside or on an inactive cell.</summary>
    public static bool TryGetCellIndex(Rect gridRect, Grid grid, Vector2 mousePosition, float cellSize, out int index)
    {
        index = -1;

        int x = Mathf.FloorToInt((mousePosition.x - gridRect.x) / cellSize);
        int visualY = Mathf.FloorToInt((mousePosition.y - gridRect.y) / cellSize);
        int y = grid.Height - 1 - visualY;

        if (x < 0 || x >= grid.Width || y < 0 || y >= grid.Height) return false;
        if (!grid.IsCellActive(x, y)) return false;

        index = y * grid.Width + x;
        return true;
    }

    /// <summary>Counts of each tile type against what the level's objectives actually require.</summary>
    public static string BuildCountsLabel(SOMatch3Level level)
    {
        if (!level || !level.GridShape || level.GridShape.Grid == null) return string.Empty;

        int obstacleCount = level.CountObjectsOfType(Match3TileObjectType.Obstacle);
        int bottomObjectCount = level.CountObjectsOfType(Match3TileObjectType.Bottom);
        int matchableCount = level.GridShape.Grid.ActiveCellCount - obstacleCount - bottomObjectCount;

        Match3LevelValidation.GetRequiredCounts(level, out int obstacleNeeded, out int bottomObjectNeeded);

        return $"Matchable: {matchableCount} | {Tally("Obstacles", obstacleCount, obstacleNeeded)} | {Tally("Bottom", bottomObjectCount, bottomObjectNeeded)}";
    }

    /// <summary>Reserves layout space for the grid and centres it horizontally in the available width.</summary>
    public static Rect GetCenteredGridRect(Grid grid, float cellSize)
    {
        float gridWidth = grid.Width * cellSize;
        Rect gridRect = GUILayoutUtility.GetRect(gridWidth, grid.Height * cellSize, GUILayout.ExpandWidth(true));
        gridRect.x += Mathf.Max(0f, (gridRect.width - gridWidth) / 2f);
        gridRect.width = gridWidth;
        return gridRect;
    }

    public static void DrawIssues(List<Match3LevelValidation.Issue> issues)
    {
        foreach (var issue in issues)
        {
            var type = issue.Severity == Match3LevelValidation.Severity.Error ? MessageType.Error : MessageType.Warning;
            EditorGUILayout.HelpBox(issue.Message, type);
        }
    }

    private static string Tally(string label, int placed, int needed)
    {
        string text = $"{label}: {placed} / {needed}";
        if (needed <= 0) return text;

        return placed == needed ? $"<color=green>{text}</color>" : $"<color=red>{text}</color>";
    }

    private static GUIStyle _richLabel;

    public static GUIStyle RichLabel => _richLabel ??= new GUIStyle(EditorStyles.label) { richText = true };
}
