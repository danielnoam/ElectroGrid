#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

internal class Match3LevelEditorWindow : EditorWindow
{
    private const string FolderPrefKey = "ElectroGrid.LevelEditor.Folder";
    private const string DefaultFolder = "Assets/3_Data/Levels";
    private const float SidebarWidth = 270f;
    private const float CellSize = Match3LevelGridGUI.DefaultCellSize;

    // Serialized so it survives an assembly reload, and mirrored into EditorPrefs so it survives a restart
    [SerializeField] private string _levelsFolder;
    [SerializeField] private SOMatch3Level _selectedLevel;

    private readonly List<SOMatch3Level> _levels = new List<SOMatch3Level>();
    private readonly Dictionary<SOMatch3Level, Match3LevelValidation.Severity?> _validation =
        new Dictionary<SOMatch3Level, Match3LevelValidation.Severity?>();
    private List<SOMatch3Level> _playOrder = new List<SOMatch3Level>();
    private SerializedObject _serializedLevel;

    private Match3TileObjectType _currentPaintMode = Match3TileObjectType.Obstacle;
    private bool _isDragging;
    private Vector2 _sidebarScroll;
    private Vector2 _editorScroll;
    private Vector2 _orderScroll;
    private SOMatch3Level _pendingSelection;
    private bool _hasPendingSelection;
    private PendingAction _pendingAction;

    // Play order edits and play mode carry an index or a level, which the enum above cannot
    private System.Action _pendingOperation;
    private bool _orderFoldout;

    private bool _randomizeFoldout;
    private int _obstacleCount = 1;
    private int _bottomCount = 1;
    private int _seed;
    private int _minObstacleSpacing;
    private bool _keepObstaclesFromUnderBottoms = true;

    private bool _shapeFoldout;
    private int _shapeWidth = 8;
    private int _shapeHeight = 8;
    private int _shapeSeed;
    private float _shapeDensity = 0.55f;
    private int _shapeSmoothing = 3;
    private Match3GridShapeRandomizer.SymmetryMode _shapeSymmetry = Match3GridShapeRandomizer.SymmetryMode.Horizontal;
    private bool _shapeRemoveIsolated = true;
    private bool _shapeKeepLargestRegion = true;

    private enum PendingAction
    {
        None,
        CreateLevel,
        DuplicateLevel,
        RandomizeShapeInPlace,
        RandomizeShapeIntoNew
    }

    [MenuItem("ElectroGrid/Level Editor")]
    public static void Open()
    {
        var window = GetWindow<Match3LevelEditorWindow>("Level Editor");
        window.minSize = new Vector2(620f, 420f);
        window.Show();
    }

    public static void Open(SOMatch3Level level)
    {
        var window = GetWindow<Match3LevelEditorWindow>("Level Editor");
        window.minSize = new Vector2(620f, 420f);
        window.Show();

        if (!level) return;

        // Follow the level that was opened, otherwise the sidebar would not list it
        string folder = System.IO.Path.GetDirectoryName(AssetDatabase.GetAssetPath(level))?.Replace('\\', '/');
        if (!string.IsNullOrEmpty(folder) && folder != window._levelsFolder)
        {
            window.SetFolder(folder);
        }

        window.SelectLevel(level);
    }

    private void OnEnable()
    {
        // Only fall back to the stored preference when a reload did not already carry the folder over
        if (string.IsNullOrEmpty(_levelsFolder))
        {
            _levelsFolder = EditorPrefs.GetString(FolderPrefKey, DefaultFolder);
        }

        RefreshLevels();

        if (_selectedLevel) SelectLevel(_selectedLevel);
    }

    private void OnFocus()
    {
        RefreshLevels();
    }

    private void OnGUI()
    {
        EditorGUILayout.BeginHorizontal();

        DrawSidebar();
        DrawEditor();

        EditorGUILayout.EndHorizontal();

        if (_hasPendingSelection)
        {
            _hasPendingSelection = false;
            SelectLevel(_pendingSelection);
            _pendingSelection = null;
            Repaint();
        }

        // Asset creation invalidates the GUI, so it waits until the pass is finished
        switch (_pendingAction)
        {
            case PendingAction.CreateLevel:
                _pendingAction = PendingAction.None;
                CreateLevel();
                break;

            case PendingAction.DuplicateLevel:
                _pendingAction = PendingAction.None;
                DuplicateSelectedLevel();
                break;

            case PendingAction.RandomizeShapeInPlace:
                _pendingAction = PendingAction.None;
                RandomizeShape(false);
                break;

            case PendingAction.RandomizeShapeIntoNew:
                _pendingAction = PendingAction.None;
                RandomizeShape(true);
                break;
        }

        if (_pendingOperation != null)
        {
            var operation = _pendingOperation;
            _pendingOperation = null;
            operation();
        }
    }

