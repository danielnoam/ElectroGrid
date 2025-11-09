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
        
        LevelStarted?.Invoke(level, _currentObjectives, _currentLoseConditions);
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
        if (levelComplete || !playHandler.CanInteract) return;

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
        NotifyObjectivesAboutMatches(allMatches);
    
        // Handle matches
        yield return StartCoroutine(playHandler.HandleMatches(allMatches));
    
        // Make objects fall
        yield return StartCoroutine(playHandler.MoveObjects(GridShape));
    
        // Repopulate and handle cascades
        yield return StartCoroutine(playHandler.PopulateGrid(level, GridShape, minPossibleMatches));
        yield return StartCoroutine(playHandler.HandleMatchesAndRepopulate(level, GridShape, minPossibleMatches));


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
    }
    

}