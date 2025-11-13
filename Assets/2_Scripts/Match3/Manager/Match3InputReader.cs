using System;
using System.Collections;
using DNExtensions;
using DNExtensions.Button;
using DNExtensions.InputSystem;
using UnityEngine;
using UnityEngine.InputSystem;
using Gyroscope = UnityEngine.InputSystem.Gyroscope;


public class Match3InputReader : InputReaderBase
{
    
    [Header("Settings")]
    [SerializeField, Min(0.1f)] private float swipeThreshold = 55;
    [SerializeField, Range(0,1)] private float swipeDeadzone = 0.3f;
    
    [Separator]
    [SerializeField, ReadOnly] private Vector3 gyroRotation;
    [SerializeField, ReadOnly] private Vector2 mousePosition;
    [SerializeField, ReadOnly] private bool isPressing;
    [SerializeField, ReadOnly] private Vector2 pressStartPosition;
    [SerializeField, ReadOnly, Preview] private Vector2 swipeDirection;

    
    private InputActionMap _match3ActionMap;
    private InputAction _selectAction;
    private InputAction _mousePositionAction;




    public Vector3 GyroRotation => gyroRotation;
    public Vector2 MousePosition => mousePosition;
    public float SwipeDeadzone => swipeDeadzone;
    
    public event Action<InputAction.CallbackContext> OnSelect;
    public event Action<Vector2> OnSwipe;
    
    

    protected override void Start()
    {
        base.Start();
        
        _match3ActionMap = PlayerInput.actions.FindActionMap("Match3",true);
        _selectAction = _match3ActionMap.FindAction("Select",true);
        _mousePositionAction = _match3ActionMap.FindAction("MousePosition",true);
        
        SubscribeToAction(_selectAction, OnSelectAction);
        SubscribeToAction(_mousePositionAction, OnMousePositionAction);

        StartCoroutine(ResubscribeToActions());
    }
    

    private void OnDestroy()
    {
        UnsubscribeFromAction(_selectAction, OnSelectAction);
        UnsubscribeFromAction(_mousePositionAction, OnMousePositionAction);
    }

    private void Update()
    {
        UpdateGyroRotation();
    }

    private void OnSelectAction(InputAction.CallbackContext callbackContext)
    {
        if (callbackContext.started)
        {
            isPressing = true;
            pressStartPosition = mousePosition;
            swipeDirection = Vector2.zero;
        } 
        else if (callbackContext.canceled)
        {
            isPressing = false;
            pressStartPosition = Vector2.zero;
        }
        
        OnSelect?.Invoke(callbackContext);
    }
    
    private void OnMousePositionAction(InputAction.CallbackContext callbackContext)
    {
        mousePosition = callbackContext.ReadValue<Vector2>();
        
        if (isPressing)
        {
            if (Vector2.Distance(pressStartPosition, mousePosition) > swipeThreshold)
            {
                isPressing = false;
                swipeDirection = (mousePosition - pressStartPosition).normalized;
                OnSwipe?.Invoke(swipeDirection);
            }
        }

    }

    private void UpdateGyroRotation()
    {
        if (!IsCurrentDeviceTouchscreen || !IsCurrentDeviceMobile) return;
        gyroRotation = Gyroscope.current.angularVelocity.value;
    }
    
    
    // Resubscribe to actions in case of reloading the scene
    // Called when with a coroutine with a delay after start
    // Because inputManager gets lost and this is a piece of shit Unity Input System design
    [Button(ButtonPlayMode.OnlyWhenPlaying)]
    private IEnumerator ResubscribeToActions()
    {
        yield return new WaitForSeconds(1f);
        Start();
    }
    
    
}
