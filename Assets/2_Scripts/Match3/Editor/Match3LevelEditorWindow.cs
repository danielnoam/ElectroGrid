using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

internal class Match3LevelEditorWindow : EditorWindow
{
    private const string FolderPrefKey = "ElectroGrid.LevelEditor.Folder";
    private const string DefaultFolder = "Assets/3_Data/Levels";
    private const float SidebarWidth = 270f;
    private const float CellSize = Match3LevelGridGUI.DefaultCellSize;
    private static readonly Vector2 MinWindowSize = new Vector2(620f, 420f);

    // Serialized so it survives an assembly reload, and mirrored into EditorPrefs so it survives a restart
    [SerializeField] private string _levelsFolder;
    [SerializeField] private SOMatch3Level _selectedLevel;

    private readonly List<SOMatch3Level> _levels = new List<SOMatch3Level>();
    private readonly Dictionary<SOMatch3Level, Match3LevelValidation.Severity?> _validation =
        new Dictionary<SOMatch3Level, Match3LevelValidation.Severity?>();
    private List<SOMatch3Level> _playOrder = new List<SOMatch3Level>();
    private SerializedObject _serializedLevel;

    // Anything that selects, creates or reorders assets changes the layout, so it runs after the GUI pass
    private Action _deferredAction;

    private Vector2 _sidebarScroll;
    private Vector2 _editorScroll;
    private ReorderableList _orderList;
    private readonly List<SOMatch3Level> _unlisted = new List<SOMatch3Level>();
    private bool _unlistedFoldout = true;
    private GUIStyle _rowStyle;
    private GUIStyle _selectedRowStyle;

    private Match3TileObjectType _currentPaintMode = Match3TileObjectType.Obstacle;
    private bool _isDragging;

    private bool _shapeFoldout;
    private int _shapeWidth = 8;
    private int _shapeHeight = 8;
    private int _shapeSeed;
    private float _shapeDensity = 0.55f;
    private int _shapeSmoothing = 3;
    private Match3GridShapeRandomizer.SymmetryMode _shapeSymmetry = Match3GridShapeRandomizer.SymmetryMode.Horizontal;
    private bool _shapeRemoveIsolated = true;
    private bool _shapeKeepLargestRegion = true;

    private bool _tileObjectsFoldout;
    private int _obstacleCount = 1;
    private int _bottomCount = 1;
    private int _seed;
    private int _minObstacleSpacing;
    private bool _keepObstaclesFromUnderBottoms = true;

    [MenuItem("ElectroGrid/Level Editor")]
    public static void Open()
    {
        Open(null);
    }

