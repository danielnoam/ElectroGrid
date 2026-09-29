using System;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Credits panel on the main menu. Built from the settings window and animates the same way, so the menu's
/// windows open and close alike.
/// </summary>
public class CreditsWindowUI : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private TweenSettings windowTweenSettings;

    [Header("References")]
    [SerializeField] private Button backButton;
    [SerializeField] private RectTransform windowRectTransform;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Image backgroundImage;

    private CanvasGroup _canvasGroup;
    private RectTransform _backButtonRectTransform;
    private Vector2 _defaultSize;
    private Sequence _toggleSequence;
    private float _backButtonDefaultYPosition;
    private float _backgroundStartAlpha;
    private Action _onClosed;

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();

        if (backButton)
        {
            _backButtonRectTransform = backButton.GetComponent<RectTransform>();
            _backButtonDefaultYPosition = _backButtonRectTransform.anchoredPosition.y;
            _backButtonRectTransform.anchoredPosition = new Vector2(_backButtonRectTransform.anchoredPosition.x, -windowRectTransform.sizeDelta.y);
        }

        if (windowRectTransform)
        {
            _defaultSize = windowRectTransform.sizeDelta;
            windowRectTransform.sizeDelta = new Vector2(_defaultSize.x, 0f);
        }

        if (titleText) titleText.alpha = 0f;

        if (backgroundImage)
        {
            _backgroundStartAlpha = backgroundImage.color.a;
            backgroundImage.color = new Color(backgroundImage.color.r, backgroundImage.color.g, backgroundImage.color.b, 0f);
        }

        if (_canvasGroup)
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }
    }

    public void Initialize()
    {
        if (backButton)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(Close);
        }

        if (backgroundImage)
        {
            var backgroundRelay = PointerClickRelay.On(backgroundImage);
            backgroundRelay.Clicked -= OnBackgroundClicked;
            backgroundRelay.Clicked += OnBackgroundClicked;
        }
    }

    public void Show(Action onClosed = null)
    {
        _onClosed = onClosed;
        Toggle(true);
    }

    private void OnBackgroundClicked(PointerEventData eventData)
    {
        var title = titleText ? titleText.rectTransform : null;
        if (PointerClickRelay.IsOutside(eventData, windowRectTransform, title, _backButtonRectTransform)) Close();
    }

    private void Close()
    {
        CameraManager.Instance?.ShakeCamera(0.1f);
        Toggle(false);

        var onClosed = _onClosed;
        _onClosed = null;
        onClosed?.Invoke();
    }

    public void Toggle(bool show)
    {
        if (!_canvasGroup || !windowRectTransform) return;

        _toggleSequence.Stop();

        var startSize = show ? new Vector2(windowRectTransform.sizeDelta.x, 0f) : _defaultSize;
        var endSize = show ? _defaultSize : new Vector2(windowRectTransform.sizeDelta.x, 0f);
        var startYPosition = show ? -_defaultSize.y : _backButtonDefaultYPosition;
        var endYPosition = show ? _backButtonDefaultYPosition : -_defaultSize.y;
        var titleStartAlpha = show ? 0f : 1f;
        var titleEndAlpha = show ? 1f : 0f;
        var backgroundStartAlpha = show ? 0f : _backgroundStartAlpha;
        var backgroundEndAlpha = show ? _backgroundStartAlpha : 0f;

        windowRectTransform.sizeDelta = startSize;
        if (_backButtonRectTransform) _backButtonRectTransform.anchoredPosition = new Vector2(_backButtonRectTransform.anchoredPosition.x, startYPosition);
        if (titleText) titleText.alpha = titleStartAlpha;
        if (backgroundImage) backgroundImage.color = new Color(backgroundImage.color.r, backgroundImage.color.g, backgroundImage.color.b, backgroundStartAlpha);

        if (show) _canvasGroup.alpha = 1f;

        _toggleSequence = Sequence.Create(useUnscaledTime: true)
            .Group(Tween.UISizeDelta(windowRectTransform, endSize, windowTweenSettings));

        if (titleText) _toggleSequence.Group(Tween.Alpha(titleText, titleEndAlpha, windowTweenSettings.duration * 0.8f));
        if (backgroundImage) _toggleSequence.Group(Tween.Alpha(backgroundImage, backgroundEndAlpha, windowTweenSettings.duration * 0.8f));
        if (_backButtonRectTransform) _toggleSequence.Group(Tween.UIAnchoredPositionY(_backButtonRectTransform, endYPosition, windowTweenSettings));

        _toggleSequence.ChainCallback(() =>
        {
            if (!show) _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = show;
            _canvasGroup.blocksRaycasts = show;
        });
    }
}
