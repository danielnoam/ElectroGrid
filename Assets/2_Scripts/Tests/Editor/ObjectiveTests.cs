using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class ObjectiveTests
{
    private readonly List<Object> _created = new List<Object>();
    private Match3TestBoard _board;

    [TearDown]
    public void TearDown()
    {
        _board?.Dispose();
        _board = null;

        foreach (var obj in _created)
        {
            if (obj) Object.DestroyImmediate(obj);
        }
        _created.Clear();
    }

    private static T Objective<T>(int requiredAmount) where T : Match3Objective, new()
    {
        var objective = new T();
        TestUtils.SetField(objective, "requiredAmount", requiredAmount);
        objective.Setup();
        return objective;
    }

    private static List<Match3Tile> Tiles(int count) => new List<Match3Tile>(new Match3Tile[count]);

    private T Component<T>() where T : Component
    {
        var go = new GameObject(typeof(T).Name);
        _created.Add(go);
        return go.AddComponent<T>();
    }

    #region Objectives

    [Test]
    public void GetMatches_CompletesWhenPiecesReachRequirement()
    {
        var objective = Objective<GetMatches>(5);
        int progressEvents = 0, completeEvents = 0;
        objective.ProgressChanged += () => progressEvents++;
        objective.Completed += () => completeEvents++;

        objective.OnMatchMade(Tiles(3));
        Assert.That(objective.IsCompleted, Is.False);
        Assert.That(objective.GetProgress(), Is.EqualTo((3, 5)));
        Assert.That(progressEvents, Is.EqualTo(1));

        objective.OnMatchMade(Tiles(3));
        Assert.That(objective.IsCompleted, Is.True);
        Assert.That(completeEvents, Is.EqualTo(1));
    }

    [Test]
    public void GetMatches_EmptyOrNullMatch_ChangesNothing()
    {
        var objective = Objective<GetMatches>(3);

        objective.OnMatchMade(null);
        objective.OnMatchMade(Tiles(0));

        Assert.That(objective.GetProgress(), Is.EqualTo((0, 3)));
    }

    [Test]
    public void GetSpecificItemMatches_CountsOnlyTheTargetItem()
    {
        _board = new Match3TestBoard("AABBA");
        var objective = Objective<GetSpecificItemMatches>(3);
        TestUtils.SetField(objective, "targetItem", _board.Item('A'));

        var matched = new List<Match3Tile>();
        for (int x = 0; x < 5; x++) matched.Add(_board.Tile(x, 0));

        objective.OnMatchMade(matched);

        Assert.That(objective.GetProgress(), Is.EqualTo((3, 3)));
        Assert.That(objective.IsCompleted, Is.True);
    }

    [Test]
    public void GetSpecificItemMatches_NoTarget_CountsNothing()
    {
        _board = new Match3TestBoard("AAA");
        var objective = Objective<GetSpecificItemMatches>(1);

        objective.OnMatchMade(new List<Match3Tile> { _board.Tile(0, 0) });

        Assert.That(objective.GetProgress(), Is.EqualTo((0, 1)));
    }

    [Test]
    public void DestroyObstacles_CountsOnlyObstaclesOnATile()
    {
        var objective = Objective<DestroyObstaclesObjective>(2);
        var obstacle = Component<Match3ObstacleObject>();

        objective.OnObstacleBreak(obstacle);
        Assert.That(objective.GetProgress(), Is.EqualTo((0, 2)), "an obstacle without a tile is not counted");

        TestUtils.SetField(obstacle, "_currentTile", Component<Match3Tile>());
        objective.OnObstacleBreak(obstacle);
        objective.OnObstacleBreak(null);
        Assert.That(objective.GetProgress(), Is.EqualTo((1, 2)));

        objective.OnObstacleBreak(obstacle);
        Assert.That(objective.IsCompleted, Is.True);
    }

    [Test]
    public void ReachBottom_CompletesAfterRequiredAmount()
    {
        var objective = Objective<ReachBottomObjective>(2);
        var bottom = Component<Match3BottomObject>();

        objective.OnBottomObjectReached(bottom);
        objective.OnBottomObjectReached(null);
        Assert.That(objective.IsCompleted, Is.False);

        objective.OnBottomObjectReached(bottom);
        Assert.That(objective.IsCompleted, Is.True);
    }

    [Test]
    public void Objectives_IgnoreEventsForOtherObjectiveTypes()
    {
        var matches = Objective<GetMatches>(1);
        var obstacles = Objective<DestroyObstaclesObjective>(1);

        matches.OnBottomObjectReached(Component<Match3BottomObject>());
        obstacles.OnMatchMade(Tiles(5));

        Assert.That(matches.GetProgress().Item1, Is.Zero);
        Assert.That(obstacles.GetProgress().Item1, Is.Zero);
    }

    [Test]
    public void Clone_DoesNotShareProgressOrEvents()
    {
        var authored = Objective<GetMatches>(3);
        int authoredEvents = 0;
        authored.ProgressChanged += () => authoredEvents++;

        var copy = authored.Clone();
        copy.Setup();
        copy.OnMatchMade(Tiles(2));

        Assert.That(copy.GetProgress(), Is.EqualTo((2, 3)));
        Assert.That(authored.GetProgress(), Is.EqualTo((0, 3)));
        Assert.That(authoredEvents, Is.Zero);
    }

    #endregion

    #region Lose conditions

    [Test]
    public void MoveLimit_MetOnLastMove()
    {
        var condition = new MoveLimit();
        TestUtils.SetField(condition, "allowedMoves", 2);
        condition.Setup();
        int metEvents = 0;
        condition.ConditionMet += () => metEvents++;

        condition.OnMoveMade();
        Assert.That(condition.IsConditionMet, Is.False);
        Assert.That(condition.GetProgress(), Is.EqualTo((1, 2)));

        condition.OnMoveMade();
        condition.OnMoveMade();
        Assert.That(condition.IsConditionMet, Is.True);
        Assert.That(metEvents, Is.EqualTo(1));
    }

    [Test]
    public void MoveLimit_AddMoves_CapsAtAllowed()
    {
        var condition = new MoveLimit();
        TestUtils.SetField(condition, "allowedMoves", 5);
        condition.Setup();

        condition.OnMoveMade();
        condition.AddMoves(3);

        Assert.That(condition.GetProgress(), Is.EqualTo((5, 5)));
    }

    [Test]
    public void TimeLimit_MetWhenTimeRunsOut_AndClampsToZero()
    {
        var condition = new TimeLimit();
        TestUtils.SetField(condition, "allowedTime", 10f);
        condition.Setup();
        int metEvents = 0;
        condition.ConditionMet += () => metEvents++;

        condition.Update(9.5f);
        Assert.That(condition.IsConditionMet, Is.False);

        condition.Update(5f);
        condition.Update(5f);
        Assert.That(condition.IsConditionMet, Is.True);
        Assert.That(condition.GetProgress(), Is.EqualTo((0, 10)));
        Assert.That(metEvents, Is.EqualTo(1));
    }

    [Test]
    public void TimeLimit_ProgressChangedOncePerWholeSecond()
    {
        var condition = new TimeLimit();
        TestUtils.SetField(condition, "allowedTime", 10f);
        condition.Setup();
        int progressEvents = 0;
        condition.ProgressChanged += () => progressEvents++;

        // 10 down to 7.5 crosses 9, 8 and 7
        for (int i = 0; i < 10; i++) condition.Update(0.25f);

        Assert.That(progressEvents, Is.EqualTo(3));
    }

    [Test]
    public void TimeLimit_AddTime_CapsAtAllowed()
    {
        var condition = new TimeLimit();
        TestUtils.SetField(condition, "allowedTime", 15f);
        condition.Setup();

        condition.Update(2f);
        condition.AddTime(5f);

        Assert.That(condition.GetProgress(), Is.EqualTo((15, 15)));
    }

    #endregion

    #region Level data

    private Match3LevelData LevelData(IEnumerable<Match3Objective> objectives, IEnumerable<Match3LoseCondition> conditions)
    {
        var level = ScriptableObject.CreateInstance<SOMatch3Level>();
        _created.Add(level);
        level.Objectives.AddRange(objectives);
        level.LoseConditions.AddRange(conditions);
        return new Match3LevelData(level);
    }

    [Test]
    public void LevelData_PlayingNeverMutatesTheLevelAsset()
    {
        var authored = Objective<GetMatches>(3);
        var data = LevelData(new Match3Objective[] { authored }, new Match3LoseCondition[0]);

        data.OnMatchesMade(Tiles(3));

        Assert.That(data.IsObjectivesComplete(), Is.True);
        Assert.That(authored.IsCompleted, Is.False);
        Assert.That(authored.GetProgress().Item1, Is.Zero);
    }

    [Test]
    public void LevelData_AllObjectivesMustComplete()
    {
        var data = LevelData(
            new Match3Objective[] { Objective<GetMatches>(3), Objective<ReachBottomObjective>(1), null },
            new Match3LoseCondition[0]);

        data.OnMatchesMade(Tiles(3));
        Assert.That(data.IsObjectivesComplete(), Is.False);

        data.OnBottomObjectReached(Component<Match3BottomObject>());
        Assert.That(data.IsObjectivesComplete(), Is.True);
        Assert.That(data.PiecesCleared, Is.EqualTo(3));
        Assert.That(data.BottomObjectsReached, Is.EqualTo(1));
    }

    [Test]
    public void LevelData_NoObjectives_IsNeverComplete()
    {
        var data = LevelData(new Match3Objective[] { null }, new Match3LoseCondition[0]);

        Assert.That(data.IsObjectivesComplete(), Is.False);
    }

    [Test]
    public void LevelData_MovesCountDownTheMoveLimit()
    {
        var moves = new MoveLimit();
        TestUtils.SetField(moves, "allowedMoves", 2);
        var data = LevelData(new Match3Objective[0], new Match3LoseCondition[] { moves });

        data.OnMoveMade();
        Assert.That(data.IsAnyLoseConditionMet(), Is.False);

        data.OnMoveMade();
        Assert.That(data.IsAnyLoseConditionMet(), Is.True);
        Assert.That(data.MovesMade, Is.EqualTo(2));
    }

    [Test]
    public void LevelData_HelperDestroyed_RefundsMovesAndTime()
    {
        var moves = new MoveLimit();
        TestUtils.SetField(moves, "allowedMoves", 10);
        var time = new TimeLimit();
        TestUtils.SetField(time, "allowedTime", 30f);
        var data = LevelData(new Match3Objective[0], new Match3LoseCondition[] { moves, time });

        for (int i = 0; i < 5; i++) data.OnMoveMade();
        data.CurrentLoseConditions[1].Update(10f);

        data.OnHelperObjectDestroyed();

        Assert.That(data.CurrentLoseConditions[0].GetProgress().Item1, Is.EqualTo(8));
        Assert.That(data.CurrentLoseConditions[1].GetProgress().Item1, Is.EqualTo(25));
    }

    [Test]
    public void LevelData_BelowHalf_WhenAConditionIsLessThanHalfLeft()
    {
        var moves = new MoveLimit();
        TestUtils.SetField(moves, "allowedMoves", 10);
        var data = LevelData(new Match3Objective[0], new Match3LoseCondition[] { moves });

        for (int i = 0; i < 5; i++) data.OnMoveMade();
        Assert.That(data.IsAnyLoseConditionBelowHalf(), Is.False);

        data.OnMoveMade();
        Assert.That(data.IsAnyLoseConditionBelowHalf(), Is.True);
    }

    #endregion
}
