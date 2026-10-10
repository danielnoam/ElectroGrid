using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

[Serializable, Browsable(false)]
public class SurvivalScoreObjective : Match3Objective
{
    private int _score;

    public override bool AllowOnlyOneObjectiveOfThisType => true;
    public override bool ShowsTotal => false;

    public SurvivalScoreObjective()
    {
    }

    public SurvivalScoreObjective(Sprite sprite)
    {
        objectiveSprite = sprite;
    }

    public void SetScore(int score)
    {
        _score = score;
        InvokeProgressChanged();
    }

    public override void Setup()
    {
        _score = 0;
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
    }

    public override string GetName()
    {
        return "Survival Score";
    }

    public override string GetDescription()
    {
        return L10n.Get("survival.description");
    }

    public override string GetRequirementText()
    {
        return L10n.Get("survival.score");
    }

    public override (int, int) GetProgress()
    {
        return (_score, 0);
    }
}

[Serializable, Browsable(false)]
public class SurvivalTimeLimit : Match3LoseCondition
{
    private float _startTime = 60f;
    private float _timeRemaining;

    public float TimeRemaining => _timeRemaining;

    public SurvivalTimeLimit()
    {
    }

    public SurvivalTimeLimit(float startTime, Sprite sprite)
    {
        _startTime = startTime;
        conditionSprite = sprite;
    }

    public void AddTime(float amount)
    {
        if (amount <= 0f) return;

        _timeRemaining += amount;
        if (_timeRemaining > 0f) _conditionMet = false;
        InvokeProgressChanged();
    }

    public override void Setup()
    {
        _timeRemaining = _startTime;
        _conditionMet = false;
    }

    public override void Update(float deltaTime)
    {
        if (_conditionMet) return;

        int previousTimeInt = Mathf.FloorToInt(_timeRemaining);
        _timeRemaining -= deltaTime;
        if (Mathf.FloorToInt(_timeRemaining) != previousTimeInt) InvokeProgressChanged();

        if (_timeRemaining <= 0f)
        {
            _timeRemaining = 0f;
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
        return (Mathf.FloorToInt(_timeRemaining), Mathf.FloorToInt(_startTime));
    }

    public override string GetName()
    {
        return "Survival Time";
    }

    public override string GetDescription()
    {
        return L10n.Get("survival.description");
    }
}
