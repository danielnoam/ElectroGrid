using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DNExtensions.Systems.ObjectPooling;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Localization.Settings;

/// <summary>
/// The debug and test actions behind the Match3 Debug window and the agent commands. Every action returns a short
/// line saying what happened, so a button and a command report the same thing.
/// </summary>
[InitializeOnLoad]
internal static class Match3DebugActions
{
    public const string MainScene = "Assets/1_Scenes/Main.unity";
    public const string Match3Scene = "Assets/1_Scenes/Match3.unity";

    private static readonly List<string> Errors = new List<string>();
    private static readonly System.Random Random = new System.Random();

    static Match3DebugActions()
    {
        Application.logMessageReceived -= OnLog;
        Application.logMessageReceived += OnLog;
    }

    private static void OnLog(string message, string stackTrace, LogType type)
    {
        if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;

        lock (Errors)
        {
            Errors.Add($"{type}: {message.Split('\n')[0]}");
            if (Errors.Count > 200) Errors.RemoveAt(0);
        }
    }

    private static Match3GameManager Game => EditorApplication.isPlaying ? Match3GameManager.Instance : null;

    private static string NotInLevel => EditorApplication.isPlaying ? "No level is running." : "Not in play mode.";

    #region Session

    public static string EnterPlay()
    {
        if (EditorApplication.isPlaying) return "Already playing.";

        var scene = EditorSceneManager.GetActiveScene();
        if (scene.isDirty) return $"{scene.name} has unsaved changes. Save it first.";

        if (scene.path != MainScene) EditorSceneManager.OpenScene(MainScene);
        Application.runInBackground = true;
        EditorApplication.isPlaying = true;
        return "Entering play mode from Main.";
    }

    public static string StopAndRestore()
    {
        if (EditorApplication.isPlaying)
        {
            EditorApplication.playModeStateChanged -= RestoreAfterStop;
            EditorApplication.playModeStateChanged += RestoreAfterStop;
            EditorApplication.isPlaying = false;
            return "Stopping play mode, then restoring the Match3 scene, 1080x1920 and run in background off.";
        }

        return Restore();
    }

    private static void RestoreAfterStop(PlayModeStateChange change)
    {
        if (change != PlayModeStateChange.EnteredEditMode) return;

        EditorApplication.playModeStateChanged -= RestoreAfterStop;
        Restore();
    }

    private static string Restore()
    {
        var scene = EditorSceneManager.GetActiveScene();
        string opened = scene.path == Match3Scene ? "Match3 already open" : scene.isDirty ? $"{scene.name} has unsaved changes, left open" : "opened Match3";
        if (scene.path != Match3Scene && !scene.isDirty) EditorSceneManager.OpenScene(Match3Scene);

        SetGameView(1080, 1920);
        PlayerSettings.runInBackground = false;
        return $"Restored: {opened}, Game view 1080x1920, run in background off.";
    }

    public static string SetGameView(int width, int height)
    {
        PlayModeWindow.SetCustomRenderingResolution((uint)width, (uint)height, $"{width}x{height}");
        PlayModeWindow.GetRenderingResolution(out uint w, out uint h);
        if (w != width || h != height) return $"Could not set {width}x{height}: the play window is {w}x{h}, probably the Simulator, which only takes its device sizes.";
        return $"Game view is {w}x{h}.";
    }

