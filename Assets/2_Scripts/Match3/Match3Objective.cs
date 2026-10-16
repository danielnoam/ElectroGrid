using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[Serializable]
public abstract class Match3Objective
{
    [SerializeField, HideInInspector] protected SOGridShape gridShape;
    
    protected bool _completed;
    
    public SOGridShape GridShape => gridShape;
    public bool IsCompleted => _completed;
    
    public abstract bool AllowOnlyOneObjectiveOfThisType { get; }
    public virtual bool ShowsTotal => true;
    public virtual bool StackedDisplay => !ShowsTotal;
    public abstract void Setup();
    public abstract void OnMatchMade(List<Match3Tile> matchedTiles);
    public abstract void OnObstacleBreak(Match3ObstacleObject obstacle);
    public abstract void OnBottomObjectReached(Match3BottomObject bottomObject);
    public abstract string GetName();
    public abstract string GetDescription();
    public abstract string GetRequirementText();
    public abstract (int, int) GetProgress();
    public virtual void OnScoreChanged(int score) { }
    public virtual void OnGlitchCleared() { }
    public virtual void OnVirusCountChanged(int remaining) { }
    public virtual void OnFuseDefused() { }

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
        if (!obstacle || !obstacle.CurrentTile || obstacle is Match3VirusObject) return;
        
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
        return "Destroy Stars";
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

[Serializable]
public class ReachScoreObjective : Match3Objective
{
    [SerializeField, Min(1)] private int requiredAmount = 1000;
    private int _score;

    public int RequiredAmount => requiredAmount;
    public override bool AllowOnlyOneObjectiveOfThisType => true;

    public override void Setup()
    {
        _score = 0;
        _completed = false;
    }

    public override void OnScoreChanged(int score)
    {
        if (_completed) return;

        _score = score;
        if (_score >= requiredAmount)
        {
            _completed = true;
            InvokeComplete();
        }
        else
        {
            InvokeProgressChanged();
        }
    }

    public override void OnMatchMade(List<Match3Tile> matchedTiles)
    {
    }

    public override void OnObstacleBreak(Match3ObstacleObject obstacle)
    {
    }

    public override void OnBottomObjectReached(Match3BottomObject bottomObject)
    {
    }

    public override string GetRequirementText()
    {
        return L10n.Get("objective.score.requirement");
    }

    public override (int, int) GetProgress()
    {
        return (Mathf.Min(_score, requiredAmount), requiredAmount);
    }

    public override string GetName()
    {
        return "Reach Score";
    }

    public override string GetDescription()
    {
        return L10n.Get("objective.score.description", ("amount", requiredAmount));
    }
}

[Serializable]
public class ClearGlitchesObjective : Match3Objective
{
    [SerializeField, Min(1)] private int requiredAmount = 5;
    private int _currentAmount;

    public int RequiredAmount => requiredAmount;
    public override bool AllowOnlyOneObjectiveOfThisType => true;

    public override void Setup()
    {
        _currentAmount = 0;
        _completed = false;
    }

    public override void OnGlitchCleared()
    {
        if (_completed) return;

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

    public override (int, int) GetProgress()
    {
        return (Mathf.Min(_currentAmount, requiredAmount), requiredAmount);
    }

    public override void OnMatchMade(List<Match3Tile> matchedTiles)
    {
    }

    public override void OnObstacleBreak(Match3ObstacleObject obstacle)
    {
    }

    public override void OnBottomObjectReached(Match3BottomObject bottomObject)
    {
    }

    public override string GetRequirementText()
    {
        return L10n.Get("objective.glitch.requirement");
    }

    public override string GetName()
    {
        return "Clear Glitches";
    }

    public override string GetDescription()
    {
        return L10n.Get("objective.glitch.description", ("amount", requiredAmount));
    }
}

[Serializable]
public class ClearVirusesObjective : Match3Objective
{
    private int _remaining;
    public override bool AllowOnlyOneObjectiveOfThisType => true;
    public override bool ShowsTotal => false;
    public override bool StackedDisplay => false;

    public override void Setup()
    {
        _remaining = 0;
        _completed = false;
    }

    public override void OnVirusCountChanged(int remaining)
    {
        if (_completed) return;

        _remaining = remaining;
        if (_remaining <= 0)
        {
            _completed = true;
            InvokeComplete();
        }
        else
        {
            InvokeProgressChanged();
        }
    }

    public override (int, int) GetProgress()
    {
        return (_remaining, _remaining);
    }

    public override void OnMatchMade(List<Match3Tile> matchedTiles)
    {
    }

    public override void OnObstacleBreak(Match3ObstacleObject obstacle)
    {
    }

    public override void OnBottomObjectReached(Match3BottomObject bottomObject)
    {
    }

    public override string GetRequirementText()
    {
        return L10n.Get("objective.virus.requirement");
    }

    public override string GetName()
    {
        return "Clear Viruses";
    }

    public override string GetDescription()
    {
        return L10n.Get("objective.virus.description");
    }
}

[Serializable]
public class DefuseObjective : Match3Objective
{
    [SerializeField, Min(1)] private int requiredAmount = 5;
    private int _currentAmount;

    public int RequiredAmount => requiredAmount;
    public override bool AllowOnlyOneObjectiveOfThisType => true;

    public override void Setup()
    {
        _currentAmount = 0;
        _completed = false;
    }

    public override void OnFuseDefused()
    {
        if (_completed) return;

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

    public override (int, int) GetProgress()
    {
        return (Mathf.Min(_currentAmount, requiredAmount), requiredAmount);
    }

    public override void OnMatchMade(List<Match3Tile> matchedTiles)
    {
    }

    public override void OnObstacleBreak(Match3ObstacleObject obstacle)
    {
    }

    public override void OnBottomObjectReached(Match3BottomObject bottomObject)
    {
    }

    public override string GetRequirementText()
    {
        return L10n.Get("objective.fuse.requirement");
    }

    public override string GetName()
    {
        return "Defuse Fuses";
    }

    public override string GetDescription()
    {
        return L10n.Get("objective.fuse.description", ("amount", requiredAmount));
    }
}
