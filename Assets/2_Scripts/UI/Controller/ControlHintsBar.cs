using System.Collections.Generic;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.UI;

/// <summary>
/// The controls for playing the board, along the bottom of the screen in a level: move, select and row ends. It slides
/// in while a gamepad or the keyboard is in use, and away on mouse or touch, while a window is open, or when the level
/// ends. Icons follow the device in use, through <see cref="ButtonPromptIcons"/> and the UI map's bindings.
/// </summary>
public class ControlHintsBar : MonoBehaviour
{
    private const string MoveControl = "dpad";
    private const string KeyboardMoveControl = "arrows";
    private const float WideIconScale = 1.35f;

    private readonly struct Hint
    {
        public readonly string LabelKey;
        public readonly string[] Actions;

        public Hint(string labelKey, params string[] actions)
        {
            LabelKey = labelKey;
            Actions = actions;
        }
    }

    private static readonly Hint[] Hints =
    {
        new Hint("controls.move", "Navigate"),
        new Hint("controls.select", "Submit"),
        new Hint("controls.rowends", "EdgeLeft", "EdgeRight"),
    };

    [Header("Layout")]
    [SerializeField, Min(40f)] private float height = 210f;
    [SerializeField] private float bottomOffset = 30f;
    [SerializeField, Min(16f)] private float iconSize = 90f;
    [SerializeField, Min(8f)] private float labelSize = 42f;
    [SerializeField, Min(0f)] private float spacing = 40f;
    [SerializeField, Min(0f)] private float sidePadding = 30f;

    [Header("Look")]
    [SerializeField] private TMP_FontAsset font;
    [SerializeField] private Color labelColor = new Color(0.92f, 0.92f, 1f, 0.9f);
    [SerializeField] private Color backgroundColor = new Color(0.04f, 0.03f, 0.1f, 0.75f);

    [Header("Animation")]
    [SerializeField, Min(0f)] private float showDuration = 0.35f;
    [SerializeField, Min(0f)] private float hideDuration = 0.25f;

    private readonly List<(Hint hint, List<Image> icons, TextMeshProUGUI label)> _entries = new List<(Hint, List<Image>, TextMeshProUGUI)>();
    private RectTransform _bar;
    private RectTransform _row;
    private CanvasGroup _group;
    private Sequence _tween;
    private bool _shown;
    private ButtonPrompt.Device _device = (ButtonPrompt.Device)(-1);

    public bool Shown => _shown;

    private void Start()
    {
        Build();
        L10n.LanguageChanged += RefreshLabels;
        RefreshLabels();
    }

    private void OnDestroy()
    {
        _tween.Stop();
        L10n.LanguageChanged -= RefreshLabels;
    }

    private void Update()
    {
        if (!_bar) return;

        bool show = ShouldShow();
        if (show != _shown) Animate(show);
        if (!show) return;

        var device = CurrentDevice();
        if (device != _device) RefreshIcons(device);
        FitWidth();
    }

    private static bool ShouldShow()
    {
        var game = Match3GameManager.Instance;
        if (!ControllerFocus.ControllerActive || !game || game.IsLevelOver) return false;

        var scope = ControllerFocus.TopScope;
        return !scope || scope.ControlsGame;
    }

    /// <summary>Shrinks the row to fit when a language's labels make it wider than the screen.</summary>
    private void FitWidth()
    {
        var parent = _bar.parent as RectTransform;
        if (!parent) return;

        float needed = LayoutUtility.GetPreferredWidth(_row) + sidePadding * 2f;
        float scale = needed > 0f ? Mathf.Min(1f, parent.rect.width / needed) : 1f;
        _row.localScale = Vector3.one * scale;
    }

    private static ButtonPrompt.Device CurrentDevice()
    {
        if (!ControllerFocus.GamepadActive) return ButtonPrompt.Device.Keyboard;
        return Gamepad.current is DualShockGamepad ? ButtonPrompt.Device.PlayStation : ButtonPrompt.Device.Xbox;
    }

