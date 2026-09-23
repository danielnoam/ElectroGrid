using System;
using System.Collections.Generic;
using UnityEngine.Serialization;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
    [SerializeField] private Button infoButton;
    [FormerlySerializedAs("muteButton")]
    [SerializeField] private Button settingsButton;
    [SerializeField] private SettingsWindowUI settingsWindowUI;
    [SerializeField] private Match3UIElement match3UIElementPrefab;
    [SerializeField] private InformationWindowUI informationWindowUI;
    [SerializeField] private BottomBarUI bottomBarUI;
    
    private readonly Dictionary<Match3Objective, Match3UIElement> _currentObjectives = new Dictionary<Match3Objective, Match3UIElement>();
    private readonly Dictionary<Match3LoseCondition, Match3UIElement> _currentLoseConditions = new Dictionary<Match3LoseCondition, Match3UIElement>();
    
    private readonly Dictionary<Match3Objective, Action> _objectiveProgressCallbacks = new Dictionary<Match3Objective, Action>();
    private readonly Dictionary<Match3Objective, Action> _objectiveCompleteCallbacks = new Dictionary<Match3Objective, Action>();
    private readonly Dictionary<Match3LoseCondition, Action> _loseConditionProgressCallbacks = new Dictionary<Match3LoseCondition, Action>();
    private readonly Dictionary<Match3LoseCondition, Action> _loseConditionMetCallbacks = new Dictionary<Match3LoseCondition, Action>();
    
    private Match3GameManager _match3Manager;
    private float _levelNameDefaultPositionY;
    private float _settingsButtonDefaultPositionY;
    private float _infoButtonDefaultPositionY;
    private Vector2 _levelNameDefaultSize;
    private Vector2 _settingsButtonDefaultSize;
    private Vector2 _infoButtonDefaultSize;
    private Vector2 _topBarDefaultSize;
    private Sequence _topBarSequence;

    private void Awake()
    {
        _topBarDefaultSize = topBar.sizeDelta;
        topBar.sizeDelta = new Vector2(_topBarDefaultSize.x, 0f);
        
        _levelNameDefaultPositionY = levelName.anchoredPosition.y;
        _levelNameDefaultSize = levelName.sizeDelta;
        levelName.sizeDelta = Vector2.zero;
        levelName.anchoredPosition = new Vector2(levelName.anchoredPosition.x, 0f);
        
        _settingsButtonDefaultSize = settingsButton.GetComponent<RectTransform>().sizeDelta;
        settingsButton.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, settingsButton.GetComponent<RectTransform>().sizeDelta.y);
        _settingsButtonDefaultPositionY = settingsButton.transform.localPosition.y;
        settingsButton.transform.localPosition = new Vector3(settingsButton.transform.localPosition.x, 0f, settingsButton.transform.localPosition.z);
        
        _infoButtonDefaultSize = infoButton.GetComponent<RectTransform>().sizeDelta;
        infoButton.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, infoButton.GetComponent<RectTransform>().sizeDelta.y);
        _infoButtonDefaultPositionY = infoButton.transform.localPosition.y;
        infoButton.transform.localPosition = new Vector3(infoButton.transform.localPosition.x, 0f, infoButton.transform.localPosition.z);
    }

    public void Initialize(Match3GameManager match3Manager)
    {
        _match3Manager = match3Manager;
        
        SetupButtons();
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
    }

    private void UnsubscribeFromEvents()
    {
        if (_match3Manager == null) return;
        
        _match3Manager.LevelStarted -= OnLevelStarted;
        _match3Manager.LevelComplete -= OnLevelComplete;
        _match3Manager.LevelFailed -= OnLevelFailed;
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

    private void SetupButtons()
    {
        settingsButton.onClick.RemoveAllListeners();
        settingsButton.onClick.AddListener(() =>
        {
            CameraManager.Instance.ShakeCamera(0.1f);
            Toggle(false);
            bottomBarUI?.Toggle(false);
            settingsWindowUI?.Show(() =>
            {
                Toggle(true);
                bottomBarUI?.Toggle(true);
            });
        });
    
        infoButton.onClick.RemoveAllListeners();
        infoButton.onClick.AddListener(() =>
        {
            CameraManager.Instance.ShakeCamera(0.1f);
            Toggle(false);
            bottomBarUI?.Toggle(false);
            informationWindowUI?.ShowAllTutorials();
            FirebaseManager.Instance?.LogInformationClicked();
        });
    }

    private void SetupLevel(Match3LevelData levelData)
    {
        if (levelData == null) return;
        
        levelNameText.text = levelData.Level.LevelName;
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
        var infoButtonStartPosition = show ? 0 : _infoButtonDefaultPositionY;
        var infoButtonEndPosition = show ? _infoButtonDefaultPositionY : 0;
        var infoButtonStartSize = show ? Vector2.zero : _infoButtonDefaultSize;
        var infoButtonEndSize = show ? _infoButtonDefaultSize : Vector2.zero;
        var settingsButtonStartPosition = show ? 0 : _settingsButtonDefaultPositionY;
        var settingsButtonEndPosition = show ? _settingsButtonDefaultPositionY : 0;
        var settingsButtonStartSize = show ? Vector2.zero : _settingsButtonDefaultSize;
        var settingsButtonEndSize = show ? _settingsButtonDefaultSize : Vector2.zero;
        
        topBar.sizeDelta = barStartSize;
        levelName.sizeDelta = nameStartSize;
        levelName.anchoredPosition = new Vector2(levelName.anchoredPosition.x, nameStartPosition);
        
        var settingsButtonRectTransform = settingsButton.GetComponent<RectTransform>();
        settingsButtonRectTransform.sizeDelta = settingsButtonStartSize;
        settingsButtonRectTransform.anchoredPosition = new Vector2(settingsButtonRectTransform.anchoredPosition.x, settingsButtonStartPosition);
        
        var infoButtonRectTransform = infoButton.GetComponent<RectTransform>();
        infoButtonRectTransform.sizeDelta = infoButtonStartSize;
        infoButtonRectTransform.anchoredPosition = new Vector2(infoButtonRectTransform.anchoredPosition.x, infoButtonStartPosition);
        
        _topBarSequence = Sequence.Create(useUnscaledTime: true)
            .Group(Tween.UISizeDelta(topBar, barEndSize, topbarTweenSettings))
            .Group(Tween.UISizeDelta(levelName, nameEndSize, startDelay: topbarTweenSettings.duration / 2, duration: topbarTweenSettings.duration * 0.8f, ease: topbarTweenSettings.ease))
            .Group(Tween.UIAnchoredPositionY(levelName, nameEndPosition, startDelay: topbarTweenSettings.duration / 2, duration: topbarTweenSettings.duration * 0.8f, ease: topbarTweenSettings.ease))
            .Group(Tween.UISizeDelta(settingsButton.transform as RectTransform, settingsButtonEndSize, duration: topbarTweenSettings.duration * 0.8f, ease: topbarTweenSettings.ease))
            .Group(Tween.UIAnchoredPositionY(settingsButton.transform as RectTransform, settingsButtonEndPosition, duration: topbarTweenSettings.duration * 0.8f, ease: topbarTweenSettings.ease))
            .Group(Tween.UISizeDelta(infoButton.transform as RectTransform, infoButtonEndSize, duration: topbarTweenSettings.duration * 0.8f, ease: topbarTweenSettings.ease))
            .Group(Tween.UIAnchoredPositionY(infoButton.transform as RectTransform, infoButtonEndPosition, duration: topbarTweenSettings.duration * 0.8f, ease: topbarTweenSettings.ease));
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