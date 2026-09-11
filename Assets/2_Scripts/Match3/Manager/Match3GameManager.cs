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

    [Header("Gameplay Settings")]
    [Tooltip("Minimum tiles required to form a match")]
    [SerializeField] private int minMatchCount = 3;
    [Tooltip("Minimum tiles required to line break")]
    [SerializeField] private int minMatchForLineBreak = 4;
    [Tooltip("Duration taken to spawn helper objects")]
    [SerializeField, Range(0f,100f)] private float chanceToSpawnHelper = 5f;
    
    [Header("Population Settings")]
    [Tooltip("Maximum attempts to create a grid with guaranteed matches")]
    [SerializeField] private int maxGuaranteedMatchAttempts = 100;
    [Tooltip("Maximum attempts to recheck matches in grid")]
    [SerializeField] private int maxAttemptsToRecheckMatches = 50;
    [Tooltip("Minimum possible matches required in grid")]
    [SerializeField] private int minPossibleMatches = 3;
    
    [Header("References")]
    [SerializeField] private Match3GridHandler gridHandler;
    [SerializeField] private Match3PlayHandler playHandler;
    [SerializeField] private Match3SelectionIndicator selectionIndicator;
    [SerializeField] private SOMatch3Level overrideLevel;

    [Separator]
    [SerializeField, ReadOnly] private SOMatch3Level currentLevel;
    [SerializeField, ReadOnly] private bool levelComplete;
    [SerializeField, ReadOnly] private bool finishedObjectives;
    [SerializeField, ReadOnly] private bool populatingGrid;

    private Match3LevelData _currentLevelData;
    
    public Match3LevelData CurrentLevelData => _currentLevelData;
    public Match3GridHandler GridHandler => gridHandler;
    public int MaxGuaranteedMatchAttempts => maxGuaranteedMatchAttempts;
    public float ChanceToSpawnHelper => chanceToSpawnHelper;
    public int MinMatchCount => minMatchCount;
    public int MinMatchForLineBreak => minMatchForLineBreak;
    public int MaxAttemptsToRecheckMatches => maxAttemptsToRecheckMatches;
    
    public event Action<Match3LevelData> LevelStarted;
    public event Action<Match3LevelData> LevelComplete;
    public event Action<Match3LevelData> LevelFailed;
    public event Action<List<Match3Tile>> MatchesMade;
    public event Action<Match3HelperObject> HelperDestroyed;
    public event Action<List<int>, List<int>> LineBreakMade;
    
    

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
        UpdateLevelTime();
        UpdateLoseConditions();
        CheckObjectives();
        CheckLoseConditions();
    }

    private void UpdateLevelTime()
    {
        if (levelComplete || _currentLevelData == null) return;

        _currentLevelData.TimeSpent += Time.deltaTime;
    }

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
    
    public void RestartLevel()
    {
        StartNewGame();
    }
    
    [Button(ButtonPlayMode.OnlyWhenPlaying)]
    public void StartNewGame()
    {
        if (populatingGrid) return;
        
        if (!currentLevel)
        {
            if (overrideLevel)
            {
                currentLevel = overrideLevel;
            }
            else if (GameManager.Instance && GameManager.Instance.SelectedMatch3Level)
            {
                currentLevel = GameManager.Instance.SelectedMatch3Level;
            }
            else if (GameManager.Instance && GameManager.Instance.Match3Levels.Length > 0)
            {
                currentLevel = GameManager.Instance.Match3Levels[0];
            }
        }
        
        if (!currentLevel)
        {
            Debug.LogError("No level assigned or found in level pool!");
            return;
        }

        levelComplete = false;
        finishedObjectives = false;
        populatingGrid = false;
        
        _currentLevelData = new Match3LevelData(currentLevel);
        StartCoroutine(InitialLevelSetup());
        
        FirebaseManager.Instance?.LogLevelStarted(_currentLevelData);
        UnityAnalyticsManager.Instance?.LogGameStarted();
        
        LevelStarted?.Invoke(_currentLevelData);
    }

    private void CheckObjectives()
    {
        if (levelComplete || populatingGrid || _currentLevelData == null || finishedObjectives) return;

        if (_currentLevelData.IsObjectivesComplete())
        {
            finishedObjectives = true;
            StartCoroutine(CompleteLevel());
        }
    }

    private void CheckLoseConditions()
    {
        if (levelComplete || populatingGrid || _currentLevelData == null || finishedObjectives) return;
        
        if (_currentLevelData.IsAnyLoseConditionMet())
        {
            StartCoroutine(FailLevel());
        }
    }
    
    private void UpdateLoseConditions()
    {
        if (levelComplete || !playHandler.CanInteract || _currentLevelData == null) return;
        
        foreach (var condition in _currentLevelData.CurrentLoseConditions)
        {
            condition?.Update(Time.deltaTime);
        }
    }
    
    public void NotifyMatchesWereMade(List<Match3Tile> matches)
    {
        _currentLevelData?.OnMatchesMade(matches);
        MatchesMade?.Invoke(matches);
    }
    
    public void NotifyHelperObjectDestroyed(Match3HelperObject helper)
    {
        HelperDestroyed?.Invoke(helper);
        _currentLevelData?.OnHelperObjectDestroyed();
    }
    
    private void NotifyAMoveWasMade()
    {
        _currentLevelData?.OnMoveMade();
    }

    public void NotifyObstacleBroke(Match3ObstacleObject obstacle)
    {
        _currentLevelData?.OnObstacleBreak(obstacle);
    }
    
    public void NotifyBottomObjectReached(Match3BottomObject bottomObject)
    {
        _currentLevelData?.OnBottomObjectReached(bottomObject);
    }
    
    public void NotifyLineBreakMade(List<int> rows, List<int> columns)
    {
        FirebaseManager.Instance?.LogLineBreak();
        LineBreakMade?.Invoke(rows, columns);
    }
    

    private IEnumerator CompleteLevel()
    {
        levelComplete = true;
        playHandler.CanInteract = false;
        
        yield return new WaitForSeconds(0.1f);
        
        yield return StartCoroutine(playHandler.ClearObjects());
        
        yield return new WaitForSeconds(0.2f);
        
        FirebaseManager.Instance?.LogLevelCompleted(_currentLevelData);
        UnityAnalyticsManager.Instance?.LogGameCompleted();
        LevelComplete?.Invoke(_currentLevelData);
    }

    private IEnumerator FailLevel()
    {
        levelComplete = true;
        playHandler.CanInteract = false;
        
        yield return new WaitForSeconds(0.1f);
        
        yield return StartCoroutine(playHandler.ClearObjects());
        
        yield return new WaitForSeconds(0.2f);

        FirebaseManager.Instance?.LogLevelFailed(_currentLevelData);
        UnityAnalyticsManager.Instance?.LogGameFailed();
        LevelFailed?.Invoke(_currentLevelData);
    }
    
    public IEnumerator RunGameLogic(Vector2Int posA, Vector2Int posB)
    {
        if (levelComplete)
        {
            yield break;
        }

        playHandler.CanInteract = false;
        populatingGrid = true;
        selectionIndicator.ResetHoveredTile();
    
        yield return StartCoroutine(playHandler.SwapObjects(posA, posB));
    
        var matchesWithTileA = playHandler.FindMatchesWithTile(gridHandler.GetTile(posA), gridHandler.GridShape);
        var matchesWithTileB = playHandler.FindMatchesWithTile(gridHandler.GetTile(posB), gridHandler.GridShape);
        var allMatches = matchesWithTileA.Concat(matchesWithTileB).Distinct().ToList();
    
        if (allMatches.Count == 0)
        {
            yield return StartCoroutine(playHandler.SwapObjects(posB, posA));
            playHandler.CanInteract = true;
            populatingGrid = false;
            yield break;
        }
        
        NotifyAMoveWasMade();
        
        yield return StartCoroutine(playHandler.HandleMatches(allMatches));
        
        NotifyMatchesWereMade(allMatches);
    
        yield return StartCoroutine(playHandler.MoveObjectsDown(gridHandler.GridShape));
    
        yield return StartCoroutine(playHandler.PopulateGrid(currentLevel, gridHandler.GridShape, minPossibleMatches, false));
        
        yield return StartCoroutine(playHandler.HandleMatchesAndRepopulate(currentLevel, gridHandler.GridShape, minPossibleMatches));

        if (!levelComplete)
        {
            var possibleMatches = playHandler.FindPossibleMatches(gridHandler.GridShape);
            if (possibleMatches.Count < minPossibleMatches)
            {
                Debug.Log($"No possible matches left");
                yield break;
            }
        
            playHandler.CanInteract = true;
            populatingGrid = false;
        }
    }
    
    private IEnumerator InitialLevelSetup()
    {
        if (!currentLevel) yield break;
        
        playHandler.CanInteract = false;
        gridHandler.CreateGrid(currentLevel);
        populatingGrid = true;
        
        int retryCount = 0;
    
        while (retryCount < maxAttemptsToRecheckMatches)
        {
            retryCount++;
        
            var gridLayout = playHandler.GenerateGridLayout(currentLevel, gridHandler.GridShape, minPossibleMatches);
            
            if (gridLayout == null || gridLayout.Count == 0)
            {
                Debug.LogError("Failed to generate grid layout");
                yield break;
            }
            
            var validationResult = playHandler.ValidateGridLayout(gridLayout, gridHandler.GridShape, minPossibleMatches, checkImmediateMatches: true);
            
            if (!validationResult.isValid)
            {
                if (validationResult.immediateMatches > 0)
                {
                    Debug.Log($"Too many immediate matches found in grid ({validationResult.immediateMatches}), retrying (attempt {retryCount}/{maxAttemptsToRecheckMatches})");
                }
                else if (validationResult.possibleMatches < minPossibleMatches)
                {
                    Debug.Log($"Not enough possible matches ({validationResult.possibleMatches}/{minPossibleMatches}), retrying (attempt {retryCount}/{maxAttemptsToRecheckMatches})");
                }
                continue; 
            }
            
            // Debug.Log($"Grid validated successfully with {validationResult.possibleMatches} possible matches");
            yield return playHandler.SpawnGridLayout(gridLayout, true);
            playHandler.CanInteract = true;
            populatingGrid = false;
            yield break;
        }
    
        Debug.LogError($"Failed to create valid grid after {maxAttemptsToRecheckMatches} attempts");
    }
    
    [Button(ButtonPlayMode.OnlyWhenPlaying)]
    private void ForceCompleteLevel()
    {
        if (levelComplete || populatingGrid) return;
        
        StartCoroutine(CompleteLevel());
    }
}