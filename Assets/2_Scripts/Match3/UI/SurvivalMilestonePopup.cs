using System.Collections.Generic;
using DNExtensions.Systems.AudioLibrary;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A banner that drops in when a Survival run reaches a milestone, showing what joins the board from now on: a new
/// piece, viruses, fuses, Plugs or Stars. Milestones reached close together queue up and show one after another.
/// </summary>
public class SurvivalMilestonePopup : MonoBehaviour
{
    [Header("Layout")]
    [SerializeField] private Vector2 size = new Vector2(760f, 190f);
    [Tooltip("Distance from the top of the screen once shown")]
    [SerializeField] private float topOffset = 730f;
    [SerializeField, Min(16f)] private float iconSize = 120f;
    [SerializeField, Min(8f)] private float titleSize = 40f;
    [SerializeField, Min(8f)] private float nameSize = 64f;

    [Header("Look")]
    [SerializeField] private TMP_FontAsset font;
    [SerializeField] private Color backgroundColor = new Color(0.07f, 0.05f, 0.16f, 0.95f);
    [SerializeField] private Color accentColor = new Color(1f, 0.031f, 0.38f, 1f);
    [SerializeField] private Color textColor = Color.white;

    [Header("Timing")]
    [SerializeField, Min(0f)] private float inDuration = 0.4f;
    [SerializeField, Min(0f)] private float holdDuration = 1.8f;
    [SerializeField, Min(0f)] private float outDuration = 0.3f;

    [Header("Feedback")]
    [SerializeField, AudioLibraryID] private string sfx = "PlusDestroy";

    [Header("Icons")]
    [SerializeField] private Sprite virusIcon;
    [SerializeField] private Sprite fuseIcon;
    [SerializeField] private Sprite plugIcon;
    [SerializeField] private Sprite starIcon;

    private readonly Queue<SurvivalMilestone> _queue = new Queue<SurvivalMilestone>();
    private RectTransform _banner;
    private CanvasGroup _group;
    private Image _icon;
    private TextMeshProUGUI _title;
    private TextMeshProUGUI _name;
    private Sequence _sequence;
    private Match3SurvivalRun _run;
    private bool _showing;

    private void Update()
    {
        var game = Match3GameManager.Instance;
        var run = game && game.CurrentLevelData != null ? game.CurrentLevelData.Survival : null;
        if (run != _run) Watch(run);

        if (!_showing && _queue.Count > 0) Show(_queue.Dequeue());
    }

    private void OnDestroy()
    {
        Watch(null);
        _sequence.Stop();
    }

    private void Watch(Match3SurvivalRun run)
    {
        if (_run != null) _run.MilestoneReached -= OnMilestoneReached;
        _run = run;
        if (_run != null) _run.MilestoneReached += OnMilestoneReached;

        _queue.Clear();
        _sequence.Stop();
        _showing = false;
        if (_banner) _banner.gameObject.SetActive(false);
    }

    private void OnMilestoneReached(SurvivalMilestone milestone)
    {
        if (milestone != null) _queue.Enqueue(milestone);
    }

    /// <summary>Shows a milestone banner right away, for testing from the debug window.</summary>
    public void Preview(SurvivalMilestone milestone)
    {
        if (milestone != null) _queue.Enqueue(milestone);
    }

    private void Show(SurvivalMilestone milestone)
    {
        if (!_banner) Build();

        bool piece = milestone.unlock == SurvivalUnlock.Piece && milestone.piece;
        _title.text = L10n.Get(piece ? "survival.unlock.piece" : "survival.unlock");
        _name.text = piece ? milestone.piece.DisplayName : UnlockName(milestone.unlock);
        _icon.sprite = piece ? milestone.piece.Sprite : UnlockIcon(milestone.unlock);
        _icon.color = piece ? milestone.piece.Color : Color.white;
        _icon.enabled = _icon.sprite;

        _showing = true;
        _banner.gameObject.SetActive(true);
        _banner.localScale = Vector3.one * 0.6f;
        _group.alpha = 0f;
        _banner.anchoredPosition = new Vector2(0f, -topOffset + 80f);

        AudioLibrary.Play(sfx);
        MobileHaptics.Play(Haptic.ComboFull);
        CameraManager.Instance?.ShakeCamera(0.4f);

        _sequence.Stop();
        _sequence = Sequence.Create()
            .Group(Tween.Alpha(_group, 1f, inDuration * 0.6f))
            .Group(Tween.Scale(_banner, Vector3.one, inDuration, Ease.OutBack))
            .Group(Tween.UIAnchoredPositionY(_banner, -topOffset, inDuration, Ease.OutBack))
            .ChainDelay(holdDuration)
            .Chain(Tween.Alpha(_group, 0f, outDuration))
            .Group(Tween.UIAnchoredPositionY(_banner, -topOffset + 60f, outDuration, Ease.InQuad))
            .ChainCallback(() =>
            {
                _banner.gameObject.SetActive(false);
                _showing = false;
            });
    }

