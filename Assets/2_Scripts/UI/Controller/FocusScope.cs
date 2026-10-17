using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One screen or window that a controller can move around in. While it is the newest open scope, controller and
/// keyboard input selects inside it, and Cancel (B, Escape) presses its cancel button.
/// A scope counts as open while it is active and every CanvasGroup above it lets input through, so windows that
/// fade with a CanvasGroup need no extra calls.
/// </summary>
[DisallowMultipleComponent]
public class FocusScope : MonoBehaviour
{
    [Tooltip("Selected when the scope opens with a controller in use. Empty picks the first interactable selectable")]
    [SerializeField] private Selectable firstSelected;
    [Tooltip("Pressed by Cancel (B on a gamepad, Escape on a keyboard). Empty means Cancel does nothing here")]
    [SerializeField] private Button cancelButton;
    [Tooltip("Pressed by Escape instead of the cancel button while the keyboard is in use. Empty uses the cancel button")]
    [SerializeField] private Button keyboardCancelButton;
    [Tooltip("Coming back to this scope selects what was selected last, rather than the first selectable")]
    [SerializeField] private bool rememberSelection = true;
    [Tooltip("Nothing is selected while this scope is on top: directions and Submit go to the game (the board cursor), and the buttons are reached through their Button Prompts")]
    [SerializeField] private bool controlsGame;

    private static readonly List<FocusScope> Scopes = new List<FocusScope>();
    private readonly List<CanvasGroup> _groups = new List<CanvasGroup>();
    private Selectable _lastSelected;
    private bool _wasOpen;

    public static IReadOnlyList<FocusScope> All => Scopes;
    public float OpenedAt { get; private set; }
    public Button CancelButton => cancelButton;
    public Button KeyboardCancelButton => keyboardCancelButton ? keyboardCancelButton : cancelButton;
    public bool IsOpen { get; private set; }
    public bool ControlsGame => controlsGame;

    public Selectable FirstSelected
    {
        get => firstSelected;
        set => firstSelected = value;
    }

    private void OnEnable()
    {
        if (!Scopes.Contains(this)) Scopes.Add(this);
        _wasOpen = false;
    }

    private void OnDisable()
    {
        Scopes.Remove(this);
        IsOpen = false;
    }

    /// <summary>Called by ControllerFocus once a frame, before it picks the top scope.</summary>
    public void Refresh()
    {
        IsOpen = isActiveAndEnabled && GroupsAllowInput();
        if (IsOpen && !_wasOpen) OpenedAt = Time.unscaledTime;
        _wasOpen = IsOpen;
    }

    public void Remember(Selectable selectable)
    {
        if (rememberSelection && selectable && Contains(selectable)) _lastSelected = selectable;
    }

    public bool Contains(Selectable selectable)
    {
        return selectable && selectable.transform.IsChildOf(transform);
    }

    /// <summary>The selectable to focus when the scope takes over: the last one if remembered, else the first.</summary>
    public Selectable FocusTarget()
    {
        if (IsUsable(_lastSelected)) return _lastSelected;
        if (IsUsable(firstSelected)) return firstSelected;

        foreach (var selectable in GetComponentsInChildren<Selectable>())
        {
            if (IsUsable(selectable)) return selectable;
        }

        return null;
    }

    public static bool IsUsable(Selectable selectable)
    {
        return selectable && selectable.isActiveAndEnabled && selectable.IsInteractable() && selectable.navigation.mode != Navigation.Mode.None;
    }

    private bool GroupsAllowInput()
    {
        _groups.Clear();
        GetComponentsInParent(false, _groups);
        foreach (var group in _groups)
        {
            if (!group.interactable || !group.blocksRaycasts || group.alpha <= 0.01f) return false;
            if (group.ignoreParentGroups) break;
        }

        return true;
    }
}
