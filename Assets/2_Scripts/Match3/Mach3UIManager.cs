using System;
using System.Collections.Generic;
using PrimeTween;
using UnityEngine;

public class Mach3UIManager : MonoBehaviour
{

    [Header("Settings")]
    [SerializeField] private TweenSettings topbarTweenSettings;
    
    
    
    [Header("References")]
    [SerializeField] private Match3GameManager gameManager;
    [SerializeField] private RectTransform topBar;
    [SerializeField] private Transform objectivesUIParent;
    [SerializeField] private Transform loseConditionsUIParent;
    [SerializeField] private Match3UIElement match3UIElementPrefab;

    
    private readonly Dictionary<Match3Objective, Match3UIElement> _currentObjectives = new Dictionary<Match3Objective, Match3UIElement>();
    private readonly Dictionary<Match3LoseCondition, Match3UIElement> _currentLoseConditions = new Dictionary<Match3LoseCondition, Match3UIElement>();
    
    private Vector2 _topBarDefaultSize;
    private Sequence _topBarSequence;
    

    private void Awake()
    {
        _topBarDefaultSize = topBar.sizeDelta;
        topBar.sizeDelta = new Vector2(_topBarDefaultSize.x, 0f);
    }

    private void OnEnable()
    {
        gameManager.LevelStarted += OnLevelStarted;
        gameManager.LevelComplete += OnLevelComplete;
    }

    private void OnLevelComplete(bool won)
    {
        AnimateTopBar(false);
    }

    private void OnDisable()
    {
        gameManager.LevelStarted -= OnLevelStarted;
        gameManager.LevelComplete -= OnLevelComplete;
    }

    
    private void Update()
    {
        UpdateUIElements();
    }
    
    private void OnLevelStarted(SOMatch3Level level, List<Match3Objective> objectives, List<Match3LoseCondition> loseConditions)
    {
        SetupUIElements(objectives, loseConditions);
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
        
        AnimateTopBar(true);
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
        _topBarSequence.Stop();
        
        var endSize = show ? _topBarDefaultSize : new Vector2(_topBarDefaultSize.x, 0f);
        
        
        
        _topBarSequence = Sequence.Create();
        _topBarSequence.Group(Tween.UISizeDelta(topBar, endSize, topbarTweenSettings));
    }
    
}

