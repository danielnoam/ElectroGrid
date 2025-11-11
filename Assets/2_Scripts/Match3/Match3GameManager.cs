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
    
    [Header("References")]
    [SerializeField] private Match3GridHandler gridHandler;
    [SerializeField] private Match3PlayHandler playHandler;
    [SerializeField] private Match3SelectionIndicator selectionIndicator;
    [SerializeField] private SOGridShape defaultGridShape;
    
    [Separator]
    [SerializeField, ReadOnly] private SOMatch3Level currentLevel;
    [SerializeField, ReadOnly] private bool levelComplete;
    [SerializeField, ReadOnly] private bool populatingGrid;


    private Match3LevelData _currentLevelData;
    public SOGridShape GridShape => currentLevel ? currentLevel.GridShape : defaultGridShape;
    
    
    public int MaxGuaranteedMatchAttempts => mxGuaranteedMatchAttempts;
    public int MinMatchCount => minMatchCount;
    public int MaxAttemptsToRecheckMatches => maxAttemptsToRecheckMatches;
    public event Action<Match3LevelData> LevelStarted;
    public event Action<Match3LevelData> LevelComplete;
    public event Action<Match3LevelData> LevelFailed;

    
    

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
        UpdateLoseConditions();
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
    
    public void RestartLevel()
    {
        StartNewGame();
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
        
        _currentLevelData = new Match3LevelData(currentLevel);
        CreateGrid();
        
        LevelStarted?.Invoke(_currentLevelData);
    }



    private void CheckObjectives()
    {
        if (levelComplete || populatingGrid || _currentLevelData == null) return;

        if (_currentLevelData.IsObjectivesComplete())
        {
            CompleteLevel();
        }
    }

    private void CheckLoseConditions()
    {
        if (levelComplete || populatingGrid || _currentLevelData == null) return;
        
        if (_currentLevelData.IsLostCondition())
        {
            FailLevel();
        }

    }
    
    public void NotifyMatchesWhereMade(List<Match3Tile> matches)
    {
        _currentLevelData?.OnMatchesMade(matches);
    }
    
    private void NotifyAMoveWasMade()
    {
        _currentLevelData?.OnMoveMade();
    }
    
    private void UpdateLoseConditions()
    {
        if (levelComplete || !playHandler.CanInteract || _currentLevelData == null) return;
        
        
        foreach (var condition in _currentLevelData.CurrentLoseConditions)
        {
            condition?.UpdateCondition(Time.deltaTime);
        }
    }

    private void CompleteLevel()
    {
        levelComplete = true;
        playHandler.CanInteract = false;
        LevelComplete?.Invoke(_currentLevelData);
    }

    private void FailLevel()
    {
        levelComplete = true;
        playHandler.CanInteract = false;
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
    
        // Swap items
        yield return StartCoroutine(playHandler.SwapObjects(posA, posB));
        
        // Notify lose conditions that a move was made
        NotifyAMoveWasMade();
    
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
        NotifyMatchesWhereMade(allMatches);
    
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
        
            
    [Button(ButtonPlayMode.OnlyWhenPlaying)]
    private void ForceCompleteLevel()
    {
        if (levelComplete || populatingGrid) return;
        
        CompleteLevel();
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




    

}