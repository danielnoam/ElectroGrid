using System;
using System.Collections.Generic;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InformationWindowUI : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private TweenSettings informationWindowTweenSettings;
    [Tooltip("Title and button label used when a single card pops up the first time its mechanic appears")]
    [SerializeField] private string introductionTitle = "New!";
    [SerializeField] private string introductionButtonLabel = "Got it";
    [Tooltip("Backdrop alpha for the introduction, lighter than the full list so the board stays visible behind it")]
    [SerializeField, Range(0f, 1f)] private float introductionBackgroundAlpha = 0.5f;

    [Header("References")]
    [SerializeField] private Button backButton;
    [SerializeField] private TopBarUI topBarUI;
    [SerializeField] private BottomBarUI bottomBarUI;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private RectTransform windowRectTransform;
    [SerializeField] private Transform tutorialElementsParent;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Match3TutorialUIElement tutorialElementPrefab;

    private CanvasGroup _canvasGroup;
    private RectTransform _backButtonRectTransform;
    private Vector2 _defaultSize;
    private Vector2 _openSize;
    private Sequence _toggleSequence;
    private float _backButtonDefaultYPosition;
    private float _backButtonOpenYPosition;
    private float _backgroundStartAlpha;
    private float _backgroundOpenAlpha;
    private Action _onClosed;
    private TMP_Text _backButtonLabel;
    private string _defaultTitle;
    private string _defaultButtonLabel;


    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        _backButtonRectTransform = backButton.GetComponent<RectTransform>();
        _backButtonLabel = backButton.GetComponentInChildren<TMP_Text>(true);
        _defaultButtonLabel = _backButtonLabel ? _backButtonLabel.text : null;
        _defaultTitle = titleText.text;
        _backButtonDefaultYPosition = _backButtonRectTransform.anchoredPosition.y;
        _backButtonOpenYPosition = _backButtonDefaultYPosition;
        _backButtonRectTransform.anchoredPosition = new Vector2(_backButtonRectTransform.anchoredPosition.x, -windowRectTransform.sizeDelta.y);
        _defaultSize = windowRectTransform.sizeDelta;
        _openSize = _defaultSize;
        windowRectTransform.sizeDelta = new Vector2(_defaultSize.x, 0);
        titleText.alpha = 0f;
        _backgroundStartAlpha = backgroundImage.color.a;
        _backgroundOpenAlpha = _backgroundStartAlpha;
        backgroundImage.color = new Color(backgroundImage.color.r, backgroundImage.color.g, backgroundImage.color.b, 0f);
        _canvasGroup.alpha = 0f;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;
    }

    public void Initialize()
    {
        SetupButtons();
        PopulateAllTutorials();
    }

    /// <summary>Opens the window showing every tutorial, which is what the info button does.</summary>
    public void ShowAllTutorials()
    {
        _onClosed = null;
        SetLabels(_defaultTitle, _defaultButtonLabel);
        _backgroundOpenAlpha = _backgroundStartAlpha;
        PopulateAllTutorials();
        Toggle(true);
    }

    /// <summary>Opens the window as a one-off introduction to a single card, the first time its mechanic appears.</summary>
    public void ShowIntroduction(SOMatch3Tutorial tutorial, Action onClosed = null)
    {
        _onClosed = onClosed;
        SetLabels(introductionTitle, introductionButtonLabel);
        _backgroundOpenAlpha = introductionBackgroundAlpha;
        PopulateTutorials(new[] { tutorial });
        Toggle(true);
    }

    private void SetLabels(string title, string buttonLabel)
    {
        if (!string.IsNullOrEmpty(title)) titleText.text = title;
        if (_backButtonLabel && !string.IsNullOrEmpty(buttonLabel)) _backButtonLabel.text = buttonLabel;
    }

    private void SetupButtons()
    {
        backButton.onClick.RemoveAllListeners();
        backButton.onClick.AddListener(Close);

        var backgroundRelay = PointerClickRelay.On(backgroundImage);
        backgroundRelay.Clicked -= OnBackgroundClicked;
        backgroundRelay.Clicked += OnBackgroundClicked;
    }

    private void OnBackgroundClicked(PointerEventData eventData)
    {
        if (PointerClickRelay.IsOutside(eventData, windowRectTransform, titleText.rectTransform, _backButtonRectTransform)) Close();
    }

    private void Close()
    {
        CameraManager.Instance?.ShakeCamera(0.1f);
        Toggle(false);
        topBarUI?.Toggle(true);
        bottomBarUI?.Toggle(true);

        var onClosed = _onClosed;
        _onClosed = null;
        onClosed?.Invoke();
    }

    private void PopulateAllTutorials()
    {
        PopulateTutorials(GameManager.Instance ? GameManager.Instance.Match3GeneralTutorials : null);
    }

    private void PopulateTutorials(IReadOnlyList<SOMatch3Tutorial> tutorials)
    {
        if (!tutorialElementsParent || !tutorialElementPrefab) return;

        foreach (Transform child in tutorialElementsParent)
        {
            // Deactivated first, Destroy only happens at the end of the frame and the layout below would still count it
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }

        if (tutorials != null)
        {
            foreach (var tutorial in tutorials)
            {
                if (!tutorial) continue;

                var tutorialElement = Instantiate(tutorialElementPrefab, tutorialElementsParent);
                tutorialElement.Setup(tutorial.TutorialSprite, tutorial.TutorialTitle, tutorial.TutorialText);
            }
        }

        FitWindowToContent();

        // Opening with a different set of cards should start at the top, not wherever the last visit was scrolled to
        var scrollRect = tutorialElementsParent.GetComponentInParent<ScrollRect>(true);
        if (scrollRect) scrollRect.verticalNormalizedPosition = 1f;
    }

    /// <summary>
    /// Shrinks the window to its cards, never past its authored size, so a single card doesn't sit in a mostly
    /// empty window. The back button keeps the same gap below the window.
    /// </summary>
    private void FitWindowToContent()
    {
        if (!(tutorialElementsParent is RectTransform content)) return;

        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        float contentHeight = LayoutUtility.GetPreferredHeight(content);

        float height = contentHeight > 0f ? Mathf.Min(_defaultSize.y, contentHeight) : _defaultSize.y;
        _openSize = new Vector2(_defaultSize.x, height);
        _backButtonOpenYPosition = _backButtonDefaultYPosition + (_defaultSize.y - height) * 0.5f;
    }

    public void Toggle(bool show)
    {
        if (!_canvasGroup || !windowRectTransform) return;

        _toggleSequence.Stop();

        var startSize = show ? new Vector2(windowRectTransform.sizeDelta.x, 0f) : _openSize;
        var endSize = show ? _openSize : new Vector2(windowRectTransform.sizeDelta.x, 0f);
        var startYPosition = show ? -_defaultSize.y : _backButtonOpenYPosition;
        var endYPosition = show ? _backButtonOpenYPosition : -_defaultSize.y;
        var titleStartAlpha = show ? 0f : 1f;
        var backgroundStartAlpha = show ? 0f : _backgroundOpenAlpha;
        var titleEndAlpha = show ? 1f : 0f;
        var backgroundEndAlpha = show ? _backgroundOpenAlpha : 0f;
        var endTimeScale = show ? 0f : 1f ;

        windowRectTransform.sizeDelta = startSize;
        _backButtonRectTransform.anchoredPosition = new Vector2(_backButtonRectTransform.anchoredPosition.x, startYPosition);
        titleText.alpha = titleStartAlpha;
        backgroundImage.color = new Color(backgroundImage.color.r, backgroundImage.color.g, backgroundImage.color.b, backgroundStartAlpha);

        if (show) _canvasGroup.alpha = 1f;
        if (!show) GameManager.Instance.TogglePause(false, false);


        _toggleSequence = Sequence.Create(useUnscaledTime: true)
            .Group(Tween.UISizeDelta(windowRectTransform, endSize, informationWindowTweenSettings))
            .Group(Tween.Alpha(titleText, titleEndAlpha, informationWindowTweenSettings.duration * 0.8f))
            .Group(Tween.Alpha(backgroundImage, backgroundEndAlpha, informationWindowTweenSettings.duration * 0.8f))
            .Group(Tween.UIAnchoredPositionY(_backButtonRectTransform, endYPosition, informationWindowTweenSettings))
            .Group(Tween.GlobalTimeScale(endTimeScale, informationWindowTweenSettings))
            .ChainCallback(() =>
            {
                if (show) GameManager.Instance.TogglePause(true, false);
                if (!show) _canvasGroup.alpha = 0f;
                _canvasGroup.interactable = show;
                _canvasGroup.blocksRaycasts = show;
            });
    }
}
