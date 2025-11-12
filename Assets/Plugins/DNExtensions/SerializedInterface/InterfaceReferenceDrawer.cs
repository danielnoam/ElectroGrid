
#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DNExtensions.SerializedInterface
{
    /// <summary>
    /// Custom property drawer for InterfaceReference types in the Unity Inspector.
    /// Provides interface validation and user-friendly object assignment.
    /// </summary>
    [CustomPropertyDrawer(typeof(InterfaceReference<>))]
    [CustomPropertyDrawer(typeof(InterfaceReference<,>))]
    public class InterfaceReferenceDrawer : PropertyDrawer
    {
        private const string UnderlyingValueFieldName = "underlyingValue";
        
        // Reflection cache for improved performance
        private static readonly ReflectionCache Cache = new ReflectionCache();
        
        // Validation cache to avoid repeated validation
        private static readonly Dictionary<int, ValidationState> ValidationCache = new Dictionary<int, ValidationState>();
        
        private struct ValidationState
        {
            public bool IsValid;
            public string ErrorMessage;
            public double LastValidationTime;
        }

        /// <summary>
        /// Draws the interface reference field in the inspector.
        /// </summary>
        /// <param name="position">The position in the inspector</param>
        /// <param name="property">The serialized property to draw</param>
        /// <param name="label">The label for the field</param>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var underlyingProperty = property.FindPropertyRelative(UnderlyingValueFieldName);
            if (underlyingProperty == null)
            {
                EditorGUI.LabelField(position, label, new GUIContent("Error: Invalid property structure"));
                return;
            }

            var args = Cache.GetInterfaceArgs(fieldInfo);
            if (!args.IsValid)
            {
                EditorGUI.LabelField(position, label, new GUIContent($"Error: {args.ErrorMessage}"));
                return;
            }

            EditorGUI.BeginProperty(position, label, property);
            
            // Draw with validation state coloring
            var validationState = GetValidationState(property, underlyingProperty.objectReferenceValue, args);
            using (new EditorGUI.DisabledScope(false))
            {
                var previousColor = GUI.backgroundColor;
                if (!validationState.IsValid && underlyingProperty.objectReferenceValue != null)
                {
                    GUI.backgroundColor = new Color(1f, 0.7f, 0.7f); // Light red for invalid
                }

                var assignedObject = EditorGUI.ObjectField(
                    position, 
                    label, 
                    underlyingProperty.objectReferenceValue,
                    args.ObjectType, 
                    true);

                GUI.backgroundColor = previousColor;

                HandleObjectAssignment(underlyingProperty, assignedObject, args);
            }

            // Draw validation error if needed
            if (!validationState.IsValid && !string.IsNullOrEmpty(validationState.ErrorMessage))
            {
                var helpBoxHeight = EditorGUIUtility.singleLineHeight * 1.5f;
                var helpBoxRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight + 2, 
                                          position.width, helpBoxHeight);
                EditorGUI.HelpBox(helpBoxRect, validationState.ErrorMessage, MessageType.Error);
            }

            EditorGUI.EndProperty();
            InterfaceReferenceUtil.OnGUI(position, underlyingProperty, label, args);
        }

        /// <summary>
        /// Gets the height of the property in the inspector.
        /// </summary>
        /// <param name="property">The serialized property</param>
        /// <param name="label">The label for the field</param>
        /// <returns>The height in pixels</returns>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var underlyingProperty = property.FindPropertyRelative(UnderlyingValueFieldName);
            if (underlyingProperty != null)
            {
                var args = Cache.GetInterfaceArgs(fieldInfo);
                var validationState = GetValidationState(property, underlyingProperty.objectReferenceValue, args);
                
                if (!validationState.IsValid && !string.IsNullOrEmpty(validationState.ErrorMessage))
                {
                    return EditorGUIUtility.singleLineHeight * 2.5f + 4;
                }
            }
            
            return base.GetPropertyHeight(property, label);
        }

        /// <summary>
        /// Handles the assignment of a new object to the interface reference.
        /// </summary>
        private void HandleObjectAssignment(SerializedProperty property, Object assignedObject, InterfaceArgs args)
        {
            if (assignedObject == null)
            {
                property.objectReferenceValue = null;
                ClearValidationCache(property);
                return;
            }

            try
            {
                var component = FindCompatibleComponent(assignedObject, args.InterfaceType);
                
                if (component != null)
                {
                    property.objectReferenceValue = component;
                    UpdateValidationCache(property, true, null);
                }
                else
                {
                    var errorMsg = $"Object '{assignedObject.name}' does not implement interface '{args.InterfaceType.Name}'";
                    Debug.LogWarning(errorMsg, assignedObject);
                    property.objectReferenceValue = null;
                    UpdateValidationCache(property, false, errorMsg);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error assigning object: {e.Message}", assignedObject);
                property.objectReferenceValue = null;
                UpdateValidationCache(property, false, e.Message);
            }
        }

        /// <summary>
        /// Finds a component that implements the required interface.
        /// </summary>
        private Object FindCompatibleComponent(Object assignedObject, Type interfaceType)
        {
            // Check if the object itself implements the interface
            if (Cache.IsAssignable(assignedObject.GetType(), interfaceType))
            {
                return assignedObject;
            }

            // If it's a GameObject, search for compatible components
            if (assignedObject is GameObject gameObject)
            {
                return Cache.FindComponentWithInterface(gameObject, interfaceType);
            }

            return null;
        }

        /// <summary>
        /// Gets the cached validation state for a property.
        /// </summary>
        private ValidationState GetValidationState(SerializedProperty property, Object obj, InterfaceArgs args)
        {
            var key = property.propertyPath.GetHashCode();
            
            // Check if we have a recent validation
            if (ValidationCache.TryGetValue(key, out var state))
            {
                var timeSinceValidation = EditorApplication.timeSinceStartup - state.LastValidationTime;
                if (timeSinceValidation < 0.5) // Cache for 500ms
                {
                    return state;
                }
            }

            // Perform validation
            bool isValid = obj == null || Cache.IsAssignable(obj.GetType(), args.InterfaceType);
            string errorMessage = isValid ? null : 
                $"Type '{obj.GetType().Name}' does not implement '{args.InterfaceType.Name}'";

            state = new ValidationState
            {
                IsValid = isValid,
                ErrorMessage = errorMessage,
                LastValidationTime = EditorApplication.timeSinceStartup
            };

            ValidationCache[key] = state;
            return state;
        }

        private void UpdateValidationCache(SerializedProperty property, bool isValid, string errorMessage)
        {
            var key = property.propertyPath.GetHashCode();
            ValidationCache[key] = new ValidationState
            {
                IsValid = isValid,
                ErrorMessage = errorMessage,
                LastValidationTime = EditorApplication.timeSinceStartup
            };
        }

        private void ClearValidationCache(SerializedProperty property)
        {
            var key = property.propertyPath.GetHashCode();
            ValidationCache.Remove(key);
        }

        /// <summary>
        /// Clears all caches when the domain reloads.
        /// </summary>
        [InitializeOnLoadMethod]
        private static void ClearCaches()
        {
            Cache.Clear();
            ValidationCache.Clear();
        }
    }

    /// <summary>
    /// Caches reflection operations for improved performance.
    /// </summary>
    internal class ReflectionCache
    {
        private readonly Dictionary<Type, InterfaceArgs> _typeArgsCache = new Dictionary<Type, InterfaceArgs>();
        private readonly Dictionary<(Type, Type), bool> _assignabilityCache = new Dictionary<(Type, Type), bool>();
        private readonly Dictionary<(int, Type), Component> _componentCache = new Dictionary<(int, Type), Component>();

        /// <summary>
        /// Gets the interface arguments for a field type with caching.
        /// </summary>
        public InterfaceArgs GetInterfaceArgs(FieldInfo fieldInfo)
        {
            var fieldType = fieldInfo.FieldType;
            
            if (!_typeArgsCache.TryGetValue(fieldType, out InterfaceArgs args))
            {
                args = ExtractInterfaceArgs(fieldInfo);
                _typeArgsCache[fieldType] = args;
            }
            
            return args;
        }

        /// <summary>
        /// Checks if a type is assignable to an interface with caching.
        /// </summary>
        public bool IsAssignable(Type objectType, Type interfaceType)
        {
            var key = (objectType, interfaceType);
            if (!_assignabilityCache.TryGetValue(key, out bool result))
            {
                result = interfaceType.IsAssignableFrom(objectType);
                _assignabilityCache[key] = result;
            }
            return result;
        }

        /// <summary>
        /// Finds a component with the specified interface on a GameObject.
        /// </summary>
        public Component FindComponentWithInterface(GameObject gameObject, Type interfaceType)
        {
            var key = (gameObject.GetInstanceID(), interfaceType);
            
            if (_componentCache.TryGetValue(key, out Component cached) && cached != null)
            {
                return cached;
            }

            var components = gameObject.GetComponents<Component>();
            foreach (var component in components)
            {
                if (component != null && IsAssignable(component.GetType(), interfaceType))
                {
                    _componentCache[key] = component;
                    return component;
                }
            }

            return null;
        }

        /// <summary>
        /// Clears all cached data.
        /// </summary>
        public void Clear()
        {
            _typeArgsCache.Clear();
            _assignabilityCache.Clear();
            _componentCache.Clear();
        }

        private InterfaceArgs ExtractInterfaceArgs(FieldInfo fieldInfo)
        {
            try
            {
                var fieldType = fieldInfo.FieldType;

                if (TryGetTypesFromInterfaceReference(fieldType, out var objectType, out var interfaceType) ||
                    TryGetTypesFromList(fieldType, out objectType, out interfaceType))
                {
                    return new InterfaceArgs(objectType, interfaceType);
                }

                return InterfaceArgs.Invalid($"Could not extract interface types from field '{fieldInfo.Name}'");
            }
            catch (Exception e)
            {
                return InterfaceArgs.Invalid($"Error extracting interface args: {e.Message}");
            }
        }

        private bool TryGetTypesFromInterfaceReference(Type type, out Type objType, out Type intfType)
        {
            objType = intfType = null;

            if (type?.IsGenericType != true) return false;

            var genericType = type.GetGenericTypeDefinition();
            if (genericType == typeof(InterfaceReference<>)) 
                type = type.BaseType;

            if (type?.GetGenericTypeDefinition() == typeof(InterfaceReference<,>))
            {
                var types = type.GetGenericArguments();
                intfType = types[0];
                objType = types[1];
                return true;
            }

            return false;
        }

        private bool TryGetTypesFromList(Type type, out Type objType, out Type intfType)
        {
            objType = intfType = null;

            var listInterface = type.GetInterfaces()
                .FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IList<>));

            if (listInterface != null)
            {
                var elementType = listInterface.GetGenericArguments()[0];
                return TryGetTypesFromInterfaceReference(elementType, out objType, out intfType);
            }

            return false;
        }
    }

    /// <summary>
    /// Represents the interface and object type arguments for an InterfaceReference field.
    /// </summary>
    public struct InterfaceArgs
    {
        /// <summary>
        /// The Unity Object type constraint.
        /// </summary>
        public readonly Type ObjectType;
        
        /// <summary>
        /// The required interface type.
        /// </summary>
        public readonly Type InterfaceType;
        
        /// <summary>
        /// Indicates whether this InterfaceArgs instance is valid.
        /// </summary>
        public readonly bool IsValid;
        
        /// <summary>
        /// Error message if the InterfaceArgs is invalid.
        /// </summary>
        public readonly string ErrorMessage;

        /// <summary>
        /// Creates a new InterfaceArgs instance.
        /// </summary>
        /// <param name="objectType">The Unity Object type</param>
        /// <param name="interfaceType">The interface type</param>
        public InterfaceArgs(Type objectType, Type interfaceType)
        {
            Debug.Assert(typeof(Object).IsAssignableFrom(objectType),
                $"{nameof(objectType)} needs to be of Type {typeof(Object)}.");
            Debug.Assert(interfaceType.IsInterface, 
                $"{nameof(interfaceType)} needs to be an interface.");

            ObjectType = objectType;
            InterfaceType = interfaceType;
            IsValid = true;
            ErrorMessage = null;
        }

        /// <summary>
        /// Creates an invalid InterfaceArgs with an error message.
        /// </summary>
        public static InterfaceArgs Invalid(string errorMessage)
        {
            return new InterfaceArgs(false, errorMessage);
        }

        private InterfaceArgs(bool isValid, string errorMessage)
        {
            ObjectType = typeof(Object);
            InterfaceType = typeof(IDisposable); // Dummy interface
            IsValid = isValid;
            ErrorMessage = errorMessage;
        }
    }
}

#endif