using System;
using UnityEngine;

[Serializable]
public abstract class Match3LoseCondition
{
    [SerializeField] protected Sprite conditionSprite;
    
    protected bool ConditionMet;
    public Sprite ConditionSprite => conditionSprite;
    public bool IsConditionMet => ConditionMet;

    public abstract void SetupCondition();
    public abstract void UpdateCondition(float deltaTime);
    public abstract void OnMoveMade();
    public abstract string GetProgressText(bool includeText);
    public abstract string GetConditionName();
}

[Serializable]
public class MoveLimit : Match3LoseCondition
{
    [SerializeField, Min(1)] private int allowedMoves = 10;
    private int _movesRemaining;

    public int AllowedMoves => allowedMoves;
    public int MovesRemaining => _movesRemaining;

    public override void SetupCondition()
    {
        _movesRemaining = allowedMoves;
        ConditionMet = false;
    }

    public override void UpdateCondition(float deltaTime)
    {
    }

    public override void OnMoveMade()
    {
        if (ConditionMet) return;

        _movesRemaining--;

        if (_movesRemaining <= 0)
        {
            ConditionMet = true;
        }
    }

    public override string GetProgressText(bool includeText)
    {
        return !includeText ? $"{_movesRemaining}" : $"Moves Left: {_movesRemaining}";
    }

    public override string GetConditionName()
    {
        return "Moves Limit";
    }
}

[Serializable]
public class TimeLimit : Match3LoseCondition
{
    [SerializeField, Min(10)] private float allowedTime = 15f;
    private float _timeRemaining;

    public float AllowedTime => allowedTime;
    public float TimeRemaining => _timeRemaining;

    public override void SetupCondition()
    {
        _timeRemaining = allowedTime;
        ConditionMet = false;
    }

    public override void UpdateCondition(float deltaTime)
    {
        if (ConditionMet) return;

        _timeRemaining -= deltaTime;

        if (_timeRemaining <= 0)
        {
            _timeRemaining = 0;
            ConditionMet = true;
        }
    }

    public override void OnMoveMade()
    {
    }

    public override string GetProgressText(bool includeText)
    {
        int minutes = Mathf.FloorToInt(_timeRemaining / 60f);
        int seconds = Mathf.FloorToInt(_timeRemaining % 60f);
        
        return !includeText ? $"{minutes:00}:{seconds:00}" : $"Time Left: {minutes:00}:{seconds:00}";
    }

    public override string GetConditionName()
    {
        return "Time Limit";
    }
}