using UnityEditor;
using UnityEngine;

/// <summary>
/// In-game debug and test actions. Every button here is also an agent command (unity command eg_...), both run
/// through Match3DebugActions.
/// </summary>
public class Match3DebugWindow : EditorWindow
{
    private static readonly string[] SpawnTypes = { "star", "plug", "virus", "surge", "plus", "fuse" };
    private static readonly string[] Locales = { "en", "es", "pt-BR", "de", "fr", "he", "ar" };

    private string _level = "1";
    private int _spawnType;
    private Vector2Int _spawnCell;
    private int _moves = 5;
    private float _seconds = 10f;
    private int _score = 10000;
    private string _lastResult = "";
    private Vector2 _scroll;

    [MenuItem("ElectroGrid/Match3 Debug")]
    private static void Open()
    {
        GetWindow<Match3DebugWindow>("Match3 Debug");
    }

    private void OnInspectorUpdate()
    {
        Repaint();
    }

    private void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        EditorGUILayout.LabelField(Match3DebugActions.Status(), EditorStyles.helpBox);

        Section("Session");
        using (new EditorGUILayout.HorizontalScope())
        {
            Run("Play", Match3DebugActions.EnterPlay);
            Run("Stop & Restore", Match3DebugActions.StopAndRestore);
            Run("Phone View", () => Match3DebugActions.SetGameView(672, 1496));
        }

        Section("Load");
        using (new EditorGUILayout.HorizontalScope())
        {
            _level = EditorGUILayout.TextField("Level", _level);
            Run("Play Level", () => Match3DebugActions.PlayLevel(_level), 90);
        }
        Run("Play Survival", Match3DebugActions.PlaySurvival);

        Section("Level");
        using (new EditorGUILayout.HorizontalScope())
        {
            var previous = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.5f, 1f, 0.6f);
            Run("Win Level", Match3DebugActions.Win, 0, 32);
            GUI.backgroundColor = new Color(1f, 0.55f, 0.55f);
            Run("Lose Level", Match3DebugActions.Lose, 0, 32);
            GUI.backgroundColor = previous;
        }
        using (new EditorGUILayout.HorizontalScope())
        {
            Run("Bot Move", Match3DebugActions.BotMove);
            Run("Close Card", Match3DebugActions.CloseCards);
            Run("Print Board", Match3DebugActions.Board);
        }
        using (new EditorGUILayout.HorizontalScope())
        {
            _moves = EditorGUILayout.IntField("Moves", _moves);
            Run("Add", () => Match3DebugActions.AddMoves(_moves), 60);
        }
        using (new EditorGUILayout.HorizontalScope())
        {
            _seconds = EditorGUILayout.FloatField("Seconds", _seconds);
            Run("Add", () => Match3DebugActions.AddTime(_seconds), 60);
        }
        using (new EditorGUILayout.HorizontalScope())
        {
            _score = EditorGUILayout.IntField("Survival Score", _score);
            Run("Add", () => Match3DebugActions.AddSurvivalScore(_score), 60);
        }

        Section("Survival");
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField("Milestone banner", GUILayout.Width(110));
            for (int i = 0; i < 6; i++)
            {
                int index = i;
                Run(index.ToString(), () => Match3DebugActions.PreviewMilestone(index));
            }
        }

        Section("Spawn");
        using (new EditorGUILayout.HorizontalScope())
        {
            _spawnType = EditorGUILayout.Popup(_spawnType, SpawnTypes, GUILayout.Width(80));
            _spawnCell = EditorGUILayout.Vector2IntField(GUIContent.none, _spawnCell);
            Run("Spawn", () => Match3DebugActions.Spawn(SpawnTypes[_spawnType], _spawnCell.x, _spawnCell.y), 60);
        }

        Section("Language");
        using (new EditorGUILayout.HorizontalScope())
        {
            foreach (var code in Locales) Run(code, () => Match3DebugActions.SetLocale(code));
        }

        Section("Controller");
        using (new EditorGUILayout.HorizontalScope())
        {
            Run("Up", () => Match3DebugActions.UiMove("up"));
            Run("Down", () => Match3DebugActions.UiMove("down"));
            Run("Left", () => Match3DebugActions.UiMove("left"));
            Run("Right", () => Match3DebugActions.UiMove("right"));
        }
        using (new EditorGUILayout.HorizontalScope())
        {
            Run("Submit", Match3DebugActions.UiSubmit);
            Run("Back", Match3DebugActions.UiCancel);
            Run("Focus", Match3DebugActions.UiStatus);
        }
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField("Open", GUILayout.Width(40));
            Run("Settings", () => Match3DebugActions.OpenWindow("settings"));
            Run("Info", () => Match3DebugActions.OpenWindow("info"));
            Run("Credits", () => Match3DebugActions.OpenWindow("credits"));
            Run("Close", () => Match3DebugActions.OpenWindow("close"));
        }
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField("Board", GUILayout.Width(40));
            Run("Up", () => Match3DebugActions.CursorMove("up"));
            Run("Down", () => Match3DebugActions.CursorMove("down"));
            Run("Left", () => Match3DebugActions.CursorMove("left"));
            Run("Right", () => Match3DebugActions.CursorMove("right"));
            Run("LB", () => Match3DebugActions.CursorMove("lb"));
            Run("RB", () => Match3DebugActions.CursorMove("rb"));
            Run("A", Match3DebugActions.CursorPress);
            Run("B", Match3DebugActions.CursorBack);
        }

        Section("Checks");
        using (new EditorGUILayout.HorizontalScope())
        {
            Run("Screenshot", () => Match3DebugActions.Screenshot(""));
            Run("Errors", () => Match3DebugActions.ReadErrors(false));
            Run("Clear Errors", Match3DebugActions.ClearErrors);
        }

        if (!string.IsNullOrEmpty(_lastResult))
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.SelectableLabel(_lastResult, EditorStyles.helpBox, GUILayout.MinHeight(EditorGUIUtility.singleLineHeight * Mathf.Min(16, _lastResult.Split('\n').Length + 1)));
        }

        EditorGUILayout.EndScrollView();
    }

    private static void Section(string title)
    {
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
    }

    private void Run(string label, System.Func<string> action, float width = 0f, float height = 22f)
    {
        bool clicked = width > 0f ? GUILayout.Button(label, GUILayout.Width(width), GUILayout.Height(height)) : GUILayout.Button(label, GUILayout.Height(height));
        if (clicked) _lastResult = action();
    }
}
