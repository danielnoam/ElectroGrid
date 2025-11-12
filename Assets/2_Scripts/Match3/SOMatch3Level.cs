using System;
using DNExtensions;
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

    public string LevelName => levelName;
    public SOGridShape GridShape => gridShape;
    public ChanceList<SOItemData> MatchObjects => matchObjects;
    public List<Match3Objective> Objectives => objectives;
    public List<Match3LoseCondition> LoseConditions => loseConditions;
    
    
    [SerializeField] public bool[] tiles;
    
    
    public bool TileHasBreakableLayer(int x, int y)
    {
        if (!gridShape || gridShape.Grid == null || objectives.Count <= 0) return false;
            
        int width = gridShape.Grid.Width;
        int height = gridShape.Grid.Height;
        
        if (x < 0 || x >= width || y < 0 || y >= height) return false;
        if (tiles == null || tiles.Length != width * height) return false;
        
        int index = y * width + x;
        return tiles[index];
    }
}