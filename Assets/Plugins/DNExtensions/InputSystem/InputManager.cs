using System;
using DNExtensions.Button;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DNExtensions.InputSystem
{

    public class InputManager : MonoBehaviour
    {
        public static InputManager Instance { get; private set; }

        [SerializeField] private PlayerInput playerInput;

        [Header("Cursor Settings")]
        [SerializeField] private bool hideCursor = true;
        
        
        // Sprite assets has to be in /Resources/Sprite Assets/ folder
        [Header("Controls Sprite Assets")] 
        [SerializeField] private TMP_SpriteAsset keyboardMouseSpriteAsset;
        [SerializeField] private TMP_SpriteAsset gamepadSpriteAsset;


        [Separator]
        [SerializeField, ReadOnly] private bool isCurrentDeviceGamepad;
        [SerializeField, ReadOnly] private bool isCurrentDeviceTouchscreen;

        public bool IsCurrentDeviceGamepad => isCurrentDeviceGamepad;
        public bool IsCurrentDeviceTouchscreen => isCurrentDeviceTouchscreen;
        

        public PlayerInput PlayerInput => playerInput;

        public event Action<PlayerInput> OnDeviceRegainedEvent;
        public event Action<PlayerInput> OnDeviceLostEvent;
        public event Action<PlayerInput> OnControlsChangedEvent;


        private void OnValidate()
        {
            if (!playerInput)
            {
                Debug.Log("No Player Input was set!");
            }

            if (playerInput && playerInput.notificationBehavior != PlayerNotifications.InvokeCSharpEvents)
            {
                playerInput.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;
                Debug.Log("Set Player Input notification to c# events");
            }
        }

        private void Awake()
        {
            
            if ((!Instance || Instance == this)&& playerInput)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }
            

            SetCursorVisibility(!hideCursor);
        }

        private void OnEnable()
        {
            if (!playerInput) return;

            playerInput.onDeviceRegained += OnDeviceRegained;
            playerInput.onDeviceLost += OnDeviceLost;
            playerInput.onControlsChanged += OnControlsChanged;
        }

        private void OnDisable()
        {
            if (!playerInput) return;

            playerInput.onDeviceRegained -= OnDeviceRegained;
            playerInput.onDeviceLost -= OnDeviceLost;
            playerInput.onControlsChanged -= OnControlsChanged;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void OnDeviceRegained(PlayerInput input)
        {
            SetActiveControlScheme(input);
            OnDeviceRegainedEvent?.Invoke(input);
        }

        private void OnDeviceLost(PlayerInput input)
        {
            SetActiveControlScheme(input);
            OnDeviceLostEvent?.Invoke(input);
        }

        private void OnControlsChanged(PlayerInput input)
        {
            SetActiveControlScheme(input);
            OnControlsChangedEvent?.Invoke(input);
        }

        private void SetActiveControlScheme(PlayerInput input)
        {
            isCurrentDeviceGamepad = input.currentControlScheme == "Gamepad";
            isCurrentDeviceTouchscreen = input.currentControlScheme == "Touch";
        }


        /// <summary>
        /// Sets the cursor visibility and lock state.
        /// </summary>
        /// <param name="state">True to show the cursor, false to hide it.</param>
        public void SetCursorVisibility(bool state)
        {
            if (state)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Confined;
                Cursor.visible = false;
            }
        }

        /// <summary>
        /// Toggles cursor visibility between visible and hidden states.
        /// </summary>
        [Button(ButtonPlayMode.OnlyWhenPlaying)]
        public void ToggleCursorVisibility()
        {
            if (Cursor.visible)
            {
                Cursor.lockState = CursorLockMode.Confined;
                Cursor.visible = false;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        public static string ReplaceActionBindingsWithSprites(string text)
        {
            if (!Instance) return text;

            TMP_SpriteAsset spriteAsset = Instance.isCurrentDeviceGamepad
                ? Instance.gamepadSpriteAsset
                : Instance.keyboardMouseSpriteAsset;

            return InputManagerBindingFormatter.ReplaceActionBindings(text, true, Instance.playerInput, spriteAsset);
        }

        public static string ReplaceActionBindingsWithText(string text)
        {
            return InputManagerBindingFormatter.ReplaceActionBindings(text, false, Instance.playerInput);
        }
        
        
            
        /// <summary>
        /// Get binding for a specific InputAction
        /// </summary>
        public static string GetActionBinding(InputAction action, bool asSprite = true)
        {
            if (!Instance?.playerInput || action == null) return action?.name ?? "Unknown";
    
            TMP_SpriteAsset spriteAsset = asSprite ? Instance.isCurrentDeviceGamepad 
                ? Instance.gamepadSpriteAsset 
                : Instance.keyboardMouseSpriteAsset : null;
    
            return InputManagerBindingFormatter.GetActionBinding(action, asSprite, Instance.playerInput, spriteAsset);
        }

        /// <summary>
        /// Get bindings for multiple InputActions
        /// </summary>
        public static string GetActionBindings(InputAction[] actions, string separator = " | ", bool asSprites = true)
        {
            if (actions == null || actions.Length == 0) return "";
    
            string[] bindings = new string[actions.Length];
            for (int i = 0; i < actions.Length; i++)
            {
                bindings[i] = GetActionBinding(actions[i], asSprites);
            }
    
            return string.Join(separator, bindings);
        }

    }
}