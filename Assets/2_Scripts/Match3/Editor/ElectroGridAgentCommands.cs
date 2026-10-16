using System.IO;
using System.Linq;
using System.Text;
using Unity.Pipeline.Commands;
using UnityEngine;

/// <summary>
/// Named commands for agents and scripts: unity command eg_&lt;name&gt;. Each wraps an editor tool or a debug action and
/// returns a short readable result. List them with unity list.
/// </summary>
internal static class ElectroGridAgentCommands
{
    [CliCommand("eg_status", "ElectroGrid: play state, scene, Game view size, locale, and the running level's goals and limits")]
    private static string Status() => Match3DebugActions.Status();

    [CliCommand("eg_play", "ElectroGrid: open Main (if the scene is saved) and enter play mode")]
    private static string Play() => Match3DebugActions.EnterPlay();

    [CliCommand("eg_restore", "ElectroGrid: stop play mode, reopen Match3, set the Game view to 1080x1920 and run in background off")]
    private static string Restore() => Match3DebugActions.StopAndRestore();

    [CliCommand("eg_game_view", "ElectroGrid: set the Game view size, e.g. 672x1496 for the phone check")]
    private static string GameView([CliArg("size", "WIDTHxHEIGHT")] string size = "672x1496")
    {
        var parts = size.ToLowerInvariant().Split('x');
        if (parts.Length != 2 || !int.TryParse(parts[0], out int w) || !int.TryParse(parts[1], out int h)) return "Use WIDTHxHEIGHT, e.g. 672x1496.";
        return Match3DebugActions.SetGameView(w, h);
    }

    [CliCommand("eg_play_level", "ElectroGrid: load a level in play mode, by play-order number or asset name (12, Level_12, Level_SurgeTest)")]
    private static string PlayLevel([CliArg("level", "Play-order number or asset name", Required = true)] string level) => Match3DebugActions.PlayLevel(level);

    [CliCommand("eg_play_survival", "ElectroGrid: start a Survival run in play mode")]
    private static string PlaySurvival() => Match3DebugActions.PlaySurvival();

    [CliCommand("eg_win", "ElectroGrid: win the running level with the real win ending")]
    private static string Win() => Match3DebugActions.Win();

    [CliCommand("eg_lose", "ElectroGrid: lose the running level with the real lose ending")]
    private static string Lose() => Match3DebugActions.Lose();

    [CliCommand("eg_close_cards", "ElectroGrid: close an open tutorial or info card")]
    private static string CloseCards() => Match3DebugActions.CloseCards();

    [CliCommand("eg_board", "ElectroGrid: print the board as text, row 0 at the bottom")]
    private static string Board() => Match3DebugActions.Board();

    [CliCommand("eg_bot_move", "ElectroGrid: make one random valid move")]
    private static string BotMove() => Match3DebugActions.BotMove();

    [CliCommand("eg_swap", "ElectroGrid: swap two neighbouring cells (x, y with row 0 at the bottom)")]
    private static string Swap(
        [CliArg("x1", "First cell x", Required = true)] int x1,
        [CliArg("y1", "First cell y", Required = true)] int y1,
        [CliArg("x2", "Second cell x", Required = true)] int x2,
        [CliArg("y2", "Second cell y", Required = true)] int y2) => Match3DebugActions.Swap(x1, y1, x2, y2);

    [CliCommand("eg_spawn", "ElectroGrid: put an object on a cell, replacing what is there: star, plug, virus, surge, plus, fuse or a piece name")]
    private static string Spawn(
        [CliArg("type", "star, plug, virus, surge, plus, fuse, or a piece like Triangle", Required = true)] string type,
        [CliArg("x", "Cell x", Required = true)] int x,
        [CliArg("y", "Cell y, row 0 at the bottom", Required = true)] int y) => Match3DebugActions.Spawn(type, x, y);

    [CliCommand("eg_add_moves", "ElectroGrid: add moves to the running level's move limit")]
    private static string AddMoves([CliArg("moves", "Moves to add")] int moves = 5) => Match3DebugActions.AddMoves(moves);

    [CliCommand("eg_add_time", "ElectroGrid: add seconds to the level's time limit or the Survival clock")]
    private static string AddTime([CliArg("seconds", "Seconds to add")] float seconds = 10f) => Match3DebugActions.AddTime(seconds);

