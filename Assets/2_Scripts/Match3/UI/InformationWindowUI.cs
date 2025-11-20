using System;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InformationWindowUI : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private TweenSettings informationWindowTweenSettings;
    
    [Header("References")]
    [SerializeField] private Button backButton;
    [SerializeField] private TopBarUI topBarUI;
    [SerializeField] private BottomBarUI bottomBarUI;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Transform tutorialElementsParent;
    [SerializeField] private Match3TutorialUIElement tutorialElementPrefab;
    
    private CanvasGroup _canvasGroup;
    private RectTransform _rectTransform;
    private RectTransform _backButtonRectTransform;
    private Vector2 _defaultSize;
    private Sequence _toggleSequence;
    private float _backButtonDefaultYPosition;


    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        _rectTransform = GetComponent<RectTransform>();
        _backButtonRectTransform = backButton.GetComponent<RectTransform>();
        _backButtonDefaultYPosition = _backButtonRectTransform.anchoredPosition.y;
        _backButtonRectTransform.anchoredPosition = new Vector2(_backButtonRectTransform.anchoredPosition.x, -_rectTransform.sizeDelta.y);
        _defaultSize = _rectTransform.sizeDelta;
        _rectTransform.sizeDelta = new Vector2(_defaultSize.x, 0);
        titleText.alpha = 0f;
        _canvasGroup.alpha = 0f;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;
    }

    public void Initialize()
    {
        
        SetupButtons();
        PopulateTutorials();
    }

    private void SetupButtons()
    {
        backButton.onClick.RemoveAllListeners();
        backButton.onClick.AddListener(() =>
        {
            CameraManager.Instance.ShakeCamera(0.1f);
            Toggle(false);
            topBarUI.Toggle(true);
            bottomBarUI.Toggle(true);
        });
    }

    private void PopulateTutorials()
    {
        if (!tutorialElementsParent || !tutorialElementPrefab) return;
        
        foreach (Transform child in tutorialElementsParent)
        {
            Destroy(child.gameObject);
        }
        
        var tutorials = GameManager.Instance.Match3GeneralTutorials;
        foreach (var tutorial in tutorials)
        {
            var tutorialElement = Instantiate(tutorialElementPrefab, tutorialElementsParent);
            tutorialElement.Setup(tutorial.TutorialSprite, tutorial.TutorialTitle, tutorial.TutorialText);
        }
    }

    public void Toggle(bool show)
    {
        if (!_canvasGroup || !_rectTransform) return;

        _toggleSequence.Stop();
        
        var startSize = show ? new Vector2(_rectTransform.sizeDelta.x, 0f) : _defaultSize;
        var endSize = show ? _defaultSize : new Vector2(_rectTransform.sizeDelta.x, 0f);
        var startYPosition = show ? -_defaultSize.y : _backButtonDefaultYPosition;
        var endYPosition = show ? _backButtonDefaultYPosition : -_defaultSize.y;
        var startAlpha = show ? 0f : 1f;
        var endAlpha = show ? 1f : 0f;
        var endTimeScale = show ? 0f : 1f ;
        
        _rectTransform.sizeDelta = startSize;
        _backButtonRectTransform.anchoredPosition = new Vector2(_backButtonRectTransform.anchoredPosition.x, startYPosition);
        titleText.alpha = startAlpha;
        
        if (show) _canvasGroup.alpha = 1f;
        if (!show) GameManager.Instance.TogglePause(false, false);
        
        
        _toggleSequence = Sequence.Create(useUnscaledTime: true)
            .Group(Tween.UISizeDelta(_rectTransform, endSize, informationWindowTweenSettings))
            .Group(Tween.Alpha(titleText, endAlpha, informationWindowTweenSettings.duration * 0.8f))
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