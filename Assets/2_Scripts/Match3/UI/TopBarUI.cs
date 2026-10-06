using System;
using System.Collections.Generic;
using PrimeTween;
using TMPro;
using UnityEngine;

public class TopBarUI : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private TweenSettings topbarTweenSettings;
    
    [Header("References")]
    [SerializeField] private Transform objectivesUIParent;
    [SerializeField] private Transform loseConditionsUIParent;
    [SerializeField] private RectTransform topBar;
    [SerializeField] private RectTransform levelName;
    [SerializeField] private TextMeshProUGUI levelNameText;
    [SerializeField] private Match3UIElement match3UIElementPrefab;
    
    private readonly Dictionary<Match3Objective, Match3UIElement> _currentObjectives = new Dictionary<Match3Objective, Match3UIElement>();
    private readonly Dictionary<Match3LoseCondition, Match3UIElement> _currentLoseConditions = new Dictionary<Match3LoseCondition, Match3UIElement>();
    
    private readonly Dictionary<Match3Objective, Action> _objectiveProgressCallbacks = new Dictionary<Match3Objective, Action>();
    private readonly Dictionary<Match3Objective, Action> _objectiveCompleteCallbacks = new Dictionary<Match3Objective, Action>();
    private readonly Dictionary<Match3LoseCondition, Action> _loseConditionProgressCallbacks = new Dictionary<Match3LoseCondition, Action>();
    private readonly Dictionary<Match3LoseCondition, Action> _loseConditionMetCallbacks = new Dictionary<Match3LoseCondition, Action>();
    
    private Match3GameManager _match3Manager;
    private float _levelNameDefaultPositionY;
    private Vector2 _levelNameDefaultSize;
    private Vector2 _topBarDefaultSize;
    private Sequence _topBarSequence;
    private SOMatch3Level _currentLevel;

    private void Awake()
    {
        _topBarDefaultSize = topBar.sizeDelta;
        topBar.sizeDelta = new Vector2(_topBarDefaultSize.x, 0f);
        
        _levelNameDefaultPositionY = levelName.anchoredPosition.y;
        _levelNameDefaultSize = levelName.sizeDelta;
        levelName.sizeDelta = Vector2.zero;
        levelName.anchoredPosition = new Vector2(levelName.anchoredPosition.x, 0f);
    }

    public void Initialize(Match3GameManager match3Manager)
    {
        _match3Manager = match3Manager;

        SubscribeToEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    private void SubscribeToEvents()
    {
        if (_match3Manager == null) return;
        
        _match3Manager.LevelStarted += OnLevelStarted;
        _match3Manager.LevelComplete += OnLevelComplete;
        _match3Manager.LevelFailed += OnLevelFailed;
        L10n.LanguageChanged += RefreshLabels;
    }

    private void UnsubscribeFromEvents()
    {
        L10n.LanguageChanged -= RefreshLabels;
        if (_match3Manager == null) return;

        _match3Manager.LevelStarted -= OnLevelStarted;
        _match3Manager.LevelComplete -= OnLevelComplete;
        _match3Manager.LevelFailed -= OnLevelFailed;
    }

    // The language can change from the settings window mid-level
    private void RefreshLabels()
    {
        if (_currentLevel) levelNameText.text = _currentLevel.DisplayName;
        foreach (var pair in _currentObjectives) pair.Value.SetLabel(pair.Key.GetRequirementText());
        foreach (var pair in _currentLoseConditions) pair.Value.SetLabel(pair.Key.GetRequirementText());
    }

    private void OnLevelStarted(Match3LevelData levelData)
    {
        SetupLevel(levelData);
        Toggle(true);
    }

    private void OnLevelComplete(Match3LevelData levelData)
    {
        Toggle(false);
    }

    private void OnLevelFailed(Match3LevelData levelData)
    {
        Toggle(false);
    }

    private void SetupLevel(Match3LevelData levelData)
    {
        if (levelData == null) return;

        _currentLevel = levelData.Level;
        levelNameText.text = levelData.Level.DisplayName;
        SetupUIElements(levelData.CurrentObjectives, levelData.CurrentLoseConditions);
    }

    private void UpdateObjectiveUIProgress(Match3Objective objective)
    {
        if (_currentObjectives.TryGetValue(objective, out var uiElement))
        {
            uiElement.UpdateProgress(objective.GetProgress());
        }
    }

    private void UpdateLoseConditionUIProgress(Match3LoseCondition loseCondition)
    {
        if (_currentLoseConditions.TryGetValue(loseCondition, out var uiElement))
        {
            uiElement.UpdateProgress(loseCondition.GetProgress().Item1);
        }
    }

    public void Toggle(bool show)
    {
        if (!topBar) return;
        
        _topBarSequence.Stop();
        
        var barSizeMultiplier = objectivesUIParent.gameObject.activeSelf && loseConditionsUIParent.gameObject.activeSelf ? 1f : 0.6f;
        var barStartSize = show ? new Vector2(_topBarDefaultSize.x, 0f) : _topBarDefaultSize * barSizeMultiplier;
        var barEndSize = show ? _topBarDefaultSize * barSizeMultiplier : new Vector2(_topBarDefaultSize.x, 0f);
        var nameStartPosition = show ? 0f : _levelNameDefaultPositionY;
        var nameEndPosition = show ? _levelNameDefaultPositionY : 0f;
        var nameStartSize = show ? Vector2.zero : _levelNameDefaultSize;
        var nameEndSize = show ? _levelNameDefaultSize : Vector2.zero;

        topBar.sizeDelta = barStartSize;
        levelName.sizeDelta = nameStartSize;
        levelName.anchoredPosition = new Vector2(levelName.anchoredPosition.x, nameStartPosition);

        _topBarSequence = Sequence.Create(useUnscaledTime: true)
            .Group(Tween.UISizeDelta(topBar, barEndSize, topbarTweenSettings))
            .Group(Tween.UISizeDelta(levelName, nameEndSize, startDelay: topbarTweenSettings.duration / 2, duration: topbarTweenSettings.duration * 0.8f, ease: topbarTweenSettings.ease))
            .Group(Tween.UIAnchoredPositionY(levelName, nameEndPosition, startDelay: topbarTweenSettings.duration / 2, duration: topbarTweenSettings.duration * 0.8f, ease: topbarTweenSettings.ease));
    }

    private void SetupUIElements(List<Match3Objective> objectives, List<Match3LoseCondition> loseConditions)
    {
        if (!objectivesUIParent || !loseConditionsUIParent) return;
    
        ClearUIElements();
    
        objectivesUIParent.gameObject.SetActive(objectives.Count > 0);
        foreach (var objective in objectives)
        {
            var uiElement = Instantiate(match3UIElementPrefab, objectivesUIParent);
            uiElement.Setup(objective.ObjectiveSprite, objective.GetRequirementText(), objective.GetProgress());
            uiElement.gameObject.name = objective.GetName();
            _currentObjectives.Add(objective, uiElement);
        
            Action progressCallback = () => UpdateObjectiveUIProgress(objective);
            _objectiveProgressCallbacks.Add(objective, progressCallback);
            objective.ProgressChanged += progressCallback;
        
            Action completeCallback = () => UpdateObjectiveUIProgress(objective);
            _objectiveCompleteCallbacks.Add(objective, completeCallback);
            objective.Completed += completeCallback;
        }

        loseConditionsUIParent.gameObject.SetActive(loseConditions.Count > 0);
        foreach (var loseCondition in loseConditions)
        {
            var uiElement = Instantiate(match3UIElementPrefab, loseConditionsUIParent);
            uiElement.Setup(loseCondition.ConditionSprite, loseCondition.GetRequirementText(), loseCondition.GetProgress().Item1);
            uiElement.gameObject.name = loseCondition.GetName();
            _currentLoseConditions.Add(loseCondition, uiElement);
        
            Action progressCallback = () => UpdateLoseConditionUIProgress(loseCondition);
            _loseConditionProgressCallbacks.Add(loseCondition, progressCallback);
            loseCondition.ProgressChanged += progressCallback;
        
            Action metCallback = () => UpdateLoseConditionUIProgress(loseCondition);
            _loseConditionMetCallbacks.Add(loseCondition, metCallback);
            loseCondition.ConditionMet += metCallback;
        }
    }

    private void ClearUIElements()
    {
        foreach (var pair in _objectiveProgressCallbacks)
        {
            pair.Key.ProgressChanged -= pair.Value;
        }
        _objectiveProgressCallbacks.Clear();
    
        foreach (var pair in _objectiveCompleteCallbacks)
        {
            pair.Key.Completed -= pair.Value;
        }
        _objectiveCompleteCallbacks.Clear();

        foreach (var pair in _loseConditionProgressCallbacks)
        {
            pair.Key.ProgressChanged -= pair.Value;
        }
        _loseConditionProgressCallbacks.Clear();
    
        foreach (var pair in _loseConditionMetCallbacks)
        {
            pair.Key.ConditionMet -= pair.Value;
        }
        _loseConditionMetCallbacks.Clear();
    
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
}