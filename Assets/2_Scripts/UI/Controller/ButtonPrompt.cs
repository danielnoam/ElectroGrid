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
    [SerializeField] private Vector2 corner = new Vector2(1f, 1f);
    [SerializeField] private Vector2 offset = new Vector2(-12f, -12f);
    [SerializeField, Min(10f)] private float size = 80f;

    private Button _button;
    private FocusScope _scope;
    private RectTransform _badge;
    private Image _icon;
    private TextMeshProUGUI _label;
    private InputAction _action;
    private Device _device;
    private string _control;

    public string ActionName => actionName;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _scope = GetComponentInParent<FocusScope>(true);
    }

    private void Update()
    {
        _action ??= ControllerFocus.FindUiAction(actionName);
        bool show = _action != null && ControllerFocus.ControllerActive && _button.IsInteractable() && _button.isActiveAndEnabled && (!_scope || ControllerFocus.TopScope == _scope);
        if (show && !_badge) CreateBadge();
        if (_badge && _badge.gameObject.activeSelf != show) _badge.gameObject.SetActive(show);
        if (!show) return;

        var device = CurrentDevice();
        var control = BoundControl(device);
        if (device != _device || control != _control) ApplyLook(device, control);

        if (actionName != "Cancel" && actionName != "Submit" && _action.WasPerformedThisFrame() && UsedDevice(_action) == device)
        {
            ExecuteEvents.Execute(gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
        }
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
    private string BoundControl(Device device)
    {
        foreach (var control in _action.controls)
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
