
#if UNITY_EDITOR




using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DNExtensions.SerializedInterface
{
    /// <summary>
    /// Custom property drawer for fields with the RequireInterface attribute.
    /// Provides visual feedback and validation in the Unity Inspector.
    /// </summary>
    [CustomPropertyDrawer(typeof(RequireInterfaceAttribute))]
    public class RequireInterfaceDrawer : PropertyDrawer
    {
        private static readonly Dictionary<string, ValidationState> ValidationCache = new Dictionary<string, ValidationState>();
        private const float ValidationCacheDuration = 0.5f; // seconds

        private struct ValidationState
        {
            public bool IsValid;
            public string ErrorMessage;
            public double LastValidationTime;
        }

        /// <summary>
        /// Gets the RequireInterface attribute for this drawer.
        /// </summary>
        private RequireInterfaceAttribute RequireInterfaceAttribute => (RequireInterfaceAttribute)attribute;

        /// <summary>
        /// Draws the property field in the inspector with interface validation.
        /// </summary>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Early exit if not an object reference
            if (property.propertyType != SerializedPropertyType.ObjectReference)
            {
                EditorGUI.LabelField(position, label, new GUIContent("RequireInterface only works with Object references"));
                return;
            }

            Type requiredInterfaceType = RequireInterfaceAttribute.InterfaceType;
            EditorGUI.BeginProperty(position, label, property);

            try
            {
                DrawInterfaceObjectField(position, property, label, requiredInterfaceType);
            }
            catch (ExitGUIException)
            {
                // ExitGUIException is expected in some Unity GUI scenarios, just rethrow
                throw;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error in RequireInterfaceDrawer: {e.Message}", property.serializedObject.targetObject);
                EditorGUI.LabelField(position, label, new GUIContent("Error in drawer"));
            }

            EditorGUI.EndProperty();

            // Draw interface type indicator using the same utility as InterfaceReference
            var args = new InterfaceArgs(GetAssignableBaseType(fieldInfo.FieldType, requiredInterfaceType), requiredInterfaceType);
            InterfaceReferenceUtil.OnGUI(position, property, label, args);
        }

        /// <summary>
        /// Gets the property height including validation messages.
        /// </summary>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.ObjectReference)
                return EditorGUIUtility.singleLineHeight;

            float height = EditorGUIUtility.singleLineHeight;
            
            // Add space for error message if validation failed
            var validationState = GetCachedValidationState(property);
            if (!validationState.IsValid && RequireInterfaceAttribute.ShowWarningInInspector && 
                !string.IsNullOrEmpty(validationState.ErrorMessage))
            {
                height += EditorGUIUtility.singleLineHeight * 1.5f + 4;
            }

            return height;
        }

        /// <summary>
        /// Draws a single object field with interface validation.
        /// </summary>
        private void DrawInterfaceObjectField(Rect position, SerializedProperty property, GUIContent label, Type interfaceType)
        {
            var fieldRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            var oldReference = property.objectReferenceValue;
            Type baseType = GetAssignableBaseType(fieldInfo.FieldType, interfaceType);
            
            // Apply visual feedback for invalid state
            var validationState = GetCachedValidationState(property);
            var previousColor = GUI.backgroundColor;
            
            if (!validationState.IsValid && oldReference != null && RequireInterfaceAttribute.ShowWarningInInspector)
            {
                GUI.backgroundColor = new Color(1f, 0.7f, 0.7f); // Light red
            }

            // Draw the object field with custom validation
            EditorGUI.BeginChangeCheck();
            var newReference = EditorGUI.ObjectField(fieldRect, label, oldReference, baseType, true);
            GUI.backgroundColor = previousColor;

            if (EditorGUI.EndChangeCheck())
            {
                ValidateAndAssignObject(property, newReference, interfaceType);
            }

            // Draw error message if needed
            if (!validationState.IsValid && RequireInterfaceAttribute.ShowWarningInInspector && 
                !string.IsNullOrEmpty(validationState.ErrorMessage))
            {
                var errorRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight + 2,
                                        position.width, EditorGUIUtility.singleLineHeight * 1.5f);
                EditorGUI.HelpBox(errorRect, validationState.ErrorMessage, MessageType.Error);
            }
        }

        /// <summary>
        /// Draws the interface type indicator similar to InterfaceReferenceUtil
        /// </summary>
        private void DrawInterfaceTypeIndicator(Rect position, Type interfaceType)
        {
            if (Event.current.type == EventType.Repaint)
            {
                var isHovering = position.Contains(Event.current.mousePosition);
                var displayString = $"({interfaceType.Name})";
                
                if (isHovering)
                {
                    var style = new GUIStyle(EditorStyles.label)
                    {
                        fontSize = 9,
                        alignment = TextAnchor.MiddleRight,
                        normal = { textColor = Color.gray }
                    };

                    var content = new GUIContent(displayString);
                    var size = style.CalcSize(content);
                    var labelRect = new Rect(
                        position.x + position.width - size.x - 20,
                        position.y + 1,
                        size.x,
                        position.height - 2
                    );
                    
                    style.Draw(labelRect, content, false, false, false, false);
                }
            }
        }

        /// <summary>
        /// Gets the appropriate base type for the object field.
        /// </summary>
        private Type GetAssignableBaseType(Type fieldType, Type interfaceType)
        {
            // If the field type already implements the interface, use it
            if (interfaceType.IsAssignableFrom(fieldType))
                return fieldType;

            // Try common Unity types that might implement the interface
            if (typeof(Component).IsAssignableFrom(fieldType))
                return fieldType;
            if (typeof(ScriptableObject).IsAssignableFrom(fieldType))
                return fieldType;

            // Default to Object to allow GameObject assignment
            return typeof(Object);
        }

        /// <summary>
        /// Validates and assigns an object to a property.
        /// </summary>
        private void ValidateAndAssignObject(SerializedProperty property, Object newReference, Type interfaceType)
        {
            if (newReference == null)
            {
                if (!RequireInterfaceAttribute.AllowNull)
                {
                    var errorMsg = RequireInterfaceAttribute.GetErrorMessage(property.name, null);
                    Debug.LogWarning(errorMsg, property.serializedObject.targetObject);
                    UpdateValidationCache(property, false, errorMsg);
                    return;
                }
                
                property.objectReferenceValue = null;
                UpdateValidationCache(property, true, null);
                return;
            }

            Object targetObject = null;

            // Check GameObject components first
            if (newReference is GameObject gameObject)
            {
                var components = gameObject.GetComponents<Component>();
                foreach (var component in components)
                {
                    if (component != null && interfaceType.IsAssignableFrom(component.GetType()))
                    {
                        targetObject = component;
                        break;
                    }
                }
            }
            // Check direct interface implementation
            else if (interfaceType.IsAssignableFrom(newReference.GetType()))
            {
                targetObject = newReference;
            }

            if (targetObject != null)
            {
                property.objectReferenceValue = targetObject;
                UpdateValidationCache(property, true, null);
            }
            else
            {
                var errorMsg = RequireInterfaceAttribute.GetErrorMessage(property.name, newReference);
                Debug.LogWarning(errorMsg, property.serializedObject.targetObject);
                property.objectReferenceValue = null;
                UpdateValidationCache(property, false, errorMsg);
            }
        }

        /// <summary>
        /// Gets cached validation state for a property.
        /// </summary>
        private ValidationState GetCachedValidationState(SerializedProperty property)
        {
            var key = GetPropertyKey(property);
            
            if (ValidationCache.TryGetValue(key, out var state))
            {
                var timeSinceValidation = EditorApplication.timeSinceStartup - state.LastValidationTime;
                if (timeSinceValidation < ValidationCacheDuration)
                    return state;
            }

            // Perform validation
            var obj = property.objectReferenceValue;
            bool isValid = RequireInterfaceAttribute.IsValid(obj);
            string errorMessage = isValid ? null : RequireInterfaceAttribute.GetErrorMessage(property.name, obj);
            
            return UpdateValidationCache(property, isValid, errorMessage);
        }

        /// <summary>
        /// Updates the validation cache for a property.
        /// </summary>
        private ValidationState UpdateValidationCache(SerializedProperty property, bool isValid, string errorMessage)
        {
            var key = GetPropertyKey(property);
            
            var state = new ValidationState
            {
                IsValid = isValid,
                ErrorMessage = errorMessage,
                LastValidationTime = EditorApplication.timeSinceStartup
            };

            ValidationCache[key] = state;
            return state;
        }

        /// <summary>
        /// Gets a unique key for a serialized property.
        /// </summary>
        private string GetPropertyKey(SerializedProperty property)
        {
            return $"{property.serializedObject.targetObject.GetInstanceID()}_{property.propertyPath}";
        }

        /// <summary>
        /// Clears validation cache when domain reloads.
        /// </summary>
        [InitializeOnLoadMethod]
        private static void ClearCache()
        {
            ValidationCache.Clear();
        }
    }
}

#endif