    private void Build()
    {
        _bar = new GameObject("Control Hints", typeof(RectTransform)).transform as RectTransform;
        _bar.SetParent(transform, false);
        _bar.anchorMin = new Vector2(0f, 0f);
        _bar.anchorMax = new Vector2(1f, 0f);
        _bar.pivot = new Vector2(0.5f, 0f);
        _bar.sizeDelta = new Vector2(0f, height);
        _bar.anchoredPosition = new Vector2(0f, HiddenY);

        var background = _bar.gameObject.AddComponent<Image>();
        background.color = backgroundColor;
        background.raycastTarget = false;

        _group = _bar.gameObject.AddComponent<CanvasGroup>();
        _group.alpha = 0f;
        _group.interactable = false;
        _group.blocksRaycasts = false;

        _row = new GameObject("Row", typeof(RectTransform)).transform as RectTransform;
        _row.SetParent(_bar, false);
        _row.anchorMin = _row.anchorMax = new Vector2(0.5f, 0.5f);
        _row.pivot = new Vector2(0.5f, 0.5f);
        var fitter = _row.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var row = _row.gameObject.AddComponent<HorizontalLayoutGroup>();
        row.childAlignment = TextAnchor.MiddleCenter;
        row.spacing = spacing;
        row.childControlWidth = true;
        row.childControlHeight = true;
        row.childForceExpandWidth = false;
        row.childForceExpandHeight = false;

        foreach (var hint in Hints)
        {
            var entry = new GameObject(hint.LabelKey, typeof(RectTransform)).transform as RectTransform;
            entry.SetParent(_row, false);
            var column = entry.gameObject.AddComponent<VerticalLayoutGroup>();
            column.childAlignment = TextAnchor.MiddleCenter;
            column.spacing = 4f;
            column.childControlWidth = true;
            column.childControlHeight = true;
            column.childForceExpandWidth = false;
            column.childForceExpandHeight = false;

            var iconRow = new GameObject("Icons", typeof(RectTransform)).transform as RectTransform;
            iconRow.SetParent(entry, false);
            iconRow.gameObject.AddComponent<RtlMirrorIgnore>();
            var rowSize = iconRow.gameObject.AddComponent<LayoutElement>();
            rowSize.minHeight = rowSize.preferredHeight = iconSize * WideIconScale;
            var iconLayout = iconRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            iconLayout.childAlignment = TextAnchor.MiddleCenter;
            iconLayout.spacing = 4f;
            iconLayout.childControlWidth = true;
            iconLayout.childControlHeight = true;
            iconLayout.childForceExpandWidth = false;
            iconLayout.childForceExpandHeight = false;

            var icons = new List<Image>();
            foreach (var _ in hint.Actions)
            {
                var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                icon.rectTransform.SetParent(iconRow, false);
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                var size = icon.gameObject.AddComponent<LayoutElement>();
                size.preferredWidth = iconSize;
                size.preferredHeight = iconSize;
                icons.Add(icon);
            }

            var label = new GameObject("Label", typeof(RectTransform)).AddComponent<RTLTMPro.RTLTextMeshPro>();
            label.rectTransform.SetParent(entry, false);
            if (font) label.font = font;
            label.fontSize = labelSize;
            label.color = labelColor;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.fontStyle = FontStyles.UpperCase;
            label.raycastTarget = false;

            _entries.Add((hint, icons, label));
        }
    }

    private float HiddenY => -height - 20f;

    private void Animate(bool show)
    {
        _shown = show;
        _tween.Stop();
        float duration = show ? showDuration : hideDuration;
        _tween = Sequence.Create()
            .Group(Tween.UIAnchoredPositionY(_bar, show ? bottomOffset : HiddenY, duration, show ? Ease.OutBack : Ease.InQuad))
            .Group(Tween.Alpha(_group, show ? 1f : 0f, duration));
    }

    private void RefreshIcons(ButtonPrompt.Device device)
    {
        _device = device;
        foreach (var (hint, icons, _) in _entries)
        {
            for (int i = 0; i < hint.Actions.Length; i++)
            {
                bool keyboard = device == ButtonPrompt.Device.Keyboard;
                string control = hint.Actions[i] switch
                {
                    "Navigate" => keyboard ? KeyboardMoveControl : MoveControl,
                    "Submit" when keyboard => "space",
                    _ => BoundControl(hint.Actions[i], device)
                };
                var sprite = ButtonPromptIcons.Get(control, device);
                icons[i].sprite = sprite;
                icons[i].enabled = sprite;
                float aspect = sprite ? Mathf.Clamp(sprite.rect.width / sprite.rect.height, 1f, 2f) : 1f;
                float scale = control is "space" or "tab" or KeyboardMoveControl ? WideIconScale : 1f;
                var size = icons[i].GetComponent<LayoutElement>();
                size.preferredWidth = iconSize * aspect * scale;
                size.preferredHeight = iconSize * scale;
            }
        }
    }

    private static string BoundControl(string actionName, ButtonPrompt.Device device)
    {
        var action = ControllerFocus.FindUiAction(actionName);
        if (action == null) return null;

        foreach (var control in action.controls)
        {
            bool match = device == ButtonPrompt.Device.Keyboard ? control.device is Keyboard : control.device is Gamepad;
            if (match) return control.name;
        }

        return null;
    }

    private void RefreshLabels()
    {
        foreach (var (hint, _, label) in _entries) label.text = L10n.Get(hint.LabelKey);
    }
}
