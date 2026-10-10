using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DNExtensions.Utilities;
using DNExtensions.Utilities.Button;
using System.Linq;

public class Match3GameManager : MonoBehaviour
{
    public static Match3GameManager Instance { get; private set; }

    private const int GenerationAttemptsPerFrame = 8;

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
    [Tooltip("Times the grid is reshuffled when no moves are left before giving up")]
    [SerializeField] private int maxReshuffleAttempts = 5;
    
    [Header("References")]
    [SerializeField] private Match3GridHandler gridHandler;
    [SerializeField] private Match3PlayHandler playHandler;
    [SerializeField] private Match3SelectionIndicator selectionIndicator;
    [SerializeField] private SOMatch3Level overrideLevel;
    [SerializeField] private SOSurvivalMode overrideSurvival;

    [Header("Combo")]
    [SerializeField] private Match3ComboSettings comboSettings = new Match3ComboSettings();

    [Separator]
    [SerializeField, ReadOnly] private SOMatch3Level currentLevel;
    [SerializeField, ReadOnly] private SOSurvivalMode currentSurvival;
    [SerializeField, ReadOnly] private int survivalScore;
    [SerializeField, ReadOnly] private bool levelComplete;
    [SerializeField, ReadOnly] private bool finishedObjectives;
    [SerializeField, ReadOnly] private bool populatingGrid;
    [SerializeField, ReadOnly] private int comboCount;
    [SerializeField, ReadOnly] private float comboFill;
    [SerializeField, ReadOnly] private float comboTimeLeft;

    private Match3LevelData _currentLevelData;

