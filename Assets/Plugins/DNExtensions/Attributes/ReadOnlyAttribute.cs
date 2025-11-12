
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace DNExtensions
{
    /// <summary>
    /// Property attribute that makes fields read-only in the Unity Inspector while keeping them serialized.
    /// Useful for displaying calculated values, debug information, or fields that should not be manually edited.
    /// </summary>
    /// <example>
    /// <code>
    /// [ReadOnly]
    /// public float calculatedValue;
    /// 
    /// [ReadOnly]
    /// public Vector3[] pathPoints;
    /// </code>
    /// </example>
    public class ReadOnlyAttribute : PropertyAttribute 
    {
    }


    /// <summary>
    /// Custom property drawer that renders read-only fields with disabled GUI in the Unity Inspector.
    /// Supports both single properties and arrays/lists with proper height calculation.
    /// </summary>
    [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    public class ReadOnlyDrawer : PropertyDrawer
    {
        /// <summary>
        /// Calculates the height needed to display the read-only property, including arrays and complex types.
        /// </summary>
        /// <param name="property">The SerializedProperty to calculate height for</param>
        /// <param name="label">The label of the property</param>
        /// <returns>The total height required for the property display</returns>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        /// <summary>
        /// Renders the property field in a disabled state, preventing user interaction while maintaining visibility.
        /// Handles both simple properties and complex types like arrays with proper expansion support.
        /// </summary>
        /// <param name="position">Rectangle on the screen to use for the property GUI</param>
        /// <param name="property">The SerializedProperty to make the custom GUI for</param>
        /// <param name="label">The label of this property</param>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            GUI.enabled = false;
            
            if (property.isArray && property.propertyType == SerializedPropertyType.Generic)
            {
                EditorGUI.PropertyField(position, property, label, true);
            }
            else
            {
                EditorGUI.PropertyField(position, property, label);
            }
        
            GUI.enabled = true;
        }
    }

}


#endif