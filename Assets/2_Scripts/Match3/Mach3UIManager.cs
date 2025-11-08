using System;
using System.Collections.Generic;
using UnityEngine;

public class Mach3UIManager : MonoBehaviour
{

    [Header("References")]
    [SerializeField] private Match3GameManager gameManager;
    [SerializeField] private Transform objectivesUIParent;
    [SerializeField] private Transform loseConditionsUIParent;
    [SerializeField] private Match3UIElement match3UIElementPrefab;

    
    private readonly Dictionary<Match3Objective, Match3UIElement> _currentObjectives = new Dictionary<Match3Objective, Match3UIElement>();
    private readonly Dictionary<Match3LoseCondition, Match3UIElement> _currentLoseConditions = new Dictionary<Match3LoseCondition, Match3UIElement>();

    private void Awake()
    {
        ClearUIElements();
    }

    private void OnEnable()
    {
        gameManager.LevelStarted += OnLevelStarted;
    }
    
    private void OnDisable()
    {
        gameManager.LevelStarted -= OnLevelStarted;
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
    
}

