using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Plays the game by itself in play mode: random valid swaps on the current level, a range of levels or Survival,
/// closing tutorial cards on the way. Logs a line per level with the result, so a long run shows which levels
/// get stuck, throw errors or can't be won.
/// </summary>
public class Match3BotWindow : EditorWindow
{
    private enum BotMode { CurrentLevel, LevelRange, Survival }

    private class LevelReport
    {
        public string level;
        public string result;
        public int moves;
        public float seconds;
        public string goals;
        public string note;
    }

    private const double SettleSeconds = 0.25;
    private const double ResultScreenSeconds = 3;

    private BotMode _mode = BotMode.LevelRange;
    private int _fromLevel = 1;
    private int _toLevel = 50;
    private int _runsPerLevel = 1;
    private float _moveInterval = 0.4f;
    private float _speed = 2f;
    private float _stuckSeconds = 20f;
    private bool _stopOnError = true;

    private bool _running;
    private bool _waitingForLevel;
    private int _levelIndex;
    private int _runIndex;
    private int _moves;
    private double _levelStartTime;
    private double _lastMoveTime;
    private double _lastProgressTime;
    private double _loadRequestTime;
    private double _settledSince = -1;
    private double _nextLoadAt = -1;
    private string _pendingError;
    private Match3GameManager _subscribedTo;
    private string _endResult;
    private readonly List<LevelReport> _reports = new List<LevelReport>();
    private Vector2 _scroll;
    private readonly System.Random _random = new System.Random();

    [MenuItem("ElectroGrid/Match3 Bot")]
    private static void Open() => GetWindow<Match3BotWindow>("Match3 Bot");

    private void OnEnable()
    {
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        Application.logMessageReceived += OnLog;
    }

    private void OnDisable()
    {
        EditorApplication.update -= Tick;
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        Application.logMessageReceived -= OnLog;
        StopBot();
    }

