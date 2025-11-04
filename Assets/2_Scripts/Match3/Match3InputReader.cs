using System;
using DNExtensions;
using DNExtensions.InputSystem;
using UnityEngine;
using UnityEngine.InputSystem;


public class Match3InputReader : InputReaderBase
{
    
    [Separator]
    [SerializeField,ReadOnly] private Vector2 mousePosition;
    
    private InputActionMap _match3ActionMap;
    private InputAction _selectAction;
    private InputAction _mousePositionAction;
    
    
    
    public event Action<InputAction.CallbackContext> OnSelect;
    public Vector2 MousePosition => mousePosition;
    
    

    protected override void Start()
    {
        base.Start();
        
        _match3ActionMap = PlayerInput.actions.FindActionMap("Match3",true);
        _selectAction = _match3ActionMap.FindAction("Select",true);
        _mousePositionAction = _match3ActionMap.FindAction("MousePosition",true);
        
        SubscribeToAction(_selectAction, OnSelectAction);
        SubscribeToAction(_mousePositionAction, OnMousePositionAction);
    }
    

    private void OnDestroy()
    {
        UnsubscribeFromAction(_selectAction, OnSelectAction);
        UnsubscribeFromAction(_mousePositionAction, OnMousePositionAction);
    }


    private void OnSelectAction(InputAction.CallbackContext callbackContext)
    {
        OnSelect?.Invoke(callbackContext);
    }
    
    private void OnMousePositionAction(InputAction.CallbackContext callbackContext)
    {
        mousePosition = callbackContext.ReadValue<Vector2>();
    }
    
}
