using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class LevelRecord
{
    public string levelName;
    public bool completed;
    public int bestMoves;
    public float bestTime;
    public int bestPiecesCleared;
}

[Serializable]
public class SurvivalRunRecord
{
    public int score;
    public float timePlayed;
    public int bestCombo;
    public long playedAtUtcTicks;

    public DateTime PlayedAt => new DateTime(playedAtUtcTicks, DateTimeKind.Utc).ToLocalTime();
}

[Serializable]
public class SettingsData
{
    public float musicVolume = 1f;
    public float sfxVolume = 1f;
    public bool hapticsEnabled = true;
    public bool screenShakeEnabled = true;
    public bool highFrameRate;
    public bool tiltEnabled = true;
    // Locale code such as "en". Empty follows the system language
    public string language;
}

[Serializable]
public class SaveData
{
    public int version = 1;
    public SettingsData settings = new SettingsData();
    public string lastPlayedLevel;
    public int highestLevelUnlocked;
    public List<LevelRecord> levels = new List<LevelRecord>();
    public List<string> seenTutorials = new List<string>();
    public int survivalBestScore;
    public int survivalRunsPlayed;
    public List<SurvivalRunRecord> survivalRuns = new List<SurvivalRunRecord>();
}

[DisallowMultipleComponent]
[DefaultExecutionOrder(-2000)]
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    private const int CurrentVersion = 1;
    private const string FileName = "save.json";
    public const int MaxSurvivalRuns = 10;

    private SaveData _data = new SaveData();

    public SaveData Data => _data;
    public SettingsData Settings => _data.settings;
    public string LastPlayedLevel => _data.lastPlayedLevel;
    public int HighestLevelUnlocked => _data.highestLevelUnlocked;
    public int SurvivalBestScore => _data.survivalBestScore;
    /// <summary>The best Survival runs, highest score first.</summary>
    public IReadOnlyList<SurvivalRunRecord> SurvivalRuns => _data.survivalRuns;

    public bool HapticsEnabled => _data.settings.hapticsEnabled;
    public bool ScreenShakeEnabled => _data.settings.screenShakeEnabled;

    /// <summary>Raised when progress is wiped, so menus showing unlock state can rebuild.</summary>
    public event Action SaveReset;

    /// <summary>Tests point this at a temp folder so they never touch the real save.</summary>
    internal static string DirectoryOverride;

    private static string SavePath => Path.Combine(DirectoryOverride ?? Application.persistentDataPath, FileName);
    private static string TempPath => SavePath + ".tmp";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance) return;

        var go = new GameObject(nameof(SaveManager));
        go.AddComponent<SaveManager>();
        DontDestroyOnLoad(go);
    }

    private void Awake()
    {
        if (Instance && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Load();
    }

    public bool HasSeenTutorial(SOMatch3Tutorial tutorial)
    {
        return tutorial && _data.seenTutorials.Contains(tutorial.name);
    }

    public void MarkTutorialSeen(SOMatch3Tutorial tutorial)
    {
        if (!tutorial || _data.seenTutorials.Contains(tutorial.name)) return;

        _data.seenTutorials.Add(tutorial.name);
        Save();
    }

    public bool IsLevelUnlocked(int levelIndex)
    {
        return levelIndex <= _data.highestLevelUnlocked;
    }

    public LevelRecord GetRecord(SOMatch3Level level)
    {
        if (!level) return null;

        foreach (var record in _data.levels)
        {
            if (record != null && record.levelName == level.name) return record;
        }

        return null;
    }

    public void RecordLevelCompleted(SOMatch3Level level, Match3LevelData levelData, int levelIndex)
    {
        if (!level || levelData == null) return;

        var record = GetRecord(level);
        if (record == null)
        {
            // Keyed by asset name rather than index, so reordering levels never scrambles existing records
            record = new LevelRecord { levelName = level.name };
            _data.levels.Add(record);
        }

        if (!record.completed)
        {
            record.completed = true;
            record.bestMoves = levelData.MovesMade;
            record.bestTime = levelData.TimeSpent;
            record.bestPiecesCleared = levelData.PiecesCleared;
        }
        else
        {
            if (levelData.MovesMade < record.bestMoves) record.bestMoves = levelData.MovesMade;
            if (levelData.TimeSpent < record.bestTime) record.bestTime = levelData.TimeSpent;
            if (levelData.PiecesCleared > record.bestPiecesCleared) record.bestPiecesCleared = levelData.PiecesCleared;
        }

        if (levelIndex >= 0 && levelIndex + 1 > _data.highestLevelUnlocked)
        {
            _data.highestLevelUnlocked = levelIndex + 1;
        }

        Save();
    }

    /// <summary>Counts a finished Survival run and keeps it if it makes the best runs list. True for a new best score.</summary>
    public bool RecordSurvivalRun(int score, float timePlayed, int bestCombo)
    {
        _data.survivalRunsPlayed++;

        bool newBest = score > _data.survivalBestScore;
        if (newBest) _data.survivalBestScore = score;

        if (score > 0)
        {
            _data.survivalRuns.Add(new SurvivalRunRecord
            {
                score = score,
                timePlayed = timePlayed,
                bestCombo = bestCombo,
                playedAtUtcTicks = DateTime.UtcNow.Ticks
            });
            _data.survivalRuns.Sort((a, b) => a.score != b.score ? b.score.CompareTo(a.score) : a.playedAtUtcTicks.CompareTo(b.playedAtUtcTicks));
            if (_data.survivalRuns.Count > MaxSurvivalRuns) _data.survivalRuns.RemoveRange(MaxSurvivalRuns, _data.survivalRuns.Count - MaxSurvivalRuns);
        }

        Save();
        return newBest;
    }

    public void SetLastPlayedLevel(SOMatch3Level level)
    {
        if (!level || _data.lastPlayedLevel == level.name) return;

        _data.lastPlayedLevel = level.name;
        Save();
    }

    public void Save()
    {
        try
        {
            // Write to a temp file first, an app kill part way through a direct write truncates the save
            File.WriteAllText(TempPath, JsonUtility.ToJson(_data, true));

            if (!File.Exists(SavePath))
            {
                File.Move(TempPath, SavePath);
                return;
            }

            try
            {
                File.Replace(TempPath, SavePath, null);
            }
            catch (PlatformNotSupportedException)
            {
                File.Delete(SavePath);
                File.Move(TempPath, SavePath);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to write save file: {e.Message}");
        }
    }

    internal void Load()
    {
        try
        {
            if (!File.Exists(SavePath))
            {
                _data = new SaveData();
                return;
            }

            _data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath)) ?? new SaveData();
            _data.levels ??= new List<LevelRecord>();
            _data.seenTutorials ??= new List<string>();
            _data.survivalRuns ??= new List<SurvivalRunRecord>();
            _data.settings ??= new SettingsData();

            // Schema changes go here, keyed off the loaded _data.version, before it is stamped forward
            _data.version = CurrentVersion;
        }
        catch (Exception e)
        {
            // A corrupt save must never block startup
            Debug.LogError($"Failed to read save file, starting fresh: {e.Message}");
            _data = new SaveData();
        }
    }

    /// <summary>Clears progress but keeps audio and accessibility preferences, which are not progress.</summary>
    public void ResetProgress()
    {
        _data.levels.Clear();
        _data.seenTutorials.Clear();
        _data.highestLevelUnlocked = 0;
        _data.lastPlayedLevel = null;
        _data.survivalBestScore = 0;
        _data.survivalRunsPlayed = 0;
        _data.survivalRuns.Clear();

        Save();
        SaveReset?.Invoke();
    }

    [ContextMenu("Delete Save")]
    public void DeleteSave()
    {
        try
        {
            if (File.Exists(SavePath)) File.Delete(SavePath);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to delete save file: {e.Message}");
        }

        _data = new SaveData();
        SaveReset?.Invoke();
    }
}