    [CliCommand("eg_survival_score", "ElectroGrid: add points to the Survival score, which also unlocks milestones")]
    private static string SurvivalScore([CliArg("points", "Points to add")] int points = 10000) => Match3DebugActions.AddSurvivalScore(points);

    [CliCommand("eg_locale", "ElectroGrid: switch language in play mode: en, es, pt-BR, de, fr, he, ar")]
    private static string Locale([CliArg("code", "Locale code", Required = true)] string code) => Match3DebugActions.SetLocale(code);

    [CliCommand("eg_screenshot", "ElectroGrid: capture the Game view to a PNG at the end of the frame")]
    private static string Screenshot([CliArg("path", "Output PNG path, temp folder by default")] string path = "") => Match3DebugActions.Screenshot(path);

    [CliCommand("eg_errors", "ElectroGrid: errors and exceptions logged since the last clear")]
    private static string Errors([CliArg("clear", "Clear the list after reading")] bool clear = false) => Match3DebugActions.ReadErrors(clear);

    [CliCommand("eg_clear_errors", "ElectroGrid: clear the collected errors, to check a test run starts clean")]
    private static string ClearErrors() => Match3DebugActions.ClearErrors();

    [CliCommand("eg_levels", "ElectroGrid: the play order with each level's grid, goals and validation result")]
    private static string Levels()
    {
        var text = new StringBuilder();
        var levels = Match3LevelRegistry.GetLevels();
        for (int i = 0; i < levels.Count; i++)
        {
            var level = levels[i];
            if (!level)
            {
                text.AppendLine($"{i + 1}. (missing)");
                continue;
            }

            var issues = Match3LevelValidation.Validate(level);
            var worst = Match3LevelValidation.WorstSeverity(issues);
            string goals = string.Join(", ", level.Objectives.Where(o => o != null).Select(o => $"{o.GetName()} {o.GetProgress().Item2}"));
            text.AppendLine($"{i + 1}. {level.name} [{(level.GridShape ? level.GridShape.name : "no grid")}] {goals} ({(worst.HasValue ? worst.Value.ToString() : "OK")})");
        }

        return text.ToString();
    }

    [CliCommand("eg_validate", "ElectroGrid: run the level validator on one level (number or name) or on all of them")]
    private static string Validate([CliArg("level", "Play-order number, asset name, or all")] string level = "all")
    {
        var targets = level == "all" ? Match3LevelRegistry.GetLevels().Where(l => l).ToList() : new[] { Match3DebugActions.FindLevel(level) }.Where(l => l).ToList();
        if (targets.Count == 0) return $"No level called {level}.";

        var text = new StringBuilder();
        int clean = 0;
        foreach (var target in targets)
        {
            var issues = Match3LevelValidation.Validate(target);
            if (issues.Count == 0)
            {
                clean++;
                continue;
            }

            foreach (var issue in issues) text.AppendLine($"{target.name} {issue.Severity}: {issue.Message}");
        }

        return $"{clean} of {targets.Count} clean.\n{text}".TrimEnd();
    }

    [CliCommand("eg_survival_settings", "ElectroGrid: print every Survival setting with its path, for eg_survival_set")]
    private static string SurvivalSettings()
    {
        var mode = Match3LevelRegistry.Prefab ? Match3LevelRegistry.Prefab.SurvivalMode : null;
        if (!mode) return "No Survival mode on the GameManager prefab.";

        var text = new StringBuilder($"{mode.name} (board {(mode.Board ? mode.Board.name : "none")})\n");
        var property = new UnityEditor.SerializedObject(mode).GetIterator();
        property.NextVisible(true);
        while (property.NextVisible(true))
        {
            string value = property.propertyType switch
            {
                UnityEditor.SerializedPropertyType.Integer => property.intValue.ToString(),
                UnityEditor.SerializedPropertyType.Float => property.floatValue.ToString("0.###"),
                UnityEditor.SerializedPropertyType.Boolean => property.boolValue.ToString(),
                UnityEditor.SerializedPropertyType.Enum => property.enumDisplayNames.Length > property.enumValueIndex && property.enumValueIndex >= 0 ? property.enumDisplayNames[property.enumValueIndex] : property.enumValueIndex.ToString(),
                UnityEditor.SerializedPropertyType.ObjectReference => property.objectReferenceValue ? property.objectReferenceValue.name : "none",
                _ => null
            };
            if (value != null) text.AppendLine($"{property.propertyPath} = {value}");
        }

        return text.ToString().TrimEnd();
    }

