using System;
using UnityEngine;

namespace DNExtensions.SerializedInterface
{
    /// <summary>
    /// Attribute that enforces interface implementation on Unity Object fields in the inspector.
    /// Use this to ensure that assigned objects implement a specific interface.
    /// </summary>
    /// <example>
    /// <code>
    /// [RequireInterface(typeof(IHealth))]
    /// public GameObject healthObject;
    /// 
    /// [RequireInterface(typeof(IDamageable), AllowNull = false)]
    /// public MonoBehaviour damageableComponent;
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class RequireInterfaceAttribute : PropertyAttribute
    {
        /// <summary>
        /// The interface type that the assigned object must implement.
        /// </summary>
        public Type InterfaceType { get; }

        /// <summary>
        /// Whether null values are allowed for this field.
        /// Default is true.
        /// </summary>
        public bool AllowNull { get; set; } = true;

        /// <summary>
        /// Custom error message to display when validation fails.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Whether to show a warning icon in the inspector when the field is invalid.
        /// Default is true.
        /// </summary>
        public bool ShowWarningInInspector { get; set; } = true;

        /// <summary>
        /// Whether to validate the field at runtime.
        /// Default is true in debug builds, false in release builds.
        /// </summary>
        public bool ValidateAtRuntime { get; set; } = Application.isEditor || Debug.isDebugBuild;

        /// <summary>
        /// Creates a new RequireInterface attribute.
        /// </summary>
        /// <param name="interfaceType">The interface type that objects must implement</param>
        /// <exception cref="ArgumentNullException">Thrown when interfaceType is null</exception>
        /// <exception cref="ArgumentException">Thrown when interfaceType is not an interface</exception>
        public RequireInterfaceAttribute(Type interfaceType)
        {
            if (interfaceType == null)
                throw new ArgumentNullException(nameof(interfaceType), "Interface type cannot be null");

            if (!interfaceType.IsInterface)
                throw new ArgumentException($"Type '{interfaceType.Name}' is not an interface", nameof(interfaceType));

            InterfaceType = interfaceType;
        }

        /// <summary>
        /// Validates whether an object implements the required interface.
        /// </summary>
        /// <param name="obj">The object to validate</param>
        /// <returns>True if the object is valid (null when allowed, or implements the interface)</returns>
        public bool IsValid(UnityEngine.Object obj)
        {
            if (obj == null)
                return AllowNull;

            return InterfaceType.IsAssignableFrom(obj.GetType());
        }

        /// <summary>
        /// Gets a formatted error message for validation failures.
        /// </summary>
        /// <param name="fieldName">The name of the field being validated</param>
        /// <param name="obj">The object that failed validation</param>
        /// <returns>A formatted error message</returns>
        public string GetErrorMessage(string fieldName, UnityEngine.Object obj)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
                return ErrorMessage;

            if (obj == null && !AllowNull)
                return $"Field '{fieldName}' requires an object implementing '{InterfaceType.Name}' (null not allowed)";

            if (obj != null)
                return $"Object '{obj.name}' assigned to field '{fieldName}' does not implement required interface '{InterfaceType.Name}'";

            return null;
        }
    }

    /// <summary>
    /// Extension methods for RequireInterface validation.
    /// </summary>
    public static class RequireInterfaceExtensions
    {
        /// <summary>
        /// Validates all fields with RequireInterface attributes on the given MonoBehaviour.
        /// </summary>
        /// <param name="behaviour">The MonoBehaviour to validate</param>
        /// <param name="logErrors">Whether to log validation errors</param>
        /// <returns>True if all fields are valid, false otherwise</returns>
        public static bool ValidateInterfaces(this MonoBehaviour behaviour, bool logErrors = true)
        {
            if (behaviour == null)
                return false;

            bool isValid = true;
            var type = behaviour.GetType();
            var fields = type.GetFields(System.Reflection.BindingFlags.Instance | 
                                       System.Reflection.BindingFlags.Public | 
                                       System.Reflection.BindingFlags.NonPublic);

            foreach (var field in fields)
            {
                var attributes = field.GetCustomAttributes(typeof(RequireInterfaceAttribute), true);
                if (attributes.Length == 0)
                    continue;

                var attribute = (RequireInterfaceAttribute)attributes[0];
                if (!attribute.ValidateAtRuntime)
                    continue;

                var fieldValue = field.GetValue(behaviour) as UnityEngine.Object;
                if (!attribute.IsValid(fieldValue))
                {
                    isValid = false;
                    if (logErrors)
                    {
                        var errorMsg = attribute.GetErrorMessage(field.Name, fieldValue);
                        Debug.LogError($"[{behaviour.GetType().Name}] {errorMsg}", behaviour);
                    }
                }
            }

            return isValid;
        }

        /// <summary>
        /// Gets all interface validation errors for the given MonoBehaviour.
        /// </summary>
        /// <param name="behaviour">The MonoBehaviour to check</param>
        /// <returns>An array of validation error messages</returns>
        public static string[] GetInterfaceValidationErrors(this MonoBehaviour behaviour)
        {
            if (behaviour == null)
                return new string[] { "Behaviour is null" };

            var errors = new System.Collections.Generic.List<string>();
            var type = behaviour.GetType();
            var fields = type.GetFields(System.Reflection.BindingFlags.Instance | 
                                       System.Reflection.BindingFlags.Public | 
                                       System.Reflection.BindingFlags.NonPublic);

            foreach (var field in fields)
            {
                var attributes = field.GetCustomAttributes(typeof(RequireInterfaceAttribute), true);
                if (attributes.Length == 0)
                    continue;

                var attribute = (RequireInterfaceAttribute)attributes[0];
                var fieldValue = field.GetValue(behaviour) as UnityEngine.Object;
                
                if (!attribute.IsValid(fieldValue))
                {
                    errors.Add(attribute.GetErrorMessage(field.Name, fieldValue));
                }
            }

            return errors.ToArray();
        }
    }
}