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
    
    [Header("Tile Objects")]
    [SerializeField, HideInInspector] private Match3TileObjectType[] tileObjects;

    
    public string LevelName => levelName;
    public SOGridShape GridShape => gridShape;
    public ChanceList<SOItemData> MatchObjects => matchObjects;
    public List<Match3Objective> Objectives => objectives;
    public List<Match3LoseCondition> LoseConditions => loseConditions;
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