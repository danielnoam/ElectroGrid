#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SOMatch3Level))]
public class SOMatch3LevelEditor : UnityEditor.Editor
{
    private Match3TileObjectType _currentPaintMode = Match3TileObjectType.Matchable;
    private bool _isDragging;
    
    private const float CellSize = 20f;
    private const float CellBorder = 1f;
    private const float GridSpacing = 10f;
    
    private static readonly Color BackgroundColor = new Color(0.2f, 0.2f, 0.2f);
    private static readonly Color ActiveTileColor = new Color(0.3f, 0.7f, 0.3f);
    private static readonly Color InactiveTileColor = new Color(0.4f, 0.4f, 0.4f);
    private static readonly Color ObstacleTileColor = new Color(0.8f, 0.3f, 0.3f);
    private static readonly Color BottomObjectTileColor = new Color(0.3f, 0.5f, 0.9f);
    private static readonly Color GridLineColor = new Color(0.1f, 0.1f, 0.1f);
    private static readonly Color HoverColor = new Color(1f, 1f, 1f, 0.3f);
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SOMatch3Level level = (SOMatch3Level)target;
        
        SerializedProperty levelNameProp = serializedObject.FindProperty("levelName");
        SerializedProperty gridShapeProp = serializedObject.FindProperty("gridShape");
        SerializedProperty matchObjectsProp = serializedObject.FindProperty("matchObjects");
        SerializedProperty objectivesProp = serializedObject.FindProperty("objectives");
        SerializedProperty loseConditionsProp = serializedObject.FindProperty("loseConditions");
        SerializedProperty tileObjectsProp = serializedObject.FindProperty("tileObjects");

        EditorGUILayout.PropertyField(levelNameProp);

        EditorGUILayout.PropertyField(matchObjectsProp);
        EditorGUILayout.PropertyField(objectivesProp);
        EditorGUILayout.PropertyField(loseConditionsProp);
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Level Grid:", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(gridShapeProp);
        
        DrawTileObjectPainter(level, tileObjectsProp, gridShapeProp);

