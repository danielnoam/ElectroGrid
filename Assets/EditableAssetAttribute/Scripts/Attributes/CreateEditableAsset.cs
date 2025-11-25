using System;
using UnityEngine;

namespace Core.Attributes
{
    /// <summary>
    /// Attribute to mark fields as editable assets, allowing quick creation and cloning of ScriptableObjects from the Unity inspector.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public class CreateEditableAsset : PropertyAttribute
    {
        public bool AddLabel { get; private set; }
        public float SetWidth { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateEditableAsset"/> attribute.
        /// </summary>
        /// <param name="addLabel">If set to <c>true</c>, a label will be added to the field in the inspector.</param>
        public CreateEditableAsset(bool addLabel = false, float setWidth = 100f)
        {
            AddLabel = addLabel;
            SetWidth = setWidth;
        }
    }
}
