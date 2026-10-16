using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Grid and validation drawing shared by the level editor window and the read-only inspector
/// preview, so the two can never drift apart.
/// </summary>
internal static class Match3LevelGridGUI
{
    public const float DefaultCellSize = 30f;

    private const float CellBorder = 1f;

    private static readonly Color BackgroundColor = new Color(0.2f, 0.2f, 0.2f);
    private static readonly Color ActiveTileColor = new Color(0.17f, 0.15f, 0.3f);
    private static readonly Color InactiveTileColor = new Color(0.4f, 0.4f, 0.4f);
    private static readonly Color StarColor = new Color(1f, 0.85f, 0.95f);
    private static readonly Color PlugColor = new Color(0.55f, 0.8f, 1f);
    private static readonly Color VirusColor = new Color(0.55f, 1f, 0.2f);
    private static readonly Color FuseColor = new Color(1f, 0.6f, 0.15f);
    private static readonly Color GlitchColor = new Color(0f, 1f, 0.9f);
    private static readonly Color ConveyorColor = new Color(0.35f, 0.7f, 1f, 0.75f);
    private static readonly Color CellOutlineColor = new Color(0.85f, 0.3f, 0.9f, 0.6f);

    private static readonly Dictionary<string, Texture2D> Icons = new Dictionary<string, Texture2D>();

    public static Texture2D Icon(string spriteName)
    {
        if (Icons.TryGetValue(spriteName, out var cached) && cached) return cached;

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Resources/Sprites/{spriteName}.png");
        var texture = sprite ? sprite.texture : null;
        Icons[spriteName] = texture;
        return texture;
    }

    public static Texture2D IconFor(Match3TileObjectType type) => type switch
    {
        Match3TileObjectType.Obstacle => Icon("ShapeDoubleStar"),
        Match3TileObjectType.Bottom => Icon("ShapePlug"),
        Match3TileObjectType.Virus => Icon("Virus"),
        Match3TileObjectType.Fuse => Icon("Fuse"),
        Match3TileObjectType.Surge => Icon("ShapeSurge"),
        _ => null
    };

    public static Color ColorFor(Match3TileObjectType type) => type switch
    {
        Match3TileObjectType.Obstacle => StarColor,
        Match3TileObjectType.Bottom => PlugColor,
        Match3TileObjectType.Virus => VirusColor,
        Match3TileObjectType.Fuse => FuseColor,
        _ => Color.white
    };

    public static string NameFor(Match3TileObjectType type) => type switch
    {
        Match3TileObjectType.Obstacle => "Star",
        Match3TileObjectType.Bottom => "Plug",
        Match3TileObjectType.Virus => "Virus",
        Match3TileObjectType.Fuse => "Fuse",
        Match3TileObjectType.Surge => "Surge",
        _ => "Piece"
    };
    private static readonly Color GridLineColor = new Color(0.1f, 0.1f, 0.1f);
    private static readonly Color HoverColor = new Color(1f, 1f, 1f, 0.3f);

    public static void DrawCells(Rect gridRect, Grid grid, SerializedProperty tileObjectsProp, float cellSize, bool drawHover, SerializedProperty glitchTilesProp = null, SOMatch3Level level = null)
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

                if (!isActive)
                {
                    EditorGUI.DrawRect(cellRect, InactiveTileColor);
                    continue;
                }

                EditorGUI.DrawRect(cellRect, ActiveTileColor);
                DrawOutline(cellRect, CellOutlineColor, 1f);

