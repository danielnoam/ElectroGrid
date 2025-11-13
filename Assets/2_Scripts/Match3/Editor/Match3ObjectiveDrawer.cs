#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(Match3Objective), true)]
public class Match3ObjectiveDrawer : PropertyDrawer 
{
    private static Dictionary<string, Type> _typeMap;
    private static readonly Dictionary<string, bool> FoldoutStates = new Dictionary<string, bool>();

    private const float CellSize = 16f;
    private const float CellBorder = 1f;
    private const float GridSpacing = 10f;
    
    private static readonly Color BackgroundColor = new Color(0.2f, 0.2f, 0.2f);
    private static readonly Color ActiveTileColor = new Color(0.3f, 0.7f, 0.3f);
    private static readonly Color InactiveTileColor = new Color(0.4f, 0.4f, 0.4f);
    private static readonly Color ObstacleTileColor = new Color(0.8f, 0.3f, 0.3f);
    private static readonly Color GridLineColor = new Color(0.1f, 0.1f, 0.1f);
    private static readonly Color HoverColor = new Color(1f, 1f, 1f, 0.3f);

    private bool _isDragging;
    private bool _dragState;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) 
    {
        if (_typeMap == null) BuildTypeMap();
    
        var typeRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        var contentRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight, position.width, position.height - EditorGUIUtility.singleLineHeight);
    
        EditorGUI.BeginProperty(position, label, property);
        
        var typeName = property.managedReferenceFullTypename;
        var displayName = GetShortTypeName(typeName);
        var propertyPath = property.propertyPath;

        FoldoutStates.TryAdd(propertyPath, true);

        var foldoutRect = new Rect(typeRect.x, typeRect.y, 15, typeRect.height);
        var dropdownRect = new Rect(typeRect.x + 15, typeRect.y, typeRect.width - 15, typeRect.height);

        if (property.managedReferenceValue != null)
        {
            FoldoutStates[propertyPath] = EditorGUI.Foldout(foldoutRect, FoldoutStates[propertyPath], GUIContent.none);
        }

        var dropdownContent = new GUIContent(displayName ?? "Select Objective Type");
        if (EditorGUI.DropdownButton(dropdownRect, dropdownContent, FocusType.Keyboard)) 
        {
            ShowTypeMenu(property, typeName);
        }

        if (property.managedReferenceValue != null && FoldoutStates[propertyPath]) 
        {
            EditorGUI.indentLevel++;
    
            var iterator = property.Copy();
            var endProperty = iterator.GetEndProperty();
    
            if (iterator.NextVisible(true))
            {
                do
                {
                    if (iterator.propertyPath.EndsWith(".m_Script"))
                        continue;
                    
                    if (property.managedReferenceValue is ClearObstaclesObjective && 
                        iterator.propertyPath.EndsWith(".obstacleTiles"))
                        continue;
                
                    var propHeight = EditorGUI.GetPropertyHeight(iterator, true);
                    var propRect = new Rect(contentRect.x, contentRect.y, contentRect.width, propHeight);
                    EditorGUI.PropertyField(propRect, iterator, true);
                    contentRect.y += propHeight + EditorGUIUtility.standardVerticalSpacing;
                }
                while (iterator.NextVisible(false) && !SerializedProperty.EqualContents(iterator, endProperty));
            }

            if (property.managedReferenceValue is ClearObstaclesObjective)
            {
                contentRect.y += GridSpacing;
                DrawObstacleGrid(property, ref contentRect);
            }
    
            EditorGUI.indentLevel--;
        }
    
        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label) 
    {
        float height = EditorGUIUtility.singleLineHeight;
    
        if (property.managedReferenceValue != null)
        {
            var propertyPath = property.propertyPath;
            if (FoldoutStates.ContainsKey(propertyPath) && FoldoutStates[propertyPath])
            {
                var iterator = property.Copy();
                var endProperty = iterator.GetEndProperty();
            
                if (iterator.NextVisible(true))
                {
                    do
                    {
                        if (iterator.propertyPath.EndsWith(".m_Script"))
                            continue;
                        
                        if (property.managedReferenceValue is ClearObstaclesObjective && 
                            iterator.propertyPath.EndsWith(".obstacleTiles"))
                            continue;
                        
                        height += EditorGUI.GetPropertyHeight(iterator, true) + EditorGUIUtility.standardVerticalSpacing;
                    }
                    while (iterator.NextVisible(false) && !SerializedProperty.EqualContents(iterator, endProperty));
                }

                if (property.managedReferenceValue is ClearObstaclesObjective)
                {
                    var gridShapeProp = property.FindPropertyRelative("gridShape");
                    if (gridShapeProp.objectReferenceValue is SOGridShape { Grid: not null } gridShape)
                    {
                        height += GridSpacing;
                        height += EditorGUIUtility.singleLineHeight + 5;
                        height += gridShape.Grid.Height * CellSize + 5;
                        height += EditorGUIUtility.singleLineHeight + 5;
                    }
                    else
                    {
                        height += GridSpacing;
                        height += EditorGUIUtility.singleLineHeight + 5;
                    }
                }
            }
        }
    
        return height;
    }

    private void DrawObstacleGrid(SerializedProperty property, ref Rect contentRect)
    {
        var gridShapeProp = property.FindPropertyRelative("gridShape");
        var tilesProp = property.FindPropertyRelative("obstacleTiles");

        if (gridShapeProp.objectReferenceValue == null)
        {
            var helpBoxRect = new Rect(contentRect.x, contentRect.y, contentRect.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.HelpBox(helpBoxRect, "Assign a Grid Shape to edit obstacle tiles.", MessageType.Info);
            contentRect.y += EditorGUIUtility.singleLineHeight + 5;
            return;
        }

        SOGridShape gridShape = (SOGridShape)gridShapeProp.objectReferenceValue;
        Grid grid = gridShape.Grid;

        if (grid == null)
        {
            var helpBoxRect = new Rect(contentRect.x, contentRect.y, contentRect.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.HelpBox(helpBoxRect, "Grid Shape has no valid grid.", MessageType.Warning);
            contentRect.y += EditorGUIUtility.singleLineHeight + 5;
            return;
        }

        int width = grid.Width;
        int height = grid.Height;
        int requiredSize = width * height;

        if (tilesProp.arraySize != requiredSize)
        {
            tilesProp.arraySize = requiredSize;
            property.serializedObject.ApplyModifiedProperties();
        }

        int obstacleCount = GetObstacleCount(tilesProp);
        var countRect = new Rect(contentRect.x, contentRect.y, contentRect.width, EditorGUIUtility.singleLineHeight);
        EditorGUI.LabelField(countRect, $"Obstacle Tiles: {obstacleCount} / {grid.ActiveCellCount}");
        contentRect.y += EditorGUIUtility.singleLineHeight + 5;

        float gridWidth = width * CellSize;
        float gridHeight = height * CellSize;
        var gridRect = new Rect(
            contentRect.x + (contentRect.width - gridWidth) / 2,
            contentRect.y,
            gridWidth,
            gridHeight
        );

        DrawGrid(gridRect, grid, tilesProp);
        contentRect.y += gridHeight + 5;

        var buttonRect = new Rect(contentRect.x, contentRect.y, contentRect.width / 2 - 2, EditorGUIUtility.singleLineHeight);
        if (GUI.Button(buttonRect, "Clear All"))
        {
            SetAllObstacles(tilesProp, grid, false);
        }

        buttonRect.x += contentRect.width / 2 + 2;
        if (GUI.Button(buttonRect, "Invert"))
        {
            InvertObstacles(tilesProp, grid);
        }

        contentRect.y += EditorGUIUtility.singleLineHeight + 5;
    }

    private void DrawGrid(Rect gridRect, Grid grid, SerializedProperty tilesProp)
    {
        Event e = Event.current;
        int width = grid.Width;
        int height = grid.Height;

        EditorGUI.DrawRect(gridRect, BackgroundColor);

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

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                if (index >= tilesProp.arraySize) continue;

                bool isActive = grid.IsCellActive(x, y);
                bool hasObstacle = tilesProp.GetArrayElementAtIndex(index).boolValue;

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
                else if (hasObstacle)
                {
                    cellColor = ObstacleTileColor;
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

    private void SetAllObstacles(SerializedProperty tilesProp, Grid grid, bool value)
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

    private void InvertObstacles(SerializedProperty tilesProp, Grid grid)
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
    
    private int GetObstacleCount(SerializedProperty tilesProp)
    {
        int count = 0;
        for (int i = 0; i < tilesProp.arraySize; i++)
        {
            if (tilesProp.GetArrayElementAtIndex(i).boolValue)
                count++;
        }
        return count;
    }

    private void ShowTypeMenu(SerializedProperty property, string currentTypeName)
    {
        var menu = new GenericMenu();
        
        menu.AddItem(new GUIContent("None"), string.IsNullOrEmpty(currentTypeName), () => {
            property.managedReferenceValue = null;
            property.serializedObject.ApplyModifiedProperties();
        });
        
        menu.AddSeparator("");
        
        if (_typeMap == null || _typeMap.Count == 0) 
        {
            menu.AddDisabledItem(new GUIContent("No Objective types available"));
        }
        else
        {
            foreach (var kvp in _typeMap.OrderBy(k => k.Key))
            {
                var name = kvp.Key;
                var type = kvp.Value;
                
                menu.AddItem(new GUIContent(name), type.FullName == currentTypeName, () => {
                    property.managedReferenceValue = Activator.CreateInstance(type);
                    property.serializedObject.ApplyModifiedProperties();
                });
            }
        }
        
        menu.ShowAsContext();
    }

    private static void BuildTypeMap() 
    {
        var baseType = typeof(Match3Objective);
        _typeMap = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(asm => {
                try { return asm.GetTypes(); }
                catch { return Type.EmptyTypes; }
            })
            .Where(t => !t.IsAbstract && baseType.IsAssignableFrom(t) && t != baseType)
            .ToDictionary(GetNiceName, t => t);
    }

    private static string GetShortTypeName(string fullTypeName) 
    {
        if (string.IsNullOrEmpty(fullTypeName)) return null;
        var parts = fullTypeName.Split(' ');
        var typeName = parts.Length > 1 ? parts[1].Split('.').Last() : fullTypeName;
        return GetNiceName(typeName);
    }
    
    private static string GetNiceName(Type type)
    {
        return GetNiceName(type.Name);
    }
    
    private static string GetNiceName(string typeName)
    {
        if (typeName.EndsWith("Objective"))
            typeName = typeName.Substring(0, typeName.Length - 9);
        
        return ObjectNames.NicifyVariableName(typeName);
    }
}
#endif