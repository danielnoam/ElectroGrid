using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR

[CustomPropertyDrawer(typeof(Grid))]
public class GridDrawer : PropertyDrawer
{
    private const int MinSize = 1;
    private const int MaxSize = 20;
    
    private const float CellSize = 16f;
    private const float ButtonHeight = 20f;
    private const float Spacing = 5f;
    private const float CellBorder = 1f;
    
    private static readonly Color BackgroundColor = new Color(0.2f, 0.2f, 0.2f);
    private static readonly Color ActiveTileColor = new Color(0.3f, 0.7f, 0.3f);
    private static readonly Color InactiveTileColor = new Color(0.4f, 0.4f, 0.4f);
    private static readonly Color GridLineColor = new Color(0.1f, 0.1f, 0.1f);
    private static readonly Color HoverColor = new Color(1f, 1f, 1f, 0.3f);

    private bool _isDragging;
    private bool _dragState;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        SerializedProperty gridSizeProp = property.FindPropertyRelative("gridSize");
        SerializedProperty tileSizeProp = property.FindPropertyRelative("tileSize");
        SerializedProperty tileSpacingProp = property.FindPropertyRelative("tileSpacing");
        
        int height = gridSizeProp.vector2IntValue.y;

        float gridHeight = height * CellSize;
        

        float tileSizeHeight = EditorGUI.GetPropertyHeight(tileSizeProp);
        float tileSpacingHeight = EditorGUI.GetPropertyHeight(tileSpacingProp);
        
        float totalHeight = EditorGUIUtility.singleLineHeight + Spacing + 
                           EditorGUIUtility.singleLineHeight + Spacing + 
                           EditorGUIUtility.singleLineHeight + Spacing +
                           tileSizeHeight + Spacing +
                           tileSpacingHeight + Spacing + 
                           EditorGUIUtility.singleLineHeight + Spacing + 
                           gridHeight + Spacing + 
                           ButtonHeight; 

        return totalHeight;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        SerializedProperty gridSizeProp = property.FindPropertyRelative("gridSize");
        SerializedProperty tileSizeProp = property.FindPropertyRelative("tileSize");
        SerializedProperty tileSpacingProp = property.FindPropertyRelative("tileSpacing");
        SerializedProperty tilesProp = property.FindPropertyRelative("tiles");

        Vector2Int gridSize = gridSizeProp.vector2IntValue;
        int width = gridSize.x;
        int height = gridSize.y;

        Rect currentRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

        // Label
        EditorGUI.LabelField(currentRect, label, EditorStyles.boldLabel);
        currentRect.y += EditorGUIUtility.singleLineHeight + Spacing;

        // Grid Width Slider
        EditorGUI.BeginChangeCheck();
        int newWidth = EditorGUI.IntSlider(currentRect, "Grid Width", width, MinSize, MaxSize);
        currentRect.y += EditorGUIUtility.singleLineHeight + Spacing;

        // Grid Height Slider
        int newHeight = EditorGUI.IntSlider(currentRect, "Grid Height", height, MinSize, MaxSize);
        currentRect.y += EditorGUIUtility.singleLineHeight + Spacing;

        if (EditorGUI.EndChangeCheck())
        {
            ResizeGrid(property, newWidth, newHeight);
            gridSize = new Vector2Int(newWidth, newHeight);
            width = newWidth;
            height = newHeight;
        }

        // Tile Size
        EditorGUI.PropertyField(currentRect, tileSizeProp, new GUIContent("Tile Size"));
        currentRect.y += EditorGUI.GetPropertyHeight(tileSizeProp) + Spacing;

        // Tile Spacing
        EditorGUI.PropertyField(currentRect, tileSpacingProp, new GUIContent("Tile Spacing"));
        currentRect.y += EditorGUI.GetPropertyHeight(tileSpacingProp) + Spacing;

        // Active Tiles Count
        int activeCount = GetActiveCount(tilesProp);
        EditorGUI.LabelField(currentRect, $"Active Tiles: {activeCount} / {width * height}");
        currentRect.y += EditorGUIUtility.singleLineHeight + Spacing;

        // Shape Painter (Grid Visualization)
        float gridWidth = width * CellSize;
        float gridHeight = height * CellSize;
        Rect gridRect = new Rect(currentRect.x + (currentRect.width - gridWidth) / 2, currentRect.y, gridWidth, gridHeight);

        DrawGrid(gridRect, width, height, tilesProp);

