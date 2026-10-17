using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.UI;

/// <summary>
/// A small badge on the corner of a button, showing the control bound to an action in the UI map of the game's input
/// actions, for whichever device is in use. Shown while a gamepad or the keyboard is in use and the button's screen is
/// on top; performing the action presses the button. Cancel only shows the badge: Back is handled by
/// <see cref="ControllerFocus"/> through the screen's <see cref="FocusScope"/>.
/// </summary>
[RequireComponent(typeof(Button))]
public class ButtonPrompt : MonoBehaviour
{
    public enum Device { Xbox, PlayStation, Keyboard }

    [Tooltip("An action in the UI map of InputSystem_Actions, such as Primary, Restart, Settings, Info or Cancel")]
    [SerializeField] private string actionName = "Primary";
    [Tooltip("The action shown and used while the keyboard is in use, when it differs, such as Cancel for Escape. Empty uses the action above")]
    [SerializeField] private string keyboardActionName;
    [SerializeField] private Vector2 corner = new Vector2(1f, 1f);
    [SerializeField] private Vector2 offset = new Vector2(-12f, -12f);
    [SerializeField, Min(10f)] private float size = 80f;
    [Tooltip("With a keyboard or gamepad the key has to be held this long, in seconds, before the button presses, and the badge fills while it is held. 0 presses at once. Mouse and touch press as usual")]
    [SerializeField, Min(0f)] private float holdSeconds;
    [SerializeField] private Color holdColor = new Color(1f, 0.031f, 0.38f, 0.6f);

    private Button _button;
    private FocusScope _scope;
    private RectTransform _badge;
    private Image _icon;
    private TextMeshProUGUI _label;
    private InputAction _action;
    private InputAction _keyboardAction;
    private Device _device;
    private string _control;
    private Image _holdFill;
    private float _held;
    private bool _holdFired;

    public string ActionName => actionName;

    /// <summary>Whether this prompt handles the action itself with a hold, for the device in use.</summary>
    public bool HoldsAction(string name)
    {
        if (holdSeconds <= 0f) return false;
        bool keyboard = !ControllerFocus.GamepadActive && !string.IsNullOrEmpty(keyboardActionName);
        return (keyboard ? keyboardActionName : actionName) == name;
    }

    private void Awake()
    {
        _button = GetComponent<Button>();
        _scope = GetComponentInParent<FocusScope>(true);
    }

