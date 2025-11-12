using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public abstract class Match3Objective
{
    [SerializeField] protected Sprite objectiveSprite;
    [SerializeField, HideInInspector] protected SOGridShape gridShape;
    
    protected bool Completed;
    
    public Sprite ObjectiveSprite => objectiveSprite;
    public SOGridShape GridShape => gridShape;
    public bool IsCompleted => Completed;
    
    

    public abstract void SetupObjective();
    public abstract void OnMatchMade(List<Match3Tile> matchedTiles);
    public abstract void OnLayerBreak(Match3Tile tile);
    public abstract string GetProgressText(bool includeText);
    public abstract string GetObjectiveName();
    
    
    public void SetGridShape(SOGridShape shape)
    {
        gridShape = shape;
    }
}

[Serializable]
public class GetMatches : Match3Objective
{
    [SerializeField, Min(1)] private int requiredAmount = 9;
    private int _currentAmount;
    

    public override void SetupObjective()
    {
        _currentAmount = 0;
        Completed = false;
    }

    public override void OnMatchMade(List<Match3Tile> matchedTiles)
    {
        if (matchedTiles == null || matchedTiles.Count == 0) return;

        _currentAmount += matchedTiles.Count;

        if (_currentAmount >= requiredAmount)
        {
            Completed = true;
        }
    }

    public override void OnLayerBreak(Match3Tile tile)
    {
        // This objective doesn't care about tile breaks
    }

    public override string GetProgressText(bool includeText)
    {
        return !includeText ? $"{_currentAmount}/{requiredAmount}" : $"Matches: {_currentAmount}/{requiredAmount}";
    }

    public override string GetObjectiveName()
    {
        return "Get Matches";
    }
}

[Serializable]
public class GetSpecificItemMatches : Match3Objective
{
    [SerializeField] private SOItemData targetItem;
    [SerializeField, Min(1)] private int requiredAmount  = 9;
    private int _currentAmount;
    

    public override void SetupObjective()
    {
        _currentAmount = 0;
        Completed = false;
    }

    public override void OnMatchMade(List<Match3Tile> matchedTiles)
    {
        if (matchedTiles == null || matchedTiles.Count == 0 || !targetItem) return;

        foreach (var tile in matchedTiles)
        {
            if (!tile.HasObject) continue;
            
            if (tile.CurrentMatch3Object.ItemData == targetItem)
            {
                _currentAmount++;
            }
        }

        if (_currentAmount >= requiredAmount)
        {
            Completed = true;
        }
    }

    public override void OnLayerBreak(Match3Tile tile)
    {
        // This objective doesn't care about tile breaks
    }

    public override string GetProgressText(bool includeText)
    {
        if (!includeText) return $"{_currentAmount}/{requiredAmount}";
        string itemName = targetItem ? targetItem.Label : "Items";
        return $"{itemName}: {_currentAmount}/{requiredAmount}";
    }

    public override string GetObjectiveName()
    {
        return $"Get {targetItem.Label} Matches";
    }
}

[Serializable]
public class ClearLayersObjective : Match3Objective
{
    [SerializeField, HideInInspector] private bool[] layeredTiles;
    private int _requiredAmount;
    private int _currentAmount;

    public bool[] LayeredTiles => layeredTiles;

    public override void SetupObjective()
    {
        _currentAmount = 0;
        Completed = false;
        
        if (layeredTiles != null)
        {
            _requiredAmount = 0;
            foreach (bool isBreakable in layeredTiles)
            {
                if (isBreakable) _requiredAmount++;
            }
        }
    }

    public override void OnMatchMade(List<Match3Tile> matchedTiles)
    {
    }

    public override void OnLayerBreak(Match3Tile tile)
    {
        if (!gridShape || gridShape.Grid == null) return;
        
        int width = gridShape.Grid.Width;
        int x = tile.GridPosition.x;
        int y = tile.GridPosition.y;
        int index = y * width + x;

        if (layeredTiles != null && index >= 0 && index < layeredTiles.Length)
        {
            if (layeredTiles[index])
            {
                _currentAmount++;

                if (_currentAmount >= _requiredAmount)
                {
                    Completed = true;
                }
            }
        }
    }

    public override string GetProgressText(bool includeText)
    {
        return !includeText ? $"{_currentAmount}/{_requiredAmount}" : $"Cleared Layers: {_currentAmount}/{_requiredAmount}";
    }

    public override string GetObjectiveName()
    {
        return "Clear Layers";
    }

    public bool TileHasLayer(int x, int y)
    {
        if (!gridShape || gridShape.Grid == null) return false;
        
        int width = gridShape.Grid.Width;
        int height = gridShape.Grid.Height;
        
        if (x < 0 || x >= width || y < 0 || y >= height) return false;
        if (layeredTiles == null || layeredTiles.Length != width * height) return false;
        
        int index = y * width + x;
        return layeredTiles[index];
    }
}