using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DNExtensions;
using DNExtensions.Button;
using UnityEngine.InputSystem;

public class Match3GameManager : MonoBehaviour
{
    public static Match3GameManager Instance { get; private set; }


    [Header("Settings")]
    [Tooltip("Max attempts to recheck for matches when populating the grid")]
    [SerializeField] private int maxAttemptsToRecheckMatches = 50;
    [Tooltip("Max possible matches allowed in grid when populating")]
    [SerializeField] private int maxImmediateMatches = 3;
    [Tooltip("Minimum possible matches required in grid")]
    [SerializeField] private int minPossibleMatches = 3;
    [Tooltip("Duration to populate the grid with match objects")]
    [SerializeField, Min(1)] private float populationDuration = 1f;
    [SerializeField] private SOMatch3Level level;
    
    
    [Header("References")]
    [SerializeField] private Tile tilePrefab;
    [SerializeField] private MatchObject matchObjectPrefab;
    [SerializeField] private Transform matchObjectsParent;
    [SerializeField] private Transform tilesParent;
    [SerializeField] private MouseIndicator mouseIndicator;
    [SerializeField] private Match3InputReader inputReader;
    [SerializeField] private SOGridShape defaultGridShape;
    
    [Separator]
    [SerializeField, ReadOnly] private bool canSelectTiles;
    [SerializeField, ReadOnly] private int movesLeft;
    [SerializeField, ReadOnly] private Tile hoveredTile;
    [SerializeField, ReadOnly] private Tile selectedTile;
    [SerializeField, ReadOnly] private MatchObject heldMatchObject;


    private SOGridShape GridShape => level ? level.GridShape : defaultGridShape;
    
    
    private readonly Dictionary<Vector2Int, Tile> _tiles = new Dictionary<Vector2Int, Tile>();
    private Camera _camera;
    
    
    private const int MinMatchCount = 3;
    private const float SwapDuration = 0.3f;
    private const float MatchHandleDelay = 0.1f;
    private const float MoveObjectsDelay = 0.1f;
    private const int MaxGuaranteedMatchAttempts = 100;
    
    

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
        
