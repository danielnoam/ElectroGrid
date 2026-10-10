using System;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public class SaveManagerTests
{
    private string _folder;
    private GameObject _host;
    private SOMatch3Level _levelA;
    private SOMatch3Level _levelB;

    private string SavePath => Path.Combine(_folder, "save.json");

    [SetUp]
    public void SetUp()
    {
        _folder = Path.Combine(Path.GetTempPath(), "ElectroGridTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_folder);
        SaveManager.DirectoryOverride = _folder;

        _host = new GameObject("SaveManagerTests");
        _levelA = Level("Level_A");
        _levelB = Level("Level_B");
    }

    [TearDown]
    public void TearDown()
    {
        SaveManager.DirectoryOverride = null;
        Object.DestroyImmediate(_host);
        Object.DestroyImmediate(_levelA);
        Object.DestroyImmediate(_levelB);

        if (Directory.Exists(_folder)) Directory.Delete(_folder, true);
    }

    private static SOMatch3Level Level(string assetName)
    {
        var level = ScriptableObject.CreateInstance<SOMatch3Level>();
        level.name = assetName;
        return level;
    }

    /// <summary>A fresh manager reading from disk, the way the game starts up.</summary>
    private SaveManager LoadedManager()
    {
        var go = new GameObject(nameof(SaveManager));
        go.transform.SetParent(_host.transform);
        var manager = go.AddComponent<SaveManager>();
        manager.Load();
        return manager;
    }

    private static Match3LevelData Result(SOMatch3Level level, int moves, float time, int pieces)
    {
        return new Match3LevelData(level) { MovesMade = moves, TimeSpent = time, PiecesCleared = pieces };
    }

    [Test]
    public void Load_NoFile_StartsWithDefaults()
    {
        var manager = LoadedManager();

        Assert.That(manager.HighestLevelUnlocked, Is.Zero);
        Assert.That(manager.Data.levels, Is.Empty);
        Assert.That(manager.Settings.musicVolume, Is.EqualTo(1f));
        Assert.That(manager.IsLevelUnlocked(0), Is.True);
        Assert.That(manager.IsLevelUnlocked(1), Is.False);
    }

    [Test]
    public void Load_CorruptFile_FallsBackToDefaults()
    {
        File.WriteAllText(SavePath, "{ this is not json");
        LogAssert.Expect(LogType.Error, new Regex("Failed to read save file"));

        var manager = LoadedManager();

        Assert.That(manager.Data, Is.Not.Null);
        Assert.That(manager.HighestLevelUnlocked, Is.Zero);
    }

    [Test]
    public void Load_PartialFile_FillsMissingParts()
    {
        File.WriteAllText(SavePath, "{\"highestLevelUnlocked\":3}");

        var manager = LoadedManager();

        Assert.That(manager.HighestLevelUnlocked, Is.EqualTo(3));
        Assert.That(manager.Data.levels, Is.Not.Null);
        Assert.That(manager.Data.seenTutorials, Is.Not.Null);
        Assert.That(manager.Settings, Is.Not.Null);
    }

    [Test]
    public void Save_ThenLoad_RoundTrips()
    {
        var first = LoadedManager();
        first.Settings.musicVolume = 0.25f;
        first.Settings.hapticsEnabled = false;
        first.SetLastPlayedLevel(_levelA);
        first.RecordLevelCompleted(_levelA, Result(_levelA, 12, 30.5f, 40), 0);

        var second = LoadedManager();
        var record = second.GetRecord(_levelA);

        Assert.That(second.Settings.musicVolume, Is.EqualTo(0.25f));
        Assert.That(second.HapticsEnabled, Is.False);
        Assert.That(second.LastPlayedLevel, Is.EqualTo("Level_A"));
        Assert.That(second.HighestLevelUnlocked, Is.EqualTo(1));
        Assert.That(record, Is.Not.Null);
        Assert.That(record.bestMoves, Is.EqualTo(12));
        Assert.That(record.bestTime, Is.EqualTo(30.5f));
        Assert.That(record.bestPiecesCleared, Is.EqualTo(40));
    }

    [Test]
    public void Save_OverExistingFile_ReplacesItAndLeavesNoTempFile()
    {
        var manager = LoadedManager();
        manager.Save();
        manager.Settings.sfxVolume = 0.5f;
        manager.Save();

        Assert.That(File.Exists(SavePath + ".tmp"), Is.False);
        Assert.That(LoadedManager().Settings.sfxVolume, Is.EqualTo(0.5f));
    }

    [Test]
    public void RecordLevelCompleted_Replay_KeepsTheBestOfEachStat()
    {
        var manager = LoadedManager();
        manager.RecordLevelCompleted(_levelA, Result(_levelA, 10, 20f, 30), 0);

        // Fewer moves but slower and fewer pieces: only moves improves
        manager.RecordLevelCompleted(_levelA, Result(_levelA, 8, 25f, 20), 0);
        // More moves but faster and more pieces: moves stays
        manager.RecordLevelCompleted(_levelA, Result(_levelA, 15, 12f, 50), 0);

        var record = manager.GetRecord(_levelA);
        Assert.That(record.bestMoves, Is.EqualTo(8));
        Assert.That(record.bestTime, Is.EqualTo(12f));
        Assert.That(record.bestPiecesCleared, Is.EqualTo(50));
        Assert.That(manager.Data.levels.Count, Is.EqualTo(1));
    }

    [Test]
    public void RecordLevelCompleted_EarlierLevel_DoesNotLowerTheUnlock()
    {
        var manager = LoadedManager();
        manager.RecordLevelCompleted(_levelB, Result(_levelB, 5, 5f, 5), 4);
        manager.RecordLevelCompleted(_levelA, Result(_levelA, 5, 5f, 5), 0);

        Assert.That(manager.HighestLevelUnlocked, Is.EqualTo(5));
        Assert.That(manager.IsLevelUnlocked(5), Is.True);
        Assert.That(manager.IsLevelUnlocked(6), Is.False);
    }

    [Test]
    public void Records_AreKeyedByAssetName()
    {
        var manager = LoadedManager();
        manager.RecordLevelCompleted(_levelA, Result(_levelA, 5, 5f, 5), 3);

        // A different object with the same asset name, as after a domain reload or a reordered level list
        var sameName = Level("Level_A");
        try
        {
            Assert.That(manager.GetRecord(sameName), Is.Not.Null);
            Assert.That(manager.GetRecord(_levelB), Is.Null);
        }
        finally
        {
            Object.DestroyImmediate(sameName);
        }
    }

    [Test]
    public void ResetProgress_KeepsSettings()
    {
        var manager = LoadedManager();
        manager.Settings.musicVolume = 0.3f;
        manager.RecordLevelCompleted(_levelA, Result(_levelA, 5, 5f, 5), 2);
        manager.SetLastPlayedLevel(_levelA);
        bool resetRaised = false;
        manager.SaveReset += () => resetRaised = true;

        manager.ResetProgress();

        var reloaded = LoadedManager();
        Assert.That(resetRaised, Is.True);
        Assert.That(reloaded.HighestLevelUnlocked, Is.Zero);
        Assert.That(reloaded.Data.levels, Is.Empty);
        Assert.That(reloaded.LastPlayedLevel, Is.Null.Or.Empty);
        Assert.That(reloaded.Settings.musicVolume, Is.EqualTo(0.3f));
    }

    [Test]
    public void SurvivalScore_KeepsOnlyTheBest()
    {
        var manager = LoadedManager();

        Assert.That(manager.RecordSurvivalRun(500, 40f, 3), Is.True);
        Assert.That(manager.RecordSurvivalRun(300, 30f, 2), Is.False);
        Assert.That(manager.RecordSurvivalRun(800, 50f, 4), Is.True);

        var reloaded = LoadedManager();
        Assert.That(reloaded.SurvivalBestScore, Is.EqualTo(800));
        Assert.That(reloaded.Data.survivalRunsPlayed, Is.EqualTo(3));
    }

    [Test]
    public void ResetProgress_ClearsTheSurvivalBest()
    {
        var manager = LoadedManager();
        manager.RecordSurvivalRun(500, 40f, 3);

        manager.ResetProgress();

        var reloaded = LoadedManager();
        Assert.That(reloaded.SurvivalBestScore, Is.Zero);
        Assert.That(reloaded.SurvivalRuns, Is.Empty);
    }

    [Test]
    public void SurvivalRuns_KeepTheTopTenHighestFirst()
    {
        var manager = LoadedManager();
        for (int i = 1; i <= 12; i++) manager.RecordSurvivalRun(i * 100, i, i);
        manager.RecordSurvivalRun(0, 5f, 0);

        var runs = LoadedManager().SurvivalRuns;
        Assert.That(runs.Count, Is.EqualTo(SaveManager.MaxSurvivalRuns));
        Assert.That(runs[0].score, Is.EqualTo(1200));
        Assert.That(runs[runs.Count - 1].score, Is.EqualTo(300));
        Assert.That(runs[0].bestCombo, Is.EqualTo(12));
    }

    [Test]
    public void DeleteSave_RemovesTheFile()
    {
        var manager = LoadedManager();
        manager.Save();

        manager.DeleteSave();

        Assert.That(File.Exists(SavePath), Is.False);
        Assert.That(manager.Settings.musicVolume, Is.EqualTo(1f));
    }
}
