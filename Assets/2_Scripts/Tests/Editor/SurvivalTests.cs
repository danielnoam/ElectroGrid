using NUnit.Framework;
using UnityEngine;

public class SurvivalTests
{
    private SOSurvivalMode _mode;
    private Match3SurvivalRun _run;

    [SetUp]
    public void SetUp()
    {
        _mode = ScriptableObject.CreateInstance<SOSurvivalMode>();
        _mode.Time.startTime = 60f;
        _mode.Time.gainByMinute = AnimationCurve.Constant(0f, 10f, 1f);
        _run = new Match3SurvivalRun(_mode);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_mode);
    }

    [Test]
    public void MultiplierIsTheComboCountByDefault()
    {
        var settings = new Match3ScoreSettings();

        Assert.AreEqual(1f, Match3SurvivalRun.ComboMultiplier(settings, 0));
        Assert.AreEqual(1f, Match3SurvivalRun.ComboMultiplier(settings, 1));
        Assert.AreEqual(3f, Match3SurvivalRun.ComboMultiplier(settings, 3));
    }

    [Test]
    public void MultiplierStopsAtTheCap()
    {
        var settings = new Match3ScoreSettings { comboMultiplierPerStep = 1f, maxComboMultiplier = 4f };

        Assert.AreEqual(4f, Match3SurvivalRun.ComboMultiplier(settings, 20));
    }

    [Test]
    public void EachPieceScoresAndBigMatchesEarnABonus()
    {
        var settings = new Match3ScoreSettings { pointsPerPiece = 10, pointsPerExtraPiece = 20 };

        Assert.AreEqual(30, Match3SurvivalRun.MatchPoints(settings, new[] { 3 }, 3));
        Assert.AreEqual(50 + 40, Match3SurvivalRun.MatchPoints(settings, new[] { 5 }, 3));
    }

    [Test]
    public void TwoSeparateMatchesAreNotOneBigMatch()
    {
        var settings = new Match3ScoreSettings { pointsPerPiece = 10, pointsPerExtraPiece = 20 };

        Assert.AreEqual(60, Match3SurvivalRun.MatchPoints(settings, new[] { 3, 3 }, 3));
    }

    [Test]
    public void BonusesAreMultipliedByTheCombo()
    {
        _mode.Score.plugBonus = 500;

        _run.OnPlugReached(3);

        Assert.AreEqual(1500, _run.Score);
    }

    [Test]
    public void LineBreakPiecesScoreLikeMatchedPieces()
    {
        _mode.Score.pointsPerPiece = 10;

        _run.OnLineBreakPieces(8, 2);

        Assert.AreEqual(160, _run.Score);
    }

    [Test]
    public void TimeGainsAreNotCappedAtTheStartingTime()
    {
        _mode.Time.plusTime = 5f;

        _run.OnPlusDestroyed(1);

        Assert.AreEqual(65f, _run.Timer.TimeRemaining, 1e-4f);
    }

    [Test]
    public void TimeGainsShrinkAsTheRunGoesOn()
    {
        _mode.Time.fullComboTime = 10f;
        _mode.Time.gainByMinute = AnimationCurve.Linear(0f, 1f, 2f, 0.5f);

        _run.Tick(120f);
        _run.OnComboFilled();

        Assert.AreEqual(65f, _run.Timer.TimeRemaining, 1e-3f);
    }

    [Test]
    public void TheRunEndsWhenTheClockRunsOut()
    {
        _run.Timer.Update(59f);
        Assert.IsFalse(_run.Timer.IsConditionMet);

        _run.Timer.Update(2f);
        Assert.IsTrue(_run.Timer.IsConditionMet);
    }

    [Test]
    public void SurvivalLevelDataNeverCompletesAndShowsScoreAndTime()
    {
        var level = ScriptableObject.CreateInstance<SOMatch3Level>();
        try
        {
            var data = new Match3LevelData(level, _mode);

            Assert.IsTrue(data.IsSurvival);
            Assert.IsFalse(data.IsObjectivesComplete());
            Assert.AreEqual(1, data.CurrentObjectives.Count);
            Assert.AreEqual(1, data.CurrentLoseConditions.Count);
            Assert.IsInstanceOf<SurvivalTimeLimit>(data.CurrentLoseConditions[0]);
        }
        finally
        {
            Object.DestroyImmediate(level);
        }
    }

    [Test]
    public void BestCombo()
    {
        _run.OnComboStep(2);
        _run.OnComboStep(5);
        _run.OnComboStep(1);

        Assert.AreEqual(5, _run.BestCombo);
    }

    [Test]
    public void PiecesUnlockAtTheirMilestone()
    {
        var piece = ScriptableObject.CreateInstance<SOItemData>();
        try
        {
            _mode.Score.plusBonus = 100;
            SetMilestones(new SurvivalMilestone { score = 200, unlock = SurvivalUnlock.Piece, piece = piece });
            var run = new Match3SurvivalRun(_mode);
            SurvivalMilestone reached = null;
            run.MilestoneReached += m => reached = m;

            run.OnPlusDestroyed(1);
            Assert.AreEqual(0, run.UnlockedPieces.Count);

            run.OnPlusDestroyed(1);
            Assert.AreEqual(1, run.UnlockedPieces.Count);
            Assert.AreSame(piece, run.UnlockedPieces[0]);
            Assert.AreSame(piece, reached.piece);

            run.OnPlusDestroyed(1);
            Assert.AreEqual(1, run.UnlockedPieces.Count);
        }
        finally
        {
            Object.DestroyImmediate(piece);
        }
    }

    [Test]
    public void VirusesOnlySpawnAfterTheirMilestone()
    {
        using var board = new Match3TestBoard("AB", "BA");
        _mode.Obstacles.plugChance = 0f;
        _mode.Obstacles.starChance = 0f;
        _mode.Obstacles.fuseChance = 0f;
        _mode.Obstacles.virusChance = 100f;
        SetMilestones(new SurvivalMilestone { score = 100, unlock = SurvivalUnlock.Viruses });
        _run = new Match3SurvivalRun(_mode);
        _mode.Score.plusBonus = 100;
        var tiles = new System.Collections.Generic.List<Match3Tile>(board.GridHandler.Tiles.Values);

        Assert.AreEqual(Match3TileObjectType.Matchable, _run.PickBonus(tiles, board.GridHandler, 0, 0).type);

        _run.OnPlusDestroyed(1);
        Assert.AreEqual(Match3TileObjectType.Virus, _run.PickBonus(tiles, board.GridHandler, 0, 0).type);
        Assert.AreEqual(Match3TileObjectType.Matchable, _run.PickBonus(tiles, board.GridHandler, 0, 0, _mode.Obstacles.maxViruses).type);
    }

    [Test]
    public void FusesOnlySpawnAfterTheirMilestone()
    {
        using var board = new Match3TestBoard("AB", "BA");
        _mode.Obstacles.plugChance = 0f;
        _mode.Obstacles.starChance = 0f;
        _mode.Obstacles.virusChance = 0f;
        _mode.Obstacles.fuseChance = 100f;
        SetMilestones(new SurvivalMilestone { score = 200, unlock = SurvivalUnlock.Fuses });
        _run = new Match3SurvivalRun(_mode);
        _mode.Score.plusBonus = 100;
        var tiles = new System.Collections.Generic.List<Match3Tile>(board.GridHandler.Tiles.Values);

        _run.OnPlusDestroyed(1);
        Assert.AreEqual(Match3TileObjectType.Matchable, _run.PickBonus(tiles, board.GridHandler, 0, 0).type);

        _run.OnPlusDestroyed(1);
        Assert.AreEqual(Match3TileObjectType.Fuse, _run.PickBonus(tiles, board.GridHandler, 0, 0).type);
    }

    [Test]
    public void StarsOnlySpawnAfterTheirMilestone()
    {
        using var board = new Match3TestBoard("AB", "BA");
        _mode.Obstacles.plugChance = 0f;
        _mode.Obstacles.virusChance = 0f;
        _mode.Obstacles.fuseChance = 0f;
        _mode.Obstacles.starChance = 100f;
        _mode.Score.plusBonus = 100;
        var tiles = new System.Collections.Generic.List<Match3Tile>(board.GridHandler.Tiles.Values);

        Assert.AreEqual(Match3TileObjectType.Matchable, _run.PickBonus(tiles, board.GridHandler, 0, 0).type);

        SetMilestones(new SurvivalMilestone { score = 100, unlock = SurvivalUnlock.Stars });
        _run = new Match3SurvivalRun(_mode);
        Assert.AreEqual(Match3TileObjectType.Matchable, _run.PickBonus(tiles, board.GridHandler, 0, 0).type);

        _run.OnPlusDestroyed(1);
        Assert.AreEqual(Match3TileObjectType.Obstacle, _run.PickBonus(tiles, board.GridHandler, 0, 0).type);
    }

    private void SetMilestones(params SurvivalMilestone[] milestones)
    {
        TestUtils.SetField(_mode, "milestones", new System.Collections.Generic.List<SurvivalMilestone>(milestones));
    }
}
