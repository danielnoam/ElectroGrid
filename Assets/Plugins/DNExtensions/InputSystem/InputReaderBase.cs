using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DNExtensions.InputSystem
{
    
    /// <summary>
    /// Base class for handling Unity Input System interactions with cursor management capabilities.
    /// Provides foundation for input handling classes with built-in cursor visibility controls.
    /// </summary>
    public class InputReaderBase : MonoBehaviour
    {
        [SerializeField] protected InputManager inputManager;
    
    
        public bool IsCurrentDeviceGamepad => inputManager?.IsCurrentDeviceGamepad ?? false;
        public bool IsCurrentDeviceTouchscreen => inputManager?.IsCurrentDeviceTouchscreen ?? false;
        
        protected PlayerInput PlayerInput => inputManager?.PlayerInput;
        
        

        
        protected virtual void OnValidate()
        {
            if (!inputManager) inputManager = FindFirstObjectByType<InputManager>();
            
        }
        

        protected virtual void Start()
        {
            if (!inputManager) inputManager = FindFirstObjectByType<InputManager>();
            
        }

        /// <summary>
        /// Subscribes a callback method to all phases of an InputAction (started, performed, canceled).
        /// </summary>
        /// <param name="action">The InputAction to subscribe to. If null, no subscription occurs.</param>
        /// <param name="callback">The callback method to invoke for all action phases.</param>
        protected void SubscribeToAction(InputAction action, Action<InputAction.CallbackContext> callback)
        {
            
            if (action == null)
            {
                Debug.LogError("Null action found!");
                return;
            }

            action.performed -= callback;
            action.started -= callback;
            action.canceled -= callback;
            
            action.performed += callback;
            action.started += callback;
            action.canceled += callback;
        }

        /// <summary>
        /// Unsubscribes a callback method from all phases of an InputAction (started, performed, canceled).
        /// </summary>
        /// <param name="action">The InputAction to unsubscribe from. If null, no unsubscription occurs.</param>
        /// <param name="callback">The callback method to remove from all action phases.</param>
        protected void UnsubscribeFromAction(InputAction action, Action<InputAction.CallbackContext> callback)
        {
            if (action == null)
            {
                Debug.LogError("Null action found!");
                return;
            }

            action.performed -= callback;
            action.started -= callback;
            action.canceled -= callback;
        }
        
    }
}