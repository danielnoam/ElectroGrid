using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DNExtensions;
using UnityEngine;
using UnityEngine.InputSystem;

public class Match3PlayHandler : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Duration taken to populate grid")]
    [SerializeField, Min(0)] private float populationDuration = 1f;
    [Tooltip("Duration taken to swap two objects")]
    [SerializeField, Min(0)] private float objectSwapDuration = 0.3f;
    [Tooltip("Delay between popping matched objects")]
    [SerializeField, Min(0)] private float delayBetweenMatchPops = 0.075f;
    [Tooltip("Delay between moving objects downwards")]
    [SerializeField, Min(0)] private float delayBetweenObjectMovement = 0.1f;
    
    
    [Header("References")]
    [SerializeField] private Match3GameManager gameManager;
    [SerializeField] private Match3GridHandler gridHandler;
    [SerializeField] private Match3SelectionIndicator selectionIndicator;
    [SerializeField] private Match3InputReader inputReader;

    [Separator]
    [SerializeField, ReadOnly] private bool canInteract;
    [SerializeField, ReadOnly] private Match3Tile selectedMatch3Tile;
    [SerializeField, ReadOnly] private Match3Object heldMatch3Object;
    private Camera _camera;


    public bool CanInteract
    {
        get => canInteract;
        set => canInteract = value;
    }
    

    private void Awake()
    {
        _camera = Camera.main;
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
    

    #region Input Handling

    private void OnSwipe(Vector2 direction)
    {
        if (!canInteract || !heldMatch3Object) return;

        Vector2Int gridDirection;

        float absX = Mathf.Abs(direction.x);
        float absY = Mathf.Abs(direction.y);

        if (absX > absY)
        {
            if (absY > absX * inputReader.SwipeDeadzone)
            {
                ReleaseObject(true);
                return;
            }
            gridDirection = direction.x > 0 ? Vector2Int.right : Vector2Int.left;
        }
        else
        {
            if (absX > absY * inputReader.SwipeDeadzone)
            {
                ReleaseObject(true);
                return;
            }
            gridDirection = direction.y > 0 ? Vector2Int.down : Vector2Int.up;
        }

        var newTilePos = selectedMatch3Tile.GridPosition + gridDirection;
        var newTile = gridHandler.GetTile(newTilePos);

        TrySwapHeldObject(newTile);
    }

    private void OnSelect(InputAction.CallbackContext callbackContext)
    {
        if (!canInteract) return;

        if (!heldMatch3Object && callbackContext.started)
        {
            Vector3 worldPos = _camera.ScreenToWorldPoint(inputReader.MousePosition);
            worldPos.z = 0;
    
            var gridPos = gridHandler.GridShape.Grid.GetPositionInGird(worldPos);
            var tile = gridHandler.GetTile(gridPos);
        
            SelectObjectInTile(tile);
        }            
        else if (heldMatch3Object && callbackContext.canceled)
        {
            ReleaseObject(false);
        }
    }



    #endregion

    
    #region Selection & Swapping

    private void SelectObjectInTile(Match3Tile match3Tile)
    {
        if (!gridHandler.IsValidTile(match3Tile) || !match3Tile.HasObject) return;
        
        selectedMatch3Tile = match3Tile;
        heldMatch3Object = selectedMatch3Tile.CurrentMatch3Object;
        heldMatch3Object.SetHeld(true);
        selectionIndicator?.EnableIndicator(selectedMatch3Tile);
    }
    
    public void ReleaseObject(bool animateReturn)
    {
        selectedMatch3Tile?.SetSelected(false);
        selectedMatch3Tile = null;
        heldMatch3Object?.SetHeld(false);
        heldMatch3Object = null;
        selectionIndicator?.DisableIndicator(animateReturn);
    }

    private void TrySwapHeldObject(Match3Tile match3Tile)
    {
        if (!gridHandler.IsValidTile(match3Tile))
        {
            ReleaseObject(true);
            return;
        } 

        if (match3Tile == selectedMatch3Tile)
        {
            ReleaseObject(false);
            return;
        }

        if (IsPositionsTouching(match3Tile.GridPosition, selectedMatch3Tile.GridPosition))
        {
            gameManager.StartCoroutine(gameManager.RunGameLogic(match3Tile.GridPosition, selectedMatch3Tile.GridPosition));
        }
        else
        {
            ReleaseObject(true);
        }
    }

    public IEnumerator SwapObjects(Vector2Int posA, Vector2Int posB)
    {
        var tileA = gridHandler.GetTile(posA);
        var tileB = gridHandler.GetTile(posB);
        
        if (!gridHandler.IsValidTile(tileA) || !gridHandler.IsValidTile(tileB)) 
        {
            yield break;
        }
        
        var itemA = tileA.CurrentMatch3Object;
        var itemB = tileB.CurrentMatch3Object;
        
        tileA.SetCurrentItem(itemB);
        tileB.SetCurrentItem(itemA);
        
        itemA.SetCurrentTile(tileB);
        itemB.SetCurrentTile(tileA);

        ReleaseObject(false);
        yield return new WaitForSeconds(objectSwapDuration);
    }

    private bool IsPositionsTouching(Vector2Int positionA, Vector2Int positionB)
    {
        int deltaX = Mathf.Abs(positionA.x - positionB.x);
        int deltaY = Mathf.Abs(positionA.y - positionB.y);
        
        return (deltaX == 1 && deltaY == 0) || (deltaX == 0 && deltaY == 1);
    }

    #endregion

    
    #region Match Detection

    public List<Match3Tile> FindImmediateMatches(SOGridShape gridShape)
    {
        HashSet<Match3Tile> matches = new HashSet<Match3Tile>();

        // Find horizontal matches in grid
        for (var y = 0; y < gridShape.Grid.Height; y++)
        {
            for (var x = 0; x < gridShape.Grid.Width - 2; x++)
            {
                var tile1 = gridHandler.GetTile(new Vector2Int(x, y));
                var tile2 = gridHandler.GetTile(new Vector2Int(x + 1, y));
                var tile3 = gridHandler.GetTile(new Vector2Int(x + 2, y));

                if (!tile1 || !tile2 || !tile3 || !tile1.HasObject || !tile2.HasObject || !tile3.HasObject) continue;

                if (tile1.CurrentMatch3Object.ItemData == tile2.CurrentMatch3Object.ItemData 
                    && tile2.CurrentMatch3Object.ItemData == tile3.CurrentMatch3Object.ItemData)
                {
                    matches.Add(tile1);
                    matches.Add(tile2);
                    matches.Add(tile3);
                }
            }
        }

        // Find vertical matches in grid
        for (var x = 0; x < gridShape.Grid.Width; x++)
        {
            for (var y = 0; y < gridShape.Grid.Height - 2; y++)
            {
                var tile1 = gridHandler.GetTile(new Vector2Int(x, y));
                var tile2 = gridHandler.GetTile(new Vector2Int(x, y + 1));
                var tile3 = gridHandler.GetTile(new Vector2Int(x, y + 2));
                
                if (!tile1 || !tile2 || !tile3 || !tile1.HasObject || !tile2.HasObject || !tile3.HasObject) continue;
                
                if (tile1.CurrentMatch3Object.ItemData == tile2.CurrentMatch3Object.ItemData 
                    && tile2.CurrentMatch3Object.ItemData == tile3.CurrentMatch3Object.ItemData)
                {
                    matches.Add(tile1);
                    matches.Add(tile2);
                    matches.Add(tile3);
                }
            }
        }
        
        return new List<Match3Tile>(matches);
    }

    public List<Match3Tile> FindMatchesWithTile(Match3Tile match3Tile, SOGridShape gridShape)
    {
        if (!match3Tile || !match3Tile.HasObject) return new List<Match3Tile>();
        
        HashSet<Match3Tile> matches = new HashSet<Match3Tile>();
        Vector2Int pos = match3Tile.GridPosition;
        SOItemData itemData = match3Tile.CurrentMatch3Object.ItemData;
        
        // Check horizontal match
        List<Match3Tile> horizontalMatches = new List<Match3Tile> { match3Tile };
        for (int x = pos.x - 1; x >= 0; x--)
        {
            var checkTile = gridHandler.GetTile(new Vector2Int(x, pos.y));
            if (!gridHandler.IsValidTile(checkTile) || !checkTile.HasObject || 
                checkTile.CurrentMatch3Object.ItemData != itemData)
                break;
            horizontalMatches.Add(checkTile);
        }
        for (int x = pos.x + 1; x < gridShape.Grid.Width; x++)
        {
            var checkTile = gridHandler.GetTile(new Vector2Int(x, pos.y));
            if (!gridHandler.IsValidTile(checkTile) || !checkTile.HasObject || 
                checkTile.CurrentMatch3Object.ItemData != itemData)
                break;
            horizontalMatches.Add(checkTile);
        }
        
        if (horizontalMatches.Count >= gameManager.MinMatchCount)
        {
            foreach (var match in horizontalMatches)
                matches.Add(match);
        }
        
        // Check vertical match
        List<Match3Tile> verticalMatches = new List<Match3Tile> { match3Tile };
        for (int y = pos.y - 1; y >= 0; y--)
        {
            var checkTile = gridHandler.GetTile(new Vector2Int(pos.x, y));
            if (!gridHandler.IsValidTile(checkTile) || !checkTile.HasObject || 
                checkTile.CurrentMatch3Object.ItemData != itemData)
                break;
            verticalMatches.Add(checkTile);
        }
        for (int y = pos.y + 1; y < gridShape.Grid.Height; y++)
        {
            var checkTile = gridHandler.GetTile(new Vector2Int(pos.x, y));
            if (!gridHandler.IsValidTile(checkTile) || !checkTile.HasObject || 
                checkTile.CurrentMatch3Object.ItemData != itemData)
                break;
            verticalMatches.Add(checkTile);
        }
        
        if (verticalMatches.Count >= gameManager.MinMatchCount)
        {
            foreach (var match in verticalMatches)
                matches.Add(match);
        }
        
        return new List<Match3Tile>(matches);
    }

    public List<Match3Tile> FindPossibleMatches(SOGridShape gridShape)
    {
        HashSet<Match3Tile> possibleMatchTiles = new HashSet<Match3Tile>();
        HashSet<(Vector2Int, Vector2Int)> checkedPairs = new HashSet<(Vector2Int, Vector2Int)>();

        foreach (var tile in gridHandler.Tiles.Values)
        {
            if (!gridHandler.IsValidTile(tile) || !tile.HasObject) continue;
    
            Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
    
            foreach (var direction in directions)
            {
                var neighborPos = tile.GridPosition + direction;
                var neighborTile = gridHandler.GetTile(neighborPos);
        
                if (!gridHandler.IsValidTile(neighborTile) || !neighborTile.HasObject) continue;
        
                var pair = tile.GridPosition.x < neighborPos.x || (tile.GridPosition.x == neighborPos.x && tile.GridPosition.y < neighborPos.y)
                    ? (tile.GridPosition, neighborPos)
                    : (neighborPos, tile.GridPosition);
            
                if (!checkedPairs.Add(pair)) continue;

                var currentItemData = tile.CurrentMatch3Object.ItemData;
                var neighborItemData = neighborTile.CurrentMatch3Object.ItemData;
        
                if (WouldCreateMatch(neighborPos, currentItemData, gridShape) || 
                    WouldCreateMatch(tile.GridPosition, neighborItemData, gridShape))
                {
                    possibleMatchTiles.Add(tile);
                    possibleMatchTiles.Add(neighborTile);
                }
            }
        }

        return new List<Match3Tile>(possibleMatchTiles);
    }

    public bool WouldCreateMatch(Vector2Int position, SOItemData itemData, SOGridShape gridShape)
    {
        int horizontalCount = 1;
        for (int x = position.x - 1; x >= 0; x--)
        {
            var checkTile = gridHandler.GetTile(new Vector2Int(x, position.y));
            if (!gridHandler.IsValidTile(checkTile) || !checkTile.HasObject || 
                checkTile.CurrentMatch3Object.ItemData != itemData)
                break;
            horizontalCount++;
        }
        for (int x = position.x + 1; x < gridShape.Grid.Width; x++)
        {
            var checkTile = gridHandler.GetTile(new Vector2Int(x, position.y));
            if (!gridHandler.IsValidTile(checkTile) || !checkTile.HasObject || 
                checkTile.CurrentMatch3Object.ItemData != itemData)
                break;
            horizontalCount++;
        }
        
        if (horizontalCount >= gameManager.MinMatchCount) return true;
        
        int verticalCount = 1;
        for (int y = position.y - 1; y >= 0; y--)
        {
            var checkTile = gridHandler.GetTile(new Vector2Int(position.x, y));
            if (!gridHandler.IsValidTile(checkTile) || !checkTile.HasObject || 
                checkTile.CurrentMatch3Object.ItemData != itemData)
                break;
            verticalCount++;
        }
        for (int y = position.y + 1; y < gridShape.Grid.Height; y++)
        {
            var checkTile = gridHandler.GetTile(new Vector2Int(position.x, y));
            if (!gridHandler.IsValidTile(checkTile) || !checkTile.HasObject || 
                checkTile.CurrentMatch3Object.ItemData != itemData)
                break;
            verticalCount++;
        }
        
        return verticalCount >= gameManager.MinMatchCount;
    }

    public bool WouldCreateMatchDuringPopulation(Vector2Int position, SOItemData itemData, 
        Dictionary<Match3Tile, SOItemData> tilesToPopulate, SOGridShape gridShape)
    {
        // Check horizontal
        int horizontalCount = 1;
        for (int x = position.x - 1; x >= 0; x--)
        {
            var checkTile = gridHandler.GetTile(new Vector2Int(x, position.y));
            if (!gridHandler.IsValidTile(checkTile)) break;
            
            SOItemData checkItemData = null;
            if (checkTile.HasObject)
                checkItemData = checkTile.CurrentMatch3Object.ItemData;
            else if (tilesToPopulate.TryGetValue(checkTile, out var value))
                checkItemData = value;
            
            if (checkItemData == null || checkItemData != itemData)
                break;
                
            horizontalCount++;
        }
        for (int x = position.x + 1; x < gridShape.Grid.Width; x++)
        {
            var checkTile = gridHandler.GetTile(new Vector2Int(x, position.y));
            if (!gridHandler.IsValidTile(checkTile)) break;
            
            SOItemData checkItemData = null;
            if (checkTile.HasObject)
                checkItemData = checkTile.CurrentMatch3Object.ItemData;
            else if (tilesToPopulate.TryGetValue(checkTile, out var value))
                checkItemData = value;
            
            if (checkItemData == null || checkItemData != itemData)
                break;
                
            horizontalCount++;
        }
        
        if (horizontalCount >= gameManager.MinMatchCount) return true;
        
        // Check vertical
        int verticalCount = 1;
        for (int y = position.y - 1; y >= 0; y--)
        {
            var checkTile = gridHandler.GetTile(new Vector2Int(position.x, y));
            if (!gridHandler.IsValidTile(checkTile)) break;
            
            SOItemData checkItemData = null;
            if (checkTile.HasObject)
                checkItemData = checkTile.CurrentMatch3Object.ItemData;
            else if (tilesToPopulate.TryGetValue(checkTile, out var value))
                checkItemData = value;
            
            if (checkItemData == null || checkItemData != itemData)
                break;
                
            verticalCount++;
        }
        for (int y = position.y + 1; y < gridShape.Grid.Height; y++)
        {
            var checkTile = gridHandler.GetTile(new Vector2Int(position.x, y));
            if (!gridHandler.IsValidTile(checkTile)) break;
            
            SOItemData checkItemData = null;
            if (checkTile.HasObject)
                checkItemData = checkTile.CurrentMatch3Object.ItemData;
            else if (tilesToPopulate.TryGetValue(checkTile, out var value))
                checkItemData = value;
            
            if (checkItemData == null || checkItemData != itemData)
                break;
                
            verticalCount++;
        }
        
        return verticalCount >= gameManager.MinMatchCount;
    }

    #endregion

    
    #region Match Handling

    public IEnumerator HandleMatches(List<Match3Tile> matches)
    {
        foreach (var match in matches)
        {
            if (match && match.CurrentMatch3Object)
            {
                match.CurrentMatch3Object.MatchFound();
                match.SetCurrentItem(null);
                yield return new WaitForSeconds(delayBetweenMatchPops);
            }
        }
    }
    

    public IEnumerator HandleMatchesAndRepopulate(SOMatch3Level level, SOGridShape gridShape, int minPossibleMatches)
    {
        while (true)
        {
            var immediateMatches = FindImmediateMatches(gridShape);
        
            if (immediateMatches.Count == 0)
            {
                break;
            }
        
            gameManager.NotifyMatchesWhereMade(immediateMatches);
            
            yield return HandleMatches(immediateMatches);
            yield return MoveObjects(gridShape);
            yield return PopulateGrid(level, gridShape, minPossibleMatches, true);
        }
    }

    #endregion

    
    #region Movement

    public IEnumerator MoveObjects(SOGridShape gridShape)
    {
        bool objectsMoved;
        
        do
        {
            objectsMoved = false;
            List<(Match3Object obj, Match3Tile fromTile, Match3Tile toTile)> movesThisWave = new List<(Match3Object, Match3Tile, Match3Tile)>();
            HashSet<Match3Tile> tilesAlreadyMoving = new HashSet<Match3Tile>();
            
            // Scan from bottom to top to find all moves that can happen in this iteration
            for (var y = gridShape.Grid.Height - 1; y >= 0; y--)
            {
                for (var x = 0; x < gridShape.Grid.Width; x++)
                {
                    var tile = gridHandler.GetTile(new Vector2Int(x, y));
                    if (!tile.HasObject && tile.IsActive)
                    {
                        // Find the nearest object above this empty tile
                        for (var i = y - 1; i >= 0; i--)
                        {
                            var aboveTile = gridHandler.GetTile(new Vector2Int(x, i));
                            if (aboveTile.HasObject && !tilesAlreadyMoving.Contains(aboveTile))
                            {
                                movesThisWave.Add((aboveTile.CurrentMatch3Object, aboveTile, tile));
                                tilesAlreadyMoving.Add(aboveTile);
                                objectsMoved = true;
                                break;
                            }
                        }
                    }
                }
            }
            
            // Execute all moves for this wave simultaneously
            foreach (var move in movesThisWave)
            {
                move.fromTile.SetCurrentItem(null);
                move.toTile.SetCurrentItem(move.obj);
                move.obj.SetCurrentTile(move.toTile);
            }
            
            // Only wait if objects actually moved
            if (objectsMoved)
            {
                yield return new WaitForSeconds(delayBetweenObjectMovement);
            }
            
        } while (objectsMoved);
    }
    

    #endregion

    
    #region Population


    public Dictionary<Match3Tile, SOItemData> GenerateGridLayout(SOMatch3Level level, SOGridShape gridShape, int minPossibleMatches)
    {
        if (!level)
        {
            Debug.LogError("Level is not set, Cannot generate grid layout");
            return null;
        }

        int totalActiveTiles = gridHandler.GetActiveTileCount();
        
        if (totalActiveTiles == 0)
        {
            return null;
        }

        Vector2Int dimensions = gridHandler.GetGridDimensions();
        int maxX = dimensions.x;
        int maxY = dimensions.y;
        
        var tilesToPopulate = new Dictionary<Match3Tile, SOItemData>();
        for (int y = maxY; y >= 0; y--)
        {
            for (int x = 0; x <= maxX; x++)
            {
                var tile = gridHandler.GetTile(new Vector2Int(x, y));
                if (tile != null && !tile.HasObject)
                {
                    tilesToPopulate.Add(tile, null);
                }
            }
        }
        
        if (tilesToPopulate.Count == 0)
        {
            return tilesToPopulate;
        }
        
        List<(Vector2Int posA, Vector2Int posB)> guaranteedMatchPairs = 
            CreateGuaranteedMatchPositions(minPossibleMatches);
        
        foreach (var tileItemPair in tilesToPopulate.ToList())
        {
            SOItemData forcedItem = GetForcedItemForGuaranteedMatch(
                tileItemPair.Key.GridPosition, guaranteedMatchPairs, tilesToPopulate, level);
            
            if (forcedItem != null)
            {
                tilesToPopulate[tileItemPair.Key] = forcedItem;
            }
            else
            {
                SOItemData selectedItem;
                int attempts = 0;

                do
                {
                    selectedItem = level.MatchObjects.GetRandomItem();
                    attempts++;
            
                    if (attempts >= gameManager.MaxAttemptsToRecheckMatches)
                    {
                        Debug.Log("Could not find non-matching item");
                        break;
                    }
                } 
                while (WouldCreateMatchDuringPopulation(tileItemPair.Key.GridPosition, selectedItem, tilesToPopulate, gridShape));

                tilesToPopulate[tileItemPair.Key] = selectedItem;
            }
        }

        return tilesToPopulate;
    }


    public (bool isValid, int immediateMatches, int possibleMatches) ValidateGridLayout(
        Dictionary<Match3Tile, SOItemData> layout, SOGridShape gridShape, int minPossibleMatches, bool checkImmediateMatches = true)
    {
        // Temporarily spawn objects to validate
        foreach (var pair in layout)
        {
            gridHandler.CreateMatchObject(pair.Value, pair.Key);
        }

        // Check for immediate matches (only if we care about them)
        int immediateMatchCount = 0;
        if (checkImmediateMatches)
        {
            var immediateMatchesInGrid = FindImmediateMatches(gridShape);
            immediateMatchCount = immediateMatchesInGrid.Count;
        }

        // Check for possible matches
        var possibleMatchesInGrid = FindPossibleMatches(gridShape);
        int possibleMatchCount = possibleMatchesInGrid.Count;

        // Destroy all spawned objects
        foreach (var pair in layout)
        {
            if (pair.Key.HasObject)
            {
                Destroy(pair.Key.CurrentMatch3Object.gameObject);
                pair.Key.SetCurrentItem(null);
            }
        }

        // Validate
        bool isValid = (!checkImmediateMatches || immediateMatchCount == 0) && possibleMatchCount >= minPossibleMatches;
        
        return (isValid, immediateMatchCount, possibleMatchCount);
    }

    /// <summary>
    /// Spawns objects based on a pre-generated and validated layout
    /// </summary>
    public IEnumerator SpawnGridLayout(Dictionary<Match3Tile, SOItemData> layout, bool orderByRow)
    {
        ReleaseObject(false);

        int totalActiveTiles = layout.Count;
        
        if (totalActiveTiles == 0)
        {
            yield break;
        }

        if (orderByRow)
        {
            // Group tiles by row (y position)
            var tilesByRow = layout
                .GroupBy(kvp => kvp.Key.GridPosition.y)
                .OrderByDescending(g => g.Key); // Start from top row (highest y) going down
        
            int totalRows = tilesByRow.Count();
        
            foreach (var row in tilesByRow)
            {
                // Spawn all objects in this row simultaneously
                foreach (var tileObjectMatch in row)
                {
                    gridHandler.CreateMatchObject(tileObjectMatch.Value, tileObjectMatch.Key);
                }
            
                // Wait before spawning the next row
                yield return new WaitForSeconds(populationDuration / totalRows);
            }
        }
        else
        {
            foreach (var tileObjectMatch in layout)
            {
                gridHandler.CreateMatchObject(tileObjectMatch.Value, tileObjectMatch.Key);
                yield return new WaitForSeconds(populationDuration / totalActiveTiles);
            }
        }
    }

    /// <summary>
    /// Legacy method that combines generation and spawning (without validation)
    /// </summary>
    public IEnumerator PopulateGrid(SOMatch3Level level, SOGridShape gridShape, int minPossibleMatches, bool orderByRow)
    {
        var layout = GenerateGridLayout(level, gridShape, minPossibleMatches);
        if (layout == null || layout.Count == 0)
        {
            canInteract = false;
            yield break;
        }

        yield return SpawnGridLayout(layout, orderByRow);
    }



    private List<(Vector2Int posA, Vector2Int posB)> CreateGuaranteedMatchPositions(int count)
    {
        List<(Vector2Int, Vector2Int)> pairs = new List<(Vector2Int, Vector2Int)>();
        List<Vector2Int> usedPositions = new List<Vector2Int>();
        
        int attempts = 0;
        while (pairs.Count < count && attempts < gameManager.MaxGuaranteedMatchAttempts)
        {
            attempts++;
            
            var randomTile = gridHandler.GetRandomValidTile();
            if (!randomTile) break;
            
            if (usedPositions.Contains(randomTile.GridPosition)) continue;
            
            Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            foreach (var dir in directions.OrderBy(x => Random.value))
            {
                var neighborPos = randomTile.GridPosition + dir;
                var neighborTile = gridHandler.GetTile(neighborPos);
                
                if (!gridHandler.IsValidTile(neighborTile) || usedPositions.Contains(neighborPos)) continue;
                
                pairs.Add((randomTile.GridPosition, neighborPos));
                usedPositions.Add(randomTile.GridPosition);
                usedPositions.Add(neighborPos);
                break;
            }
        }
        
        return pairs;
    }

    private SOItemData GetForcedItemForGuaranteedMatch(Vector2Int position, 
        List<(Vector2Int posA, Vector2Int posB)> guaranteedPairs, 
        Dictionary<Match3Tile, SOItemData> tilesToPopulate, 
        SOMatch3Level level)
    {
        foreach (var pair in guaranteedPairs)
        {
            if (pair.posA == position)
            {
                var neighborPos = pair.posB;
                Vector2Int direction = neighborPos - position;
                Vector2Int thirdPos = neighborPos + direction;
                var thirdTile = gridHandler.GetTile(thirdPos);
                
                if (gridHandler.IsValidTile(thirdTile))
                {
                    var itemData = level.MatchObjects.GetRandomItem();
                    
                    if (tilesToPopulate.ContainsKey(thirdTile))
                    {
                        tilesToPopulate[thirdTile] = itemData;
                    }
                    
                    return itemData;
                }
            }
            else if (pair.posB == position)
            {
                var tileA = gridHandler.GetTile(pair.posA);
                if (tilesToPopulate.ContainsKey(tileA) && tilesToPopulate[tileA])
                {
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
    
    
    #endregion
}