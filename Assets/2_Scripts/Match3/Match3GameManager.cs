using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DNExtensions;
using DNExtensions.Button;
using System.Linq;

public class Match3GameManager : MonoBehaviour
{
    public static Match3GameManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private int maxAttemptsToRecheckMatches = 50;
    [Tooltip("Max possible matches allowed in grid when populating")]
    [SerializeField] private int maxImmediateMatches;
    [Tooltip("Minimum possible matches required in grid")]
    [SerializeField] private int minPossibleMatches = 3;
    [Tooltip("Duration taken to populate grid")]
    [SerializeField] private float populationDuration = 1f;
    [SerializeField] private SOMatch3Level level;

    
    [Header("References")]
    [SerializeField] private ChanceList<SOMatch3Level> levelPool;
    [SerializeField] private Match3GridManager gridManager;
    [SerializeField] private Match3PlayHandler playHandler;
    [SerializeField] private Match3SelectionIndicator selectionIndicator;
    [SerializeField] private SOGridShape defaultGridShape;
    
    [Separator]
    [SerializeField, ReadOnly] private bool levelComplete;

    private readonly List<Match3Objective> _currentObjectives = new List<Match3Objective>();
    private readonly List<Match3LoseCondition> _currentLoseConditions = new List<Match3LoseCondition>();
    private SOGridShape GridShape => level ? level.GridShape : defaultGridShape;
    
    
    
    public int MaxAttemptsToRecheckMatches => maxAttemptsToRecheckMatches;
    public float PopulationDuration => populationDuration;
    
    public event Action<bool> LevelComplete;
    
    

    private void Awake()
    {
        if (!Instance || Instance == this)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        StartNewGame();
    }

    private void Update()
    {
        if (levelComplete) return;
        
        foreach (var condition in _currentLoseConditions)
        {
            condition?.UpdateCondition(Time.deltaTime);
        }
        
        CheckLoseConditions();
    }

    #region Game Management
    
    [Button(ButtonPlayMode.OnlyWhenPlaying)]
    private void StartNewGame()
    {
        if (!level)
        {
            level = levelPool.GetRandomItem();
        }
        
        if (!level)
        {
            Debug.LogError("No level assigned or found in level pool!");
            return;
        }

        levelComplete = false;
        
        CopyObjectivesAndConditions();
        CreateGrid();
    }

    private void CopyObjectivesAndConditions()
    {
        _currentObjectives.Clear();
        _currentLoseConditions.Clear();
        
        foreach (var objective in level.Objectives)
        {
            if (objective != null)
            {
                string json = JsonUtility.ToJson(objective);
                Match3Objective copy = (Match3Objective)JsonUtility.FromJson(json, objective.GetType());
                copy.SetupObjective();
                _currentObjectives.Add(copy);
            }
        }
        
        foreach (var condition in level.LoseConditions)
        {
            if (condition != null)
            {
                string json = JsonUtility.ToJson(condition);
                Match3LoseCondition copy = (Match3LoseCondition)JsonUtility.FromJson(json, condition.GetType());
                copy.SetupCondition();
                _currentLoseConditions.Add(copy);
            }
        }
    }

    private void CheckObjectives()
    {
        if (levelComplete) return;

        bool allComplete = true;
        foreach (var objective in _currentObjectives)
        {
            if (objective == null || !objective.IsCompleted)
            {
                allComplete = false;
                break;
            }
        }

        if (allComplete && _currentObjectives.Count > 0)
        {
            OnLevelComplete();
        }
    }

    private void CheckLoseConditions()
    {
        if (levelComplete) return;

        foreach (var condition in _currentLoseConditions)
        {
            if (condition != null && condition.IsConditionMet)
            {
                OnLevelFailed();
                return;
            }
        }
    }

    private void OnLevelComplete()
    {
        levelComplete = true;
        playHandler.CanInteract = false;
        LevelComplete?.Invoke(true);
        Debug.Log("Level Complete! All objectives achieved!");
    }

    private void OnLevelFailed()
    {
        levelComplete = true;
        playHandler.CanInteract = false;
        LevelComplete?.Invoke(false);
        Debug.Log("Level Failed! Lose condition met.");
    }
    
    #endregion

    #region Grid Setup

    private void CreateGrid()
    {
        if (!GridShape || !level) return;
    
        playHandler.CanInteract = false;
        playHandler.DestroyAllObjects();
        gridManager.CreateGrid(GridShape);
    
        StartCoroutine(InitialGridSetup());
    }
    
