using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[Serializable]
public abstract class Match3Objective
{
    [SerializeField] protected Sprite objectiveSprite;
    [SerializeField, HideInInspector] protected SOGridShape gridShape;
    
    protected bool _completed;
    
    public Sprite ObjectiveSprite => objectiveSprite;
    public SOGridShape GridShape => gridShape;
    public bool IsCompleted => _completed;
    
    public abstract bool AllowOnlyOneObjectiveOfThisType { get; }
    public abstract void Setup();
    public abstract void OnMatchMade(List<Match3Tile> matchedTiles);
    public abstract void OnObstacleBreak(Match3ObstacleObject obstacle);
    public abstract void OnBottomObjectReached(Match3BottomObject bottomObject);
    public abstract string GetName();
    public abstract string GetDescription();
    public abstract string GetRequirementText();
    public abstract (int, int) GetProgress();

    public event Action ProgressChanged;
    public event Action Completed;
    
    
    protected void InvokeProgressChanged()
    {
        ProgressChanged?.Invoke();
    }
    
    protected void InvokeComplete()
    {
        Completed?.Invoke();
    }

    /// <summary>Runtime copy of the authored objective, so playing a level never mutates the level asset.</summary>
    public Match3Objective Clone()
    {
        var clone = (Match3Objective)MemberwiseClone();
        clone.ProgressChanged = null;
        clone.Completed = null;
        return clone;
    }
    
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
        _completed = false;
    }

    public override void OnMatchMade(List<Match3Tile> matchedTiles)
    {
        if (matchedTiles == null || matchedTiles.Count == 0) return;

        _currentAmount += matchedTiles.Count;


        if (_currentAmount >= requiredAmount)
        {
            _completed = true;
            InvokeComplete();
        }
        else
        {
            InvokeProgressChanged();
        }
    }

    public override void OnObstacleBreak(Match3ObstacleObject obstacle)
    {
    }

    public override void OnBottomObjectReached(Match3BottomObject bottomObject)
    {
    }

    public override string GetRequirementText()
    {
        return L10n.Get("objective.pieces.requirement");
    }
    

    public override (int, int) GetProgress()
    {
        return (_currentAmount, requiredAmount);
    }
    

    public override string GetName()
    {
        return "Collect Pieces";
    }

    public override string GetDescription()
    {
        return L10n.Get("objective.pieces.description", ("amount", requiredAmount));
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
        _completed = false;
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
                InvokeProgressChanged();
            }
        }

        if (_currentAmount >= requiredAmount)
        {
            _completed = true;
            InvokeComplete();
        }
    }

    public override void OnObstacleBreak(Match3ObstacleObject obstacle)
    {
    }

    public override void OnBottomObjectReached(Match3BottomObject bottomObject)
    {
    }

    public override string GetRequirementText()
    {
        string itemName = targetItem ? targetItem.DisplayName : L10n.Get("objective.item.fallback");
        return $"{itemName}:";
    }
    
    public override (int, int) GetProgress()
    {
        return (_currentAmount, requiredAmount);
    }

    public override string GetName()
    {
        return $"Collect {(targetItem ? targetItem.Label : "Items")} Pieces";
    }
    
    public override string GetDescription()
    {
        return L10n.Get("objective.item.description", ("amount", requiredAmount), ("item", targetItem ? targetItem.DisplayName : L10n.Get("objective.item.fallback")));
    }
}

[Serializable]
public class DestroyObstaclesObjective : Match3Objective
{
    [SerializeField, Min(1)] private int requiredAmount = 1;
    private int _currentAmount;

    public int RequiredAmount => requiredAmount;
    public override bool AllowOnlyOneObjectiveOfThisType => true;

    public override void Setup()
    {
        _currentAmount = 0;
        _completed = false;
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
            _completed = true;
            InvokeComplete();
        }
        else
        {
            InvokeProgressChanged();
        }
    }

    public override void OnBottomObjectReached(Match3BottomObject bottomObject)
    {
    }

    public override string GetRequirementText()
    {
        return L10n.Get("objective.doublestar.requirement");
    }

    public override (int, int) GetProgress()
    {
        return (_currentAmount, requiredAmount);
    }
    public override string GetName()
    {
        return "Destroy Double Stars";
    }
    
    public override string GetDescription()
    {
        return L10n.Get("objective.doublestar.description", ("amount", requiredAmount));
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
        _completed = false;
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
            _completed = true;
            InvokeComplete();
        }
        else
        {
            InvokeProgressChanged();
        }
    }

    public override string GetRequirementText()
    {
        return L10n.Get("objective.squarestar.requirement");
    }

    public override (int, int) GetProgress()
    {
        return (_currentAmount, requiredAmount);
    }
    
    public override string GetName()
    {
        return "Reach Bottom";
    }
    
    public override string GetDescription()
    {
        return L10n.Get("objective.squarestar.description", ("amount", requiredAmount));
    }
}