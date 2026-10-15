using System;
using DNExtensions.Utilities;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Match3 Level", menuName = "Scriptable Objects/Match3 Level")]
public class SOMatch3Level : ScriptableObject
{
    [Header("Level Settings")]
    [SerializeField] private string levelName = "Level";
    [SerializeField] private SOGridShape gridShape;
    [SerializeField] private ChanceList<SOItemData> matchObjects = new ChanceList<SOItemData>();
    [SerializeReference] private List<Match3Objective> objectives = new List<Match3Objective>();
    [SerializeReference] private List<Match3LoseCondition> loseConditions = new List<Match3LoseCondition>();
    [Tooltip("Quick matches build a combo and fill the combo bar. Survival always has it")]
    [SerializeField] private bool comboBar;
    [Tooltip("Moves a fuse lasts before it blows and the level is lost")]
    [SerializeField, Min(1)] private int fuseMoves = 8;
    [Tooltip("Rows that shift one cell along after every move. Row 0 is the bottom row")]
    [SerializeField] private List<Match3ConveyorRow> conveyorRows = new List<Match3ConveyorRow>();
    
    [Header("Tile Objects")]
    [SerializeField, HideInInspector] private Match3TileObjectType[] tileObjects;
    [SerializeField, HideInInspector] private bool[] glitchTiles;
    [SerializeField, HideInInspector] private bool[] starFuseTiles;

    
    /// <summary>The authored name. Stays in English for analytics and save records, players see DisplayName.</summary>
    public string LevelName => levelName;

    /// <summary>"Level 3" in the player's language, numbered by play order. Levels outside the play order show their authored name.</summary>
    public string DisplayName
    {
        get
        {
            var levels = GameManager.Instance ? GameManager.Instance.Match3Levels : null;
            int index = levels != null ? Array.IndexOf(levels, this) : -1;
            return index >= 0 ? L10n.Get("level.name", ("number", index + 1)) : levelName;
        }
    }
    public SOGridShape GridShape => gridShape;
    public ChanceList<SOItemData> MatchObjects => matchObjects;
    public List<Match3Objective> Objectives => objectives;
    public List<Match3LoseCondition> LoseConditions => loseConditions;
    public bool ComboBar => comboBar;
    public int FuseMoves => fuseMoves;
    public List<Match3ConveyorRow> ConveyorRows => conveyorRows;
    public bool[] GlitchTiles => glitchTiles;
    public bool[] StarFuseTiles => starFuseTiles;
    public Match3TileObjectType[] TileObjects => tileObjects;
    
    
    private void OnValidate()
    {
        if (objectives == null) return;
    
        foreach (var objective in objectives)
        {
            objective?.SetGridShape(gridShape);
        }
    }
    
    public bool TileHasObjectType(int x, int y, Match3TileObjectType objectType)
    {
        if (!gridShape || gridShape.Grid == null) return false;
        
        int width = gridShape.Grid.Width;
        int height = gridShape.Grid.Height;
        
        if (x < 0 || x >= width || y < 0 || y >= height) return false;
        if (tileObjects == null || tileObjects.Length != width * height) return false;
        
        int index = y * width + x;
        return tileObjects[index] == objectType;
    }
    
    public bool HasGlitch(int x, int y)
    {
        if (!gridShape || gridShape.Grid == null || glitchTiles == null) return false;

        int width = gridShape.Grid.Width;
        if (x < 0 || x >= width || y < 0 || y >= gridShape.Grid.Height) return false;

        int index = y * width + x;
        return index < glitchTiles.Length && glitchTiles[index] && gridShape.Grid.IsCellActive(x, y);
    }

    /// <summary>A fuse on the Star or Plug in this cell. Only counts when the cell holds a Star.</summary>
    public bool HasStarFuse(int x, int y)
    {
        if (!gridShape || gridShape.Grid == null || starFuseTiles == null) return false;

        int width = gridShape.Grid.Width;
        if (x < 0 || x >= width || y < 0 || y >= gridShape.Grid.Height) return false;

        int index = y * width + x;
        if (index >= starFuseTiles.Length || !starFuseTiles[index] || !gridShape.Grid.IsCellActive(x, y)) return false;
        return TileHasObjectType(x, y, Match3TileObjectType.Obstacle) || TileHasObjectType(x, y, Match3TileObjectType.Bottom);
    }

    /// <summary>Every fuse the level starts with: on pieces and on Stars.</summary>
    public int CountFuses()
    {
        int count = CountObjectsOfType(Match3TileObjectType.Fuse);
        if (!gridShape || gridShape.Grid == null) return count;

        for (int y = 0; y < gridShape.Grid.Height; y++)
        {
            for (int x = 0; x < gridShape.Grid.Width; x++)
            {
                if (HasStarFuse(x, y)) count++;
            }
        }

        return count;
    }

    public int CountGlitches()
    {
        if (!gridShape || gridShape.Grid == null) return 0;

        int count = 0;
        for (int y = 0; y < gridShape.Grid.Height; y++)
        {
            for (int x = 0; x < gridShape.Grid.Width; x++)
            {
                if (HasGlitch(x, y)) count++;
            }
        }

        return count;
    }

    public int CountObjectsOfType(Match3TileObjectType objectType)
    {
        if (tileObjects == null) return 0;
        
        int count = 0;
        foreach (var tileObj in tileObjects)
        {
            if (tileObj == objectType) count++;
        }
        return count;
    }
}

[Serializable]
public class Match3ConveyorRow
{
    [Min(0)] public int row;
    public bool moveRight = true;
}