    private void OnGUI()
    {
        EditorGUILayout.HelpBox("Enter play mode first. The bot makes random valid swaps, closes tutorial cards and logs one line per level.", MessageType.Info);

        using (new EditorGUI.DisabledScope(_running))
        {
            _mode = (BotMode)EditorGUILayout.EnumPopup("Mode", _mode);
            if (_mode == BotMode.LevelRange)
            {
                int count = GameManager.Instance ? GameManager.Instance.Match3Levels.Length : 50;
                _fromLevel = EditorGUILayout.IntSlider("From level", _fromLevel, 1, Mathf.Max(1, count));
                _toLevel = EditorGUILayout.IntSlider("To level", Mathf.Max(_toLevel, _fromLevel), _fromLevel, Mathf.Max(1, count));
            }
            if (_mode != BotMode.CurrentLevel) _runsPerLevel = EditorGUILayout.IntSlider("Runs per level", _runsPerLevel, 1, 20);
        }

        _moveInterval = EditorGUILayout.Slider("Seconds between moves", _moveInterval, 0f, 3f);
        _speed = EditorGUILayout.Slider("Game speed", _speed, 0.25f, 8f);
        _stuckSeconds = EditorGUILayout.Slider("Stuck after (s)", _stuckSeconds, 5f, 120f);
        _stopOnError = EditorGUILayout.Toggle("Stop on error", _stopOnError);

        EditorGUILayout.Space();
        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying || _running))
            {
                if (GUILayout.Button("Start", GUILayout.Height(28))) StartBot();
            }
            using (new EditorGUI.DisabledScope(!_running))
            {
                if (GUILayout.Button("Stop", GUILayout.Height(28))) StopBot();
            }
        }

        if (_running) EditorGUILayout.LabelField(StatusLine(), EditorStyles.miniBoldLabel);

        EditorGUILayout.Space();
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField($"Results ({_reports.Count})", EditorStyles.boldLabel);
            if (GUILayout.Button("Copy", GUILayout.Width(60))) EditorGUIUtility.systemCopyBuffer = ReportText();
            if (GUILayout.Button("Clear", GUILayout.Width(60))) _reports.Clear();
        }

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        foreach (var report in _reports)
        {
            var style = new GUIStyle(EditorStyles.label) { richText = true, wordWrap = true };
            EditorGUILayout.LabelField(Line(report, true), style);
        }
        EditorGUILayout.EndScrollView();
    }

    private void StartBot()
    {
        _running = true;
        _levelIndex = _fromLevel - 1;
        _runIndex = 0;
        _pendingError = null;

        if (_mode == BotMode.CurrentLevel)
        {
            BeginLevel();
        }
        else
        {
            LoadNext();
        }
    }

    private void StopBot()
    {
        if (_running && EditorApplication.isPlaying) Time.timeScale = 1f;
        _running = false;
        _waitingForLevel = false;
        _nextLoadAt = -1;
        Unsubscribe();
        Repaint();
    }

    private void OnPlayModeChanged(PlayModeStateChange change)
    {
        if (change == PlayModeStateChange.ExitingPlayMode) StopBot();
    }

    private void OnLog(string message, string stackTrace, LogType type)
    {
        if (!_running || (type != LogType.Error && type != LogType.Exception)) return;
        _pendingError ??= message.Split('\n')[0];
    }

    private void LoadNext()
    {
        var gameManager = GameManager.Instance;
        if (!gameManager)
        {
            StopBot();
            return;
        }

        if (_mode == BotMode.Survival)
        {
            gameManager.SelectSurvival();
        }
        else
        {
            if (_levelIndex >= Mathf.Min(_toLevel, gameManager.Match3Levels.Length))
            {
                StopBot();
                return;
            }
            gameManager.SelectMatch3Level(gameManager.Match3Levels[_levelIndex]);
        }

        Unsubscribe();
        _waitingForLevel = true;
        _loadRequestTime = EditorApplication.timeSinceStartup;
        gameManager.Match3Scene.LoadScene();
    }

    private void BeginLevel()
    {
        var match3 = Match3GameManager.Instance;
        if (!match3)
        {
            StopBot();
            return;
        }

        Unsubscribe();
        _subscribedTo = match3;
        match3.LevelComplete += OnLevelComplete;
        match3.LevelFailed += OnLevelFailed;

        _waitingForLevel = false;
        _endResult = null;
        _pendingError = null;
        _moves = 0;
        _levelStartTime = EditorApplication.timeSinceStartup;
        _lastMoveTime = _levelStartTime;
        _lastProgressTime = _levelStartTime;
    }

    private void Unsubscribe()
    {
        if (!_subscribedTo) return;
        _subscribedTo.LevelComplete -= OnLevelComplete;
        _subscribedTo.LevelFailed -= OnLevelFailed;
        _subscribedTo = null;
    }

    private void OnLevelComplete(Match3LevelData data) => _endResult ??= "Won";
    private void OnLevelFailed(Match3LevelData data) => _endResult ??= "Lost";

    private void Tick()
    {
        if (!_running || !EditorApplication.isPlaying) return;

        double now = EditorApplication.timeSinceStartup;
        if (_nextLoadAt >= 0)
        {
            if (now < _nextLoadAt) return;
            _nextLoadAt = -1;
            _pendingError = null;
            LoadNext();
            return;
        }

        if (_waitingForLevel)
        {
            var match3 = Match3GameManager.Instance;
            if (match3 && match3.CurrentLevelData != null && now - _loadRequestTime > 1.5) BeginLevel();
            else if (now - _loadRequestTime > 30) Finish("Stuck", "level never loaded");
            return;
        }

        var manager = Match3GameManager.Instance;
        if (!manager)
        {
            if (_mode == BotMode.CurrentLevel) StopBot();
            return;
        }

        Time.timeScale = _speed;

        if (_pendingError != null)
        {
            string error = _pendingError;
            _pendingError = null;
            Finish("Error", error);
            if (_stopOnError) StopBot();
            return;
        }

        if (_endResult != null)
        {
            Finish(_endResult, null);
            return;
        }

        if (CloseOpenTutorial()) return;

        var play = manager.PlayHandler;
        if (!play || !play.CanInteract)
        {
            _settledSince = -1;
            if (now - _lastProgressTime > _stuckSeconds) Finish("Stuck", "board never settled");
            return;
        }

        var data = manager.CurrentLevelData;
        if (data != null && (data.IsObjectivesComplete() || data.IsAnyLoseConditionMet()))
        {
            if (now - _lastProgressTime > _stuckSeconds) Finish("Stuck", "goal met but the level never ended");
            return;
        }

        if (_settledSince < 0) _settledSince = now;
        if (now - _settledSince < SettleSeconds || now - _lastMoveTime < _moveInterval) return;

        if (TryMove(manager))
        {
            _moves++;
            _lastMoveTime = now;
            _settledSince = -1;
            _lastProgressTime = now;
        }
        else if (now - _lastProgressTime > _stuckSeconds)
        {
            Finish("Stuck", "no valid move found");
        }

        Repaint();
    }

    private bool CloseOpenTutorial()
    {
        var window = Object.FindAnyObjectByType<InformationWindowUI>();
        if (!window) return false;

        var group = window.GetComponent<CanvasGroup>();
        if (!group || !group.blocksRaycasts || !group.interactable) return false;

        var back = (UnityEngine.UI.Button)typeof(InformationWindowUI)
            .GetField("backButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
            .GetValue(window);
        if (!back) return false;

        back.onClick.Invoke();
        _lastProgressTime = EditorApplication.timeSinceStartup;
        return true;
    }

    private bool TryMove(Match3GameManager manager)
    {
        var grid = manager.GridHandler;
        var play = manager.PlayHandler;
        var directions = new[] { Vector2Int.up, Vector2Int.right };
        var tiles = grid.Tiles.Values.OrderBy(_ => _random.Next()).ToList();

        foreach (var tile in tiles)
        {
            if (tile.CurrentMatch3Object is not Match3SwappableObject first) continue;

            foreach (var direction in directions)
            {
                var other = grid.GetTile(tile.GridPosition + direction);
                if (!grid.IsValidTile(other) || other.CurrentMatch3Object is not Match3SwappableObject second) continue;

                bool matches = (first.ItemData && play.WouldCreateMatch(other.GridPosition, first.ItemData, grid.GridShape, tile.GridPosition))
                               || (second.ItemData && play.WouldCreateMatch(tile.GridPosition, second.ItemData, grid.GridShape, other.GridPosition))
                               || first is Match3HelperObject || second is Match3HelperObject;
                if (!matches) continue;

                manager.StartCoroutine(manager.RunGameLogic(tile.GridPosition, other.GridPosition));
                return true;
            }
        }

        return false;
    }

    private void Finish(string result, string note)
    {
        var manager = Match3GameManager.Instance;
        var data = manager ? manager.CurrentLevelData : null;
        _reports.Add(new LevelReport
        {
            level = _mode == BotMode.Survival ? $"Survival (score {data?.Survival?.Score ?? 0})" : (data?.Level ? data.Level.name : "?"),
            result = result,
            moves = _moves,
            seconds = (float)(EditorApplication.timeSinceStartup - _levelStartTime),
            goals = Goals(data),
            note = note
        });
        Debug.Log($"[Match3 Bot] {Line(_reports[_reports.Count - 1], false)}");

        Unsubscribe();
        _endResult = null;

        if (!_running) return;
        if (_mode == BotMode.CurrentLevel)
        {
            StopBot();
            return;
        }

        _runIndex++;
        if (_runIndex >= _runsPerLevel)
        {
            _runIndex = 0;
            _levelIndex++;
        }
        _nextLoadAt = EditorApplication.timeSinceStartup + ResultScreenSeconds;
    }

    private static string Goals(Match3LevelData data)
    {
        if (data == null) return "";
        return string.Join(", ", data.CurrentObjectives.Where(o => o != null).Select(o =>
        {
            var (current, total) = o.GetProgress();
            return $"{o.GetType().Name.Replace("Objective", "")} {current}/{total}";
        }));
    }

    private string StatusLine()
    {
        if (_nextLoadAt >= 0) return "Waiting on the result screen...";
        if (_waitingForLevel) return "Loading the next level...";
        var data = Match3GameManager.Instance ? Match3GameManager.Instance.CurrentLevelData : null;
        string level = _mode == BotMode.Survival ? "Survival" : data?.Level ? data.Level.name : "?";
        return $"{level}: {_moves} moves, {Goals(data)}";
    }

    private static string Line(LevelReport report, bool rich)
    {
        string color = report.result switch { "Won" => "#6fdc6f", "Lost" => "#e0b040", _ => "#ff6060" };
        string result = rich ? $"<color={color}><b>{report.result}</b></color>" : report.result;
        string note = string.IsNullOrEmpty(report.note) ? "" : $" ({report.note})";
        return $"{report.level}: {result}{note}, {report.moves} moves, {report.seconds:0}s, {report.goals}";
    }

    private string ReportText()
    {
        var builder = new StringBuilder();
        foreach (var report in _reports) builder.AppendLine(Line(report, false));
        return builder.ToString();
    }
}