        _camera = Camera.main;
    }

    private void Start()
    {
        StartNewGame();
    }

    private void OnEnable()
    {
        inputReader.OnSelect += OnSelect;
        inputReader.OnSwipe += OnSwipe;
    }
    
    private void OnDisable()
    {
        inputReader.OnSelect -= OnSelect;
        inputReader.OnSwipe -= OnSwipe;
    }

    private void OnSwipe(Vector2 direction)
    {
        if (!canSelectTiles || !heldMatchObject) return;

        Vector2Int gridDirection;

        float absX = Mathf.Abs(direction.x);
        float absY = Mathf.Abs(direction.y);

        if (absX > absY)
        {
            if (absY > absX * inputReader.SwipeDeadzone)
            {
                ReleaseObject(false);
                return;
            }
            gridDirection = direction.x > 0 ? Vector2Int.left : Vector2Int.right;
        }
        else
        {
            if (absX > absY * inputReader.SwipeDeadzone)
            {
                ReleaseObject(false);
                return;
            }
            gridDirection = direction.y > 0 ? Vector2Int.down : Vector2Int.up;
        }

        var newTilePos = selectedTile.GridPosition + gridDirection;
        var newTile = GetTile(newTilePos);

        TrySwapHeldObject(newTile);
    }

    private void OnSelect(InputAction.CallbackContext callbackContext)
    {
        if (!canSelectTiles) return;

        if (!heldMatchObject && callbackContext.started)
        {
            Vector3 worldPos = _camera.ScreenToWorldPoint(inputReader.MousePosition);
            worldPos.z = 0;
    
            var gridPos = GridShape.Grid.GetPositionInGird(worldPos);
            var tile = GetTile(gridPos);
        
        
            SelectObjectInTile(tile);

        }            
        else if (heldMatchObject && callbackContext.canceled)
        {
            ReleaseObject(false);
        }
    }
    
    
    private void Update()
    {
        UpdateHoveredTile();
    }


    #region Game Managemenet
    
    [Button(ButtonPlayMode.OnlyWhenPlaying)]
    private void StartNewGame()
    {
        if (!level) return;

        movesLeft = level.AllowedMoves;
        CreateGird();
    }

    private void CheckMovesLeft()
    {
        if (movesLeft <= 0)
        {
            canSelectTiles = false;
            Debug.Log("No more moves left");
        }
    }
    
    #endregion
    

    #region Objects Creation
    

    private IEnumerator PopulateGrid()
    {
        if (!level)
        {
            Debug.LogError("Level is not set, Cannot populate grid");
            yield break;
        }
        
        ReleaseObject(false);

        int totalActiveTiles = 0;
        foreach (var kvp in _tiles)
        {
            var tile = kvp.Value;
            if (tile.IsActive)
            {
                totalActiveTiles += 1;
            }
        }
        
        if (totalActiveTiles == 0)
        {
            canSelectTiles = false;
            yield break;
        }

        // Get grid dimensions
        int maxX = 0, maxY = 0;
        foreach (var pos in _tiles.Keys)
        {
            if (pos.x > maxX) maxX = pos.x;
            if (pos.y > maxY) maxY = pos.y;
        }
        
        // Create a dictionary of tiles that need to be populated
        var tilesToPopulate = new Dictionary<Tile, SOItemData>();
        for (int y = maxY; y >= 0; y--)
        {
            for (int x = 0; x <= maxX; x++)
            {
                if (_tiles.TryGetValue(new Vector2Int(x, y), out var tile) && !tile.HasObject)
                {
                    tilesToPopulate.Add(tile, null);
                }
            }
        }
        
        // Step 1: Identify positions where we'll force possible matches
        List<(Vector2Int posA, Vector2Int posB)> guaranteedMatchPairs = CreateGuaranteedMatchPositions(minPossibleMatches);
        
        // Step 2: Assign random items to tiles, ensuring no immediate matches
        // BUT respect the guaranteed match pairs
        foreach (var tileItemPair in tilesToPopulate.ToList())
        {
            // Check if this tile is part of a guaranteed match pair
            SOItemData forcedItem = GetForcedItemForGuaranteedMatch(tileItemPair.Key.GridPosition, guaranteedMatchPairs, tilesToPopulate);
            
            if (forcedItem != null)
            {
                // This tile is part of a guaranteed match setup
                tilesToPopulate[tileItemPair.Key] = forcedItem;
            }
            else
            {
                // Normal random assignment avoiding immediate matches
                SOItemData selectedItem;
                int attempts = 0;

                do
                {
                    selectedItem = level.MatchObjects.GetRandomItem();
                    attempts++;
            
                    if (attempts >= maxAttemptsToRecheckMatches)
                    {
                        Debug.Log("Could not find non-matching item");
                        break;
                    }
                } 
                while (WouldCreateMatchDuringPopulation(tileItemPair.Key.GridPosition, selectedItem, tilesToPopulate));

                tilesToPopulate[tileItemPair.Key] = selectedItem;
            }
        }

        // Create objects
        foreach (var tileObjectMatch in tilesToPopulate)
        {
            CreateMatchObject(tileObjectMatch.Value, tileObjectMatch.Key);
            yield return new WaitForSeconds(populationDuration / totalActiveTiles);
        }
        
        // Validate
        var immediateMatchesInGrid = FindImmediateMatches();
        if (immediateMatchesInGrid.Count > maxImmediateMatches)
        {
            Debug.Log($"Too many immediate matches found in grid ({immediateMatchesInGrid.Count})");
            CreateGird();
            yield break;
        }
        
        var possibleMatchesInGrid = FindPossibleMatches();
        if (possibleMatchesInGrid.Count < minPossibleMatches)
        {
            Debug.Log($"Failed to create enough possible matches ({possibleMatchesInGrid.Count}/{minPossibleMatches})");
            CreateGird();
            yield break;
        }
        
        Debug.Log($"Grid populated successfully with {possibleMatchesInGrid.Count} possible matches");
        canSelectTiles = true;
    }
    
    private MatchObject CreateMatchObject(SOItemData itemData, Tile tile)
    {
        if (!IsValidTile(tile)) return null;


        var topMostTile = GetTile(new Vector2Int(tile.GridPosition.x, 0));
        
        var spawnPosition = new Vector3(tile.transform.position.x,
            topMostTile.transform.position.y + topMostTile.transform.localScale.y,
            tile.transform.position.z);
        
        var item = Instantiate(matchObjectPrefab, spawnPosition, Quaternion.identity, matchObjectsParent);
        
        item.Initialize(itemData);
        tile.SetCurrentItem(item);
        item.SetCurrentTile(tile);
        
        return item;
    }


    #endregion


    #region Grid Setup
    



    private Tile CreateTile(Vector3 position, Vector2Int gridPos, bool isActive)
    {
        var tile = Instantiate(tilePrefab, position, Quaternion.identity, tilesParent);
        tile.Initialize(gridPos, isActive);
        return tile;
    }
    

    private void CreateGird()
    {
        if (!GridShape || !tilePrefab || !matchObjectPrefab) return;
        
        canSelectTiles = false;
        DestroyGrid();
        
        var grid = GridShape.Grid;
        
        for (int x = 0; x < grid.Width; x++)
        {
            for (int y = 0; y < grid.Height; y++)
            {

                Vector2Int tileGridPosition = new Vector2Int(x, y);
                Vector3 tileWorldPosition = grid.GetTileWorldPosition(x,y);
                bool tileState = grid.IsTileActive(x, y);

                var tile = CreateTile(tileWorldPosition, tileGridPosition, tileState);
                _tiles.Add(tileGridPosition, tile);
            }
        }
        
        StartCoroutine(PopulateGrid());
    }
    


    private void DestroyGrid()
    {
        foreach (var tile in _tiles.Values)
        {
            if (tile != null)
            {
#if UNITY_EDITOR
                DestroyImmediate(tile.gameObject);
#else
                Destroy(tile.gameObject);
#endif
            }
        }
        _tiles.Clear();
    
#if UNITY_EDITOR
        while (matchObjectsParent.childCount > 0)
        {
            DestroyImmediate(matchObjectsParent.GetChild(0).gameObject);
        }
#else
        foreach (Transform child in matchObjectsParent)
        {
            Destroy(child.gameObject);
        }
#endif
        
#if UNITY_EDITOR
        while (tilesParent.childCount > 0)
        {
            DestroyImmediate(tilesParent.GetChild(0).gameObject);
        }
#else
        foreach (Transform child in tilesParent)
        {
            Destroy(child.gameObject);
        }
#endif
    }


    #endregion


    #region Selection & Swapping

    private void UpdateHoveredTile()
    {
        if (!canSelectTiles) return;
        
        Vector2 mousePos = inputReader.MousePosition;
        Vector2 worldPos = _camera.ScreenToWorldPoint(mousePos);
    
        RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);
    
        
        if (inputReader.IsCurrentDeviceTouchscreen) return;
        Tile newHoveredTile = null;
        if (hit.collider)
        {
            newHoveredTile = hit.collider.GetComponent<Tile>();
        }
        
        if (newHoveredTile != hoveredTile)
        {
            hoveredTile?.SetHovered(false);
        
            newHoveredTile?.SetHovered(true);
        
            hoveredTile = newHoveredTile;
        }
    }
    
    private void TrySwapHeldObject(Tile tile)
    {
        if (!IsValidTile(tile))
        {
            ReleaseObject(true);
            return;
        } 

        if (tile == selectedTile)
        {
            ReleaseObject(false);
            return;
        }

        if (IsPositionsTouching(tile.GridPosition, selectedTile.GridPosition))
        {
            StartCoroutine(RunGameLogic(tile.GridPosition, selectedTile.GridPosition));
            movesLeft -= 1;
        }
        else
        {
            ReleaseObject(true);
        }
    }
    
    private void SelectObjectInTile(Tile tile)
    {
        if (!IsValidTile(tile) || !tile.HasObject) return;
        
        selectedTile = tile;
        heldMatchObject = selectedTile.CurrentMatchObject;
        heldMatchObject.SetHeld(true);
        mouseIndicator?.EnableIndicator(selectedTile);
    }
    
    private void ReleaseObject(bool animateReturn)
    {
        selectedTile?.SetSelected(false);
        selectedTile = null;
        heldMatchObject?.SetHeld(false);
        heldMatchObject = null;
        mouseIndicator?.DisableIndicator(animateReturn);
    }
    
    private IEnumerator RunGameLogic(Vector2Int posA, Vector2Int posB)
    {
        canSelectTiles = false;
        hoveredTile?.SetHovered(false);
        
        // Swap items
        yield return StartCoroutine(SwapObjects(posA, posB));
        
        // Check for matches only in nearby tiles of swapped objects
        List<Tile> matchesWithTile = new List<Tile>();
        matchesWithTile.AddRange(FindMatchesWithTile(GetTile(posA)));
        matchesWithTile.AddRange(FindMatchesWithTile(GetTile(posB)));
        matchesWithTile = matchesWithTile.Distinct().ToList();
        if (matchesWithTile.Count == 0)
        {
            yield return StartCoroutine(SwapObjects(posB, posA));
            canSelectTiles = true;
            CheckMovesLeft();
            yield break;
        }
        yield return StartCoroutine(HandleMatches(matchesWithTile));
        
        
        // Make objects fall
        yield return StartCoroutine(MoveObjects());
        
        
        // Repopulate grid
        yield return StartCoroutine(PopulateGrid());
        CheckMovesLeft();

    }
    
    private IEnumerator SwapObjects(Vector2Int posA, Vector2Int posB)
    {
        var tileA = GetTile(posA);
        var tileB = GetTile(posB);
        
        if (!IsValidTile(tileA) || !IsValidTile(tileB)) yield return null;
        
        var itemA = tileA.CurrentMatchObject;
        var itemB = tileB.CurrentMatchObject;
        
        tileA.SetCurrentItem(itemB);
        tileB.SetCurrentItem(itemA);
        
        itemA.SetCurrentTile(tileB);
        itemB.SetCurrentTile(tileA);

        ReleaseObject(false);
        yield return new WaitForSeconds(SwapDuration);
    }
    

    private IEnumerator HandleMatches(List<Tile> matches)
    {
        foreach (var match in matches)
        {
            match.CurrentMatchObject.MatchFound();
            match.SetCurrentItem(null);
            yield return new WaitForSeconds(MatchHandleDelay);
        }
    }

    private IEnumerator MoveObjects()
    {
        for (var x = 0; x < GridShape.Grid.Width; x++)
        {
            for (var y = GridShape.Grid.Height - 1; y >= 0; y--)
            {
                var tile = GetTile(new Vector2Int(x, y));
                if (!tile.HasObject && tile.IsActive)
                {
                    for (var i = y - 1; i >= 0; i--)
                    {
                        var aboveTile = GetTile(new Vector2Int(x, i));
                        if (aboveTile.HasObject)
                        {
                            var matchObject = aboveTile.CurrentMatchObject;
                            aboveTile.SetCurrentItem(null);
                            tile.SetCurrentItem(matchObject);
                            matchObject.SetCurrentTile(tile);
                            yield return new WaitForSeconds(MoveObjectsDelay);
                            break;
                        }
                    }
                }
            }
        }
    }



    

    #endregion


    #region Helper

    private Tile GetTile(Vector2Int position)
    {
        _tiles.TryGetValue(position, out Tile tile);
        return tile;
    }
    
    
    private bool IsValidTile(Tile tile)
    {
        return tile && tile.IsActive;
    }
    
    private bool IsPositionsTouching(Vector2Int positionA, Vector2Int positionB)
    {
        int deltaX = Mathf.Abs(positionA.x - positionB.x);
        int deltaY = Mathf.Abs(positionA.y - positionB.y);
        
        return (deltaX == 1 && deltaY == 0) || (deltaX == 0 && deltaY == 1);
    }
    
        private List<Tile> FindImmediateMatches()
    {
        HashSet<Tile> matches = new HashSet<Tile>();

        // Find horizontal matches in grid
        for (var y = 0; y < GridShape.Grid.Height; y++)
        {
            for (var x = 0; x < GridShape.Grid.Width - 2; x++)
            {
                var tile1 = GetTile(new Vector2Int(x, y));
                var tile2 = GetTile(new Vector2Int(x + 1, y));
                var tile3 = GetTile(new Vector2Int(x + 2, y));

                if (!tile1 || !tile2 || !tile3 || !tile1.HasObject || !tile2.HasObject || !tile3.HasObject) continue;

                if (
                    tile1.CurrentMatchObject.ItemData == tile2.CurrentMatchObject.ItemData 
                    && tile2.CurrentMatchObject.ItemData == tile3.CurrentMatchObject.ItemData)
                {
                    matches.Add(tile1);
                    matches.Add(tile2);
                    matches.Add(tile3);
                }
            }
        }

        // Find vertical matches in grid
        for (var x = 0; x < GridShape.Grid.Width; x++)
        {
            for (var y = 0; y < GridShape.Grid.Height - 2; y++)
            {
                var tile1 = GetTile(new Vector2Int(x, y));
                var tile2 = GetTile(new Vector2Int(x, y + 1));
                var tile3 = GetTile(new Vector2Int(x, y + 2));
                
                
                if (!tile1 || !tile2 || !tile3 || !tile1.HasObject || !tile2.HasObject || !tile3.HasObject) continue;
                
                if (
                    tile1.CurrentMatchObject.ItemData == tile2.CurrentMatchObject.ItemData 
                    && tile2.CurrentMatchObject.ItemData == tile3.CurrentMatchObject.ItemData)
                {
                    matches.Add(tile1);
                    matches.Add(tile2);
                    matches.Add(tile3);
                }


            }
        }
        
        return new List<Tile>(matches);
    }
    
    private List<Tile> FindMatchesWithTile(Tile tile)
    {
        if (!tile || !tile.HasObject) return new List<Tile>();
        
        HashSet<Tile> matches = new HashSet<Tile>();
        Vector2Int pos = tile.GridPosition;
        SOItemData itemData = tile.CurrentMatchObject.ItemData;
        
        // Check horizontal match (left and right from the tile)
        List<Tile> horizontalMatches = new List<Tile> { tile };
        for (int x = pos.x - 1; x >= 0; x--)
        {
            var checkTile = GetTile(new Vector2Int(x, pos.y));
            if (!IsValidTile(checkTile) || !checkTile.HasObject || 
                checkTile.CurrentMatchObject.ItemData != itemData)
                break;
            horizontalMatches.Add(checkTile);
        }
        for (int x = pos.x + 1; x < GridShape.Grid.Width; x++)
        {
            var checkTile = GetTile(new Vector2Int(x, pos.y));
            if (!IsValidTile(checkTile) || !checkTile.HasObject || 
                checkTile.CurrentMatchObject.ItemData != itemData)
                break;
            horizontalMatches.Add(checkTile);
        }
        
        if (horizontalMatches.Count >= 3)
        {
            foreach (var match in horizontalMatches)
                matches.Add(match);
        }
        
        // Check vertical match (up and down from the tile)
        List<Tile> verticalMatches = new List<Tile> { tile };
        for (int y = pos.y - 1; y >= 0; y--)
        {
            var checkTile = GetTile(new Vector2Int(pos.x, y));
            if (!IsValidTile(checkTile) || !checkTile.HasObject || 
                checkTile.CurrentMatchObject.ItemData != itemData)
                break;
            verticalMatches.Add(checkTile);
        }
        for (int y = pos.y + 1; y < GridShape.Grid.Height; y++)
        {
            var checkTile = GetTile(new Vector2Int(pos.x, y));
            if (!IsValidTile(checkTile) || !checkTile.HasObject || 
                checkTile.CurrentMatchObject.ItemData != itemData)
                break;
            verticalMatches.Add(checkTile);
        }
        
        if (verticalMatches.Count >= MinMatchCount)
        {
            foreach (var match in verticalMatches)
                matches.Add(match);
        }
        
        return new List<Tile>(matches);
    }
    
    
    
    private List<Tile> FindPossibleMatches()
    {
        HashSet<Tile> possibleMatchTiles = new HashSet<Tile>();
        HashSet<(Vector2Int, Vector2Int)> checkedPairs = new HashSet<(Vector2Int, Vector2Int)>();

        foreach (var tile in _tiles.Values)
        {
            if (!IsValidTile(tile) || !tile.HasObject) continue;
    
            Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
    
            foreach (var direction in directions)
            {
                var neighborPos = tile.GridPosition + direction;
                var neighborTile = GetTile(neighborPos);
        
                if (!IsValidTile(neighborTile) || !neighborTile.HasObject) continue;
        
                // Avoid checking the same pair twice
                var pair = tile.GridPosition.x < neighborPos.x || (tile.GridPosition.x == neighborPos.x && tile.GridPosition.y < neighborPos.y)
                    ? (tile.GridPosition, neighborPos)
                    : (neighborPos, tile.GridPosition);
            
                if (checkedPairs.Contains(pair)) continue;
                checkedPairs.Add(pair);
        
                // Check if swapping these would create a match
                var currentItemData = tile.CurrentMatchObject.ItemData;
                var neighborItemData = neighborTile.CurrentMatchObject.ItemData;
        
                if (WouldCreateMatch(neighborPos, currentItemData) || WouldCreateMatch(tile.GridPosition, neighborItemData))
                {
                    possibleMatchTiles.Add(tile);
                    possibleMatchTiles.Add(neighborTile);
                }
            }
        }

        return new List<Tile>(possibleMatchTiles);
    }

    private bool WouldCreateMatch(Vector2Int position, SOItemData itemData)
    {
        int horizontalCount = 1;
        for (int x = position.x - 1; x >= 0; x--)
        {
            var checkTile = GetTile(new Vector2Int(x, position.y));
            if (!IsValidTile(checkTile) || !checkTile.HasObject || 
                checkTile.CurrentMatchObject.ItemData != itemData)
                break;
            horizontalCount++;
        }
        for (int x = position.x + 1; x < GridShape.Grid.Width; x++)
        {
            var checkTile = GetTile(new Vector2Int(x, position.y));
            if (!IsValidTile(checkTile) || !checkTile.HasObject || 
                checkTile.CurrentMatchObject.ItemData != itemData)
                break;
            horizontalCount++;
        }
        
        if (horizontalCount >= MinMatchCount) return true;
        int verticalCount = 1;
        for (int y = position.y - 1; y >= 0; y--)
        {
            var checkTile = GetTile(new Vector2Int(position.x, y));
            if (!IsValidTile(checkTile) || !checkTile.HasObject || 
                checkTile.CurrentMatchObject.ItemData != itemData)
                break;
            verticalCount++;
        }
        for (int y = position.y + 1; y < GridShape.Grid.Height; y++)
        {
            var checkTile = GetTile(new Vector2Int(position.x, y));
            if (!IsValidTile(checkTile) || !checkTile.HasObject || 
                checkTile.CurrentMatchObject.ItemData != itemData)
                break;
            verticalCount++;
        }
        
        return verticalCount >= MinMatchCount;
    }
    
    private bool WouldCreateMatchDuringPopulation(Vector2Int position, SOItemData itemData, Dictionary<Tile, SOItemData> tilesToPopulate)
    {
        // Check horizontal
        int horizontalCount = 1;
        for (int x = position.x - 1; x >= 0; x--)
        {
            var checkTile = GetTile(new Vector2Int(x, position.y));
            if (!IsValidTile(checkTile)) break;
            
            // Check if tile already has an object OR is in the populate queue
            SOItemData checkItemData = null;
            if (checkTile.HasObject)
                checkItemData = checkTile.CurrentMatchObject.ItemData;
            else if (tilesToPopulate.ContainsKey(checkTile))
                checkItemData = tilesToPopulate[checkTile];
            
            if (checkItemData == null || checkItemData != itemData)
                break;
                
            horizontalCount++;
        }
        for (int x = position.x + 1; x < GridShape.Grid.Width; x++)
        {
            var checkTile = GetTile(new Vector2Int(x, position.y));
            if (!IsValidTile(checkTile)) break;
            
            SOItemData checkItemData = null;
            if (checkTile.HasObject)
                checkItemData = checkTile.CurrentMatchObject.ItemData;
            else if (tilesToPopulate.ContainsKey(checkTile))
                checkItemData = tilesToPopulate[checkTile];
            
            if (checkItemData == null || checkItemData != itemData)
                break;
                
            horizontalCount++;
        }
        
        if (horizontalCount >= MinMatchCount) return true;
        
        // Check vertical (same pattern)
        int verticalCount = 1;
        for (int y = position.y - 1; y >= 0; y--)
        {
            var checkTile = GetTile(new Vector2Int(position.x, y));
            if (!IsValidTile(checkTile)) break;
            
            SOItemData checkItemData = null;
            if (checkTile.HasObject)
                checkItemData = checkTile.CurrentMatchObject.ItemData;
            else if (tilesToPopulate.ContainsKey(checkTile))
                checkItemData = tilesToPopulate[checkTile];
            
            if (checkItemData == null || checkItemData != itemData)
                break;
                
            verticalCount++;
        }
        for (int y = position.y + 1; y < GridShape.Grid.Height; y++)
        {
            var checkTile = GetTile(new Vector2Int(position.x, y));
            if (!IsValidTile(checkTile)) break;
            
            SOItemData checkItemData = null;
            if (checkTile.HasObject)
                checkItemData = checkTile.CurrentMatchObject.ItemData;
            else if (tilesToPopulate.ContainsKey(checkTile))
                checkItemData = tilesToPopulate[checkTile];
            
            if (checkItemData == null || checkItemData != itemData)
                break;
                
            verticalCount++;
        }
        
        return verticalCount >= MinMatchCount;
    }
    
    private List<(Vector2Int posA, Vector2Int posB)> CreateGuaranteedMatchPositions(int count)
    {
        List<(Vector2Int, Vector2Int)> pairs = new List<(Vector2Int, Vector2Int)>();
        List<Vector2Int> usedPositions = new List<Vector2Int>();
        
        int attempts = 0;
        while (pairs.Count < count && attempts < MaxGuaranteedMatchAttempts)
        {
            attempts++;
            
            // Find a random tile
            var randomTile = GetRandomValidTile();
            if (randomTile == null) break;
            
            if (usedPositions.Contains(randomTile.GridPosition)) continue;
            
            // Find an adjacent tile
            Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            foreach (var dir in directions.OrderBy(x => UnityEngine.Random.value))
            {
                var neighborPos = randomTile.GridPosition + dir;
                var neighborTile = GetTile(neighborPos);
                
                if (!IsValidTile(neighborTile) || usedPositions.Contains(neighborPos)) continue;
                
                pairs.Add((randomTile.GridPosition, neighborPos));
                usedPositions.Add(randomTile.GridPosition);
                usedPositions.Add(neighborPos);
                break;
            }
        }
        
        return pairs;
    }

    private SOItemData GetForcedItemForGuaranteedMatch(Vector2Int position, List<(Vector2Int posA, Vector2Int posB)> guaranteedPairs, Dictionary<Tile, SOItemData> tilesToPopulate)
    {
        foreach (var pair in guaranteedPairs)
        {
            if (pair.posA == position)
            {
                // This is position A - find a third tile to create the match pattern
                // Pattern: A-B where if we swap A with neighbor, B matches with something
                var neighborPos = pair.posB;
                
                // Look for a tile 2 steps away from B in the same direction
                Vector2Int direction = neighborPos - position;
                Vector2Int thirdPos = neighborPos + direction;
                var thirdTile = GetTile(thirdPos);
                
                if (IsValidTile(thirdTile))
                {
                    // Create pattern: A-B-C where A and C are the same
                    var itemData = level.MatchObjects.GetRandomItem();
                    
                    // Make sure C gets the same item
                    if (tilesToPopulate.ContainsKey(thirdTile))
                    {
                        tilesToPopulate[thirdTile] = itemData;
                    }
                    
                    return itemData;
                }
            }
            else if (pair.posB == position)
            {
                // This is position B - it should get a different item
                // Find what A is getting and pick something different
                var tileA = GetTile(pair.posA);
                if (tilesToPopulate.ContainsKey(tileA) && tilesToPopulate[tileA] != null)
                {
                    // Pick a different item
                    SOItemData differentItem;
                    do
                    {
                        differentItem = level.MatchObjects.GetRandomItem();
                    } while (differentItem == tilesToPopulate[tileA] && level.MatchObjects.Count > 1);
                    
                    return differentItem;
                }
            }
        }
        
        return null;
    }

    private Tile GetRandomValidTile()
    {
        var validTiles = _tiles.Values.Where(t => IsValidTile(t)).ToList();
        return validTiles.Count > 0 ? validTiles[UnityEngine.Random.Range(0, validTiles.Count)] : null;
    }
    

    #endregion

    
    
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        GridShape?.Grid?.DrawGrid();
    }


#endif
}