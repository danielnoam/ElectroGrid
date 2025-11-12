using System.Collections.Generic;
using DNExtensions;
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
    [SerializeField] private SOAudioEvent levelCompleteWinSfx;
    [SerializeField] private SOAudioEvent levelCompleteFailSfx;
    [SerializeField] private TweenSettings levelCompleteTweenSettings;
    
    [Header("Bottom Bar")]
    [SerializeField] private RectTransform bottomBar;
    [SerializeField] private Button bottomBarQuitButton;
    [SerializeField] private TweenSettings bottomBarTweenSettings;
    
    [Header("References")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private Match3GameManager match3Manager;
    [SerializeField] private Match3UIElement match3UIElementPrefab;

    
    private readonly Dictionary<Match3Objective, Match3UIElement> _currentObjectives = new Dictionary<Match3Objective, Match3UIElement>();
    private readonly Dictionary<Match3LoseCondition, Match3UIElement> _currentLoseConditions = new Dictionary<Match3LoseCondition, Match3UIElement>();
    
    private RectTransform _levelCompleteWindowRectTransform;
    private float _levelNameDefaultPositionY;
    private float _bottomBarDefaultYPosition;
    private Vector2 _levelNameDefaultSize;
    private Vector2 _topBarDefaultSize;
    private Vector2 _levelCompleteWindowDefaultSize;
    private Sequence _topBarSequence;
    private Sequence _bottomBarSequence;
    private Sequence _levelCompleteSequence;
    

    private void Awake()
    {
        _levelCompleteWindowRectTransform = levelCompleteWindow.GetComponent<RectTransform>();
        _levelCompleteWindowDefaultSize = _levelCompleteWindowRectTransform.sizeDelta;
        _levelCompleteWindowRectTransform.sizeDelta = new Vector2(_levelCompleteWindowDefaultSize.x, 0);
        levelCompleteWindow.alpha = 0f;
        levelCompleteWindow.interactable = false;
        levelCompleteWindow.blocksRaycasts = false; 
        
        _topBarDefaultSize = topBar.sizeDelta;
        topBar.sizeDelta = new Vector2(_topBarDefaultSize.x, 0f);
        
        _bottomBarDefaultYPosition = bottomBar.anchoredPosition.y;
        bottomBar.anchoredPosition = new Vector2(bottomBar.anchoredPosition.x, -bottomBar.sizeDelta.y);
        
        _levelNameDefaultPositionY = levelName.anchoredPosition.y;
        _levelNameDefaultSize = levelName.sizeDelta;
        levelName.sizeDelta = Vector2.zero;
        levelName.anchoredPosition = new Vector2(levelName.anchoredPosition.x,0f);
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
        
        bottomBarQuitButton.onClick.RemoveAllListeners();
        bottomBarQuitButton.onClick.AddListener(() =>
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
        
        levelCompleteWinSfx.Play(audioSource);
        
        UpdateLevelButton(true);
        UpdateLevelCompleteStats(levelData);
        levelCompleteTitle.text = $"{levelData.Level.LevelName} Complete!";
        
        AnimateTopBar(false);
        AnimateBottomBar(false);
        AnimateLevelCompleteWindow(true);
        

    }
    
    private void OnLevelFailed(Match3LevelData levelData)
    {
        if (levelData == null) return;
        
        levelCompleteFailSfx.Play(audioSource);
        
        UpdateLevelButton(false);
        UpdateLevelCompleteStats(levelData);
        levelCompleteTitle.text = $"{levelData.Level.LevelName} Failed!";
        
        AnimateTopBar(false);
        AnimateBottomBar(false);
        AnimateLevelCompleteWindow(true);
    }
    
    private void OnLevelStarted(Match3LevelData levelData)
    {
        if (levelData == null) return;
        
        levelNameText.text = levelData.Level.LevelName;
        SetupUIElements(levelData.CurrentObjectives, levelData.CurrentLoseConditions);
        AnimateTopBar(true);
        AnimateBottomBar(true);
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
        var nameEndSize = show ? _levelNameDefaultSize : Vector2.zero;
        
        _topBarSequence = Sequence.Create()
            .Group(Tween.UISizeDelta(topBar, barEndSize, topbarTweenSettings))
            .Group(Tween.UISizeDelta(levelName, nameEndSize, startDelay: topbarTweenSettings.duration/2, duration: topbarTweenSettings.duration * 0.8f, ease: topbarTweenSettings.ease))
            .Group(Tween.UIAnchoredPositionY(levelName, nameEndPosition, startDelay: topbarTweenSettings.duration/2, duration: topbarTweenSettings.duration * 0.8f, ease: topbarTweenSettings.ease));

    }
    
    private void AnimateBottomBar(bool show)
    {
        if (!bottomBar) return;

        _bottomBarSequence.Stop();
        var endYPosition = show ? _bottomBarDefaultYPosition : -bottomBar.sizeDelta.y;
        _bottomBarSequence = Sequence.Create()
            .Group(Tween.UIAnchoredPositionY(bottomBar, endYPosition, bottomBarTweenSettings));
    }
    
    
    private void AnimateLevelCompleteWindow(bool show)
    {
        if (_levelCompleteSequence.isAlive) return;
        if (!levelCompleteWindow || !_levelCompleteWindowRectTransform) return;
        

        var startSize = show ? new Vector2(_levelCompleteWindowRectTransform.sizeDelta.x, 0f) : _levelCompleteWindowDefaultSize;
        var endSize = show ? _levelCompleteWindowDefaultSize : new Vector2(_levelCompleteWindowRectTransform.sizeDelta.x, 0f);
        
        if (show) levelCompleteWindow.alpha =  1f;
        _levelCompleteWindowRectTransform.sizeDelta = startSize;
        
        _levelCompleteSequence = Sequence.Create()
            .Group(Tween.UISizeDelta(_levelCompleteWindowRectTransform, endSize, levelCompleteTweenSettings))
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

