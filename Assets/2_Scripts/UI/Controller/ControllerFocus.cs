using System.Collections.Generic;
using DNExtensions.Systems.MenuSystem;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// Gives menus and windows controller and keyboard support. It reads the UI map of the game's input actions
/// (InputSystem_Actions) and notices which kind of device performed an action last: with a gamepad or keyboard it keeps
/// something selected inside the newest open <see cref="FocusScope"/> and turns Cancel into that scope's back button.
/// With a mouse or touch it stays out of the way. Created on its own at startup.
/// </summary>
public class ControllerFocus : MonoBehaviour
{
    private const string UiMapName = "UI";
    private const string GamepadScheme = "Gamepad";
    private const float FramePadding = 10f;
    private const float FrameThickness = 6f;
    private static readonly Color FrameColor = new Color(0.85f, 0.3f, 1f, 1f);

    private readonly Dictionary<GameObject, bool> _hasOwnHighlight = new Dictionary<GameObject, bool>();
    private RectTransform _frame;
    private static InputActionMap _uiMap;
    private static InputActionAsset _uiAsset;
    private static InputActionAsset _uiMapSource;
    private static int _cancelConsumedFrame = -1;

    public static ControllerFocus Instance { get; private set; }

    /// <summary>True while the last input came from a gamepad or the keyboard.</summary>
    public static bool ControllerActive { get; private set; }

    /// <summary>True while the last input came from a gamepad, which is what rumble needs.</summary>
    public static bool GamepadActive { get; private set; }

    public static FocusScope TopScope { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        ControllerActive = false;
        GamepadActive = false;
        TopScope = null;
        _uiMap = null;
        _uiAsset = null;
        _uiMapSource = null;
        _cancelConsumedFrame = -1;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        if (Instance) return;

        var focus = new GameObject(nameof(ControllerFocus)).AddComponent<ControllerFocus>();
        DontDestroyOnLoad(focus.gameObject);
    }