    public static void Open(SOMatch3Level level)
    {
        var window = GetWindow<Match3LevelEditorWindow>("Level Editor");
        window.minSize = MinWindowSize;
        window.Show();

        if (!level) return;

        // Follow the level that was opened, otherwise the sidebar would not list it
        string folder = GetAssetFolder(level);
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

    private void OnProjectChange()
    {
        if (!Match3LevelRegistry.Prefab) Match3LevelRegistry.ClearCache();
        RefreshLevels();
    }

    private void OnGUI()
    {
        EditorGUILayout.BeginHorizontal();

        DrawSidebar();
        DrawEditor();

        EditorGUILayout.EndHorizontal();

        if (_deferredAction == null) return;

        var action = _deferredAction;
        _deferredAction = null;
        action();
        Repaint();
    }

    private void Defer(Action action)
    {
        _deferredAction = action;
    }

    private void DrawSidebar()
    {
        _rowStyle ??= new GUIStyle(EditorStyles.label) { richText = true };
        _selectedRowStyle ??= new GUIStyle(_rowStyle) { fontStyle = FontStyle.Bold };

        EditorGUILayout.BeginVertical(GUILayout.Width(SidebarWidth), GUILayout.ExpandHeight(true));

        DrawSidebarToolbar();

        _sidebarScroll = EditorGUILayout.BeginScrollView(_sidebarScroll);

        if (Match3LevelRegistry.Prefab)
        {
            GetOrderList().DoLayoutList();
        }
        else
        {
            EditorGUILayout.HelpBox("No GameManager prefab found, so the play order cannot be shown or edited.", MessageType.Warning);
        }

        DrawUnlistedLevels();

        EditorGUILayout.EndScrollView();

        DrawFolderField();

        EditorGUILayout.EndVertical();
    }

    private void DrawSidebarToolbar()
    {
        bool hasFolder = !string.IsNullOrEmpty(_levelsFolder) && AssetDatabase.IsValidFolder(_levelsFolder);

        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        using (new EditorGUI.DisabledScope(!hasFolder))
        {
            if (GUILayout.Button("New", EditorStyles.toolbarButton)) Defer(CreateLevel);
        }

        using (new EditorGUI.DisabledScope(!_selectedLevel))
        {
            if (GUILayout.Button("Duplicate", EditorStyles.toolbarButton)) Defer(() => DuplicateLevel(_selectedLevel));
        }

        GUILayout.FlexibleSpace();

        using (new EditorGUI.DisabledScope(_playOrder.Count == 0 && _levels.Count == 0))
        {
            if (GUILayout.Button("Validate All", EditorStyles.toolbarButton)) Defer(ValidateAll);
        }

        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// The GameManager array is the play order, not just a bag of levels — Match3GameManager walks it
    /// by index — so this list is the progression itself, and dragging a row reorders the game.
    /// </summary>
    private ReorderableList GetOrderList()
    {
        if (_orderList != null && _orderList.list == _playOrder) return _orderList;

        _orderList = new ReorderableList(_playOrder, typeof(SOMatch3Level), true, true, false, false)
        {
            elementHeight = EditorGUIUtility.singleLineHeight + 4f,
            drawHeaderCallback = rect => EditorGUI.LabelField(rect, $"Play Order ({_playOrder.Count})", EditorStyles.boldLabel),
            drawElementCallback = DrawOrderRow,
            onReorderCallback = _ => Defer(CommitPlayOrder),
            onSelectCallback = list =>
            {
                var level = list.index >= 0 && list.index < _playOrder.Count ? _playOrder[list.index] : null;
                if (level) Defer(() => SelectLevel(level));
            }
        };

        _orderList.index = _playOrder.IndexOf(_selectedLevel);

        return _orderList;
    }

    private void DrawOrderRow(Rect rect, int index, bool isActive, bool isFocused)
    {
        if (index < 0 || index >= _playOrder.Count) return;

        var level = _playOrder[index];
        rect.y += 2f;
        rect.height = EditorGUIUtility.singleLineHeight;

        string name = level ? level.name : "(missing)";
        string dot = level ? ValidationDot(level) : "<color=#FF5252>●</color>";
        var style = level == _selectedLevel ? _selectedRowStyle : _rowStyle;
        EditorGUI.LabelField(rect, $"{index + 1,2}.  {dot}  {name}", style);

        Event e = Event.current;
        if (e.type != EventType.ContextClick || !rect.Contains(e.mousePosition)) return;

        ShowLevelMenu(level, index);
        e.Use();
    }

    private void ShowLevelMenu(SOMatch3Level level, int orderIndex)
    {
        var menu = new GenericMenu();

        if (level)
        {
            menu.AddItem(new GUIContent("Ping"), false, () => EditorGUIUtility.PingObject(level));
            menu.AddItem(new GUIContent("Duplicate"), false, () => Defer(() => DuplicateLevel(level)));
        }

        if (orderIndex >= 0)
        {
            menu.AddItem(new GUIContent("Remove From Play Order"), false, () => Defer(() => RemoveFromPlayOrder(orderIndex)));
        }
        else if (level)
        {
            menu.AddItem(new GUIContent("Add To Play Order"), false, () => Defer(() => AddToPlayOrder(level)));
        }

        if (level)
        {
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Delete Level..."), false, () => Defer(() => DeleteLevel(level)));
        }

        menu.ShowAsContext();
    }

    /// <summary>Levels in the folder that the game can never reach, because nothing puts them in the play order.</summary>
    private void DrawUnlistedLevels()
    {
        _unlisted.Clear();
        foreach (var level in _levels)
        {
            if (level && !_playOrder.Contains(level)) _unlisted.Add(level);
        }

        if (_unlisted.Count == 0) return;

        EditorGUILayout.Space(6);
        EditorGUILayout.BeginHorizontal();
        _unlistedFoldout = EditorGUILayout.Foldout(_unlistedFoldout, $"Not In Game ({_unlisted.Count})", true, EditorStyles.foldoutHeader);

        using (new EditorGUI.DisabledScope(!Match3LevelRegistry.Prefab))
        {
            if (GUILayout.Button("Add All", EditorStyles.miniButton, GUILayout.Width(60f))) Defer(AddUnregisteredToPlayOrder);
        }

        EditorGUILayout.EndHorizontal();

        if (!_unlistedFoldout) return;

        foreach (var level in _unlisted)
        {
            EditorGUILayout.BeginHorizontal();

            var style = level == _selectedLevel ? _selectedRowStyle : _rowStyle;
            if (GUILayout.Button($"{ValidationDot(level)}  {level.name}", style)) Defer(() => SelectLevel(level));

            Rect rowRect = GUILayoutUtility.GetLastRect();
            Event e = Event.current;
            if (e.type == EventType.ContextClick && rowRect.Contains(e.mousePosition))
            {
                ShowLevelMenu(level, -1);
                e.Use();
            }

            using (new EditorGUI.DisabledScope(!Match3LevelRegistry.Prefab))
            {
                if (GUILayout.Button("Add", EditorStyles.miniButton, GUILayout.Width(40f))) Defer(() => AddToPlayOrder(level));
            }

            EditorGUILayout.EndHorizontal();
        }
    }

    private void DrawFolderField()
    {
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("New Levels Folder", EditorStyles.miniBoldLabel);

        var folderAsset = string.IsNullOrEmpty(_levelsFolder)
            ? null
            : AssetDatabase.LoadAssetAtPath<DefaultAsset>(_levelsFolder);

        EditorGUI.BeginChangeCheck();
        var picked = (DefaultAsset)EditorGUILayout.ObjectField(folderAsset, typeof(DefaultAsset), false);
        if (!EditorGUI.EndChangeCheck()) return;

        string path = picked ? AssetDatabase.GetAssetPath(picked) : string.Empty;

        if (string.IsNullOrEmpty(path) || AssetDatabase.IsValidFolder(path)) SetFolder(path);
        else Debug.LogWarning($"{path} is not a folder");
    }

    private string ValidationDot(SOMatch3Level level)
    {
        if (!_validation.TryGetValue(level, out var severity)) return "<color=#808080>●</color>";
        if (severity == null) return "<color=#6FCF6F>●</color>";

        return severity == Match3LevelValidation.Severity.Error
            ? "<color=#FF5252>●</color>"
            : "<color=#FFC107>●</color>";
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
    }

    private void ValidateAll()
    {
        _validation.Clear();

        int errors = 0;
        int warnings = 0;

        foreach (var level in _levels)
        {
            if (!level) continue;

            var issues = Match3LevelValidation.Validate(level);
            var severity = Match3LevelValidation.WorstSeverity(issues);
            _validation[level] = severity;

            if (severity == null) continue;

            if (severity == Match3LevelValidation.Severity.Error) errors++;
            else warnings++;

            foreach (var issue in issues)
            {
                string message = $"{level.name}: {issue.Message}";

                if (issue.Severity == Match3LevelValidation.Severity.Error) Debug.LogError(message, level);
                else Debug.LogWarning(message, level);
            }
        }

        Debug.Log(errors == 0 && warnings == 0
            ? $"Level Editor: all {_levels.Count} levels are clean."
            : $"Level Editor: {errors} level(s) with errors and {warnings} with warnings, out of {_levels.Count}.");
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
        if (level) _validation[level] = Match3LevelValidation.WorstSeverity(Match3LevelValidation.Validate(level));
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

        DrawLevelHeader();

        EditorGUILayout.Space(4);
        EditorGUILayout.PropertyField(_serializedLevel.FindProperty("levelName"));
        EditorGUILayout.PropertyField(_serializedLevel.FindProperty("matchObjects"));
        EditorGUILayout.PropertyField(_serializedLevel.FindProperty("objectives"));
        EditorGUILayout.PropertyField(_serializedLevel.FindProperty("loseConditions"));

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Level Grid", EditorStyles.boldLabel);

        var gridShapeProp = _serializedLevel.FindProperty("gridShape");
        EditorGUILayout.PropertyField(gridShapeProp);

        var shape = (SOGridShape)gridShapeProp.objectReferenceValue;
        var grid = shape ? shape.Grid : null;

        DrawTileObjectPainter(shape, grid);
        DrawShapeRandomizer(shape);
        DrawTileObjectRandomizer(grid);
        DrawValidation();

        EditorGUILayout.EndScrollView();

        if (_serializedLevel.ApplyModifiedProperties()) CacheValidation(_selectedLevel);

        EditorGUILayout.EndVertical();
    }

    private void DrawLevelHeader()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(_selectedLevel.name, EditorStyles.boldLabel);

        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
        {
            if (GUILayout.Button("Play This Level", GUILayout.Width(120f)))
            {
                var levelToPlay = _selectedLevel;
                Defer(() => Match3LevelPlayer.Play(levelToPlay));
            }
        }

        EditorGUILayout.EndHorizontal();

        if (_playOrder.Contains(_selectedLevel)) return;

        EditorGUILayout.HelpBox("Not in the GameManager play order, so the game can never reach it.", MessageType.Warning);

        if (GUILayout.Button("Add To Play Order"))
        {
            var levelToAdd = _selectedLevel;
            Defer(() => AddToPlayOrder(levelToAdd));
        }
    }

    private void DrawTileObjectPainter(SOGridShape shape, Grid grid)
    {
        if (!shape)
        {
            EditorGUILayout.HelpBox("Assign a Grid Shape to paint tile objects.", MessageType.Info);
            return;
        }

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

        Rect gridRect = Match3LevelGridGUI.GetCenteredGridRect(grid, CellSize);

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
        if (GUILayout.Button("Clear All")) ResetCells(tileObjectsProp, grid, (_, _, _) => true);
        if (GUILayout.Button("Clear Obstacles")) ResetCells(tileObjectsProp, grid, (x, y, type) => grid.IsCellActive(x, y) && type == Match3TileObjectType.Obstacle);
        if (GUILayout.Button("Clear Bottom")) ResetCells(tileObjectsProp, grid, (x, y, type) => grid.IsCellActive(x, y) && type == Match3TileObjectType.Bottom);
        EditorGUILayout.EndHorizontal();
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

    /// <summary>Sets every cell the predicate accepts back to a plain matchable piece.</summary>
    private static void ResetCells(SerializedProperty tileObjectsProp, Grid grid, Func<int, int, Match3TileObjectType, bool> shouldReset)
    {
        for (int y = 0; y < grid.Height; y++)
        {
            for (int x = 0; x < grid.Width; x++)
            {
                int index = y * grid.Width + x;
                if (index >= tileObjectsProp.arraySize) continue;

                var element = tileObjectsProp.GetArrayElementAtIndex(index);
                if (!shouldReset(x, y, (Match3TileObjectType)element.enumValueIndex)) continue;

                element.enumValueIndex = (int)Match3TileObjectType.Matchable;
            }
        }

        tileObjectsProp.serializedObject.ApplyModifiedProperties();
        GUI.changed = true;
    }

    private void DrawShapeRandomizer(SOGridShape currentShape)
    {
        EditorGUILayout.Space(10);
        _shapeFoldout = EditorGUILayout.Foldout(_shapeFoldout, "Randomize Grid Shape", true, EditorStyles.foldoutHeader);
        if (!_shapeFoldout) return;

        EditorGUI.indentLevel++;

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
        _shapeSeed = DrawSeedField(_shapeSeed);

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
                Defer(() => RandomizeShape(false));
            }
        }

        if (GUILayout.Button("Randomize Into New Shape")) Defer(() => RandomizeShape(true));

        EditorGUILayout.EndHorizontal();

        EditorGUI.indentLevel--;
    }

