using System;
using UnityEngine;

[Serializable]
public abstract class Match3LoseCondition
{
    [SerializeField] protected Sprite conditionSprite;
    
    protected bool _conditionMet;
    public Sprite ConditionSprite => conditionSprite;
    public bool IsConditionMet => _conditionMet;

    public abstract void Setup();
    public abstract void Update(float deltaTime);
    public abstract void OnMoveMade();
    public abstract string GetRequirementText();
    public abstract (int, int) GetProgress();
    public abstract string GetName();
    public abstract string GetDescription();
    
    
    public event Action ProgressChanged;
    public event Action ConditionMet;
    
    
    protected void InvokeProgressChanged()
    {
        ProgressChanged?.Invoke();
    }
    
    protected void InvokeConditionMet()
    {
        ConditionMet?.Invoke();
    }

    /// <summary>Runtime copy of the authored condition, so playing a level never mutates the level asset.</summary>
    public Match3LoseCondition Clone()
    {
        var clone = (Match3LoseCondition)MemberwiseClone();
        clone.ProgressChanged = null;
        clone.ConditionMet = null;
        return clone;
    }
}

[Serializable]
public class MoveLimit : Match3LoseCondition
{
    [SerializeField, Min(1)] private int allowedMoves = 10;
    private int _movesRemaining;
    
    public void AddMoves(int amount)
    {
        _movesRemaining += amount;
        if (_movesRemaining > allowedMoves)
        {
            _movesRemaining = allowedMoves;
        }

        // The last move is spent before its matches resolve, so a Plus or a full combo bar it sets off has to be able
        // to take the loss back; the level only checks for a loss once the board has settled
        if (_movesRemaining > 0) _conditionMet = false;
        InvokeProgressChanged();
    }

    public override void Setup()
    {
        _movesRemaining = allowedMoves;
        if (FirebaseManager.Instance) _movesRemaining += FirebaseManager.Instance.GlobalMoveBonus;
        _conditionMet = false;
    }

    public override void Update(float deltaTime)
    {
    }

    public override void OnMoveMade()
    {
        if (_conditionMet) return;

        _movesRemaining--;

        if (_movesRemaining <= 0)
        {
            _conditionMet = true;
            InvokeConditionMet();
        }
        else
        {
            InvokeProgressChanged();
        }
    }

    public override string GetRequirementText()
    {
        return L10n.Get("lose.moves.requirement");
    }
    public override (int, int) GetProgress()
    {
        return (_movesRemaining, allowedMoves);
    }

    public override string GetName()
    {
        return "Moves Limit";
    }
    
    public override string GetDescription()
    {
        return L10n.Get("lose.moves.description", ("moves", allowedMoves));
    }
}

[Serializable]
public class TimeLimit : Match3LoseCondition
{
    [SerializeField, Min(10)] private float allowedTime = 15f;
    private float _timeRemaining;
    
    
    public void AddTime(float amount)
    {
        _timeRemaining += amount;
        if (_timeRemaining > allowedTime)
        {
            _timeRemaining = allowedTime;
        }

        if (_timeRemaining > 0f) _conditionMet = false;
        InvokeProgressChanged();
    }

    public override void Setup()
    {
        _timeRemaining = allowedTime;
        _conditionMet = false;
    }

    public override void Update(float deltaTime)
    {
        if (_conditionMet) return;


        int previousTimeInt = Mathf.FloorToInt(_timeRemaining);
        _timeRemaining -= deltaTime;
        int currentTimeInt = Mathf.FloorToInt(_timeRemaining);
        if (currentTimeInt != previousTimeInt)
        {
            InvokeProgressChanged();
        }

        if (_timeRemaining <= 0)
        {
            _timeRemaining = 0;
            _conditionMet = true;
            InvokeConditionMet();
        }
    }

    public override void OnMoveMade()
    {
    }

    public override string GetRequirementText()
    {
        return L10n.Get("lose.time.requirement");
    }
    
    
    public override (int, int) GetProgress()
    {
        return (Mathf.FloorToInt(_timeRemaining), Mathf.FloorToInt(allowedTime));
    }

    public override string GetName()
    {
        return "Time Limit";
    }
    
    public override string GetDescription()
    {
        int minutes = Mathf.FloorToInt(allowedTime / 60f);
        int seconds = Mathf.FloorToInt(allowedTime % 60f);
        
        return L10n.Get("lose.time.description", ("minutes", minutes), ("seconds", seconds));
    }
}