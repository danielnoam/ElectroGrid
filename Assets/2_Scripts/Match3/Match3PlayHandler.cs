using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DNExtensions;
using UnityEngine;
using UnityEngine.InputSystem;

public class Match3PlayHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Match3GameManager gameManager;
    [SerializeField] private Match3GridManager gridManager;
    [SerializeField] private Match3SelectionIndicator selectionIndicator;
    [SerializeField] private Match3InputReader inputReader;
    [SerializeField] private Match3Object match3ObjectPrefab;
    [SerializeField] private Transform matchObjectsParent;

    [Separator]
    [SerializeField, ReadOnly] private bool canInteract;
    [SerializeField, ReadOnly] private Match3Tile selectedMatch3Tile;
    [SerializeField, ReadOnly] private Match3Object heldMatch3Object;
    private Camera _camera;
    
    private const int MinMatchCount = 3;
    private const float SwapDuration = 0.3f;
    private const float MatchHandleDelay = 0.1f;
    private const float MoveObjectsDelay = 0.1f;
    private const int MaxGuaranteedMatchAttempts = 100;

    public bool CanInteract
    {
        get => canInteract;
        set => canInteract = value;
    }
    
    public Match3Tile SelectedMatch3Tile => selectedMatch3Tile;
    public Match3Object HeldMatch3Object => heldMatch3Object;

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
        var newTile = gridManager.GetTile(newTilePos);

        TrySwapHeldObject(newTile);
    }

    private void OnSelect(InputAction.CallbackContext callbackContext)
    {
        if (!canInteract) return;

        if (!heldMatch3Object && callbackContext.started)
        {
            Vector3 worldPos = _camera.ScreenToWorldPoint(inputReader.MousePosition);
            worldPos.z = 0;
    
            var gridPos = gridManager.GridShape.Grid.GetPositionInGird(worldPos);
            var tile = gridManager.GetTile(gridPos);
        
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
        if (!gridManager.IsValidTile(match3Tile) || !match3Tile.HasObject) return;
        
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
        if (!gridManager.IsValidTile(match3Tile))
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
            Match3GameManager.Instance.StartCoroutine(
                Match3GameManager.Instance.RunGameLogic(match3Tile.GridPosition, selectedMatch3Tile.GridPosition));
        }
        else
        {
            ReleaseObject(true);
        }
    }

    public IEnumerator SwapObjects(Vector2Int posA, Vector2Int posB)
    {
        var tileA = gridManager.GetTile(posA);
        var tileB = gridManager.GetTile(posB);
        
        if (!gridManager.IsValidTile(tileA) || !gridManager.IsValidTile(tileB)) 
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
        yield return new WaitForSeconds(SwapDuration);
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
                var tile1 = gridManager.GetTile(new Vector2Int(x, y));
                var tile2 = gridManager.GetTile(new Vector2Int(x + 1, y));
                var tile3 = gridManager.GetTile(new Vector2Int(x + 2, y));

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
                var tile1 = gridManager.GetTile(new Vector2Int(x, y));
                var tile2 = gridManager.GetTile(new Vector2Int(x, y + 1));
                var tile3 = gridManager.GetTile(new Vector2Int(x, y + 2));
                
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
            var checkTile = gridManager.GetTile(new Vector2Int(x, pos.y));
            if (!gridManager.IsValidTile(checkTile) || !checkTile.HasObject || 
                checkTile.CurrentMatch3Object.ItemData != itemData)
                break;
            horizontalMatches.Add(checkTile);
        }
        for (int x = pos.x + 1; x < gridShape.Grid.Width; x++)
        {
            var checkTile = gridManager.GetTile(new Vector2Int(x, pos.y));
            if (!gridManager.IsValidTile(checkTile) || !checkTile.HasObject || 
                checkTile.CurrentMatch3Object.ItemData != itemData)
                break;
            horizontalMatches.Add(checkTile);
        }
        
        if (horizontalMatches.Count >= MinMatchCount)
        {
            foreach (var match in horizontalMatches)
                matches.Add(match);
        }
        
        // Check vertical match
        List<Match3Tile> verticalMatches = new List<Match3Tile> { match3Tile };
        for (int y = pos.y - 1; y >= 0; y--)
        {
            var checkTile = gridManager.GetTile(new Vector2Int(pos.x, y));
            if (!gridManager.IsValidTile(checkTile) || !checkTile.HasObject || 
                checkTile.CurrentMatch3Object.ItemData != itemData)
                break;
            verticalMatches.Add(checkTile);
        }
        for (int y = pos.y + 1; y < gridShape.Grid.Height; y++)
        {
            var checkTile = gridManager.GetTile(new Vector2Int(pos.x, y));
            if (!gridManager.IsValidTile(checkTile) || !checkTile.HasObject || 
                checkTile.CurrentMatch3Object.ItemData != itemData)
                break;
            verticalMatches.Add(checkTile);
        }
        
        if (verticalMatches.Count >= MinMatchCount)
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

        foreach (var tile in gridManager.Tiles.Values)
        {
            if (!gridManager.IsValidTile(tile) || !tile.HasObject) continue;
    
            Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
    
            foreach (var direction in directions)
            {
                var neighborPos = tile.GridPosition + direction;
                var neighborTile = gridManager.GetTile(neighborPos);
        
                if (!gridManager.IsValidTile(neighborTile) || !neighborTile.HasObject) continue;
        
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
            var checkTile = gridManager.GetTile(new Vector2Int(x, position.y));
            if (!gridManager.IsValidTile(checkTile) || !checkTile.HasObject || 
                checkTile.CurrentMatch3Object.ItemData != itemData)
                break;
            horizontalCount++;
        }
        for (int x = position.x + 1; x < gridShape.Grid.Width; x++)
        {
            var checkTile = gridManager.GetTile(new Vector2Int(x, position.y));
            if (!gridManager.IsValidTile(checkTile) || !checkTile.HasObject || 
                checkTile.CurrentMatch3Object.ItemData != itemData)
                break;
            horizontalCount++;
        }
        
        if (horizontalCount >= MinMatchCount) return true;
        
        int verticalCount = 1;
        for (int y = position.y - 1; y >= 0; y--)
        {
            var checkTile = gridManager.GetTile(new Vector2Int(position.x, y));
            if (!gridManager.IsValidTile(checkTile) || !checkTile.HasObject || 
                checkTile.CurrentMatch3Object.ItemData != itemData)
                break;
            verticalCount++;
        }
        for (int y = position.y + 1; y < gridShape.Grid.Height; y++)
        {
            var checkTile = gridManager.GetTile(new Vector2Int(position.x, y));
            if (!gridManager.IsValidTile(checkTile) || !checkTile.HasObject || 
                checkTile.CurrentMatch3Object.ItemData != itemData)
                break;
            verticalCount++;
        }
        
        return verticalCount >= MinMatchCount;
    }

    public bool WouldCreateMatchDuringPopulation(Vector2Int position, SOItemData itemData, 
        Dictionary<Match3Tile, SOItemData> tilesToPopulate, SOGridShape gridShape)
    {
        // Check horizontal
        int horizontalCount = 1;
        for (int x = position.x - 1; x >= 0; x--)
        {
            var checkTile = gridManager.GetTile(new Vector2Int(x, position.y));
            if (!gridManager.IsValidTile(checkTile)) break;
            
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
            var checkTile = gridManager.GetTile(new Vector2Int(x, position.y));
            if (!gridManager.IsValidTile(checkTile)) break;
            
            SOItemData checkItemData = null;
            if (checkTile.HasObject)
                checkItemData = checkTile.CurrentMatch3Object.ItemData;
            else if (tilesToPopulate.TryGetValue(checkTile, out var value))
                checkItemData = value;
            
            if (checkItemData == null || checkItemData != itemData)
                break;
                
            horizontalCount++;
        }
        
        if (horizontalCount >= MinMatchCount) return true;
        
        // Check vertical
        int verticalCount = 1;
        for (int y = position.y - 1; y >= 0; y--)
        {
            var checkTile = gridManager.GetTile(new Vector2Int(position.x, y));
            if (!gridManager.IsValidTile(checkTile)) break;
            
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
            var checkTile = gridManager.GetTile(new Vector2Int(position.x, y));
            if (!gridManager.IsValidTile(checkTile)) break;
            
            SOItemData checkItemData = null;
            if (checkTile.HasObject)
                checkItemData = checkTile.CurrentMatch3Object.ItemData;
            else if (tilesToPopulate.TryGetValue(checkTile, out var value))
                checkItemData = value;
            
            if (checkItemData == null || checkItemData != itemData)
                break;
                
            verticalCount++;
        }
        
        return verticalCount >= MinMatchCount;
    }

    #endregion

    #region Match Handling

    public IEnumerator HandleMatches(List<Match3Tile> matches)
    {
        foreach (var match in matches)
        {
            match.CurrentMatch3Object.MatchFound();
            match.SetCurrentItem(null);
            yield return new WaitForSeconds(MatchHandleDelay);
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
        
            gameManager.NotifyObjectivesAboutMatches(immediateMatches);
            
            yield return HandleMatches(immediateMatches);
            yield return MoveObjects(gridShape);
            yield return PopulateGrid(level, gridShape, minPossibleMatches);
        }
    }

    #endregion

    #region Movement

    public IEnumerator MoveObjects(SOGridShape gridShape)
    {
        for (var x = 0; x < gridShape.Grid.Width; x++)
        {
            for (var y = gridShape.Grid.Height - 1; y >= 0; y--)
            {
                var tile = gridManager.GetTile(new Vector2Int(x, y));
                if (!tile.HasObject && tile.IsActive)
                {
                    for (var i = y - 1; i >= 0; i--)
                    {
                        var aboveTile = gridManager.GetTile(new Vector2Int(x, i));
                        if (aboveTile.HasObject)
                        {
                            var matchObject = aboveTile.CurrentMatch3Object;
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

    #region Population

    public IEnumerator PopulateGrid(SOMatch3Level level, SOGridShape gridShape, int minPossibleMatches)
    {
        if (!level)
        {
            Debug.LogError("Level is not set, Cannot populate grid");
            yield break;
        }
        
        ReleaseObject(false);

        int totalActiveTiles = gridManager.GetActiveTileCount();
        
        if (totalActiveTiles == 0)
        {
            canInteract = false;
            yield break;
        }

        Vector2Int dimensions = gridManager.GetGridDimensions();
        int maxX = dimensions.x;
        int maxY = dimensions.y;
        
        var tilesToPopulate = new Dictionary<Match3Tile, SOItemData>();
        for (int y = maxY; y >= 0; y--)
        {
            for (int x = 0; x <= maxX; x++)
            {
                var tile = gridManager.GetTile(new Vector2Int(x, y));
                if (tile != null && !tile.HasObject)
                {
                    tilesToPopulate.Add(tile, null);
                }
            }
        }
        
        if (tilesToPopulate.Count == 0)
        {
            yield break;
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

        foreach (var tileObjectMatch in tilesToPopulate)
        {
            CreateMatchObject(tileObjectMatch.Value, tileObjectMatch.Key);
            yield return new WaitForSeconds(gameManager.PopulationDuration / totalActiveTiles);
        }
    }

    public Match3Object CreateMatchObject(SOItemData itemData, Match3Tile match3Tile)
    {
        if (!gridManager.IsValidTile(match3Tile)) return null;

        var topMostTile = gridManager.GetTile(new Vector2Int(match3Tile.GridPosition.x, 0));
        
        var spawnPosition = new Vector3(match3Tile.transform.position.x,
            topMostTile.transform.position.y + topMostTile.transform.localScale.y,
            match3Tile.transform.position.z);
        
        var item = Instantiate(match3ObjectPrefab, spawnPosition, Quaternion.identity, matchObjectsParent);
        
        item.Initialize(itemData);
        match3Tile.SetCurrentItem(item);
        item.SetCurrentTile(match3Tile);
        
        return item;
    }

    private List<(Vector2Int posA, Vector2Int posB)> CreateGuaranteedMatchPositions(int count)
    {
        List<(Vector2Int, Vector2Int)> pairs = new List<(Vector2Int, Vector2Int)>();
        List<Vector2Int> usedPositions = new List<Vector2Int>();
        
        int attempts = 0;
        while (pairs.Count < count && attempts < MaxGuaranteedMatchAttempts)
        {
            attempts++;
            
            var randomTile = gridManager.GetRandomValidTile();
            if (!randomTile) break;
            
            if (usedPositions.Contains(randomTile.GridPosition)) continue;
            
            Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            foreach (var dir in directions.OrderBy(x => Random.value))
            {
                var neighborPos = randomTile.GridPosition + dir;
                var neighborTile = gridManager.GetTile(neighborPos);
                
                if (!gridManager.IsValidTile(neighborTile) || usedPositions.Contains(neighborPos)) continue;
                
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
                var thirdTile = gridManager.GetTile(thirdPos);
                
                if (gridManager.IsValidTile(thirdTile))
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
                var tileA = gridManager.GetTile(pair.posA);
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

    public void DestroyAllObjects()
    {
        foreach (var tile in gridManager.Tiles.Values)
        {
            if (tile)
            {
                tile.SetCurrentItem(null);
            }
        }
        
        if (Application.isEditor)
        {
            while (matchObjectsParent.childCount > 0)
            {
                DestroyImmediate(matchObjectsParent.GetChild(0).gameObject);
            }
        }
        else
        {
            foreach (Transform child in matchObjectsParent)
            {
                Destroy(child.gameObject);
            }
        }
    }

    
    #endregion
}