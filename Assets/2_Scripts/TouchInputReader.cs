using System;
using System.Collections;
using DNExtensions.Utilities;
using DNExtensions.Utilities.Button;
using DNExtensions.Systems.InputSystem;
using UnityEngine;
using UnityEngine.InputSystem;


public class TouchInputReader : InputReaderBase
{
    public static TouchInputReader Instance { get; private set; }
    
    [Header("Settings")]
    [Tooltip("Fallback swipe distance in screen pixels, used until the board reports its tile size")]
    [SerializeField, Min(0.1f)] private float swipeThreshold = 55;
    [Tooltip("How far the finger moves, as a share of one tile, before the drag counts as a swipe")]
    [SerializeField, Range(0.1f, 1f)] private float swipeTileFraction = 0.4f;
    [Tooltip("A swipe whose weaker axis is at least this share of the stronger one is too close to diagonal to call, so it waits for the finger to move further")]
    [SerializeField, Range(0.5f, 1f)] private float diagonalTolerance = 0.85f;
    [Tooltip("Past this many swipe distances an undecided swipe goes with its stronger axis anyway")]
    [SerializeField, Min(1f)] private float maxSwipeWait = 2.5f;
    
    [Separator]
    [SerializeField, ReadOnly] private Vector2 mousePosition;
    [SerializeField, ReadOnly] private bool isPressing;
    [SerializeField, ReadOnly] private Vector2 pressStartPosition;
    [SerializeField, ReadOnly, Preview] private Vector2 swipeDirection;
    [SerializeField, ReadOnly] private float tileSwipeDistance;

    
    private InputActionMap _match3ActionMap;
    private InputAction _selectAction;
    private InputAction _mousePositionAction;
    private bool _resubscribeScheduled;




    public Vector2 MousePosition => mousePosition;
    private float SwipeDistance => tileSwipeDistance > 0f ? tileSwipeDistance : swipeThreshold;
    
    public event Action<InputAction.CallbackContext> OnSelect;
    public event Action<Vector2> OnSwipe;


    private void Awake()
    {
        if (Instance && Instance != this)
        {
            return;
        }
        
        Instance = this;
    }

    private void Start()
    {
        _match3ActionMap = PlayerInput.actions.FindActionMap("Match3",true);
        _selectAction = _match3ActionMap.FindAction("Select",true);
        _mousePositionAction = _match3ActionMap.FindAction("MousePosition",true);
        
        // Start runs again from ResubscribeToActions, and SubscribeToAction no longer clears old handlers
        UnsubscribeFromAction(_selectAction, OnSelectAction);
        UnsubscribeFromAction(_mousePositionAction, OnMousePositionAction);
        SubscribeToAction(_selectAction, OnSelectAction);
        SubscribeToAction(_mousePositionAction, OnMousePositionAction);

        if (_resubscribeScheduled) return;
        _resubscribeScheduled = true;
        StartCoroutine(ResubscribeToActions());
    }
    

    private void OnDestroy()
    {
        UnsubscribeFromAction(_selectAction, OnSelectAction);
        UnsubscribeFromAction(_mousePositionAction, OnMousePositionAction);
    }

    private void OnSelectAction(InputAction.CallbackContext callbackContext)
    {
        if (callbackContext.started)
        {
            if (Pointer.current != null) mousePosition = Pointer.current.position.ReadValue();
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
    
    /// <summary>The on-screen size of one tile, so a swipe needs the same finger travel on every screen.</summary>
    public void SetTileScreenSize(float pixels)
    {
        tileSwipeDistance = pixels > 0f ? pixels * swipeTileFraction : 0f;
    }

    private void OnMousePositionAction(InputAction.CallbackContext callbackContext)
    {
        mousePosition = callbackContext.ReadValue<Vector2>();

        if (!isPressing) return;

        Vector2 delta = mousePosition - pressStartPosition;
        float distance = delta.magnitude;
        if (distance <= SwipeDistance) return;

        float absX = Mathf.Abs(delta.x);
        float absY = Mathf.Abs(delta.y);
        bool tooDiagonal = Mathf.Min(absX, absY) >= Mathf.Max(absX, absY) * diagonalTolerance;
        if (tooDiagonal && distance < SwipeDistance * maxSwipeWait) return;

        isPressing = false;
        swipeDirection = absX >= absY ? new Vector2(Mathf.Sign(delta.x), 0f) : new Vector2(0f, Mathf.Sign(delta.y));
        OnSwipe?.Invoke(swipeDirection);
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
