using System.Collections.Generic;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Mach3UIManager : MonoBehaviour
{

    [Header("Top Bar")]
    [SerializeField] private Transform objectivesUIParent;
    [SerializeField] private Transform loseConditionsUIParent;
    [SerializeField] private RectTransform topBar;
    [SerializeField] private RectTransform levelName;
    [SerializeField] private TextMeshProUGUI levelNameText;
    [SerializeField] private TweenSettings topbarTweenSettings;
    
    [Header("Level Complete Window")]
    [SerializeField] private Transform levelCompleteStatsParent;
    [SerializeField] private CanvasGroup levelCompleteWindow;
    [SerializeField] private TextMeshProUGUI levelCompleteTitle;
    [SerializeField] private Button levelButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private TweenSettings levelCompleteTweenSettings;
    
    
    [Header("References")]
    [SerializeField] private Match3GameManager match3Manager;
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
        match3Manager.LevelFailed += OnLevelFailed;
        
        quitButton.onClick.RemoveAllListeners();
        quitButton.onClick.AddListener(() =>
        {
            GameManager.Instance?.LoadMainMenuScene();
        });
    }



    private void OnDisable()
    {
        match3Manager.LevelStarted -= OnLevelStarted;
        match3Manager.LevelComplete -= OnLevelComplete;
        match3Manager.LevelFailed -= OnLevelFailed;
    }




    private void Update()
    {
        UpdateUIElements();
    }
    
    private void OnLevelComplete(Match3LevelData levelData)
    {
        if (levelData == null) return;
        
        UpdateLevelButton(true);
        UpdateLevelCompleteStats(levelData);
        levelCompleteTitle.text = $"{levelData.Level.LevelName} Complete!";
        
        AnimateTopBar(false);
        AnimateLevelCompleteWindow(true);
        

    }
    
    private void OnLevelFailed(Match3LevelData levelData)
    {
        if (levelData == null) return;
        
        
        UpdateLevelButton(false);
        UpdateLevelCompleteStats(levelData);
        levelCompleteTitle.text = $"{levelData.Level.LevelName} Failed!";
        
        AnimateTopBar(false);
        AnimateLevelCompleteWindow(true);
    }
    
    private void OnLevelStarted(Match3LevelData levelData)
    {
        if (levelData == null) return;
        
        levelNameText.text = levelData.Level.LevelName;
        SetupUIElements(levelData.CurrentObjectives, levelData.CurrentLoseConditions);
        AnimateTopBar(true);
        AnimateLevelCompleteWindow(false);
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
        if (_levelCompleteSequence.isAlive) return;
        if (!levelCompleteWindow || !_levelCompleteWindowRectTransform) return;
        
        var startSize = show ? 0: 1;
        var endSize = show ? 1 : 0;
        
        if (show) levelCompleteWindow.alpha =  1f;
        _levelCompleteWindowRectTransform.localScale = new Vector3(_levelCompleteWindowRectTransform.localScale.x, startSize);
        
        _levelCompleteSequence = Sequence.Create()
            .Group(Tween.ScaleY(_levelCompleteWindowRectTransform, endSize, levelCompleteTweenSettings))
            .ChainCallback(() => 
            { 
                levelCompleteWindow.alpha = show ? 1f : 0f;
                levelCompleteWindow.interactable = show;
                levelCompleteWindow.blocksRaycasts = show; 
            });

    }
    
    private void UpdateLevelCompleteStats(Match3LevelData levelData)
    {
        if (!levelCompleteStatsParent) return;

        foreach (Transform child in levelCompleteStatsParent)
        {
            Destroy(child.gameObject);
        }

        var matchedMadeElement = Instantiate(match3UIElementPrefab, levelCompleteStatsParent);
        matchedMadeElement.Setup(null, $"Matches Made: {levelData.MatchesMade}");
        matchedMadeElement.gameObject.name = "MatchesMade";
        
        var movesMadeElement = Instantiate(match3UIElementPrefab, levelCompleteStatsParent);
        movesMadeElement.Setup(null, $"Moves Made: {levelData.MovesMade}");
        movesMadeElement.gameObject.name = "MovesMade";
    }

    private void UpdateLevelButton(bool won)
    {
        if (!levelButton) return;
        
        levelButton.onClick.RemoveAllListeners();
        var levelButtonText = levelButton.GetComponentInChildren<TextMeshProUGUI>();
        if (won)
        {
            levelButtonText.text = "Next Level";
            
            levelButton.onClick.AddListener(() =>
            {
                AnimateLevelCompleteWindow(false);
                _levelCompleteSequence.InsertCallback(levelCompleteTweenSettings.duration * 0.5f, () =>
                {
                    match3Manager.SetNextLevel();
                });
            });
        }
        else
        {
            levelButtonText.text = "Try Again";
            
            levelButton.onClick.AddListener(() =>
            {
                AnimateLevelCompleteWindow(false);
                _levelCompleteSequence.InsertCallback(levelCompleteTweenSettings.duration * 0.5f, () =>
                {
                    match3Manager.RestartLevel();
                });
            });
        }
    }
}