        serializedObject.ApplyModifiedProperties();
    }
    
    private void DrawTileObjectPainter(SOMatch3Level level, SerializedProperty tileObjectsProp, SerializedProperty gridShapeProp)
    {
        if (gridShapeProp.objectReferenceValue == null)
        {
            EditorGUILayout.HelpBox("Assign a Grid Shape to paint tile objects.", MessageType.Info);
            return;
        }

        SOGridShape gridShape = (SOGridShape)gridShapeProp.objectReferenceValue;
        Grid grid = gridShape.Grid;

        if (grid == null)
        {
            EditorGUILayout.HelpBox("Grid Shape has no valid grid.", MessageType.Warning);
            return;
        }

        int width = grid.Width;
        int height = grid.Height;
        int requiredSize = width * height;

        if (tileObjectsProp.arraySize != requiredSize)
        {
            tileObjectsProp.arraySize = requiredSize;
            serializedObject.ApplyModifiedProperties();
        }


        int obstacleCount = level.CountObjectsOfType(Match3TileObjectType.Obstacle);
        int bottomObjectCount = level.CountObjectsOfType(Match3TileObjectType.Bottom);
        int matchableCount = level.GridShape.Grid.ActiveCellCount - obstacleCount - bottomObjectCount;
        int obstacleNeeded = 0;
        int bottomObjectNeeded = 0;

        foreach (var objective in level.Objectives)
        {
            if (objective is ClearObstaclesObjective clearObstaclesObjective)
            {
                obstacleNeeded += clearObstaclesObjective.RequiredAmount;
            }
            else if (objective is ReachBottomObjective reachBottomObjective)
            {
                bottomObjectNeeded += reachBottomObjective.RequiredAmount;
            }
        }
        
        EditorGUILayout.LabelField($"Matchable: {matchableCount} | Obstacles: {obstacleCount} / {obstacleNeeded} | Bottom Objects: {bottomObjectCount} / {bottomObjectNeeded}");
        
        EditorGUILayout.Space(5);
        

        float gridWidth = width * CellSize;
        float gridHeight = height * CellSize;
        Rect gridRect = GUILayoutUtility.GetRect(gridWidth, gridHeight);
        gridRect.x += (EditorGUIUtility.currentViewWidth - gridWidth) / 2 - 20;

        DrawGrid(gridRect, grid, tileObjectsProp);
        
        EditorGUILayout.Space(5);
        
        EditorGUILayout.BeginHorizontal();
        
        // GUI.backgroundColor = _currentPaintMode == Match3TileObjectType.Matchable ? Color.yellow : Color.white;
        // if (GUILayout.Button("None", GUILayout.Height(30)))
        // {
        //     _currentPaintMode = Match3TileObjectType.Matchable;
        // }
        //
        GUI.backgroundColor = _currentPaintMode == Match3TileObjectType.Obstacle ? Color.yellow : Color.white;
        if (GUILayout.Button("Obstacles", GUILayout.Height(30)))
        {
            _currentPaintMode = Match3TileObjectType.Obstacle;
        }
        
        GUI.backgroundColor = _currentPaintMode == Match3TileObjectType.Bottom ? Color.yellow : Color.white;
        if (GUILayout.Button("Bottom", GUILayout.Height(30)))
        {
            _currentPaintMode = Match3TileObjectType.Bottom;
        }
        
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Clear All"))
        {
            ClearAll(tileObjectsProp);
        }
        if (GUILayout.Button("Clear Obstacles"))
        {
            ClearType(tileObjectsProp, grid, Match3TileObjectType.Obstacle);
        }
        if (GUILayout.Button("Clear Bottom"))
        {
            ClearType(tileObjectsProp, grid, Match3TileObjectType.Bottom);
        }
        EditorGUILayout.EndHorizontal();
    }
    
    private void DrawGrid(Rect gridRect, Grid grid, SerializedProperty tileObjectsProp)
    {
        Event e = Event.current;
        int width = grid.Width;
        int height = grid.Height;

        float preciseWidth = width * CellSize;
        float preciseHeight = height * CellSize;


        Rect backgroundRect = new Rect(gridRect.x, gridRect.y, preciseWidth, preciseHeight);
        EditorGUI.DrawRect(backgroundRect, BackgroundColor);

        if (e.type == EventType.MouseDown && gridRect.Contains(e.mousePosition))
        {
            int x = Mathf.FloorToInt((e.mousePosition.x - gridRect.x) / CellSize);
            int visualY = Mathf.FloorToInt((e.mousePosition.y - gridRect.y) / CellSize);
            int y = height - 1 - visualY;

            if (x >= 0 && x < width && y >= 0 && y < height && grid.IsCellActive(x, y))
            {
                _isDragging = true;
                int index = y * width + x;
                
                Match3TileObjectType currentTileType = (Match3TileObjectType)tileObjectsProp.GetArrayElementAtIndex(index).enumValueIndex;
                Match3TileObjectType newType = currentTileType == _currentPaintMode 
                    ? Match3TileObjectType.Matchable 
                    : _currentPaintMode;
        
                tileObjectsProp.GetArrayElementAtIndex(index).enumValueIndex = (int)newType;
                tileObjectsProp.serializedObject.ApplyModifiedProperties();
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
                Match3TileObjectType currentTileType = (Match3TileObjectType)tileObjectsProp.GetArrayElementAtIndex(index).enumValueIndex;
                Match3TileObjectType newType = currentTileType == _currentPaintMode 
                    ? Match3TileObjectType.Matchable 
                    : _currentPaintMode;
                
                
                tileObjectsProp.GetArrayElementAtIndex(index).enumValueIndex = (int)newType;
                tileObjectsProp.serializedObject.ApplyModifiedProperties();
                GUI.changed = true;
                e.Use();
            }
        }
        else if (e.type == EventType.MouseUp)
        {
            _isDragging = false;
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                if (index >= tileObjectsProp.arraySize) continue;

                bool isActive = grid.IsCellActive(x, y);
                Match3TileObjectType tileObjectType = (Match3TileObjectType)tileObjectsProp.GetArrayElementAtIndex(index).enumValueIndex;

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
                else
                {
                    cellColor = tileObjectType switch
                    {
                        Match3TileObjectType.Obstacle => ObstacleTileColor,
                        Match3TileObjectType.Bottom => BottomObjectTileColor,
                        _ => ActiveTileColor
                    };
                }

                EditorGUI.DrawRect(cellRect, cellColor);

                if (cellRect.Contains(Event.current.mousePosition))
                {
                    EditorGUI.DrawRect(cellRect, HoverColor);
                }
            }
        }

        Handles.color = GridLineColor;
        float gridLineEndX = gridRect.x + width * CellSize;
        float gridLineEndY = gridRect.y + height * CellSize;
        
        for (int x = 0; x <= width; x++)
        {
            float xPos = gridRect.x + x * CellSize;
            Handles.DrawLine(new Vector3(xPos, gridRect.y), new Vector3(xPos, gridLineEndY));
        }
        for (int y = 0; y <= height; y++)
        {
            float yPos = gridRect.y + y * CellSize;
            Handles.DrawLine(new Vector3(gridRect.x, yPos), new Vector3(gridLineEndX, yPos));
        }

        if (gridRect.Contains(Event.current.mousePosition))
        {
            HandleUtility.Repaint();
        }
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
        int width = grid.Width;
        for (int y = 0; y < grid.Height; y++)
        {
            for (int x = 0; x < grid.Width; x++)
            {
                if (grid.IsCellActive(x, y))
                {
                    int index = y * width + x;
                    if ((Match3TileObjectType)tileObjectsProp.GetArrayElementAtIndex(index).enumValueIndex == typeToRemove)
                    {
                        tileObjectsProp.GetArrayElementAtIndex(index).enumValueIndex = (int)Match3TileObjectType.Matchable;
                    }
                }
            }
        }
        tileObjectsProp.serializedObject.ApplyModifiedProperties();
        GUI.changed = true;
    }
}

#endif