    private void Update()
    {
        _action ??= ControllerFocus.FindUiAction(actionName);
        if (!string.IsNullOrEmpty(keyboardActionName)) _keyboardAction ??= ControllerFocus.FindUiAction(keyboardActionName);
        var device = CurrentDevice();
        bool keyboardOverride = device == Device.Keyboard && _keyboardAction != null;
        var action = keyboardOverride ? _keyboardAction : _action;
        string name = keyboardOverride ? keyboardActionName : actionName;
        var control = action != null ? BoundControl(action, device) : null;

        bool show = control != null && ControllerFocus.ControllerActive && _button.IsInteractable() && _button.isActiveAndEnabled && (!_scope || ControllerFocus.TopScope == _scope);
        if (show && !_badge) CreateBadge();
        if (_badge && _badge.gameObject.activeSelf != show) _badge.gameObject.SetActive(show);
        if (!show)
        {
            ResetHold();
            return;
        }

        if (device != _device || control != _control) ApplyLook(device, control);

        if (holdSeconds > 0f)
        {
            UpdateHold(action, device);
            return;
        }

        if (name != "Cancel" && name != "Submit" && action.WasPerformedThisFrame() && UsedDevice(action) == device)
        {
            ExecuteEvents.Execute(gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
        }
    }

    private void UpdateHold(InputAction action, Device device)
    {
        bool pressed = action.IsPressed() && UsedDevice(action) == device;
        if (!pressed)
        {
            ResetHold();
            return;
        }

        if (_holdFired) return;

        _held += Time.unscaledDeltaTime;
        if (_holdFill) _holdFill.fillAmount = Mathf.Clamp01(_held / holdSeconds);
        if (_held < holdSeconds) return;

        _holdFired = true;
        if (_holdFill) _holdFill.fillAmount = 0f;
        ExecuteEvents.Execute(gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
    }

    private void ResetHold()
    {
        _held = 0f;
        _holdFired = false;
        if (_holdFill) _holdFill.fillAmount = 0f;
    }

    private static Device CurrentDevice()
    {
        if (!ControllerFocus.GamepadActive) return Device.Keyboard;
        return Gamepad.current is DualShockGamepad ? Device.PlayStation : Device.Xbox;
    }

    private static Device? UsedDevice(InputAction action)
    {
        var device = action.activeControl?.device;
        if (device is Keyboard) return Device.Keyboard;
        if (device is Gamepad) return device is DualShockGamepad ? Device.PlayStation : Device.Xbox;
        return null;
    }

    /// <summary>The name of the control the action is bound to on the device in use, read from the bindings.</summary>
    private static string BoundControl(InputAction action, Device device)
    {
        foreach (var control in action.controls)
        {
            bool match = device == Device.Keyboard ? control.device is Keyboard : control.device is Gamepad;
            if (match) return control.name;
        }

        return null;
    }

    private void CreateBadge()
    {
        _badge = new GameObject("Button Prompt", typeof(RectTransform)).transform as RectTransform;
        _badge.SetParent(transform, false);
        _badge.anchorMin = _badge.anchorMax = corner;
        _badge.pivot = new Vector2(0.5f, 0.5f);
        _badge.anchoredPosition = offset;
        _badge.sizeDelta = Vector2.one * size;

        var background = _badge.gameObject.AddComponent<Image>();
        background.sprite = Resources.Load<Sprite>("Sprites/ShapeCircle");
        background.color = new Color(0.07f, 0.06f, 0.15f, 0.95f);
        background.raycastTarget = false;

        _holdFill = new GameObject("Hold", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        _holdFill.rectTransform.SetParent(_badge, false);
        _holdFill.rectTransform.anchorMin = Vector2.zero;
        _holdFill.rectTransform.anchorMax = Vector2.one;
        _holdFill.rectTransform.sizeDelta = Vector2.zero;
        _holdFill.sprite = background.sprite;
        _holdFill.type = Image.Type.Filled;
        _holdFill.fillMethod = Image.FillMethod.Radial360;
        _holdFill.fillOrigin = (int)Image.Origin360.Top;
        _holdFill.fillClockwise = true;
        _holdFill.fillAmount = 0f;
        _holdFill.color = holdColor;
        _holdFill.raycastTarget = false;

        _icon = new GameObject("Icon", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        _icon.rectTransform.SetParent(_badge, false);
        _icon.rectTransform.anchorMin = Vector2.one * 0.08f;
        _icon.rectTransform.anchorMax = Vector2.one * 0.92f;
        _icon.rectTransform.sizeDelta = Vector2.zero;
        _icon.preserveAspect = true;
        _icon.raycastTarget = false;

        _label = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        _label.rectTransform.SetParent(_badge, false);
        _label.rectTransform.anchorMin = Vector2.zero;
        _label.rectTransform.anchorMax = Vector2.one;
        _label.rectTransform.sizeDelta = Vector2.zero;
        _label.alignment = TextAlignmentOptions.Center;
        _label.enableAutoSizing = true;
        _label.fontSizeMin = 8f;
        _label.fontSizeMax = size * 0.55f;
        _label.fontStyle = FontStyles.Bold;
        _label.raycastTarget = false;

        _holdFill.transform.SetAsLastSibling();
        _device = (Device)(-1);
    }

    /// <summary>Uses the icon from <see cref="ButtonPromptIcons"/> when there is one, otherwise the control's name.</summary>
    private void ApplyLook(Device device, string control)
    {
        _device = device;
        _control = control;
        var sprite = ButtonPromptIcons.Get(control, device);
        _icon.sprite = sprite;
        _icon.enabled = sprite;
        _label.enabled = !sprite;
        _label.text = control != null ? control.ToUpperInvariant() : "?";
    }
}
