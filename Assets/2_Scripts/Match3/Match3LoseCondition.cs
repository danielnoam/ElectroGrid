using System;
using UnityEngine;

[Serializable]
public abstract class Match3LoseCondition
{
    [SerializeField] protected string conditionName;
    [SerializeField] protected Sprite conditionSprite;
    
    protected bool ConditionMet;
    
    public string ConditionName => conditionName;
    public Sprite ConditionSprite => conditionSprite;
    public bool IsConditionMet => ConditionMet;

    public abstract void SetupCondition();
    public abstract void UpdateCondition(float deltaTime);
    public abstract void OnMoveMade();
    public abstract string GetProgressText();
    public abstract float GetProgress(); 
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

    public override string GetProgressText()
    {
        return $"{_movesRemaining} Moves";
    }

    public override float GetProgress()
    {
        return 1f - Mathf.Clamp01((float)_movesRemaining / allowedMoves);
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

    public override string GetProgressText()
    {
        int minutes = Mathf.FloorToInt(_timeRemaining / 60f);
        int seconds = Mathf.FloorToInt(_timeRemaining % 60f);
        return $"{minutes:00}:{seconds:00}";
    }

    public override float GetProgress()
    {
        return 1f - Mathf.Clamp01(_timeRemaining / allowedTime);
    }
}