    private IEnumerator InitialGridSetup()
    {
        int maxRetries = 10;
        int retryCount = 0;
    
        while (retryCount < maxRetries)
        {
            retryCount++;
        
            yield return playHandler.PopulateGrid(level, GridShape, minPossibleMatches);
        
            // Validate immediate matches
            var immediateMatchesInGrid = playHandler.FindImmediateMatches(GridShape);
            if (immediateMatchesInGrid.Count > maxImmediateMatches)
            {
                Debug.Log($"Too many immediate matches found in grid ({immediateMatchesInGrid.Count}), restarting grid (attempt {retryCount}/{maxRetries})");
                CreateGrid();
                yield break;
            }
        
            // Validate possible matches
            var possibleMatchesInGrid = playHandler.FindPossibleMatches(GridShape);
            if (possibleMatchesInGrid.Count < minPossibleMatches)
            {
                Debug.Log($"Failed to create enough possible matches ({possibleMatchesInGrid.Count}/{minPossibleMatches}), restarting grid (attempt {retryCount}/{maxRetries})");
                CreateGrid();
                yield break;
            }
        
            // Success! Handle any cascading matches
            Debug.Log($"Grid populated successfully with {possibleMatchesInGrid.Count} possible matches");
            yield return playHandler.HandleMatchesAndRepopulate(level, GridShape, minPossibleMatches);
        
            playHandler.CanInteract = true;
            yield break;
        }
    
        Debug.LogError($"Failed to create valid grid after {maxRetries} attempts");
    }

    #endregion

    #region Game Logic

    public IEnumerator RunGameLogic(Vector2Int posA, Vector2Int posB)
    {
        if (levelComplete)
        {
            yield break;
        }

        playHandler.CanInteract = false;
        selectionIndicator.ResetHoveredTile();
    
        // Swap items
        yield return StartCoroutine(playHandler.SwapObjects(posA, posB));
        
        // Notify lose conditions that a move was made
        foreach (var condition in _currentLoseConditions)
        {
            condition?.OnMoveMade();
        }
    
        // Check for matches
        var matchesWithTileA = playHandler.FindMatchesWithTile(gridManager.GetTile(posA), GridShape);
        var matchesWithTileB = playHandler.FindMatchesWithTile(gridManager.GetTile(posB), GridShape);
        var allMatches = matchesWithTileA.Concat(matchesWithTileB).Distinct().ToList();
    
        if (allMatches.Count == 0)
        {
            // No match - swap back
            yield return StartCoroutine(playHandler.SwapObjects(posB, posA));
            playHandler.CanInteract = true;
            yield break;
        }
        

        // Notify objectives about the matches
        foreach (var objective in _currentObjectives)
        {
            objective?.OnMatchMade(allMatches);
        }
    
        // Handle matches
        yield return StartCoroutine(playHandler.HandleMatches(allMatches));
    
        // Make objects fall
        yield return StartCoroutine(playHandler.MoveObjects(GridShape));
    
        // Repopulate and handle cascades
        yield return StartCoroutine(playHandler.PopulateGrid(level, GridShape, minPossibleMatches));
        yield return StartCoroutine(playHandler.HandleMatchesAndRepopulate(level, GridShape, minPossibleMatches));
        
        // Check objectives after matches
        CheckObjectives();

        // Check lose conditions
        CheckLoseConditions();

        // If game is still active, check if there are still possible matches
        if (!levelComplete)
        {
            var possibleMatches = playHandler.FindPossibleMatches(GridShape);
            if (possibleMatches.Count < minPossibleMatches)
            {
                Debug.Log($"No possible matches left, recreating grid");
                CreateGrid();
                yield break;
            }
        
            playHandler.CanInteract = true;
        }
    }

    #endregion

    private void OnDrawGizmos()
    {
        GridShape?.Grid?.DrawGrid();
        
        if (!Application.isPlaying) return;
        
        DrawObjectivesWorld();
        DrawLoseConditionsWorld();
    }

    private void DrawObjectivesWorld()
    {
        if (_currentObjectives == null || _currentObjectives.Count == 0) return;

        #if UNITY_EDITOR
        Vector3 basePosition = transform.position + Vector3.up * 8f;
        
        for (int i = 0; i < _currentObjectives.Count; i++)
        {
            var objective = _currentObjectives[i];
            if (objective == null) continue;
            
            Vector3 position = basePosition + Vector3.right * (i * 3f);
            
            // Draw icon/sphere
            Color iconColor = objective.IsCompleted ? Color.green : Color.cyan;
            Gizmos.color = iconColor;
            
            
            // Draw text
            UnityEditor.Handles.Label(position + Vector3.up * 0.8f, 
                $"{objective.ObjectiveName}\n{objective.GetProgressText()}",
                new GUIStyle() 
                { 
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = iconColor },
                    fontSize = 12,
                    fontStyle = FontStyle.Bold
                });
        }
        #endif
    }

    private void DrawLoseConditionsWorld()
    {
        if (_currentLoseConditions == null || _currentLoseConditions.Count == 0) return;

        #if UNITY_EDITOR
        Vector3 basePosition = transform.position + Vector3.up * 10f;
        
        for (int i = 0; i < _currentLoseConditions.Count; i++)
        {
            var condition = _currentLoseConditions[i];
            if (condition == null) continue;
            
            Vector3 position = basePosition + Vector3.right * (i * 3f);
            
            // Draw icon/sphere
            Color iconColor = condition.IsConditionMet ? Color.red : Color.yellow;
            Gizmos.color = iconColor;
            
            // Draw text
            UnityEditor.Handles.Label(position + Vector3.down * 0.8f, 
                $"{condition.ConditionName}\n{condition.GetProgressText()}",
                new GUIStyle() 
                { 
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = iconColor },
                    fontSize = 12,
                    fontStyle = FontStyle.Bold
                });
        }
        #endif
    }

}