    private void Awake()
    {
        if (Instance && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Update()
    {
        DetectInputDevice();
        DriveUiModule();
        TopScope = FindTopScope();
        if (ControllerActive && TopScope)
        {
            if (TopScope.ControlsGame) ClearSelection();
            else KeepSelectionInScope();
            if (CancelPressed() && _cancelConsumedFrame != Time.frameCount) PressCancel();
        }

        UpdateFrame();
    }

    /// <summary>
    /// Buttons show their own selected state through SelectableAnimator. Sliders and toggles have none, so a frame is
    /// drawn around them while a controller has them selected.
    /// </summary>
    private void UpdateFrame()
    {
        var selected = ControllerActive && EventSystem.current ? EventSystem.current.currentSelectedGameObject : null;
        var target = selected && !HasOwnHighlight(selected) ? selected.transform as RectTransform : null;
        var canvas = target ? target.GetComponentInParent<Canvas>() : null;
        if (!target || !canvas)
        {
            if (_frame) _frame.gameObject.SetActive(false);
            return;
        }

        var root = canvas.rootCanvas.transform as RectTransform;
        if (!_frame) _frame = CreateFrame();
        if (_frame.parent != root) _frame.SetParent(root, false);
        _frame.SetAsLastSibling();
        _frame.gameObject.SetActive(true);

        var corners = new Vector3[4];
        target.GetWorldCorners(corners);
        Vector2 min = root.InverseTransformPoint(corners[0]);
        Vector2 max = root.InverseTransformPoint(corners[2]);
        _frame.anchorMin = _frame.anchorMax = new Vector2(0.5f, 0.5f);
        _frame.anchoredPosition = (min + max) * 0.5f;
        _frame.sizeDelta = max - min + Vector2.one * (FramePadding * 2f);
    }

    private bool HasOwnHighlight(GameObject selected)
    {
        if (!_hasOwnHighlight.TryGetValue(selected, out bool own))
        {
            own = selected.GetComponent<SelectableAnimator>() || selected.GetComponentInParent<TMPro.TMP_Dropdown>();
            _hasOwnHighlight[selected] = own;
        }

        return own;
    }

    private RectTransform CreateFrame()
    {
        var frame = new GameObject("Controller Selection Frame", typeof(RectTransform)).transform as RectTransform;
        DontDestroyOnLoad(frame.gameObject);
        var edges = new[] { (new Vector2(0f, 1f), new Vector2(1f, 1f)), (new Vector2(0f, 0f), new Vector2(1f, 0f)), (new Vector2(0f, 0f), new Vector2(0f, 1f)), (new Vector2(1f, 0f), new Vector2(1f, 1f)) };
        foreach (var (anchorMin, anchorMax) in edges)
        {
            var edge = new GameObject("Edge", typeof(RectTransform), typeof(Image)).transform as RectTransform;
            edge.SetParent(frame, false);
            edge.anchorMin = anchorMin;
            edge.anchorMax = anchorMax;
            bool horizontal = Mathf.Approximately(anchorMin.y, anchorMax.y);
            edge.sizeDelta = horizontal ? new Vector2(0f, FrameThickness) : new Vector2(FrameThickness, 0f);
            edge.anchoredPosition = Vector2.zero;
            var image = edge.GetComponent<Image>();
            image.color = FrameColor;
            image.raycastTarget = false;
        }

        return frame;
    }

    /// <summary>
    /// A copy of the UI map from the game's input actions, so PlayerInput's control scheme switches never limit which
    /// devices it hears. Edit the bindings in InputSystem_Actions; the copy is remade when the scene's actions change.
    /// </summary>
    private static InputActionMap UiMap
    {
        get
        {
            var asset = PlayerInput.all.Count > 0 ? PlayerInput.all[0].actions : null;
            if (_uiMap != null && asset == _uiMapSource) return _uiMap;

            ReleaseUiMap();
            _uiMapSource = asset;
            var source = asset ? asset.FindActionMap(UiMapName) : null;
            if (source == null) return null;

            _uiMap = source.Clone();
            _uiAsset = ScriptableObject.CreateInstance<InputActionAsset>();
            _uiAsset.name = "UI (ControllerFocus)";
            _uiAsset.AddActionMap(_uiMap);
            _uiMap.Enable();
            return _uiMap;
        }
    }

    /// <summary>An action from the UI map of the game's input actions, such as Cancel, Restart or Info.</summary>
    public static InputAction FindUiAction(string actionName)
    {
        return UiMap?.FindAction(actionName);
    }

    private void OnDestroy()
    {
        if (Instance != this) return;

        ReleaseUiMap();
    }

    private static void ReleaseUiMap()
    {
        _uiMap?.Disable();
        if (_uiAsset) Destroy(_uiAsset);
        _uiAsset = null;
        _uiMap = null;
    }

    /// <summary>
    /// The EventSystem's input module reads the same UI map copy, so buttons answer to the game's own bindings (Space
    /// selects too) and PlayerInput's scheme switches never cut off the mouse or touch.
    /// </summary>
    private static void DriveUiModule()
    {
        var module = EventSystem.current ? EventSystem.current.GetComponent<InputSystemUIInputModule>() : null;
        if (!module || !_uiAsset || module.actionsAsset == _uiAsset) return;

        module.actionsAsset = _uiAsset;
    }

    private void DetectInputDevice()
    {
        var map = UiMap;
        if (map == null) return;

        foreach (var action in map.actions)
        {
            if (!action.WasPerformedThisFrame()) continue;

            var device = action.activeControl?.device;
            if (device is Gamepad gamepad)
            {
                if (!GamepadActive) OnGamepadStarted(gamepad);
                ControllerActive = true;
                GamepadActive = true;
                return;
            }

            if (device is Keyboard)
            {
                ControllerActive = true;
                GamepadActive = false;
                return;
            }

            if (device is Pointer)
            {
                ControllerActive = false;
                GamepadActive = false;
            }
        }
    }

    /// <summary>
    /// PlayerInput only plays the Match3 map, which has no gamepad bindings, so it never moves to the Gamepad scheme by
    /// itself. Moving it there is what lets the DNExtensions rumble listener find the pad; mouse or touch moves it back.
    /// </summary>
    private static void OnGamepadStarted(Gamepad gamepad)
    {
        var playerInput = PlayerInput.all.Count > 0 ? PlayerInput.all[0] : null;
        if (!playerInput || playerInput.currentControlScheme == GamepadScheme) return;
        if (playerInput.actions == null || playerInput.actions.FindControlSchemeIndex(GamepadScheme) < 0) return;

        playerInput.SwitchCurrentControlScheme(GamepadScheme, gamepad);
    }

    private static FocusScope FindTopScope()
    {
        FocusScope top = null;
        foreach (var scope in FocusScope.All)
        {
            scope.Refresh();
            if (scope.IsOpen && (!top || scope.OpenedAt >= top.OpenedAt)) top = scope;
        }

        return top;
    }

    private static void KeepSelectionInScope()
    {
        var eventSystem = EventSystem.current;
        if (!eventSystem) return;

        var selected = eventSystem.currentSelectedGameObject ? eventSystem.currentSelectedGameObject.GetComponent<Selectable>() : null;
        if (selected && TopScope.Contains(selected) && FocusScope.IsUsable(selected))
        {
            TopScope.Remember(selected);
            return;
        }

        var target = TopScope.FocusTarget();
        if (target) eventSystem.SetSelectedGameObject(target.gameObject);
    }

    private static void ClearSelection()
    {
        var eventSystem = EventSystem.current;
        if (eventSystem && eventSystem.currentSelectedGameObject) eventSystem.SetSelectedGameObject(null);
    }

    /// <summary>Keeps this frame's Cancel press from reaching the top scope, for game input that used it already.</summary>
    public static void ConsumeCancel()
    {
        _cancelConsumedFrame = Time.frameCount;
    }

    private static bool CancelPressed()
    {
        var cancel = FindUiAction("Cancel");
        return cancel != null && cancel.WasPerformedThisFrame();
    }

    /// <summary>Presses the top scope's cancel button, as B or Escape does.</summary>
    public static void PressCancel()
    {
        if (!TopScope) return;

        foreach (var dropdown in TopScope.GetComponentsInChildren<TMPro.TMP_Dropdown>())
        {
            if (!dropdown.IsExpanded) continue;

            dropdown.Hide();
            if (EventSystem.current) EventSystem.current.SetSelectedGameObject(dropdown.gameObject);
            return;
        }

        var button = GamepadActive ? TopScope.CancelButton : TopScope.KeyboardCancelButton;
        if (!button || !button.isActiveAndEnabled || !button.IsInteractable()) return;

        var prompt = button.GetComponent<ButtonPrompt>();
        if (prompt && prompt.HoldsAction("Cancel")) return;

        ExecuteEvents.Execute(button.gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
    }

    /// <summary>Selects something in the top scope right away, for tests and for screens that rebuild their buttons.</summary>
    public static void FocusTopScope()
    {
        if (!TopScope || !EventSystem.current) return;

        var target = TopScope.FocusTarget();
        if (target) EventSystem.current.SetSelectedGameObject(target.gameObject);
    }

    /// <summary>Test hook: behave as if a gamepad had just been used.</summary>
    public static void SimulateController(bool active, bool gamepad = true)
    {
        ControllerActive = active;
        GamepadActive = active && gamepad;
    }
}
