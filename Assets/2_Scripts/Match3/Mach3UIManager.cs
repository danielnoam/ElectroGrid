using System.Collections.Generic;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Mach3UIManager : MonoBehaviour
{

    [Header("Settings")]
    [SerializeField] private TweenSettings topbarTweenSettings;
    [SerializeField] private TweenSettings levelCompleteTweenSettings;
    
    
    
    [Header("References")]
    [SerializeField] private Match3GameManager match3Manager;
    [SerializeField] private RectTransform topBar;
    [SerializeField] private RectTransform levelName;
    [SerializeField] private TextMeshProUGUI levelNameText;
    [SerializeField] private CanvasGroup levelCompleteWindow;
    [SerializeField] private Button levelButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Transform objectivesUIParent;
    [SerializeField] private Transform loseConditionsUIParent;
    [SerializeField] private Match3UIElement match3UIElementPrefab;

    
    private readonly Dictionary<Match3Objective, Match3UIElement> _currentObjectives = new Dictionary<Match3Objective, Match3UIElement>();
    private readonly Dictionary<Match3LoseCondition, Match3UIElement> _currentLoseConditions = new Dictionary<Match3LoseCondition, Match3UIElement>();
    
    private RectTransform _levelCompleteWindowRectTransform;
    private float _levelNameDefaultPositionY;
    private Vector2 _topBarDefaultSize;
    private Sequence _topBarSequence;
    private Sequence _levelCompleteSequence;
    

    private void Awake()
    {
        _levelCompleteWindowRectTransform = levelCompleteWindow.GetComponent<RectTransform>();
        levelCompleteWindow.alpha = 0f;
        levelCompleteWindow.interactable = false;
        levelCompleteWindow.blocksRaycasts = false; 
        
        _topBarDefaultSize = topBar.sizeDelta;
        topBar.sizeDelta = new Vector2(_topBarDefaultSize.x, 0f);
        
        _levelNameDefaultPositionY = levelName.anchoredPosition.y;
        levelName.anchoredPosition = new Vector2(levelName.anchoredPosition.x, 0f);
    }

    private void OnEnable()
    {
        match3Manager.LevelStarted += OnLevelStarted;
        match3Manager.LevelComplete += OnLevelComplete;
        
        levelButton.onClick.RemoveAllListeners();
        levelButton.onClick.AddListener(() =>
        {
            match3Manager.SetNextLevel();
        });
        
        quitButton.onClick.AddListener(() =>
        {
            GameManager.Instance?.LoadMainMenuScene();
        });
    }



    private void OnDisable()
    {
        match3Manager.LevelStarted -= OnLevelStarted;
        match3Manager.LevelComplete -= OnLevelComplete;
    }

    
    private void Update()
    {
        UpdateUIElements();
    }
    
    private void OnLevelComplete(bool won)
    {
        AnimateTopBar(false);
        AnimateLevelCompleteWindow(true);
    }
    
    private void OnLevelStarted(SOMatch3Level level, List<Match3Objective> objectives, List<Match3LoseCondition> loseConditions)
    {
        levelNameText.text = level.LevelName;
        SetupUIElements(objectives, loseConditions);
        AnimateTopBar(true);
        AnimateLevelCompleteWindow(false);
    }
    

    private void UpdateUIElements()
    {
        foreach (var objectivePair in _currentObjectives)
        {
            objectivePair.Value.UpdateProgress(objectivePair.Key.GetProgressText(!objectivePair.Key.ObjectiveSprite));
        }

        foreach (var loseConditionPair in _currentLoseConditions)
        {
            loseConditionPair.Value.UpdateProgress(loseConditionPair.Key.GetProgressText(!loseConditionPair.Key.ConditionSprite));
        }
    }

    private void SetupUIElements(List<Match3Objective> objectives, List<Match3LoseCondition> loseConditions)
    {
        if (!objectivesUIParent || !loseConditionsUIParent) return;
        
        ClearUIElements();
        
        foreach (var objective in objectives)
        {
            var uiElement = Instantiate(match3UIElementPrefab, objectivesUIParent);
            uiElement.Setup(objective.ObjectiveSprite, objective.GetProgressText(!objective.ObjectiveSprite));
            uiElement.gameObject.name = objective.GetObjectiveName();
            _currentObjectives.Add(objective, uiElement);
        }

        foreach (var loseCondition in loseConditions)
        {
            var uiElement = Instantiate(match3UIElementPrefab, loseConditionsUIParent);
            uiElement.Setup(loseCondition.ConditionSprite, loseCondition.GetProgressText(!loseCondition.ConditionSprite));
            uiElement.gameObject.name = loseCondition.GetConditionName();
            _currentLoseConditions.Add(loseCondition, uiElement);
        }
    }
    
    private void ClearUIElements()
    {
        foreach (Transform child in objectivesUIParent)
        {
            Destroy(child.gameObject);
        }

        foreach (Transform child in loseConditionsUIParent)
        {
            Destroy(child.gameObject);
        }
        
        _currentObjectives.Clear();
        _currentLoseConditions.Clear();
    }
    
    private void AnimateTopBar(bool show)
    {
        if (!topBar) return;
        
        _topBarSequence.Stop();
        
        var barEndSize = show ? _topBarDefaultSize : new Vector2(_topBarDefaultSize.x, 0f);
        var nameEndPosition = show ? _levelNameDefaultPositionY : 0f;
        
        _topBarSequence = Sequence.Create();

        if (show)
        {
            _topBarSequence.Group(Tween.UISizeDelta(topBar, barEndSize, topbarTweenSettings));
            _topBarSequence.Group(Tween.UIAnchoredPositionY(levelName, nameEndPosition, startDelay: topbarTweenSettings.duration/2, duration: topbarTweenSettings.duration * 0.8f, ease: topbarTweenSettings.ease));
        }
        else
        {
            _topBarSequence.Group(Tween.UIAnchoredPositionY(levelName, nameEndPosition, topbarTweenSettings));
            _topBarSequence.Group(Tween.UISizeDelta(topBar, barEndSize, startDelay: topbarTweenSettings.duration/2, duration: topbarTweenSettings.duration * 0.8f, ease: topbarTweenSettings.ease));
        }
    }
    
    
    private void AnimateLevelCompleteWindow(bool show)
    {
        if (!levelCompleteWindow || !_levelCompleteWindowRectTransform) return;
        
        _levelCompleteSequence.Stop();
        
        var startSize = show ? 0: 1;
        var endSize = show ? 1 : 0;
        
        if (show) levelCompleteWindow.alpha =  1f;
        _levelCompleteWindowRectTransform.localScale = new Vector3(_levelCompleteWindowRectTransform.localScale.x, startSize);
        
        _levelCompleteSequence = Sequence.Create()
            .Group(Tween.ScaleY(_levelCompleteWindowRectTransform, endSize, levelCompleteTweenSettings))
            .OnComplete(() => {
                levelCompleteWindow.alpha = show ? 1f : 0f;
            levelCompleteWindow.interactable = show;
            levelCompleteWindow.blocksRaycasts = show; 
        });
    }
}