    private static string UnlockName(SurvivalUnlock unlock) => unlock switch
    {
        SurvivalUnlock.Viruses => L10n.GetContent("tutorial.virus.title"),
        SurvivalUnlock.Fuses => L10n.GetContent("tutorial.fuse.title"),
        SurvivalUnlock.Plugs => L10n.GetContent("tutorial.squarestars.title"),
        SurvivalUnlock.Stars => L10n.GetContent("tutorial.doublestars.title"),
        _ => string.Empty
    };

    private Sprite UnlockIcon(SurvivalUnlock unlock) => unlock switch
    {
        SurvivalUnlock.Viruses => virusIcon,
        SurvivalUnlock.Fuses => fuseIcon,
        SurvivalUnlock.Plugs => plugIcon,
        SurvivalUnlock.Stars => starIcon,
        _ => null
    };

    private void Build()
    {
        _banner = new GameObject("Milestone Banner", typeof(RectTransform)).transform as RectTransform;
        _banner.SetParent(transform, false);
        _banner.anchorMin = _banner.anchorMax = new Vector2(0.5f, 1f);
        _banner.pivot = new Vector2(0.5f, 0.5f);
        _banner.sizeDelta = size;

        var background = _banner.gameObject.AddComponent<Image>();
        background.color = backgroundColor;
        background.raycastTarget = false;
        var outline = _banner.gameObject.AddComponent<Outline>();
        outline.effectColor = accentColor;
        outline.effectDistance = new Vector2(4f, -4f);

        _group = _banner.gameObject.AddComponent<CanvasGroup>();
        _group.interactable = false;
        _group.blocksRaycasts = false;

        var row = _banner.gameObject.AddComponent<HorizontalLayoutGroup>();
        row.childAlignment = TextAnchor.MiddleCenter;
        row.spacing = 30f;
        row.padding = new RectOffset(30, 30, 20, 20);
        row.childControlWidth = true;
        row.childControlHeight = true;
        row.childForceExpandWidth = false;
        row.childForceExpandHeight = false;

        _icon = new GameObject("Icon", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        _icon.rectTransform.SetParent(_banner, false);
        _icon.preserveAspect = true;
        _icon.raycastTarget = false;
        var iconLayout = _icon.gameObject.AddComponent<LayoutElement>();
        iconLayout.preferredWidth = iconLayout.preferredHeight = iconSize;

        var texts = new GameObject("Texts", typeof(RectTransform)).transform as RectTransform;
        texts.SetParent(_banner, false);
        var column = texts.gameObject.AddComponent<VerticalLayoutGroup>();
        column.childAlignment = TextAnchor.MiddleCenter;
        column.childControlWidth = true;
        column.childControlHeight = true;
        column.childForceExpandWidth = false;
        column.childForceExpandHeight = false;
        texts.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        _title = CreateText(texts, "Title", titleSize, accentColor);
        _name = CreateText(texts, "Name", nameSize, textColor);
        _banner.gameObject.SetActive(false);
    }

    private TextMeshProUGUI CreateText(Transform parent, string name, float fontSize, Color color)
    {
        var text = new GameObject(name, typeof(RectTransform)).AddComponent<RTLTMPro.RTLTextMeshPro>();
        text.rectTransform.SetParent(parent, false);
        if (font) text.font = font;
        text.fontSize = fontSize;
        text.enableAutoSizing = true;
        text.fontSizeMin = fontSize * 0.5f;
        text.fontSizeMax = fontSize;
        text.color = color;
        text.fontStyle = FontStyles.UpperCase;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        return text;
    }
}
