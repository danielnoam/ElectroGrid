#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(Match3LoseCondition), true)]
public class Match3LoseConditionDrawer : PropertyDrawer 
{
    private static Dictionary<string, Type> _typeMap;
    private static readonly Dictionary<string, bool> _foldoutStates = new Dictionary<string, bool>();

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) 
    {
        if (_typeMap == null) BuildTypeMap();
    
        var typeRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        var contentRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight, position.width, position.height - EditorGUIUtility.singleLineHeight);
    
        EditorGUI.BeginProperty(position, label, property);
        
        var typeName = property.managedReferenceFullTypename;
        var displayName = GetShortTypeName(typeName);
        var propertyPath = property.propertyPath;

        if (!_foldoutStates.ContainsKey(propertyPath))
            _foldoutStates[propertyPath] = true;

        var foldoutRect = new Rect(typeRect.x, typeRect.y, 15, typeRect.height);
        var dropdownRect = new Rect(typeRect.x + 15, typeRect.y, typeRect.width - 15, typeRect.height);

        if (property.managedReferenceValue != null)
        {
            _foldoutStates[propertyPath] = EditorGUI.Foldout(foldoutRect, _foldoutStates[propertyPath], GUIContent.none);
        }

        var dropdownContent = new GUIContent(displayName ?? "Select Lose Condition Type");
        if (EditorGUI.DropdownButton(dropdownRect, dropdownContent, FocusType.Keyboard)) 
        {
            ShowTypeMenu(property, typeName);
        }

        if (property.managedReferenceValue != null && _foldoutStates[propertyPath]) 
        {
            EditorGUI.indentLevel++;
    
            var iterator = property.Copy();
            var endProperty = iterator.GetEndProperty();
    
            // Enter children on first call
            if (iterator.NextVisible(true))
            {
                do
                {
                    // Skip drawing the property if it's the script field
                    if (iterator.propertyPath.EndsWith(".m_Script"))
                        continue;
                
                    var propHeight = EditorGUI.GetPropertyHeight(iterator, true);
                    var propRect = new Rect(contentRect.x, contentRect.y, contentRect.width, propHeight);
                    EditorGUI.PropertyField(propRect, iterator, true);
                    contentRect.y += propHeight + EditorGUIUtility.standardVerticalSpacing;
                }
                while (iterator.NextVisible(false) && !SerializedProperty.EqualContents(iterator, endProperty));
            }
    
            EditorGUI.indentLevel--;
        }
    
        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label) 
    {
        float height = EditorGUIUtility.singleLineHeight; // Type dropdown
    
        if (property.managedReferenceValue != null)
        {
            var propertyPath = property.propertyPath;
            if (_foldoutStates.ContainsKey(propertyPath) && _foldoutStates[propertyPath])
            {
                var iterator = property.Copy();
                var endProperty = iterator.GetEndProperty();
            
                if (iterator.NextVisible(true))
                {
                    do
                    {
                        // Skip the m_Script field
                        if (iterator.propertyPath.EndsWith(".m_Script"))
                            continue;
                        
                        height += EditorGUI.GetPropertyHeight(iterator, true) + EditorGUIUtility.standardVerticalSpacing;
                    }
                    while (iterator.NextVisible(false) && !SerializedProperty.EqualContents(iterator, endProperty));
                }
            }
        }
    
        return height;
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
            menu.AddDisabledItem(new GUIContent("No Lose Condition types available"));
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
        var baseType = typeof(Match3LoseCondition);
        _typeMap = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(asm => {
                try { return asm.GetTypes(); }
                catch { return Type.EmptyTypes; }
            })
            .Where(t => !t.IsAbstract && baseType.IsAssignableFrom(t) && t != baseType)
            .ToDictionary(t => GetNiceName(t), t => t);
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
        if (typeName.EndsWith("LoseCondition"))
            typeName = typeName.Substring(0, typeName.Length - 13);
        else if (typeName.EndsWith("Condition"))
            typeName = typeName.Substring(0, typeName.Length - 9);
        
        return ObjectNames.NicifyVariableName(typeName);
    }
}
#endif