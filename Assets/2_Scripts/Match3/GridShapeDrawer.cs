using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(Grid))]
public class GridDrawer : PropertyDrawer
{
    private const int MinSize = 1;
    private const int MaxSize = 20;
    
    private const float CellSize = 16f;
    private const float HeaderHeight = 60f;
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
        SerializedProperty heightProp = property.FindPropertyRelative("height");

        float gridHeight = heightProp.intValue * CellSize;
        float totalHeight = HeaderHeight + EditorGUIUtility.singleLineHeight + gridHeight + ButtonHeight * 2 + Spacing * 4;

        return totalHeight;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        SerializedProperty widthProp = property.FindPropertyRelative("width");
        SerializedProperty heightProp = property.FindPropertyRelative("height");
        SerializedProperty tilesProp = property.FindPropertyRelative("tiles");

        Rect currentRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

        EditorGUI.LabelField(currentRect, label, EditorStyles.boldLabel);
        currentRect.y += EditorGUIUtility.singleLineHeight + Spacing;

        currentRect = new Rect(position.x, currentRect.y, position.width, EditorGUIUtility.singleLineHeight);

        EditorGUI.BeginChangeCheck();
        int newWidth = EditorGUI.IntSlider(currentRect, "Width", widthProp.intValue, MinSize, MaxSize);
        currentRect.y += EditorGUIUtility.singleLineHeight + Spacing;

        int newHeight = EditorGUI.IntSlider(currentRect, "Height", heightProp.intValue, MinSize, MaxSize);
        currentRect.y += Spacing;

        if (EditorGUI.EndChangeCheck())
        {
            ResizeGrid(property, newWidth, newHeight);
        }

        currentRect.y += EditorGUIUtility.singleLineHeight + Spacing;

        int activeCount = GetActiveCount(tilesProp);
        EditorGUI.LabelField(currentRect, $"Active Tiles: {activeCount} / {widthProp.intValue * heightProp.intValue}");
        currentRect.y += EditorGUIUtility.singleLineHeight + Spacing;

        float gridWidth = widthProp.intValue * CellSize;
        float gridHeight = heightProp.intValue * CellSize;
        Rect gridRect = new Rect(currentRect.x + (currentRect.width - gridWidth) / 2, currentRect.y, gridWidth, gridHeight);

        DrawGrid(gridRect, widthProp.intValue, heightProp.intValue, tilesProp);

        currentRect.y += gridHeight + Spacing;

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
    }

    private void ResizeGrid(SerializedProperty property, int newWidth, int newHeight)
    {
        SerializedProperty widthProp = property.FindPropertyRelative("width");
        SerializedProperty heightProp = property.FindPropertyRelative("height");
        SerializedProperty tilesProp = property.FindPropertyRelative("tiles");

        int oldWidth = widthProp.intValue;
        int oldHeight = heightProp.intValue;

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

        widthProp.intValue = newWidth;
        heightProp.intValue = newHeight;
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
    }

    private void InvertGrid(SerializedProperty tilesProp)
    {
        for (int i = 0; i < tilesProp.arraySize; i++)
        {
            SerializedProperty element = tilesProp.GetArrayElementAtIndex(i);
            element.boolValue = !element.boolValue;
        }
        tilesProp.serializedObject.ApplyModifiedProperties();
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