    private static int DrawSeedField(int seed)
    {
        EditorGUILayout.BeginHorizontal();
        seed = EditorGUILayout.IntField("Seed", seed);
        if (GUILayout.Button("New", GUILayout.Width(60))) seed = Random.Range(int.MinValue, int.MaxValue);
        EditorGUILayout.EndHorizontal();

        return seed;
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
            string folder = GetAssetFolder(shape ? shape : _selectedLevel);
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

        ResetCells(tileObjectsProp, grid, (x, y, _) => !grid.IsCellActive(x, y));
    }

    private void DrawTileObjectRandomizer(Grid grid)
    {
        if (grid == null) return;

        EditorGUILayout.Space(6);
        _tileObjectsFoldout = EditorGUILayout.Foldout(_tileObjectsFoldout, "Randomize Tile Objects", true, EditorStyles.foldoutHeader);
        if (!_tileObjectsFoldout) return;

        EditorGUI.indentLevel++;

        Match3LevelValidation.GetRequiredCounts(_selectedLevel, out int obstaclesNeeded, out int bottomsNeeded);

        _obstacleCount = DrawCountField("Double Stars", _obstacleCount, obstaclesNeeded);
        _bottomCount = DrawCountField("Square Stars", _bottomCount, bottomsNeeded);
        _seed = DrawSeedField(_seed);
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

    private static int DrawCountField(string label, int value, int needed)
    {
        EditorGUILayout.BeginHorizontal();
        value = Mathf.Max(0, EditorGUILayout.IntField(label, value));

        using (new EditorGUI.DisabledScope(needed <= 0))
        {
            if (GUILayout.Button($"From objectives ({needed})", GUILayout.Width(150))) value = needed;
        }

        EditorGUILayout.EndHorizontal();

        return value;
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

        Match3LevelGridGUI.DrawIssues(issues);
    }

    private void CreateLevel()
    {
        string path = AssetDatabase.GenerateUniqueAssetPath($"{_levelsFolder}/New Level.asset");

        var level = CreateInstance<SOMatch3Level>();
        AssetDatabase.CreateAsset(level, path);

        ApplyAssetNameToLevelName(level);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        ShowNewLevel(level);
    }

    private void DuplicateLevel(SOMatch3Level source)
    {
        if (!source) return;

        string sourcePath = AssetDatabase.GetAssetPath(source);
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

        ShowNewLevel(copy);
    }

    /// <summary>Takes the level out of the play order first, then moves the asset to the OS trash so it can be recovered.</summary>
    private void DeleteLevel(SOMatch3Level level)
    {
        if (!level) return;

        string path = AssetDatabase.GetAssetPath(level);
        if (!EditorUtility.DisplayDialog("Delete Level", $"Delete {level.name}?\n\n{path}\n\nIt is moved to the trash and removed from the play order.", "Delete", "Cancel")) return;

        if (_playOrder.Remove(level)) CommitPlayOrder();
        if (level == _selectedLevel) SelectLevel(null);

        if (!AssetDatabase.MoveAssetToTrash(path)) Debug.LogError($"Could not delete {path}");

        RefreshLevels();
    }

    private void ShowNewLevel(SOMatch3Level level)
    {
        RefreshLevels();
        SelectLevel(level);
        EditorGUIUtility.PingObject(level);
    }

    /// <summary>Keeps the in-game level name in step with the asset, which is what a new or copied level needs.</summary>
    private static void ApplyAssetNameToLevelName(SOMatch3Level level)
    {
        if (!level) return;

        var serialized = new SerializedObject(level);
        serialized.FindProperty("levelName").stringValue = level.name;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static string GetAssetFolder(Object asset)
    {
        return Path.GetDirectoryName(AssetDatabase.GetAssetPath(asset))?.Replace('\\', '/');
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

        if (_orderList != null) _orderList.index = level ? _playOrder.IndexOf(level) : -1;
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
