using System;
using UnityEngine;

[Serializable]
public abstract class Match3LoseCondition
{
    [SerializeField] protected Sprite conditionSprite;
    
    protected bool ConditionMet;
    public Sprite ConditionSprite => conditionSprite;
    public bool IsConditionMet => ConditionMet;

    public abstract void Setup();
    public abstract void Update(float deltaTime);
    public abstract void OnMoveMade();
    public abstract string GetProgressText(bool includeText);
    public abstract string GetName();
    public abstract string GetDescription();
}

[Serializable]
public class MoveLimit : Match3LoseCondition
{
    [SerializeField, Min(1)] private int allowedMoves = 10;
    private int _movesRemaining;

    public int AllowedMoves => allowedMoves;
    public int MovesRemaining => _movesRemaining;

    public override void Setup()
    {
        _movesRemaining = allowedMoves;
        ConditionMet = false;
    }

    public override void Update(float deltaTime)
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

    public override string GetName()
    {
        return "Moves Limit";
    }
    
    public override string GetDescription()
    {
        return $"{allowedMoves} Moves allowed";
    }
}

[Serializable]
public class TimeLimit : Match3LoseCondition
{
    [SerializeField, Min(10)] private float allowedTime = 15f;
    private float _timeRemaining;

    public float AllowedTime => allowedTime;
    public float TimeRemaining => _timeRemaining;

    public override void Setup()
    {
        _timeRemaining = allowedTime;
        ConditionMet = false;
    }

    public override void Update(float deltaTime)
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

    public override string GetName()
    {
        return "Time Limit";
    }
    
    public override string GetDescription()
    {
        int minutes = Mathf.FloorToInt(allowedTime / 60f);
        int seconds = Mathf.FloorToInt(allowedTime % 60f);
        
        return $"Allotted Time: {minutes:00}:{seconds:00}";
    }
}