
using System.Collections.Generic;
using UnityEngine;

public class Match3LevelData
{
    public readonly SOMatch3Level Level;
    
    
    public readonly List<Match3Objective> CurrentObjectives = new List<Match3Objective>();
    public readonly List<Match3LoseCondition> CurrentLoseConditions = new List<Match3LoseCondition>();
    public int MovesMade;
    public int MatchesMade;
    public int LayersBroken;
    
    public Match3LevelData(SOMatch3Level level)
    {
        Level = level;
        MovesMade = 0;
        MatchesMade = 0;
        LayersBroken = 0;
        
        CurrentObjectives.Clear();
        CurrentLoseConditions.Clear();
        
        foreach (var objective in Level.Objectives)
        {
            if (objective != null)
            {
                string json = JsonUtility.ToJson(objective);
                Match3Objective copy = (Match3Objective)JsonUtility.FromJson(json, objective.GetType());
                copy.SetupObjective();
                CurrentObjectives.Add(copy);
            }
        }
        
        foreach (var condition in Level.LoseConditions)
        {
            if (condition != null)
            {
                string json = JsonUtility.ToJson(condition);
                Match3LoseCondition copy = (Match3LoseCondition)JsonUtility.FromJson(json, condition.GetType());
                copy.SetupCondition();
                CurrentLoseConditions.Add(copy);
            }
        }
    }
    
    public void OnMoveMade()
    {
        MovesMade++;
        
        foreach (var condition in CurrentLoseConditions)
        {
            condition?.OnMoveMade();
        }
    }
    
    public void OnMatchesMade(List<Match3Tile> allMatches)
    {
        MatchesMade += allMatches.Count;
        
        foreach (var objective in CurrentObjectives)
        {
            objective?.OnMatchMade(allMatches);
        }
    }
    
    public void OnLayerBreak(Match3Tile tile)
    {
        LayersBroken++;
        
        foreach (var objective in CurrentObjectives)
        {
            objective?.OnLayerBreak(tile);
        }
    }
    
    
    public bool IsObjectivesComplete()
    {
        bool allComplete = true;
        
        foreach (var objective in CurrentObjectives)
        {
            if (objective == null)  continue;
            
            if (!objective.IsCompleted)
            {
                allComplete = false;
                break;
            }
        }
        
        return allComplete;
    }
    
    public bool IsLostCondition()
    {
        foreach (var condition in CurrentLoseConditions)
        {
            if (condition is { IsConditionMet: true })
            {
                return true;
            }
        }

        return false;
    }
}