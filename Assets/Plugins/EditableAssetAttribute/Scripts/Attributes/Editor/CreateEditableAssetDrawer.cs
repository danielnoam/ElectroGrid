using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static UnityEditor.EditorGUI;

namespace Core.Attributes.Drawers
{
    /// <summary>
    /// Custom Property Drawer for <see cref="CreateEditableAsset"/> attribute, allowing users to quickly create and clone ScriptableObjects.
    /// </summary>
    [CustomPropertyDrawer(typeof(CreateEditableAsset))]
    public class CreateEditableAssetDrawer : PropertyDrawer
    {
        private bool quickEdit;
        private const float WidthSymbol = 30.0f;
        private UnityEditor.Editor cachedEditor;

        // Static variable to store the last directory path
        private static string lastSavePath = "Assets";

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.ObjectReference)
            {
                base.OnGUI(position, property, label);
                return;
            }

            // Retrieve the CreateEditableAsset attribute to check for the _AddLabel property
            var createEditableAsset = (CreateEditableAsset)attribute;

            float spacing = 5f;
            float buttonWidth = WidthSymbol + 15f;
            float objectFieldWidth = position.width - WidthSymbol - 2 * buttonWidth - 3 * spacing;

            Rect quickEditRect = new Rect(position.x, position.y, WidthSymbol, position.height);
            Rect objectFieldRect = new Rect(quickEditRect.xMax + spacing, position.y, objectFieldWidth, position.height);
            Rect newButtonRect = new Rect(objectFieldRect.xMax + spacing, position.y, buttonWidth, position.height);
            Rect cloneButtonRect = new Rect(newButtonRect.xMax + spacing, position.y, buttonWidth, position.height);

            // Draw the Quick Edit toggle button
            DrawEditButton(quickEditRect, property);

            // Draw Object Field to assign or view the ScriptableObject
            GUIContent objectFieldLabel = createEditableAsset.AddLabel ? label : GUIContent.none;
            EditorGUIUtility.labelWidth = createEditableAsset.SetWidth; // Set the label width only once

            // Draw the Object Field with proper label handling
            ObjectField(objectFieldRect, property, objectFieldLabel);

            // Reset label width back to default (optional, but good practice)
            EditorGUIUtility.labelWidth = 0;

            // Draw the New and Clone buttons
            DrawNewButton(newButtonRect, property);
            DrawCloneButton(cloneButtonRect, property);
        }


        /// <summary>
        /// Draws the Quick Edit button to toggle editing of the ScriptableObject.
        /// </summary>
        private void DrawEditButton(Rect position, SerializedProperty property)
        {
            using (new DisabledScope(property.objectReferenceValue == null))
            {
                quickEdit = GUI.Toggle(position, quickEdit, EditorGUIUtility.IconContent("editicon.sml"), EditorStyles.miniButton);
                if (quickEdit && property.objectReferenceValue != null)
                {

                    UnityEditor.Editor.CreateCachedEditor(property.objectReferenceValue, null, ref cachedEditor);
                    EditorGUILayout.BeginVertical(new GUIStyle(EditorStyles.helpBox));
                    cachedEditor.OnInspectorGUI();
                    EditorGUILayout.EndVertical();
                }
            }

        }

        /// <summary>
        /// Draws the New button for creating a new ScriptableObject.
        /// </summary>
        private void DrawNewButton(Rect position, SerializedProperty property)
        {
            if (GUI.Button(position, new GUIContent("New", "Create a new ScriptableObject of the specified type")))
            {
                Type type = fieldInfo.FieldType;

                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
                {
                    type = type.GetGenericArguments()[0];
                }

                if (typeof(ScriptableObject).IsAssignableFrom(type))
                {
                    ScriptableObject newAsset = ScriptableObject.CreateInstance(type);
                    string path = EditorUtility.SaveFilePanelInProject(
                        "Save ScriptableObject",
                        $"New{type.Name}",
                        "asset",
                        "Please enter a file name to save the ScriptableObject.",
                        lastSavePath // Use last save directory path
                    );

                    if (!string.IsNullOrEmpty(path))
                    {
                        lastSavePath = System.IO.Path.GetDirectoryName(path); // Update last directory path
                        AssetDatabase.CreateAsset(newAsset, path);
                        AssetDatabase.SaveAssets();
                        property.objectReferenceValue = newAsset;
                        property.serializedObject.ApplyModifiedProperties();
                    }
                }
                else
                {
                    Debug.LogError($"Type '{type}' is not a valid ScriptableObject type.");
                }
            }
        }

        /// <summary>
        /// Draws the Clone button for cloning the current ScriptableObject.
        /// </summary>
        private void DrawCloneButton(Rect position, SerializedProperty property)
        {
            using (new DisabledScope(property.objectReferenceValue == null))
            {
                if (GUI.Button(position, new GUIContent("Clone", "Clone the current ScriptableObject")))
                {

                    if (property.objectReferenceValue == null)
                    {
                        Debug.LogWarning("No ScriptableObject to clone.");
                        return;
                    }

                    string path = EditorUtility.SaveFilePanelInProject(
                        "Save Cloned ScriptableObject",
                        $"{property.objectReferenceValue.name}_Clone",
                        "asset",
                        "Please enter a file name to save the cloned ScriptableObject.",
                        lastSavePath // Use last save directory path
                    );

                    if (!string.IsNullOrEmpty(path))
                    {
                        lastSavePath = System.IO.Path.GetDirectoryName(path); // Update last directory path
                        ScriptableObject clone = UnityEngine.Object.Instantiate(property.objectReferenceValue) as ScriptableObject;
                        AssetDatabase.CreateAsset(clone, AssetDatabase.GenerateUniqueAssetPath(path));
                        AssetDatabase.SaveAssets();
                        property.objectReferenceValue = clone;
                        property.serializedObject.ApplyModifiedProperties();
                    }
                }
            }
        }


    }
}
