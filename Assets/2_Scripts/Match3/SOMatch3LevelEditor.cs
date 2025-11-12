

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SOMatch3Level))]
public class SOMatch3LevelEditor : UnityEditor.Editor
{
    private const float CellSize = 16f;
    private const float CellBorder = 1f;
    private const float Spacing = 5f;
    
    private static readonly Color BackgroundColor = new Color(0.2f, 0.2f, 0.2f);
    private static readonly Color ActiveTileColor = new Color(0.3f, 0.7f, 0.3f);
    private static readonly Color InactiveTileColor = new Color(0.4f, 0.4f, 0.4f);
    private static readonly Color BreakableTileColor = new Color(0.8f, 0.3f, 0.3f);
    private static readonly Color GridLineColor = new Color(0.1f, 0.1f, 0.1f);
    private static readonly Color HoverColor = new Color(1f, 1f, 1f, 0.3f);

    private bool _isDragging;
    private bool _dragState;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SOMatch3Level level = (SOMatch3Level)target;
        
        // Draw default fields except tiles
        SerializedProperty levelNameProp = serializedObject.FindProperty("levelName");
        SerializedProperty gridShapeProp = serializedObject.FindProperty("gridShape");
        SerializedProperty matchObjectsProp = serializedObject.FindProperty("matchObjects");
        SerializedProperty objectivesProp = serializedObject.FindProperty("objectives");
        SerializedProperty loseConditionsProp = serializedObject.FindProperty("loseConditions");

        EditorGUILayout.PropertyField(levelNameProp);
        EditorGUILayout.PropertyField(gridShapeProp);
        EditorGUILayout.PropertyField(matchObjectsProp);
        EditorGUILayout.PropertyField(objectivesProp);
        EditorGUILayout.PropertyField(loseConditionsProp);

        EditorGUILayout.Space(10);

        // Draw breakable tiles grid
        if (gridShapeProp.objectReferenceValue != null)
        {
            SOGridShape gridShape = (SOGridShape)gridShapeProp.objectReferenceValue;
            Grid grid = gridShape.Grid;
            
            if (grid != null)
            {
                // Initialize tiles array if needed
                SerializedProperty tilesProp = serializedObject.FindProperty("tiles");
                int requiredSize = grid.Width * grid.Height;
                
                if (tilesProp.arraySize != requiredSize)
                {
                    tilesProp.arraySize = requiredSize;
                    serializedObject.ApplyModifiedProperties();
                }

                DrawBreakableTilesGrid(grid, tilesProp);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Assign a Grid Shape to edit breakable tiles.", MessageType.Info);
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawBreakableTilesGrid(Grid grid, SerializedProperty tilesProp)
    {
        int width = grid.Width;
        int height = grid.Height;

        EditorGUILayout.LabelField("Breakable Tiles", EditorStyles.boldLabel);
        
        // Active breakable tile count
        int breakableCount = GetBreakableCount(tilesProp);
        EditorGUILayout.LabelField($"Breakable Tiles: {breakableCount} / {grid.ActiveCellCount}");
        
        EditorGUILayout.Space(5);

        // Calculate grid rect
        float gridWidth = width * CellSize;
        float gridHeight = height * CellSize;
        
        Rect controlRect = GUILayoutUtility.GetRect(gridWidth, gridHeight);
        Rect gridRect = new Rect(
            controlRect.x + (controlRect.width - gridWidth) / 2,
            controlRect.y,
            gridWidth,
            gridHeight
        );

        DrawGrid(gridRect, grid, tilesProp);

        EditorGUILayout.Space(5);

        // Buttons
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("None Breakable"))
        {
            SetAllBreakable(tilesProp, grid, false);
        }
        
        if (GUILayout.Button("Invert Breakable"))
        {
            InvertBreakable(tilesProp, grid);
        }
        
        EditorGUILayout.EndHorizontal();
    }

    private void DrawGrid(Rect gridRect, Grid grid, SerializedProperty tilesProp)
    {
        Event e = Event.current;
        int width = grid.Width;
        int height = grid.Height;

        EditorGUI.DrawRect(gridRect, BackgroundColor);

        // Handle mouse input
        if (e.type == EventType.MouseDown && gridRect.Contains(e.mousePosition))
        {
            int x = Mathf.FloorToInt((e.mousePosition.x - gridRect.x) / CellSize);
            int visualY = Mathf.FloorToInt((e.mousePosition.y - gridRect.y) / CellSize);
            int y = height - 1 - visualY;

            if (x >= 0 && x < width && y >= 0 && y < height && grid.IsCellActive(x, y))
            {
                _isDragging = true;
                int index = y * width + x;
                _dragState = !tilesProp.GetArrayElementAtIndex(index).boolValue;
                tilesProp.GetArrayElementAtIndex(index).boolValue = _dragState;
                tilesProp.serializedObject.ApplyModifiedProperties();
                GUI.changed = true;
                e.Use();
            }
        }
        else if (e.type == EventType.MouseDrag && _isDragging && gridRect.Contains(e.mousePosition))
        {
            int x = Mathf.FloorToInt((e.mousePosition.x - gridRect.x) / CellSize);
            int visualY = Mathf.FloorToInt((e.mousePosition.y - gridRect.y) / CellSize);
            int y = height - 1 - visualY;

            if (x >= 0 && x < width && y >= 0 && y < height && grid.IsCellActive(x, y))
            {
                int index = y * width + x;
                tilesProp.GetArrayElementAtIndex(index).boolValue = _dragState;
                tilesProp.serializedObject.ApplyModifiedProperties();
                GUI.changed = true;
                e.Use();
            }
        }
        else if (e.type == EventType.MouseUp)
        {
            _isDragging = false;
        }

        // Draw cells
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                if (index >= tilesProp.arraySize) continue;

                bool isActive = grid.IsCellActive(x, y);
                bool isBreakable = tilesProp.GetArrayElementAtIndex(index).boolValue;

                int visualY = height - 1 - y;
                Rect cellRect = new Rect(
                    gridRect.x + x * CellSize,
                    gridRect.y + visualY * CellSize,
                    CellSize - CellBorder,
                    CellSize - CellBorder
                );

                Color cellColor;
                if (!isActive)
                {
                    cellColor = InactiveTileColor;
                }
                else if (isBreakable)
                {
                    cellColor = BreakableTileColor;
                }
                else
                {
                    cellColor = ActiveTileColor;
                }

                EditorGUI.DrawRect(cellRect, cellColor);

                if (cellRect.Contains(Event.current.mousePosition))
                {
                    EditorGUI.DrawRect(cellRect, HoverColor);
                }
            }
        }

