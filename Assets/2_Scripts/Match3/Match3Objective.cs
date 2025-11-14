using System;
using System.Collections.Generic;
using Unity.VisualScripting;
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
    
    public abstract bool AllowOnlyOneObjectiveOfThisType { get; }
    public abstract void Setup();
    public abstract void OnMatchMade(List<Match3Tile> matchedTiles);
    public abstract void OnObstacleBreak(Match3ObstacleObject obstacle);
    public abstract void OnBottomObjectReached(Match3BottomObject bottomObject);
    public abstract string GetProgressText(bool includeText);
    public abstract string GetName();
    public abstract string GetDescription();
    
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
    
    public override bool AllowOnlyOneObjectiveOfThisType => true;

    public override void Setup()
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

    public override void OnObstacleBreak(Match3ObstacleObject obstacle)
    {
    }

    public override void OnBottomObjectReached(Match3BottomObject bottomObject)
    {
    }

    public override string GetProgressText(bool includeText)
    {
        return !includeText ? $"{_currentAmount}/{requiredAmount}" : $"Matches: {_currentAmount}/{requiredAmount}";
    }

    public override string GetName()
    {
        return "Get Matches";
    }

    public override string GetDescription()
    {
        return $"Get {requiredAmount} Matches";
    }
}

[Serializable]
public class GetSpecificItemMatches : Match3Objective
{
    [SerializeField] private SOItemData targetItem;
    [SerializeField, Min(1)] private int requiredAmount  = 9;
    private int _currentAmount;

    public override bool AllowOnlyOneObjectiveOfThisType => true;

    public override void Setup()
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
            
            if (tile.CurrentMatch3Object is Match3MatchableObject matchable && matchable.ItemData == targetItem)
            {
                _currentAmount++;
            }
        }

        if (_currentAmount >= requiredAmount)
        {
            Completed = true;
        }
    }

    public override void OnObstacleBreak(Match3ObstacleObject obstacle)
    {
    }

    public override void OnBottomObjectReached(Match3BottomObject bottomObject)
    {
    }

    public override string GetProgressText(bool includeText)
    {
        if (!includeText) return $"{_currentAmount}/{requiredAmount}";
        string itemName = targetItem ? targetItem.Label : "Items";
        return $"{itemName}: {_currentAmount}/{requiredAmount}";
    }

    public override string GetName()
    {
        return $"Get {targetItem.Label} Matches";
    }
    
    public override string GetDescription()
    {
        return $"Get {requiredAmount} Matches of {targetItem.Label}";
    }
}

[Serializable]
public class ClearObstaclesObjective : Match3Objective
{
    [SerializeField, Min(1)] private int requiredAmount = 1;
    private int _currentAmount;

    public int RequiredAmount => requiredAmount;
    public override bool AllowOnlyOneObjectiveOfThisType => true;

    public override void Setup()
    {
        _currentAmount = 0;
        Completed = false;
    }

    public override void OnMatchMade(List<Match3Tile> matchedTiles)
    {
    }

    public override void OnObstacleBreak(Match3ObstacleObject obstacle)
    {
        if (!obstacle || !obstacle.CurrentTile) return;
        
        _currentAmount++;

        if (_currentAmount >= requiredAmount)
        {
            Completed = true;
        }
    }

    public override void OnBottomObjectReached(Match3BottomObject bottomObject)
    {
    }

    public override string GetProgressText(bool includeText)
    {
        return !includeText ? $"{_currentAmount}/{requiredAmount}" : $"Kernels: {_currentAmount}/{requiredAmount}";
    }

    public override string GetName()
    {
        return "Pop Kernels";
    }
    
    public override string GetDescription()
    {
        return $"Pop {requiredAmount} Kernels";
    }
}

[Serializable]
public class ReachBottomObjective : Match3Objective
{
    [SerializeField, Min(1)] private int requiredAmount = 1;
    private int _currentAmount;

    public int RequiredAmount => requiredAmount;
    public override bool AllowOnlyOneObjectiveOfThisType => true;

    public override void Setup()
    {
        _currentAmount = 0;
        Completed = false;
    }

    public override void OnMatchMade(List<Match3Tile> matchedTiles)
    {
    }

    public override void OnObstacleBreak(Match3ObstacleObject obstacle)
    {
    }

    public override void OnBottomObjectReached(Match3BottomObject bottomObject)
    {
        if (!bottomObject) return;
        
        _currentAmount++;

        if (_currentAmount >= requiredAmount)
        {
            Completed = true;
        }
    }

    public override string GetProgressText(bool includeText)
    {
        return !includeText ? $"{_currentAmount}/{requiredAmount}" : $"Burned Popcorns: {_currentAmount}/{requiredAmount}";
    }

    public override string GetName()
    {
        return "Reach Bottom";
    }
    
    public override string GetDescription()
    {
        return $"Get {requiredAmount} Burned Popcorns to the Bottom";
    }
}