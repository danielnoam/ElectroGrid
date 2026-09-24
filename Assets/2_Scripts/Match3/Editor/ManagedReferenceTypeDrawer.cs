using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Draws a [SerializeReference] field as a type picker plus a foldout of the chosen type's fields.
/// Shared by the objective and lose condition drawers, which differ only in their base type and naming.
/// </summary>
internal abstract class ManagedReferenceTypeDrawer<TBase> : PropertyDrawer where TBase : class
{
    private const float FoldoutWidth = 15f;

    private static Dictionary<string, Type> _typeMap;
    private static readonly Dictionary<string, bool> FoldoutStates = new Dictionary<string, bool>();

    /// <summary>Display name of the base type for the dropdown placeholder and the empty menu, e.g. "Objective".</summary>
    protected abstract string TypeLabel { get; }

    /// <summary>Strips the base type's suffix from a class name before it is nicified for display.</summary>
    protected abstract string TrimTypeName(string typeName);

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        _typeMap ??= BuildTypeMap();

        EditorGUI.BeginProperty(position, label, property);

        string typeName = property.managedReferenceFullTypename;
        string propertyPath = property.propertyPath;
        FoldoutStates.TryAdd(propertyPath, true);

        var foldoutRect = new Rect(position.x, position.y, FoldoutWidth, EditorGUIUtility.singleLineHeight);
        var dropdownRect = new Rect(position.x + FoldoutWidth, position.y, position.width - FoldoutWidth, EditorGUIUtility.singleLineHeight);

        bool hasValue = property.managedReferenceValue != null;
        if (hasValue)
        {
            FoldoutStates[propertyPath] = EditorGUI.Foldout(foldoutRect, FoldoutStates[propertyPath], GUIContent.none);
        }

        var dropdownContent = new GUIContent(GetShortTypeName(typeName) ?? $"Select {TypeLabel} Type");
        if (EditorGUI.DropdownButton(dropdownRect, dropdownContent, FocusType.Keyboard))
        {
            ShowTypeMenu(property, typeName);
        }

        if (hasValue && FoldoutStates[propertyPath])
        {
            EditorGUI.indentLevel++;

            float y = position.y + EditorGUIUtility.singleLineHeight;
            foreach (var child in VisibleChildren(property))
            {
                float height = EditorGUI.GetPropertyHeight(child, true);
                EditorGUI.PropertyField(new Rect(position.x, y, position.width, height), child, true);
                y += height + EditorGUIUtility.standardVerticalSpacing;
            }

            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float height = EditorGUIUtility.singleLineHeight;

        if (property.managedReferenceValue == null) return height;
        if (!FoldoutStates.TryGetValue(property.propertyPath, out bool expanded) || !expanded) return height;

        foreach (var child in VisibleChildren(property))
        {
            height += EditorGUI.GetPropertyHeight(child, true) + EditorGUIUtility.standardVerticalSpacing;
        }

        return height;
    }

    /// <summary>
    /// Yields the same iterator advanced in place, so each child must be used before the next is requested.
    /// </summary>
    private static IEnumerable<SerializedProperty> VisibleChildren(SerializedProperty property)
    {
        var iterator = property.Copy();
        var endProperty = iterator.GetEndProperty();

        if (!iterator.NextVisible(true)) yield break;

        do
        {
            if (SerializedProperty.EqualContents(iterator, endProperty)) yield break;
            if (!iterator.propertyPath.EndsWith(".m_Script")) yield return iterator;
        }
        while (iterator.NextVisible(false));
    }

    private void ShowTypeMenu(SerializedProperty property, string currentTypeName)
    {
        var menu = new GenericMenu();

        menu.AddItem(new GUIContent("None"), string.IsNullOrEmpty(currentTypeName), () =>
        {
            property.managedReferenceValue = null;
            property.serializedObject.ApplyModifiedProperties();
        });

        menu.AddSeparator("");

        if (_typeMap.Count == 0)
        {
            menu.AddDisabledItem(new GUIContent($"No {TypeLabel} types available"));
        }

        foreach (var entry in _typeMap.OrderBy(pair => pair.Key))
        {
            var type = entry.Value;

            menu.AddItem(new GUIContent(entry.Key), type.FullName == currentTypeName, () =>
            {
                property.managedReferenceValue = Activator.CreateInstance(type);
                property.serializedObject.ApplyModifiedProperties();
            });
        }

        menu.ShowAsContext();
    }

    private Dictionary<string, Type> BuildTypeMap()
    {
        return TypeCache.GetTypesDerivedFrom<TBase>()
            .Where(type => !type.IsAbstract)
            .ToDictionary(type => GetNiceName(type.Name), type => type);
    }

    private string GetShortTypeName(string fullTypeName)
    {
        if (string.IsNullOrEmpty(fullTypeName)) return null;

        // managedReferenceFullTypename is "Assembly Namespace.TypeName"
        var parts = fullTypeName.Split(' ');
        string typeName = parts.Length > 1 ? parts[1].Split('.').Last() : fullTypeName;
        return GetNiceName(typeName);
    }

    private string GetNiceName(string typeName)
    {
        return ObjectNames.NicifyVariableName(TrimTypeName(typeName));
    }
}
