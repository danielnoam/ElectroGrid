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
    [Tooltip("Minimum tiles required to form a match")]
    [SerializeField] private int minMatchCount = 3;
    [Tooltip("Maximum attempts to create a grid with guaranteed matches")]
    [SerializeField] private int mxGuaranteedMatchAttempts = 100;
    [Tooltip("Maximum attempts to recheck matches in grid")]
    [SerializeField] private int maxAttemptsToRecheckMatches = 50;
    [Tooltip("Minimum possible matches required in grid")]
    [SerializeField] private int minPossibleMatches = 3;
    [Tooltip("Duration taken to populate grid")]
    [SerializeField] private float populationDuration = 1f;


    
    [Header("References")]
    [SerializeField] private Match3GridHandler gridHandler;
    [SerializeField] private Match3PlayHandler playHandler;
    [SerializeField] private Match3SelectionIndicator selectionIndicator;
    [SerializeField] private SOGridShape defaultGridShape;
    
    [Separator]
    [SerializeField, ReadOnly] private SOMatch3Level currentLevel;
    [SerializeField, ReadOnly] private bool levelComplete;
    [SerializeField, ReadOnly] private bool populatingGrid;

    private readonly List<Match3Objective> _currentObjectives = new List<Match3Objective>();
    private readonly List<Match3LoseCondition> _currentLoseConditions = new List<Match3LoseCondition>();
    private SOGridShape GridShape => currentLevel ? currentLevel.GridShape : defaultGridShape;
    
    
    
    public int MaxGuaranteedMatchAttempts => mxGuaranteedMatchAttempts;
    public int MinMatchCount => minMatchCount;
    public int MaxAttemptsToRecheckMatches => maxAttemptsToRecheckMatches;
    public float PopulationDuration => populationDuration;
    public SOMatch3Level CurrentLevel => currentLevel;
    public event Action<SOMatch3Level, List<Match3Objective>, List<Match3LoseCondition>> LevelStarted;
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
        
        if (!levelComplete && playHandler.CanInteract)
        {
            foreach (var condition in _currentLoseConditions)
            {
                condition?.UpdateCondition(Time.deltaTime);
            }
        }
        
        CheckLoseConditions();
        CheckObjectives();
    }

    #region Game Management
    
    public void SetNextLevel()
    {
        var levels = GameManager.Instance.Match3Levels;
        if (levels != null && levels.Length != 0)
        {
            int currentIndex = Array.IndexOf(levels, currentLevel);
            int nextIndex = (currentIndex + 1) % levels.Length;

            currentLevel = levels[nextIndex];
            StartNewGame();
        }
    }
    
    [Button(ButtonPlayMode.OnlyWhenPlaying)]
    private void StartNewGame()
    {
        if (!currentLevel)
        {
            currentLevel = GameManager.Instance.Match3Levels[0];
        }
        
        if (!currentLevel)
        {
            Debug.LogError("No level assigned or found in level pool!");
            return;
        }

        levelComplete = false;
        populatingGrid = false;
        
        CopyObjectivesAndConditions();
        CreateGrid();
        
        LevelStarted?.Invoke(currentLevel, _currentObjectives, _currentLoseConditions);
    }

    private void CopyObjectivesAndConditions()
    {
        _currentObjectives.Clear();
        _currentLoseConditions.Clear();
        
        foreach (var objective in currentLevel.Objectives)
        {
            if (objective != null)
            {
                string json = JsonUtility.ToJson(objective);
                Match3Objective copy = (Match3Objective)JsonUtility.FromJson(json, objective.GetType());
                copy.SetupObjective();
                _currentObjectives.Add(copy);
            }
        }
        
        foreach (var condition in currentLevel.LoseConditions)
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

        if (allComplete && _currentObjectives.Count > 0 && !populatingGrid)
        {
            OnLevelComplete();
        }
    }

    private void CheckLoseConditions()
    {
        if (levelComplete || populatingGrid) return;

        foreach (var condition in _currentLoseConditions)
        {
            if (condition is { IsConditionMet: true })
            {
                OnLevelFailed();
                return;
            }
        }
    }
    
    public void NotifyObjectivesAboutMatches(List<Match3Tile> allMatches)
    {
        foreach (var objective in _currentObjectives)
        {
            objective?.OnMatchMade(allMatches);
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
        if (!GridShape || !currentLevel) return;
    
        
        playHandler.CanInteract = false;
        gridHandler.CreateGrid(GridShape);
    
        StartCoroutine(InitialGridSetup());
    }
    
    private IEnumerator InitialGridSetup()
    {
        populatingGrid = true;
        int maxRetries = 10;
        int retryCount = 0;
    
        while (retryCount < maxRetries)
        {
            retryCount++;
        
            // Generate the grid layout (data only, no spawning yet)
            var gridLayout = playHandler.GenerateGridLayout(currentLevel, GridShape, minPossibleMatches);
            
            if (gridLayout == null || gridLayout.Count == 0)
            {
                Debug.LogError("Failed to generate grid layout");
                yield break;
            }
            
            // Validate the layout before spawning
            var validationResult = playHandler.ValidateGridLayout(gridLayout, GridShape, minPossibleMatches, checkImmediateMatches: true);
            
            if (!validationResult.isValid)
            {
                if (validationResult.immediateMatches > 0)
                {
                    Debug.Log($"Too many immediate matches found in grid ({validationResult.immediateMatches}), retrying (attempt {retryCount}/{maxRetries})");
                }
                else if (validationResult.possibleMatches < minPossibleMatches)
                {
                    Debug.Log($"Not enough possible matches ({validationResult.possibleMatches}/{minPossibleMatches}), retrying (attempt {retryCount}/{maxRetries})");
                }
                continue; 
            }
            
            Debug.Log($"Grid validated successfully with {validationResult.possibleMatches} possible matches");
            yield return playHandler.SpawnGridLayout(gridLayout, false);
            playHandler.CanInteract = true;
            populatingGrid = false;
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
        populatingGrid = true;
        selectionIndicator.ResetHoveredTile();
    
        // Swap items
        yield return StartCoroutine(playHandler.SwapObjects(posA, posB));
        
        // Notify lose conditions that a move was made
        foreach (var condition in _currentLoseConditions)
        {
            condition?.OnMoveMade();
        }
    
        // Check for matches
        var matchesWithTileA = playHandler.FindMatchesWithTile(gridHandler.GetTile(posA), GridShape);
        var matchesWithTileB = playHandler.FindMatchesWithTile(gridHandler.GetTile(posB), GridShape);
        var allMatches = matchesWithTileA.Concat(matchesWithTileB).Distinct().ToList();
    
        if (allMatches.Count == 0)
        {
            // No match - swap back
            yield return StartCoroutine(playHandler.SwapObjects(posB, posA));
            playHandler.CanInteract = true;
            populatingGrid = false;
            yield break;
        }
        

        // Notify objectives about the matches
        NotifyObjectivesAboutMatches(allMatches);
    
        // Handle matches
        yield return StartCoroutine(playHandler.HandleMatches(allMatches));
    
        // Make objects fall
        yield return StartCoroutine(playHandler.MoveObjects(GridShape));
    
        // Repopulate and handle cascades
        yield return StartCoroutine(playHandler.PopulateGrid(currentLevel, GridShape, minPossibleMatches, true));
        yield return StartCoroutine(playHandler.HandleMatchesAndRepopulate(currentLevel, GridShape, minPossibleMatches));


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
            populatingGrid = false;
        }
    }



    #endregion

    [Button(ButtonPlayMode.OnlyWhenPlaying)]
    private void ForceCompleteLevel()
    {
        if (levelComplete || populatingGrid) return;
        
        OnLevelComplete();
    }
    
    
    private void OnDrawGizmos()
    {
        GridShape?.Grid?.DrawGrid();
    }
    

}