    public static string Status()
    {
        var text = new StringBuilder();
        var scene = EditorSceneManager.GetActiveScene();
        text.Append($"playing={EditorApplication.isPlaying} compiling={EditorApplication.isCompiling} scene={scene.name} dirty={scene.isDirty}");
        PlayModeWindow.GetRenderingResolution(out uint w, out uint h);
        text.Append($" view={w}x{h}");
        if (EditorApplication.isPlaying && LocalizationSettings.SelectedLocale) text.Append($" locale={LocalizationSettings.SelectedLocale.Identifier.Code}");

        var game = Game;
        if (!game || game.CurrentLevelData == null) return text.ToString();

        var data = game.CurrentLevelData;
        text.Append($"\nlevel={(data.IsSurvival ? "Survival" : data.Level ? data.Level.name : "-")} canAct={game.CanForceEnd} moves={data.MovesMade} time={data.TimeSpent:0.0}s");
        foreach (var objective in data.CurrentObjectives)
        {
            var (current, required) = objective.GetProgress();
            text.Append($"\n  goal {objective.GetName()}: {current}/{required}");
        }
        foreach (var limit in data.CurrentLoseConditions)
        {
            var (current, total) = limit.GetProgress();
            text.Append($"\n  limit {limit.GetName()}: {current}/{total}");
        }
        if (data.Survival != null) text.Append($"\n  survival score {data.Survival.Score}, time left {data.Survival.Timer.GetProgress().Item1}");
        return text.ToString();
    }

    #endregion

    #region Levels

    public static SOMatch3Level FindLevel(string level)
    {
        if (string.IsNullOrWhiteSpace(level)) return null;

        var levels = Match3LevelRegistry.GetLevels();
        if (int.TryParse(level, out int number) && number >= 1 && number <= levels.Count) return levels[number - 1];

        string name = level.StartsWith("Level_") ? level : $"Level_{level}";
        return levels.FirstOrDefault(l => l && (l.name == level || l.name == name))
               ?? AssetDatabase.LoadAssetAtPath<SOMatch3Level>($"Assets/3_Data/Levels/{name}.asset")
               ?? AssetDatabase.LoadAssetAtPath<SOMatch3Level>($"Assets/3_Data/Levels/{level}.asset");
    }

    public static string PlayLevel(string level)
    {
        if (!EditorApplication.isPlaying || !GameManager.Instance) return "Enter play mode first (play), then wait for the menu.";

        var asset = FindLevel(level);
        if (!asset) return $"No level called {level}.";

        GameManager.Instance.SelectMatch3Level(asset);
        GameManager.Instance.Match3Scene.LoadScene();
        return $"Loading {asset.name}.";
    }

    public static string PlaySurvival()
    {
        if (!EditorApplication.isPlaying || !GameManager.Instance) return "Enter play mode first (play), then wait for the menu.";

        GameManager.Instance.SelectSurvival();
        GameManager.Instance.Match3Scene.LoadScene();
        return "Loading Survival.";
    }

    #endregion

    #region Level actions

    public static string Win()
    {
        var game = Game;
        if (!game) return NotInLevel;
        if (!game.CanForceEnd) return "Can't end now: the board is moving or the level has ended.";

        game.ForceCompleteLevel();
        return "Winning the level.";
    }

    public static string Lose()
    {
        var game = Game;
        if (!game) return NotInLevel;
        if (!game.CanForceEnd) return "Can't end now: the board is moving or the level has ended.";

        game.ForceFailLevel();
        return "Losing the level.";
    }