        currentRect.y += gridHeight + Spacing;

        // Buttons
        Rect buttonRect = new Rect(currentRect.x, currentRect.y, currentRect.width / 3 - 5, ButtonHeight);

        if (GUI.Button(buttonRect, "Activate All"))
        {
            SetAllTiles(tilesProp, true);
        }

        buttonRect.x += currentRect.width / 3 + 5;
        if (GUI.Button(buttonRect, "Deactivate All"))
        {
            SetAllTiles(tilesProp, false);
        }

        buttonRect.x += currentRect.width / 3 + 5;
        if (GUI.Button(buttonRect, "Invert"))
        {
            InvertGrid(tilesProp);
        }

        EditorGUI.EndProperty();
    }

    private void DrawGrid(Rect gridRect, int width, int height, SerializedProperty tilesProp)
    {
        Event e = Event.current;

        EditorGUI.DrawRect(gridRect, BackgroundColor);

        if (e.type == EventType.MouseDown && gridRect.Contains(e.mousePosition))
        {
            int x = Mathf.FloorToInt((e.mousePosition.x - gridRect.x) / CellSize);
            int y = Mathf.FloorToInt((e.mousePosition.y - gridRect.y) / CellSize);

            if (x >= 0 && x < width && y >= 0 && y < height)
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
            int y = Mathf.FloorToInt((e.mousePosition.y - gridRect.y) / CellSize);

            if (x >= 0 && x < width && y >= 0 && y < height)
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

        // Draw tiles
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                if (index >= tilesProp.arraySize) continue;

                bool isActive = tilesProp.GetArrayElementAtIndex(index).boolValue;

                Rect cellRect = new Rect(
                    gridRect.x + x * CellSize,
                    gridRect.y + y * CellSize,
                    CellSize - CellBorder,
                    CellSize - CellBorder
                );

                Color cellColor = isActive ? ActiveTileColor : InactiveTileColor;
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

        // Request repaint if mouse is over the grid to show hover effect
        if (gridRect.Contains(Event.current.mousePosition))
        {
            HandleUtility.Repaint();
        }
    }

    private void ResizeGrid(SerializedProperty property, int newWidth, int newHeight)
    {
        SerializedProperty gridSizeProp = property.FindPropertyRelative("gridSize");
        SerializedProperty tilesProp = property.FindPropertyRelative("tiles");

        Vector2Int oldGridSize = gridSizeProp.vector2IntValue;
        int oldWidth = oldGridSize.x;
        int oldHeight = oldGridSize.y;

        bool[] newTiles = new bool[newWidth * newHeight];

        for (int y = 0; y < Mathf.Min(oldHeight, newHeight); y++)
        {
            for (int x = 0; x < Mathf.Min(oldWidth, newWidth); x++)
            {
                int oldIndex = y * oldWidth + x;
                int newIndex = y * newWidth + x;
                if (oldIndex < tilesProp.arraySize)
                {
                    newTiles[newIndex] = tilesProp.GetArrayElementAtIndex(oldIndex).boolValue;
                }
            }
        }

        for (int y = 0; y < newHeight; y++)
        {
            for (int x = 0; x < newWidth; x++)
            {
                if (x >= oldWidth || y >= oldHeight)
                {
                    newTiles[y * newWidth + x] = true;
                }
            }
        }

        gridSizeProp.vector2IntValue = new Vector2Int(newWidth, newHeight);
        tilesProp.arraySize = newWidth * newHeight;

        for (int i = 0; i < newTiles.Length; i++)
        {
            tilesProp.GetArrayElementAtIndex(i).boolValue = newTiles[i];
        }

        property.serializedObject.ApplyModifiedProperties();
    }

    private void SetAllTiles(SerializedProperty tilesProp, bool value)
    {
        for (int i = 0; i < tilesProp.arraySize; i++)
        {
            tilesProp.GetArrayElementAtIndex(i).boolValue = value;
        }
        tilesProp.serializedObject.ApplyModifiedProperties();
        GUI.changed = true;
    }

    private void InvertGrid(SerializedProperty tilesProp)
    {
        for (int i = 0; i < tilesProp.arraySize; i++)
        {
            SerializedProperty element = tilesProp.GetArrayElementAtIndex(i);
            element.boolValue = !element.boolValue;
        }
        tilesProp.serializedObject.ApplyModifiedProperties();
        GUI.changed = true;
    }

    private int GetActiveCount(SerializedProperty tilesProp)
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