    [CliCommand("eg_survival_set", "ElectroGrid: set one Survival setting by its path from eg_survival_settings, e.g. obstacles.plugChance 20")]
    private static string SurvivalSet(
        [CliArg("path", "Setting path, e.g. obstacles.plugChance or milestones.Array.data[2].score", Required = true)] string path,
        [CliArg("value", "New value", Required = true)] string value)
    {
        var mode = Match3LevelRegistry.Prefab ? Match3LevelRegistry.Prefab.SurvivalMode : null;
        if (!mode) return "No Survival mode on the GameManager prefab.";

        var serialized = new UnityEditor.SerializedObject(mode);
        var property = serialized.FindProperty(path);
        if (property == null) return $"No setting at {path}. List them with eg_survival_settings.";

        switch (property.propertyType)
        {
            case UnityEditor.SerializedPropertyType.Integer when int.TryParse(value, out int i): property.intValue = i; break;
            case UnityEditor.SerializedPropertyType.Float when float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float f): property.floatValue = f; break;
            case UnityEditor.SerializedPropertyType.Boolean when bool.TryParse(value, out bool b): property.boolValue = b; break;
            case UnityEditor.SerializedPropertyType.Enum when System.Array.IndexOf(property.enumNames, value) is var e && e >= 0: property.enumValueIndex = e; break;
            default: return $"Can't set {path} ({property.propertyType}) to {value}.";
        }

        serialized.ApplyModifiedProperties();
        UnityEditor.AssetDatabase.SaveAssets();
        return $"{path} = {value}";
    }

    [CliCommand("eg_bot_start", "ElectroGrid: start the Match3 Bot in play mode: mode current, range or survival")]
    private static string BotStart(
        [CliArg("mode", "current, range or survival")] string mode = "range",
        [CliArg("from", "First level number")] int from = 1,
        [CliArg("to", "Last level number")] int to = 100,
        [CliArg("speed", "Time scale while the bot plays")] float speed = 2f) => Match3BotWindow.StartRun(mode, from, to, speed);

    [CliCommand("eg_bot_stop", "ElectroGrid: stop the Match3 Bot")]
    private static string BotStop() => Match3BotWindow.StopRun();

    [CliCommand("eg_bot_report", "ElectroGrid: the Match3 Bot's state and one line per finished level")]
    private static string BotReport() => Match3BotWindow.Report();

    [CliCommand("eg_build", "ElectroGrid: queue a release build with the build tool; check it with eg_build_status")]
    private static string Build(
        [CliArg("upload", "Upload to GitHub after building")] bool upload = true,
        [CliArg("release_type", "Published, Draft or Prerelease; empty keeps the config")] string releaseType = "Published")
    {
        if (Application.isPlaying) return "Stop play mode first.";

        Directory.CreateDirectory(Path.GetDirectoryName(BuildRequestWatcher.RequestFile));
        if (File.Exists(BuildRequestWatcher.StatusFile)) File.Delete(BuildRequestWatcher.StatusFile);
        File.WriteAllText(BuildRequestWatcher.RequestFile, $"{{\"upload\":{upload.ToString().ToLowerInvariant()},\"releaseType\":\"{releaseType}\",\"replaceExistingRelease\":\"false\"}}");
        return $"Build {ElectroGridBuild.Version} queued (upload {upload}). It starts within a few seconds; poll eg_build_status.";
    }

    [CliCommand("eg_build_status", "ElectroGrid: the last build's state and step log")]
    private static string BuildStatus()
    {
        if (File.Exists(BuildRequestWatcher.RequestFile)) return "Queued, not started yet.";
        return File.Exists(BuildRequestWatcher.StatusFile) ? File.ReadAllText(BuildRequestWatcher.StatusFile) : "No build has run since the editor started.";
    }
}