    public static string CloseCards()
    {
        if (!EditorApplication.isPlaying) return NotInLevel;

        var window = Object.FindAnyObjectByType<InformationWindowUI>();
        if (!window) return "No info window in the scene.";

        var group = window.GetComponent<CanvasGroup>();
        if (!group || !group.blocksRaycasts) return "No card is open.";

        var back = (UnityEngine.UI.Button)typeof(InformationWindowUI)
            .GetField("backButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
            .GetValue(window);
        if (!back) return "Info window has no back button.";

        back.onClick.Invoke();
        return "Closed the open card.";
    }

    public static string AddMoves(int moves)
    {
        var game = Game;
        if (!game || game.CurrentLevelData == null) return NotInLevel;

        var limit = game.CurrentLevelData.CurrentLoseConditions.OfType<MoveLimit>().FirstOrDefault();
        if (limit == null) return "This level has no move limit.";

        limit.AddMoves(moves);
        return $"Added {moves} moves.";
    }

    public static string AddTime(float seconds)
    {
        var game = Game;
        if (!game || game.CurrentLevelData == null) return NotInLevel;

        var survival = game.CurrentLevelData.Survival;
        if (survival != null)
        {
            survival.Timer.AddTime(seconds);
            return $"Added {seconds}s to the Survival clock.";
        }

        var limit = game.CurrentLevelData.CurrentLoseConditions.OfType<TimeLimit>().FirstOrDefault();
        if (limit == null) return "This level has no time limit.";

        limit.AddTime(seconds);
        return $"Added {seconds}s.";
    }

    public static string AddSurvivalScore(int points)
    {
        var game = Game;
        var survival = game && game.CurrentLevelData != null ? game.CurrentLevelData.Survival : null;
        if (survival == null) return "Not in a Survival run.";

        survival.DebugAddScore(points);
        return $"Survival score is {survival.Score}.";
    }

    #endregion

    #region Board

    public static string Board()
    {
        var game = Game;
        if (!game) return NotInLevel;

        var grid = game.GridHandler;
        var text = new StringBuilder();
        text.AppendLine("Row 0 is the bottom. . off, o empty, letters are pieces (first letter), D Star, S Plug, V virus, U Surge, + Plus, * fused.");
        for (int y = grid.Grid.Height - 1; y >= 0; y--)
        {
            text.Append($"{y,2} ");
            for (int x = 0; x < grid.Grid.Width; x++)
            {
                var tile = grid.GetTile(new Vector2Int(x, y));
                text.Append(Symbol(tile));
            }
            text.AppendLine();
        }
        text.Append("   ");
        for (int x = 0; x < grid.Grid.Width; x++) text.Append(x % 10);
        return text.ToString();
    }

    private static string Symbol(Match3Tile tile)
    {
        if (!tile || !tile.IsActive) return ".";

        var item = tile.CurrentMatch3Object;
        if (!item) return "o";
        if (item.HasFuse && item is Match3MatchableObject) return "*";

        return item switch
        {
            Match3SurgeObject => "U",
            Match3BottomObject => "S",
            Match3VirusObject => "V",
            Match3ObstacleObject => "D",
            Match3HelperObject => "+",
            Match3MatchableObject piece when piece.ItemData => piece.ItemData.Label.Substring(0, 1),
            _ => "?"
        };
    }

    public static string BotMove()
    {
        var game = Game;
        if (!game) return NotInLevel;
        if (!game.CanForceEnd || !game.PlayHandler.CanInteract) return "Board is busy.";

        var grid = game.GridHandler;
        var play = game.PlayHandler;
        foreach (var tile in grid.Tiles.Values.OrderBy(_ => Random.Next()))
        {
            if (tile.CurrentMatch3Object is not Match3SwappableObject first) continue;

            foreach (var direction in new[] { Vector2Int.up, Vector2Int.right })
            {
                var other = grid.GetTile(tile.GridPosition + direction);
                if (!grid.IsValidTile(other) || other.CurrentMatch3Object is not Match3SwappableObject second) continue;

                bool matches = (first.ItemData && play.WouldCreateMatch(other.GridPosition, first.ItemData, grid.GridShape, tile.GridPosition))
                               || (second.ItemData && play.WouldCreateMatch(tile.GridPosition, second.ItemData, grid.GridShape, other.GridPosition))
                               || first is Match3HelperObject || second is Match3HelperObject
                               || first is Match3SurgeObject || second is Match3SurgeObject;
                if (!matches) continue;

                game.StartCoroutine(game.RunGameLogic(tile.GridPosition, other.GridPosition));
                return $"Swapped {tile.GridPosition} with {other.GridPosition}.";
            }
        }

        return "No valid move found.";
    }

    public static string Swap(int x1, int y1, int x2, int y2)
    {
        var game = Game;
        if (!game) return NotInLevel;
        if (!game.CanForceEnd || !game.PlayHandler.CanInteract) return "Board is busy.";

        var a = new Vector2Int(x1, y1);
        var b = new Vector2Int(x2, y2);
        if (!game.GridHandler.IsValidTile(game.GridHandler.GetTile(a)) || !game.GridHandler.IsValidTile(game.GridHandler.GetTile(b))) return "One of those cells is not on the board.";
        if (Mathf.Abs(x1 - x2) + Mathf.Abs(y1 - y2) != 1) return "Cells must be neighbours.";

        game.StartCoroutine(game.RunGameLogic(a, b));
        return $"Swapping {a} with {b}.";
    }

    /// <summary>Puts an object on a cell, replacing what is there. Type: star, plug, virus, surge, plus, fuse or a piece name.</summary>
    public static string Spawn(string type, int x, int y)
    {
        var game = Game;
        if (!game) return NotInLevel;

        var grid = game.GridHandler;
        var tile = grid.GetTile(new Vector2Int(x, y));
        if (!grid.IsValidTile(tile)) return $"({x}, {y}) is not on the board.";

        string kind = (type ?? string.Empty).Trim().ToLowerInvariant();
        var level = game.CurrentLevelData?.Level;
        SOItemData piece = null;
        for (int i = 0; level && i < level.MatchObjects.Count; i++)
        {
            var item = level.MatchObjects[i];
            if (item && item.Label.ToLowerInvariant() == kind) piece = item;
        }
        var randomPiece = level && level.MatchObjects.Count > 0 ? level.MatchObjects[Random.Next(level.MatchObjects.Count)] : null;

        if (kind is not ("star" or "plug" or "virus" or "surge" or "plus" or "fuse") && !piece) return $"Unknown type {type}. Use star, plug, virus, surge, plus, fuse or a piece name.";

        if (tile.CurrentMatch3Object)
        {
            var old = tile.CurrentMatch3Object;
            tile.SetCurrentItem(null);
            ObjectPooler.ReturnObjectToPool(old.gameObject);
        }

        switch (kind)
        {
            case "star": grid.CreateObstacleObject(tile); break;
            case "plug": grid.CreateBottomObject(tile); grid.OpenCatcher(x); break;
            case "virus": grid.CreateVirusObject(tile); break;
            case "surge": grid.CreateSurgeObject(randomPiece, tile); break;
            case "plus": grid.CreateHelperObject(tile); break;
            case "fuse":
                var fused = grid.CreateMatchableObject(randomPiece, tile);
                if (fused) fused.SetFuse(level ? level.FuseMoves : 8);
                break;
            default: grid.CreateMatchableObject(piece, tile); break;
        }

        return $"Spawned {kind} at ({x}, {y}).";
    }

    #endregion

    #region Utilities

    public static string SetLocale(string code)
    {
        if (!EditorApplication.isPlaying) return "Locale can only be switched in play mode.";

        var locale = LocalizationSettings.AvailableLocales.GetLocale(code);
        if (!locale) return $"No locale {code}. Try en, es, pt-BR, de, fr, he or ar.";

        LocalizationSettings.SelectedLocale = locale;
        return $"Locale is {code}.";
    }

    public static string Screenshot(string path)
    {
        if (!EditorApplication.isPlaying) return "Screenshots need play mode.";

        if (string.IsNullOrWhiteSpace(path)) path = Path.Combine(Path.GetTempPath(), "electrogrid-shot.png");
        string folder = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(folder)) Directory.CreateDirectory(folder);
        if (File.Exists(path)) File.Delete(path);

        ScreenCapture.CaptureScreenshot(path);
        return $"Capturing to {path} at the end of the frame.";
    }

    public static string ReadErrors(bool clear)
    {
        lock (Errors)
        {
            string result = Errors.Count == 0 ? "No errors." : $"{Errors.Count} errors:\n" + string.Join("\n", Errors);
            if (clear) Errors.Clear();
            return result;
        }
    }

    public static string ClearErrors()
    {
        lock (Errors) Errors.Clear();
        return "Error list cleared.";
    }

    #endregion
}