                int conveyor = level ? Match3GridHandler.ConveyorDirection(level, y) : 0;
                if (conveyor != 0)
                {
                    var arrowStyle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.RoundToInt(cellSize * 0.55f) };
                    arrowStyle.normal.textColor = ConveyorColor;
                    GUI.Label(cellRect, conveyor > 0 ? "\u203A" : "\u2039", arrowStyle);
                }

                var icon = IconFor(tileObjectType);
                if (icon)
                {
                    var iconRect = new Rect(cellRect.x + cellRect.width * 0.12f, cellRect.y + cellRect.height * 0.12f, cellRect.width * 0.76f, cellRect.height * 0.76f);
                    GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit, true, 0f, ColorFor(tileObjectType), 0f, 0f);

                    if (tileObjectType == Match3TileObjectType.Surge && level) DrawSurgeTarget(cellRect, level, x, y);
                }
                else if (level && level.PlacedPiece(x, y) is { } placed && placed.Sprite)
                {
                    var pieceRect = new Rect(cellRect.x + cellRect.width * 0.2f, cellRect.y + cellRect.height * 0.2f, cellRect.width * 0.6f, cellRect.height * 0.6f);
                    GUI.DrawTexture(pieceRect, AssetPreview.GetAssetPreview(placed.Sprite) ?? placed.Sprite.texture, ScaleMode.ScaleToFit, true, 0f, placed.Color, 0f, 0f);
                }
                else
                {
                    float dot = cellRect.width * 0.18f;
                    EditorGUI.DrawRect(new Rect(cellRect.center.x - dot * 0.5f, cellRect.center.y - dot * 0.5f, dot, dot), new Color(1f, 1f, 1f, 0.12f));
                }

                if (isActive && glitchTilesProp != null && index < glitchTilesProp.arraySize && glitchTilesProp.GetArrayElementAtIndex(index).boolValue)
                {
                    float border = Mathf.Max(2f, cellSize * 0.1f);
                    DrawOutline(cellRect, GlitchColor, border);
                    float slice = Mathf.Max(1f, cellSize * 0.07f);
                    EditorGUI.DrawRect(new Rect(cellRect.x + cellRect.width * 0.18f, cellRect.y + cellRect.height * 0.3f, cellRect.width * 0.4f, slice), new Color(1f, 0f, 0.6f, 0.9f));
                    EditorGUI.DrawRect(new Rect(cellRect.x + cellRect.width * 0.42f, cellRect.y + cellRect.height * 0.62f, cellRect.width * 0.4f, slice), GlitchColor);
                }

                if (isActive && level && level.HasStarFuse(x, y))
                {
                    float ring = Mathf.Max(2f, cellSize * 0.08f);
                    DrawOutline(new Rect(cellRect.x + ring, cellRect.y + ring, cellRect.width - ring * 2f, cellRect.height - ring * 2f), FuseColor, ring);
                }

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

    private static void DrawOutline(Rect rect, Color color, float thickness)
    {
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
        EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
        EditorGUI.DrawRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
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
    private static void DrawSurgeTarget(Rect cellRect, SOMatch3Level level, int x, int y)
    {
        var targets = level.SurgeTargets;
        int index = y * level.GridShape.Grid.Width + x;
        int target = targets != null && index < targets.Length ? targets[index] : -1;
        var item = target >= 0 && target < level.MatchObjects.Count ? level.MatchObjects[target] : null;

        var coreRect = new Rect(cellRect.x + cellRect.width * 0.33f, cellRect.y + cellRect.height * 0.33f, cellRect.width * 0.34f, cellRect.height * 0.34f);
        if (item && item.Sprite)
        {
            GUI.DrawTexture(coreRect, AssetPreview.GetAssetPreview(item.Sprite) ?? item.Sprite.texture, ScaleMode.ScaleToFit, true, 0f, item.Color, 0f, 0f);
            return;
        }

        var style = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.RoundToInt(cellRect.height * 0.3f) };
        style.normal.textColor = Color.white;
        GUI.Label(coreRect, "?", style);
    }

    public static string BuildCountsLabel(SOMatch3Level level)
    {
        if (!level || !level.GridShape || level.GridShape.Grid == null) return string.Empty;

        int obstacleCount = level.CountObjectsOfType(Match3TileObjectType.Obstacle);
        int bottomObjectCount = level.CountObjectsOfType(Match3TileObjectType.Bottom);
        int virusCount = level.CountObjectsOfType(Match3TileObjectType.Virus);
        int fuseCount = level.CountFuses();
        int surgeCount = level.CountObjectsOfType(Match3TileObjectType.Surge);
        int glitchCount = level.CountGlitches();
        int matchableCount = level.GridShape.Grid.ActiveCellCount - obstacleCount - bottomObjectCount - virusCount;

        Match3LevelValidation.GetRequiredCounts(level, out int obstacleNeeded, out int bottomObjectNeeded);

        var parts = new List<string> { $"Pieces: {matchableCount}", Tally("Stars", obstacleCount, obstacleNeeded), Tally("Plugs", bottomObjectCount, bottomObjectNeeded) };
        if (virusCount > 0) parts.Add($"Viruses: {virusCount}");
        if (fuseCount > 0) parts.Add($"Fuses: {fuseCount}");
        if (surgeCount > 0) parts.Add($"Surges: {surgeCount}");
        if (glitchCount > 0) parts.Add($"Glitches: {glitchCount}");
        return string.Join(" | ", parts);
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