    private void DrawSidebar()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(SidebarWidth), GUILayout.ExpandHeight(true));

        EditorGUILayout.LabelField("Levels Folder", EditorStyles.boldLabel);

        var folderAsset = string.IsNullOrEmpty(_levelsFolder)
            ? null
            : AssetDatabase.LoadAssetAtPath<DefaultAsset>(_levelsFolder);

        EditorGUI.BeginChangeCheck();
        var picked = (DefaultAsset)EditorGUILayout.ObjectField(folderAsset, typeof(DefaultAsset), false);
        if (EditorGUI.EndChangeCheck())
        {
            string path = picked ? AssetDatabase.GetAssetPath(picked) : string.Empty;

            if (string.IsNullOrEmpty(path) || AssetDatabase.IsValidFolder(path))
            {
                SetFolder(path);
            }
            else
            {
                Debug.LogWarning($"{path} is not a folder");
            }
        }

        EditorGUILayout.LabelField(string.IsNullOrEmpty(_levelsFolder) ? "No folder set" : _levelsFolder, EditorStyles.miniLabel);

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField($"Levels ({_levels.Count})", EditorStyles.boldLabel);

        _sidebarScroll = EditorGUILayout.BeginScrollView(_sidebarScroll);

        if (_levels.Count == 0)
        {
            EditorGUILayout.HelpBox("No levels in this folder.", MessageType.Info);
        }

        foreach (var level in _levels)
        {
            if (!level) continue;

            bool isSelected = level == _selectedLevel;

            var style = new GUIStyle(EditorStyles.miniButton)
            {
                alignment = TextAnchor.MiddleLeft,
                fontStyle = isSelected ? FontStyle.Bold : FontStyle.Normal,
                richText = true
            };

            Color previousBackground = GUI.backgroundColor;
            if (isSelected) GUI.backgroundColor = new Color(0.4f, 0.7f, 1f);

            int order = _playOrder.IndexOf(level);
            string position = order >= 0 ? $"{order + 1}." : "·";
            string label = $"{(isSelected ? "▸" : " ")} {ValidationDot(level)} {position} {level.name}";

            if (GUILayout.Button(label, style, GUILayout.Height(22f)))
            {
                // Applied after the GUI pass, changing it here would unbalance the layout groups
                _pendingSelection = level;
                _hasPendingSelection = true;
            }

            GUI.backgroundColor = previousBackground;
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(4);

        bool hasFolder = !string.IsNullOrEmpty(_levelsFolder) && AssetDatabase.IsValidFolder(_levelsFolder);

        using (new EditorGUI.DisabledScope(!hasFolder))
        {
            if (GUILayout.Button("New Level")) _pendingAction = PendingAction.CreateLevel;
        }

        using (new EditorGUI.DisabledScope(!_selectedLevel))
        {
            if (GUILayout.Button("Duplicate Selected")) _pendingAction = PendingAction.DuplicateLevel;
            if (GUILayout.Button("Ping Selected")) EditorGUIUtility.PingObject(_selectedLevel);
        }

        if (GUILayout.Button("Refresh"))
        {
            Match3LevelRegistry.ClearCache();
            RefreshLevels();
        }

        EditorGUILayout.Space(4);

        using (new EditorGUI.DisabledScope(_levels.Count == 0))
        {
            if (GUILayout.Button("Validate All")) _pendingOperation = ValidateAll;
        }

        DrawPlayOrder();

        EditorGUILayout.EndVertical();
    }

    private string ValidationDot(SOMatch3Level level)
    {
        if (!_validation.TryGetValue(level, out var severity)) return "<color=#808080>\u25CF</color>";
        if (severity == null) return "<color=#6FCF6F>\u25CF</color>";

        return severity == Match3LevelValidation.Severity.Error
            ? "<color=#FF5252>\u25CF</color>"
            : "<color=#FFC107>\u25CF</color>";
    }

    /// <summary>
    /// The GameManager array is the play order, not just a bag of levels — Match3GameManager walks it
    /// by index — so reordering here is the same edit as reordering the progression.
    /// </summary>
    private void DrawPlayOrder()
    {
        EditorGUILayout.Space(4);
        _orderFoldout = EditorGUILayout.Foldout(_orderFoldout, $"Play Order ({_playOrder.Count})", true, EditorStyles.foldoutHeader);
        if (!_orderFoldout) return;

        if (!Match3LevelRegistry.Prefab)
        {
            EditorGUILayout.HelpBox("No GameManager prefab found, so the play order cannot be edited.", MessageType.Warning);
            return;
        }

        // Pinned under the action buttons, so it needs its own scroll rather than the sidebar's
        _orderScroll = EditorGUILayout.BeginScrollView(_orderScroll, GUILayout.MaxHeight(160f));

        for (int i = 0; i < _playOrder.Count; i++)
        {
            int index = i;
            var level = _playOrder[i];

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"{i + 1}. {(level ? level.name : "(missing)")}", EditorStyles.miniLabel);

            using (new EditorGUI.DisabledScope(i == 0))
            {
                if (GUILayout.Button("\u25B2", EditorStyles.miniButtonLeft, GUILayout.Width(22f)))
                {
                    _pendingOperation = () => MoveInPlayOrder(index, -1);
                }
            }

            using (new EditorGUI.DisabledScope(i == _playOrder.Count - 1))
            {
                if (GUILayout.Button("\u25BC", EditorStyles.miniButtonMid, GUILayout.Width(22f)))
                {
                    _pendingOperation = () => MoveInPlayOrder(index, 1);
                }
            }

            if (GUILayout.Button("\u2715", EditorStyles.miniButtonRight, GUILayout.Width(22f)))
            {
                _pendingOperation = () => RemoveFromPlayOrder(index);
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();

        int unregistered = CountUnregistered();

        using (new EditorGUI.DisabledScope(unregistered == 0))
        {
            if (GUILayout.Button(unregistered == 1 ? "Add 1 Unlisted Level" : $"Add {unregistered} Unlisted Levels"))
            {
                _pendingOperation = AddUnregisteredToPlayOrder;
            }
        }
    }

    private int CountUnregistered()
    {
        int count = 0;

        foreach (var level in _levels)
        {
            if (level && !_playOrder.Contains(level)) count++;
        }

        return count;
    }

    private void MoveInPlayOrder(int index, int offset)
    {
        int target = index + offset;
        if (index < 0 || index >= _playOrder.Count || target < 0 || target >= _playOrder.Count) return;

        (_playOrder[index], _playOrder[target]) = (_playOrder[target], _playOrder[index]);
        CommitPlayOrder();
    }

    private void RemoveFromPlayOrder(int index)
    {
        if (index < 0 || index >= _playOrder.Count) return;

        _playOrder.RemoveAt(index);
        CommitPlayOrder();
    }

    private void AddToPlayOrder(SOMatch3Level level)
    {
        if (!level || _playOrder.Contains(level)) return;

        _playOrder.Add(level);
        CommitPlayOrder();
    }

    private void AddUnregisteredToPlayOrder()
    {
        foreach (var level in _levels)
        {
            if (level && !_playOrder.Contains(level)) _playOrder.Add(level);
        }

        CommitPlayOrder();
    }

    private void CommitPlayOrder()
    {
        Match3LevelRegistry.SetLevels(_playOrder);
        _playOrder = Match3LevelRegistry.GetLevels();
        Repaint();
    }

    private void ValidateAll()
    {
        CacheValidation();

        int errors = 0;
        int warnings = 0;

        foreach (var level in _levels)
        {
            if (!level || !_validation.TryGetValue(level, out var severity) || severity == null) continue;

            if (severity == Match3LevelValidation.Severity.Error) errors++;
            else warnings++;

            foreach (var issue in Match3LevelValidation.Validate(level))
            {
                string message = $"{level.name}: {issue.Message}";

                if (issue.Severity == Match3LevelValidation.Severity.Error) Debug.LogError(message, level);
                else Debug.LogWarning(message, level);
            }
        }

        Debug.Log(errors == 0 && warnings == 0
            ? $"Level Editor: all {_levels.Count} levels are clean."
            : $"Level Editor: {errors} level(s) with errors and {warnings} with warnings, out of {_levels.Count}.");

        Repaint();
    }

    private void CacheValidation()
    {
        _validation.Clear();

        foreach (var level in _levels)
        {
            CacheValidation(level);
        }
    }

    private void CacheValidation(SOMatch3Level level)
    {
        if (level) _validation[level] = Match3LevelValidation.WorstSeverity(level);
    }

    private void DrawEditor()
    {
        EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

        if (!_selectedLevel || _serializedLevel == null)
        {
            EditorGUILayout.Space(20);
            EditorGUILayout.HelpBox("Select a level on the left to edit it.", MessageType.Info);
            EditorGUILayout.EndVertical();
            return;
        }

        // The asset can be deleted or renamed while the window is open
        if (!_serializedLevel.targetObject)
        {
            SelectLevel(null);
            RefreshLevels();
            EditorGUILayout.EndVertical();
            return;
        }

        _serializedLevel.Update();

        _editorScroll = EditorGUILayout.BeginScrollView(_editorScroll);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(_selectedLevel.name, EditorStyles.boldLabel);

        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
        {
            if (GUILayout.Button("Play This Level", GUILayout.Width(120f)))
            {
                var levelToPlay = _selectedLevel;
                _pendingOperation = () => Match3LevelPlayer.Play(levelToPlay);
            }
        }

        EditorGUILayout.EndHorizontal();

        if (!_playOrder.Contains(_selectedLevel))
        {
            EditorGUILayout.HelpBox("Not in the GameManager play order, so the game can never reach it.", MessageType.Warning);

            if (GUILayout.Button("Add To Play Order"))
            {
                var levelToAdd = _selectedLevel;
                _pendingOperation = () => AddToPlayOrder(levelToAdd);
            }
        }

        EditorGUILayout.Space(4);

        EditorGUILayout.PropertyField(_serializedLevel.FindProperty("levelName"));
        EditorGUILayout.PropertyField(_serializedLevel.FindProperty("matchObjects"));
        EditorGUILayout.PropertyField(_serializedLevel.FindProperty("objectives"));
        EditorGUILayout.PropertyField(_serializedLevel.FindProperty("loseConditions"));

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Level Grid", EditorStyles.boldLabel);

        var gridShapeProp = _serializedLevel.FindProperty("gridShape");
        EditorGUILayout.PropertyField(gridShapeProp);

        DrawShapeRandomizer(gridShapeProp);
        DrawTileObjectPainter(gridShapeProp);
        DrawRandomizer(gridShapeProp);
        DrawValidation();

        EditorGUILayout.EndScrollView();

        if (_serializedLevel.ApplyModifiedProperties()) CacheValidation(_selectedLevel);

        EditorGUILayout.EndVertical();
    }

    private void DrawTileObjectPainter(SerializedProperty gridShapeProp)
    {
        if (gridShapeProp.objectReferenceValue == null)
        {
            EditorGUILayout.HelpBox("Assign a Grid Shape to paint tile objects.", MessageType.Info);
            return;
        }

        Grid grid = ((SOGridShape)gridShapeProp.objectReferenceValue).Grid;
        if (grid == null)
        {
            EditorGUILayout.HelpBox("Grid Shape has no valid grid.", MessageType.Warning);
            return;
        }

        var tileObjectsProp = _serializedLevel.FindProperty("tileObjects");
        int requiredSize = grid.Width * grid.Height;

        if (tileObjectsProp.arraySize != requiredSize)
        {
            tileObjectsProp.arraySize = requiredSize;
            _serializedLevel.ApplyModifiedProperties();
        }

        EditorGUILayout.LabelField(Match3LevelGridGUI.BuildCountsLabel(_selectedLevel), Match3LevelGridGUI.RichLabel);
        EditorGUILayout.Space(5);

        float gridWidth = grid.Width * CellSize;
        Rect gridRect = GUILayoutUtility.GetRect(gridWidth, grid.Height * CellSize, GUILayout.ExpandWidth(true));
        gridRect.x += Mathf.Max(0f, (gridRect.width - gridWidth) / 2f);
        gridRect.width = gridWidth;

        HandlePainting(gridRect, grid, tileObjectsProp);
        Match3LevelGridGUI.DrawCells(gridRect, grid, tileObjectsProp, CellSize, true);

        if (gridRect.Contains(Event.current.mousePosition)) Repaint();

        EditorGUILayout.Space(5);

        EditorGUILayout.BeginHorizontal();
        DrawPaintModeButton("Obstacles", Match3TileObjectType.Obstacle);
        DrawPaintModeButton("Bottom", Match3TileObjectType.Bottom);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Clear All")) ClearAll(tileObjectsProp);
        if (GUILayout.Button("Clear Obstacles")) ClearType(tileObjectsProp, grid, Match3TileObjectType.Obstacle);
        if (GUILayout.Button("Clear Bottom")) ClearType(tileObjectsProp, grid, Match3TileObjectType.Bottom);
        EditorGUILayout.EndHorizontal();
    }

    private void DrawShapeRandomizer(SerializedProperty gridShapeProp)
    {
        EditorGUILayout.Space(6);
        _shapeFoldout = EditorGUILayout.Foldout(_shapeFoldout, "Randomize Grid Shape", true, EditorStyles.foldoutHeader);
        if (!_shapeFoldout) return;

        EditorGUI.indentLevel++;

        var currentShape = (SOGridShape)gridShapeProp.objectReferenceValue;
        int usedBy = CountLevelsUsingShape(currentShape);

        if (currentShape && currentShape.Grid != null && GUILayout.Button("Copy size from current shape"))
        {
            _shapeWidth = currentShape.Grid.Width;
            _shapeHeight = currentShape.Grid.Height;
        }

        _shapeWidth = Mathf.Clamp(EditorGUILayout.IntField("Width", _shapeWidth), 2, 32);
        _shapeHeight = Mathf.Clamp(EditorGUILayout.IntField("Height", _shapeHeight), 2, 32);
        _shapeDensity = EditorGUILayout.Slider("Density", _shapeDensity, 0.2f, 0.9f);
        _shapeSymmetry = (Match3GridShapeRandomizer.SymmetryMode)EditorGUILayout.EnumPopup("Symmetry", _shapeSymmetry);
        _shapeSmoothing = Mathf.Clamp(EditorGUILayout.IntField("Smoothing Passes", _shapeSmoothing), 0, 8);
        _shapeRemoveIsolated = EditorGUILayout.Toggle("Remove Isolated Cells", _shapeRemoveIsolated);
        _shapeKeepLargestRegion = EditorGUILayout.Toggle("Keep Largest Region", _shapeKeepLargestRegion);

        EditorGUILayout.BeginHorizontal();
        _shapeSeed = EditorGUILayout.IntField("Seed", _shapeSeed);
        if (GUILayout.Button("New", GUILayout.Width(60))) _shapeSeed = Random.Range(int.MinValue, int.MaxValue);
        EditorGUILayout.EndHorizontal();

        if (usedBy > 1)
        {
            EditorGUILayout.HelpBox(
                $"{currentShape.name} is used by {usedBy} levels. Randomizing it in place changes all of them.",
                MessageType.Warning);
        }

        EditorGUILayout.HelpBox(
            "Changing the shape resets tile objects that end up on inactive cells, and can strand Square Stars in columns that no longer reach row 0. Check the validation below afterwards.",
            MessageType.None);

        EditorGUILayout.BeginHorizontal();

        using (new EditorGUI.DisabledScope(!currentShape))
        {
            if (GUILayout.Button(usedBy > 1 ? "Randomize In Place (affects all)" : "Randomize In Place"))
            {
                _pendingAction = PendingAction.RandomizeShapeInPlace;
            }
        }

        if (GUILayout.Button("Randomize Into New Shape")) _pendingAction = PendingAction.RandomizeShapeIntoNew;

        EditorGUILayout.EndHorizontal();

        EditorGUI.indentLevel--;
    }

    private static int CountLevelsUsingShape(SOGridShape shape)
    {
        if (!shape) return 0;

        int count = 0;
        foreach (string guid in AssetDatabase.FindAssets($"t:{nameof(SOMatch3Level)}"))
        {
            var level = AssetDatabase.LoadAssetAtPath<SOMatch3Level>(AssetDatabase.GUIDToAssetPath(guid));
            if (level && level.GridShape == shape) count++;
        }

        return count;
    }

    private void RandomizeShape(bool intoNewAsset)
    {
        if (!_selectedLevel || _serializedLevel == null) return;

        var gridShapeProp = _serializedLevel.FindProperty("gridShape");
        var shape = (SOGridShape)gridShapeProp.objectReferenceValue;

        if (intoNewAsset)
        {
            string folder = shape
                ? System.IO.Path.GetDirectoryName(AssetDatabase.GetAssetPath(shape))?.Replace('\\', '/')
                : System.IO.Path.GetDirectoryName(AssetDatabase.GetAssetPath(_selectedLevel))?.Replace('\\', '/');

            if (string.IsNullOrEmpty(folder)) return;

            string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{_selectedLevel.name} Shape.asset");
            var created = CreateInstance<SOGridShape>();
            AssetDatabase.CreateAsset(created, path);

            shape = created;
            gridShapeProp.objectReferenceValue = created;
            _serializedLevel.ApplyModifiedProperties();
        }

        if (!shape) return;

        var cells = Match3GridShapeRandomizer.Generate(new Match3GridShapeRandomizer.Options
        {
            Width = _shapeWidth,
            Height = _shapeHeight,
            Seed = _shapeSeed,
            Density = _shapeDensity,
            Symmetry = _shapeSymmetry,
            SmoothingPasses = _shapeSmoothing,
            RemoveIsolatedCells = _shapeRemoveIsolated,
            KeepLargestRegionOnly = _shapeKeepLargestRegion
        });

        WriteShape(shape, cells, _shapeWidth, _shapeHeight);
        ClearTileObjectsOnInactiveCells(shape);

        AssetDatabase.SaveAssets();
        RefreshLevels();
        Repaint();
    }

    private static void WriteShape(SOGridShape shape, bool[] cells, int width, int height)
    {
        var serializedShape = new SerializedObject(shape);

        serializedShape.FindProperty("grid.size").vector2IntValue = new Vector2Int(width, height);

        var cellsProp = serializedShape.FindProperty("grid.cells");
        cellsProp.arraySize = cells.Length;

        for (int i = 0; i < cells.Length; i++)
        {
            cellsProp.GetArrayElementAtIndex(i).boolValue = cells[i];
        }

        serializedShape.ApplyModifiedProperties();
    }

    /// <summary>
    /// Tile objects are stored for every cell, active or not. A painted object left on a cell the
    /// new shape deactivated never spawns but still counts, which would make the tally lie.
    /// </summary>
    private void ClearTileObjectsOnInactiveCells(SOGridShape shape)
    {
        if (!shape || shape.Grid == null) return;

        _serializedLevel.Update();

        var tileObjectsProp = _serializedLevel.FindProperty("tileObjects");
        Grid grid = shape.Grid;
        int required = grid.Width * grid.Height;

        if (tileObjectsProp.arraySize != required) tileObjectsProp.arraySize = required;

        for (int y = 0; y < grid.Height; y++)
        {
            for (int x = 0; x < grid.Width; x++)
            {
                if (grid.IsCellActive(x, y)) continue;

                int index = y * grid.Width + x;
                if (index < tileObjectsProp.arraySize)
                {
                    tileObjectsProp.GetArrayElementAtIndex(index).enumValueIndex = (int)Match3TileObjectType.Matchable;
                }
            }
        }

        _serializedLevel.ApplyModifiedProperties();
    }

    private void DrawValidation()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);

        var issues = Match3LevelValidation.Validate(_selectedLevel);

        if (issues.Count == 0)
        {
            EditorGUILayout.HelpBox("No problems found.", MessageType.Info);
            return;
        }

        foreach (var issue in issues)
        {
            var type = issue.Severity == Match3LevelValidation.Severity.Error ? MessageType.Error : MessageType.Warning;
            EditorGUILayout.HelpBox(issue.Message, type);
        }
    }

    private void DrawRandomizer(SerializedProperty gridShapeProp)
    {
        if (gridShapeProp.objectReferenceValue == null) return;

        Grid grid = ((SOGridShape)gridShapeProp.objectReferenceValue).Grid;
        if (grid == null) return;

        EditorGUILayout.Space(10);
        _randomizeFoldout = EditorGUILayout.Foldout(_randomizeFoldout, "Randomize", true, EditorStyles.foldoutHeader);
        if (!_randomizeFoldout) return;

        EditorGUI.indentLevel++;

        GetRequiredCounts(out int obstaclesNeeded, out int bottomsNeeded);

        EditorGUILayout.BeginHorizontal();
        _obstacleCount = Mathf.Max(0, EditorGUILayout.IntField("Double Stars", _obstacleCount));
        using (new EditorGUI.DisabledScope(obstaclesNeeded <= 0))
        {
            if (GUILayout.Button($"From objectives ({obstaclesNeeded})", GUILayout.Width(150))) _obstacleCount = obstaclesNeeded;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        _bottomCount = Mathf.Max(0, EditorGUILayout.IntField("Square Stars", _bottomCount));
        using (new EditorGUI.DisabledScope(bottomsNeeded <= 0))
        {
            if (GUILayout.Button($"From objectives ({bottomsNeeded})", GUILayout.Width(150))) _bottomCount = bottomsNeeded;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        _seed = EditorGUILayout.IntField("Seed", _seed);
        if (GUILayout.Button("New", GUILayout.Width(60))) _seed = Random.Range(int.MinValue, int.MaxValue);
        EditorGUILayout.EndHorizontal();

        _minObstacleSpacing = Mathf.Max(0, EditorGUILayout.IntField("Min Obstacle Spacing", _minObstacleSpacing));
        _keepObstaclesFromUnderBottoms = EditorGUILayout.Toggle("Keep Clear Under Square Stars", _keepObstaclesFromUnderBottoms);

        EditorGUILayout.HelpBox(
            "Square Stars are only placed in columns that reach row 0, and never on row 0 itself. Undo reverts a roll, so reroll freely.",
            MessageType.None);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Randomize All")) ApplyRandomize(grid, true, true);
        if (GUILayout.Button("Double Stars")) ApplyRandomize(grid, true, false);
        if (GUILayout.Button("Square Stars")) ApplyRandomize(grid, false, true);
        EditorGUILayout.EndHorizontal();

        EditorGUI.indentLevel--;
    }

    private void GetRequiredCounts(out int obstaclesNeeded, out int bottomsNeeded)
    {
        obstaclesNeeded = 0;
        bottomsNeeded = 0;

        foreach (var objective in _selectedLevel.Objectives)
        {
            if (objective is DestroyObstaclesObjective destroyObstacles) obstaclesNeeded += destroyObstacles.RequiredAmount;
            else if (objective is ReachBottomObjective reachBottom) bottomsNeeded += reachBottom.RequiredAmount;
        }
    }

    private void ApplyRandomize(Grid grid, bool obstacles, bool bottoms)
    {
        var tileObjectsProp = _serializedLevel.FindProperty("tileObjects");

        Match3LevelRandomizer.Randomize(tileObjectsProp, grid, new Match3LevelRandomizer.Options
        {
            ObstacleCount = _obstacleCount,
            BottomCount = _bottomCount,
            Seed = _seed,
            MinObstacleSpacing = _minObstacleSpacing,
            KeepObstaclesFromUnderBottoms = _keepObstaclesFromUnderBottoms,
            RandomizeObstacles = obstacles,
            RandomizeBottoms = bottoms
        });

        // Goes through SerializedProperty so the whole roll is one undo step
        _serializedLevel.ApplyModifiedProperties();
        GUI.changed = true;
    }

    private void DrawPaintModeButton(string label, Match3TileObjectType mode)
    {
        Color previousBackground = GUI.backgroundColor;
        GUI.backgroundColor = _currentPaintMode == mode ? Color.yellow : Color.white;

        if (GUILayout.Button(label, GUILayout.Height(30)))
        {
            _currentPaintMode = _currentPaintMode == mode ? Match3TileObjectType.Matchable : mode;
        }

        GUI.backgroundColor = previousBackground;
    }

    private void HandlePainting(Rect gridRect, Grid grid, SerializedProperty tileObjectsProp)
    {
        Event e = Event.current;

        if (e.type == EventType.MouseUp)
        {
            _isDragging = false;
            return;
        }

        bool isPress = e.type == EventType.MouseDown;
        bool isDrag = e.type == EventType.MouseDrag && _isDragging;
        if (!isPress && !isDrag) return;
        if (!gridRect.Contains(e.mousePosition)) return;

        if (!Match3LevelGridGUI.TryGetCellIndex(gridRect, grid, e.mousePosition, CellSize, out int index)) return;
        if (index >= tileObjectsProp.arraySize) return;

        if (isPress) _isDragging = true;

        var element = tileObjectsProp.GetArrayElementAtIndex(index);
        var currentType = (Match3TileObjectType)element.enumValueIndex;

        // Painting the mode already on a cell clears it, so the same button both paints and erases
        element.enumValueIndex = (int)(currentType == _currentPaintMode ? Match3TileObjectType.Matchable : _currentPaintMode);

        tileObjectsProp.serializedObject.ApplyModifiedProperties();
        GUI.changed = true;
        e.Use();
    }

    private void ClearAll(SerializedProperty tileObjectsProp)
    {
        for (int i = 0; i < tileObjectsProp.arraySize; i++)
        {
            tileObjectsProp.GetArrayElementAtIndex(i).enumValueIndex = (int)Match3TileObjectType.Matchable;
        }

        tileObjectsProp.serializedObject.ApplyModifiedProperties();
        GUI.changed = true;
    }

    private void ClearType(SerializedProperty tileObjectsProp, Grid grid, Match3TileObjectType typeToRemove)
    {
        for (int y = 0; y < grid.Height; y++)
        {
            for (int x = 0; x < grid.Width; x++)
            {
                if (!grid.IsCellActive(x, y)) continue;

                int index = y * grid.Width + x;
                if (index >= tileObjectsProp.arraySize) continue;

                var element = tileObjectsProp.GetArrayElementAtIndex(index);
                if ((Match3TileObjectType)element.enumValueIndex == typeToRemove)
                {
                    element.enumValueIndex = (int)Match3TileObjectType.Matchable;
                }
            }
        }

        tileObjectsProp.serializedObject.ApplyModifiedProperties();
        GUI.changed = true;
    }

    private void CreateLevel()
    {
        string path = AssetDatabase.GenerateUniqueAssetPath($"{_levelsFolder}/New Level.asset");

        var level = CreateInstance<SOMatch3Level>();
        AssetDatabase.CreateAsset(level, path);

        ApplyAssetNameToLevelName(level);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        RefreshLevels();
        SelectLevel(level);
        EditorGUIUtility.PingObject(level);
        Repaint();
    }

    private void DuplicateSelectedLevel()
    {
        if (!_selectedLevel) return;

        string sourcePath = AssetDatabase.GetAssetPath(_selectedLevel);
        if (string.IsNullOrEmpty(sourcePath)) return;

        string path = AssetDatabase.GenerateUniqueAssetPath(sourcePath);

        // CopyAsset rather than Instantiate, so the SerializeReference objectives and
        // conditions come across as real copies rather than shared references
        if (!AssetDatabase.CopyAsset(sourcePath, path))
        {
            Debug.LogError($"Could not duplicate {sourcePath}");
            return;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        var copy = AssetDatabase.LoadAssetAtPath<SOMatch3Level>(path);
        ApplyAssetNameToLevelName(copy);
        AssetDatabase.SaveAssets();

        RefreshLevels();
        SelectLevel(copy);
        EditorGUIUtility.PingObject(copy);
        Repaint();
    }

    /// <summary>Keeps the in-game level name in step with the asset, which is what a new or copied level needs.</summary>
    private static void ApplyAssetNameToLevelName(SOMatch3Level level)
    {
        if (!level) return;

        var serialized = new SerializedObject(level);
        serialized.FindProperty("levelName").stringValue = level.name;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private void SetFolder(string folder)
    {
        _levelsFolder = folder;
        EditorPrefs.SetString(FolderPrefKey, folder ?? string.Empty);
        RefreshLevels();
    }

    private void SelectLevel(SOMatch3Level level)
    {
        _selectedLevel = level;
        _serializedLevel = level ? new SerializedObject(level) : null;
        _isDragging = false;
        _editorScroll = Vector2.zero;
    }

    private void RefreshLevels()
    {
        _levels.Clear();
        _playOrder = Match3LevelRegistry.GetLevels();

        if (string.IsNullOrEmpty(_levelsFolder) || !AssetDatabase.IsValidFolder(_levelsFolder))
        {
            _validation.Clear();
            Repaint();
            return;
        }

        foreach (string guid in AssetDatabase.FindAssets($"t:{nameof(SOMatch3Level)}", new[] { _levelsFolder }))
        {
            var level = AssetDatabase.LoadAssetAtPath<SOMatch3Level>(AssetDatabase.GUIDToAssetPath(guid));
            if (level) _levels.Add(level);
        }

        _levels.Sort((a, b) => EditorUtility.NaturalCompare(a.name, b.name));

        CacheValidation();

        // A selected level that is no longer in the folder would otherwise keep showing in the right pane
        if (_selectedLevel && !_levels.Contains(_selectedLevel)) SelectLevel(null);

        Repaint();
    }
}
#endif
