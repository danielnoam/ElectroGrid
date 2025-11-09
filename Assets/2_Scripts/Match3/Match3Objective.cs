using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public abstract class Match3Objective
{
    [SerializeField] protected Sprite objectiveSprite;
    
    protected bool Completed;
    
    public Sprite ObjectiveSprite => objectiveSprite;
    public bool IsCompleted => Completed;

    public abstract void SetupObjective();
    public abstract void OnMatchMade(List<Match3Tile> matchedTiles);
    public abstract string GetProgressText(bool includeText);
    public abstract string GetObjectiveName();
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