    /// <summary>The current run of quick matches, read by the combo bar, the full-bar ability and Survival scoring.</summary>
    public Match3Combo Combo => _combo ??= new Match3Combo(comboSettings);
    private Match3Combo _combo;
    public Match3LevelData CurrentLevelData => _currentLevelData;
    public bool IsSurvival => currentSurvival;
    public Match3GridHandler GridHandler => gridHandler;
    public Match3PlayHandler PlayHandler => playHandler;
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
    public event Action<Match3TileObjectType> BonusSpawned;
    
    

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
        Combo.Filled += OnComboFilled;
        Combo.StepAdded += OnComboStep;
        StartNewGame();
    }

    private void OnDestroy()
    {
        if (_combo == null) return;

        _combo.Filled -= OnComboFilled;
        _combo.StepAdded -= OnComboStep;
    }

    private void OnComboFilled()
    {
        _currentLevelData?.OnComboFilled(comboSettings.fullBarSeconds, comboSettings.fullBarMoves);
        if (!levelComplete) _currentLevelData?.Survival?.OnComboFilled();
    }

    private void OnComboStep(int count)
    {
        _currentLevelData?.Survival?.OnComboStep(count);
    }

    private void Update()
    {
        UpdateLevelTime();
        UpdateLoseConditions();
        CheckObjectives();
        CheckLoseConditions();
        UpdateCombo();
    }

    private void UpdateCombo()
    {
        if (!levelComplete) Combo.Tick(Time.deltaTime);

        comboCount = Combo.Count;
        comboFill = Combo.Fill;
        comboTimeLeft = Combo.TimeLeft;
    }

    private void UpdateLevelTime()
    {
        if (levelComplete || _currentLevelData == null) return;

        _currentLevelData.TimeSpent += Time.deltaTime;
        _currentLevelData.Survival?.Tick(Time.deltaTime);
        survivalScore = _currentLevelData.Survival?.Score ?? 0;
    }

    public bool HasNextLevel()
    {
        if (IsSurvival) return false;

        var levels = GameManager.Instance ? GameManager.Instance.Match3Levels : null;
        if (levels == null || levels.Length == 0) return false;

        return Array.IndexOf(levels, currentLevel) + 1 < levels.Length;
    }

    public void SetNextLevel()
    {
        var levels = GameManager.Instance ? GameManager.Instance.Match3Levels : null;
        if (levels == null || levels.Length == 0) return;

        int nextIndex = Array.IndexOf(levels, currentLevel) + 1;
        if (nextIndex <= 0 || nextIndex >= levels.Length) return;

        currentLevel = levels[nextIndex];
        StartNewGame();
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
            if (overrideSurvival)
            {
                currentSurvival = overrideSurvival;
            }
            else if (overrideLevel)
            {
                currentLevel = overrideLevel;
            }
            else if (GameManager.Instance && GameManager.Instance.SurvivalSelected)
            {
                currentSurvival = GameManager.Instance.SurvivalMode;
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
        
        if (currentSurvival) currentLevel = currentSurvival.Board;
        
        if (!currentLevel)
        {
            Debug.LogError("No level assigned or found in level pool!");
            return;
        }

        levelComplete = false;
        finishedObjectives = false;
        populatingGrid = false;
        Combo.Clear();

        _currentLevelData = new Match3LevelData(currentLevel, currentSurvival);
        if (!IsSurvival) SaveManager.Instance?.SetLastPlayedLevel(currentLevel);
        StartCoroutine(InitialLevelSetup());
        
        FirebaseManager.Instance?.LogLevelStarted(_currentLevelData);
        
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
        if (!levelComplete) _currentLevelData?.Survival?.OnMatches(matches, minMatchCount, Combo.Count);
        MatchesMade?.Invoke(matches);
    }
    
    public void NotifyHelperObjectDestroyed(Match3HelperObject helper)
    {
        HelperDestroyed?.Invoke(helper);
        if (levelComplete) return;

        _currentLevelData?.OnHelperObjectDestroyed();
        _currentLevelData?.Survival?.OnPlusDestroyed(Combo.Count);
    }
    
    private void NotifyAMoveWasMade()
    {
        _currentLevelData?.OnMoveMade();
    }

    public void NotifyObstacleBroke(Match3ObstacleObject obstacle)
    {
        _currentLevelData?.OnObstacleBreak(obstacle);
        if (!levelComplete) _currentLevelData?.Survival?.OnDoubleStarBroken(Combo.Count);
    }
    
    public void NotifyBottomObjectReached(Match3BottomObject bottomObject)
    {
        _currentLevelData?.OnBottomObjectReached(bottomObject);
        if (!levelComplete) _currentLevelData?.Survival?.OnSquareStarReached(Combo.Count);
    }
    
    public void NotifyLineBreakMade(List<int> rows, List<int> columns, int piecesDestroyed)
    {
        if (!levelComplete) _currentLevelData?.Survival?.OnLineBreakPieces(piecesDestroyed, Combo.Count);
        FirebaseManager.Instance?.LogLineBreak();
        LineBreakMade?.Invoke(rows, columns);
    }
    

    /// <summary>
    /// Called when the player leaves a level before it ends, so abandonment shows up in the funnel.
    /// </summary>
    public void LogLevelQuit()
    {
        if (levelComplete || _currentLevelData == null) return;

        FirebaseManager.Instance?.LogLevelQuit(_currentLevelData);
    }

    private IEnumerator CompleteLevel()
    {
        levelComplete = true;
        playHandler.CanInteract = false;
        Combo.Clear();
        
        yield return new WaitForSeconds(0.1f);
        
        yield return StartCoroutine(playHandler.ClearObjects());
        
        yield return new WaitForSeconds(0.2f);
        
        FirebaseManager.Instance?.LogLevelCompleted(_currentLevelData);

        if (SaveManager.Instance && GameManager.Instance)
        {
            int levelIndex = Array.IndexOf(GameManager.Instance.Match3Levels, currentLevel);
            SaveManager.Instance.RecordLevelCompleted(currentLevel, _currentLevelData, levelIndex);
        }

        LevelComplete?.Invoke(_currentLevelData);
    }

    private IEnumerator FailLevel()
    {
        levelComplete = true;
        playHandler.CanInteract = false;
        Combo.Clear();
        
        yield return new WaitForSeconds(0.1f);
        
        yield return StartCoroutine(playHandler.ClearObjects());
        
        yield return new WaitForSeconds(0.2f);

        var survival = _currentLevelData.Survival;
        if (survival != null)
        {
            survival.PreviousBest = SaveManager.Instance ? SaveManager.Instance.SurvivalBestScore : 0;
            survival.IsNewBest = SaveManager.Instance && SaveManager.Instance.RecordSurvivalRun(survival.Score, _currentLevelData.TimeSpent, survival.BestCombo);
            FirebaseManager.Instance?.LogSurvivalEnded(_currentLevelData);
        }
        else
        {
            FirebaseManager.Instance?.LogLevelFailed(_currentLevelData);
        }

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

        // The combo countdown stops while this swap resolves, so its cascades can't run out the clock
        Combo.BeginResolve();
        Combo.AddSwapStep();

        // Must run before the matches are handled, objectives inspect the matched tiles while they still hold their objects
        NotifyMatchesWereMade(allMatches);

        yield return StartCoroutine(playHandler.HandleMatches(allMatches));
    
        yield return StartCoroutine(playHandler.MoveObjectsDown(gridHandler.GridShape));
    
        yield return StartCoroutine(playHandler.PopulateGrid(currentLevel, gridHandler.GridShape, minPossibleMatches, false));
        
        yield return StartCoroutine(playHandler.HandleMatchesAndRepopulate(currentLevel, gridHandler.GridShape, minPossibleMatches, Combo.AddCascadeWave));

        if (!levelComplete)
        {
            if (playHandler.FindPossibleMatches(gridHandler.GridShape).Count < minPossibleMatches)
            {
                yield return StartCoroutine(ReshuffleGrid());
            }

            playHandler.CanInteract = true;
            populatingGrid = false;
            Combo.EndResolve();
        }
    }

    private IEnumerator ReshuffleGrid()
    {
        for (int attempt = 0; attempt < maxReshuffleAttempts; attempt++)
        {
            yield return StartCoroutine(playHandler.ClearMatchableObjects());
            yield return StartCoroutine(playHandler.PopulateGrid(currentLevel, gridHandler.GridShape, minPossibleMatches, false));
            yield return StartCoroutine(playHandler.HandleMatchesAndRepopulate(currentLevel, gridHandler.GridShape, minPossibleMatches));

            if (levelComplete) yield break;

            if (playHandler.FindPossibleMatches(gridHandler.GridShape).Count >= minPossibleMatches) yield break;
        }

        Debug.LogError($"Grid still has no possible matches after {maxReshuffleAttempts} reshuffles");
    }
    
    private IEnumerator InitialLevelSetup()
    {
        if (!currentLevel) yield break;
        
        playHandler.CanInteract = false;
        gridHandler.CreateGrid(currentLevel, IsSurvival && currentSurvival.Bonuses.squareStarChance > 0f);
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
                // Generating and validating a layout is expensive, so spread long retry runs over several frames
                if (retryCount % GenerationAttemptsPerFrame == 0) yield return null;
                continue;
            }
            
            // Debug.Log($"Grid validated successfully with {validationResult.possibleMatches} possible matches");
            yield return playHandler.SpawnGridLayout(gridLayout, true);
            gridHandler.OpenCatchers();
            playHandler.CanInteract = true;
            populatingGrid = false;
            yield break;
        }
    
        Debug.LogError($"Failed to create valid grid after {maxAttemptsToRecheckMatches} attempts");
    }
    
    /// <summary>For a board refill in Survival: which new piece, if any, becomes a bonus Star instead.</summary>
    public (Match3Tile tile, Match3TileObjectType type) PickSurvivalBonus(ICollection<Match3Tile> refillTiles)
    {
        var survival = _currentLevelData?.Survival;
        if (survival == null || levelComplete) return (null, Match3TileObjectType.Matchable);

        int squareStars = 0;
        int doubleStars = 0;
        foreach (var tile in gridHandler.Tiles.Values)
        {
            if (!tile || !tile.CurrentMatch3Object) continue;
            if (tile.CurrentMatch3Object is Match3BottomObject) squareStars++;
            else if (tile.CurrentMatch3Object is Match3ObstacleObject) doubleStars++;
        }

        return survival.PickBonus(refillTiles, gridHandler, squareStars, doubleStars);
    }

    public void SpawnSurvivalBonus(Match3Tile tile, Match3TileObjectType type)
    {
        if (type == Match3TileObjectType.Bottom) gridHandler.CreateBottomObject(tile);
        else if (type == Match3TileObjectType.Obstacle) gridHandler.CreateObstacleObject(tile);
        else return;

        BonusSpawned?.Invoke(type);
    }

    [Button(ButtonPlayMode.OnlyWhenPlaying)]
    private void ForceEndRun()
    {
        if (levelComplete || populatingGrid || !IsSurvival) return;

        StartCoroutine(FailLevel());
    }
    
    [Button(ButtonPlayMode.OnlyWhenPlaying)]
    private void ForceCompleteLevel()
    {
        if (levelComplete || populatingGrid) return;
        
        StartCoroutine(CompleteLevel());
    }
}