        // Draw grid lines
        Handles.color = GridLineColor;
        for (int x = 0; x <= width; x++)
        {
            float xPos = gridRect.x + x * CellSize;
            Handles.DrawLine(new Vector3(xPos, gridRect.y), new Vector3(xPos, gridRect.y + gridRect.height));
        }
        for (int y = 0; y <= height; y++)
        {
            float yPos = gridRect.y + y * CellSize;
            Handles.DrawLine(new Vector3(gridRect.x, yPos), new Vector3(gridRect.x + gridRect.width, yPos));
        }

        if (gridRect.Contains(Event.current.mousePosition))
        {
            HandleUtility.Repaint();
        }
    }

    private void SetAllBreakable(SerializedProperty tilesProp, Grid grid, bool value)
    {
        for (int y = 0; y < grid.Height; y++)
        {
            for (int x = 0; x < grid.Width; x++)
            {
                if (grid.IsCellActive(x, y))
                {
                    int index = y * grid.Width + x;
                    tilesProp.GetArrayElementAtIndex(index).boolValue = value;
                }
            }
        }
        tilesProp.serializedObject.ApplyModifiedProperties();
        GUI.changed = true;
    }

    private void InvertBreakable(SerializedProperty tilesProp, Grid grid)
    {
        for (int y = 0; y < grid.Height; y++)
        {
            for (int x = 0; x < grid.Width; x++)
            {
                if (grid.IsCellActive(x, y))
                {
                    int index = y * grid.Width + x;
                    SerializedProperty element = tilesProp.GetArrayElementAtIndex(index);
                    element.boolValue = !element.boolValue;
                }
            }
        }
        tilesProp.serializedObject.ApplyModifiedProperties();
        GUI.changed = true;
    }
    
    private int GetBreakableCount(SerializedProperty tilesProp)
    {
        int count = 0;
        for (int i = 0; i < tilesProp.arraySize; i++)
        {
            if (tilesProp.GetArrayElementAtIndex(i).boolValue)
                count++;
        }
        return count;
    }
}

#endif