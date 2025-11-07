using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public abstract class Match3Objective
{
    [SerializeField] protected string objectiveName;
    [SerializeField] protected Sprite objectiveSprite;
    [SerializeField, TextArea] protected string description;
    
    protected bool Completed;
    
    public string ObjectiveName => objectiveName;
    public Sprite ObjectiveSprite => objectiveSprite;
    public string Description => description;
    public bool IsCompleted => Completed;

    public abstract void SetupObjective();
    public abstract void OnMatchMade(List<Tile> matchedTiles);
    public abstract string GetProgressText();
    public abstract float GetProgress();
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

    public override void OnMatchMade(List<Tile> matchedTiles)
    {
        if (Completed || matchedTiles == null || matchedTiles.Count == 0) return;

        _currentAmount += matchedTiles.Count;

        if (_currentAmount >= requiredAmount)
        {
            Completed = true;
        }
    }

    public override string GetProgressText()
    {
        return $"{_currentAmount}/{requiredAmount}";
    }

    public override float GetProgress()
    {
        return Mathf.Clamp01((float)_currentAmount / requiredAmount);
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

    public override void OnMatchMade(List<Tile> matchedTiles)
    {
        if (Completed || matchedTiles == null || matchedTiles.Count == 0 || !targetItem) return;

        foreach (var tile in matchedTiles)
        {
            if (!tile.HasObject) continue;
            
            if (tile.CurrentMatchObject.ItemData == targetItem)
            {
                _currentAmount++;
            }
        }

        if (_currentAmount >= requiredAmount)
        {
            Completed = true;
        }
    }

    public override string GetProgressText()
    {
        string itemName = targetItem ? targetItem.name : "Items";
        return $"{_currentAmount}/{requiredAmount} {itemName}";
    }

    public override float GetProgress()
    {
        return Mathf.Clamp01((float)_currentAmount / requiredAmount);
    }
}