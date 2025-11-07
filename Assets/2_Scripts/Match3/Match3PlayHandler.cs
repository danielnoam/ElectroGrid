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
    [SerializeField] private MatchObject matchObjectPrefab;
    [SerializeField] private Transform matchObjectsParent;

    [Separator]
    [SerializeField, ReadOnly] private bool canInteract;
    [SerializeField, ReadOnly] private Tile selectedTile;
    [SerializeField, ReadOnly] private MatchObject heldMatchObject;
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
    
    public Tile SelectedTile => selectedTile;
    public MatchObject HeldMatchObject => heldMatchObject;

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
        if (!canInteract || !heldMatchObject) return;

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
            gridDirection = direction.x > 0 ? Vector2Int.left : Vector2Int.right;
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

        var newTilePos = selectedTile.GridPosition + gridDirection;
        var newTile = gridManager.GetTile(newTilePos);

        TrySwapHeldObject(newTile);
    }

    private void OnSelect(InputAction.CallbackContext callbackContext)
    {
        if (!canInteract) return;

        if (!heldMatchObject && callbackContext.started)
        {
            Vector3 worldPos = _camera.ScreenToWorldPoint(inputReader.MousePosition);
            worldPos.z = 0;
    
            var gridPos = gridManager.GridShape.Grid.GetPositionInGird(worldPos);
            var tile = gridManager.GetTile(gridPos);
        
            SelectObjectInTile(tile);
        }            
        else if (heldMatchObject && callbackContext.canceled)
        {
            ReleaseObject(false);
        }
    }



    #endregion

    #region Selection & Swapping

    private void SelectObjectInTile(Tile tile)
    {
        if (!gridManager.IsValidTile(tile) || !tile.HasObject) return;
        
        selectedTile = tile;
        heldMatchObject = selectedTile.CurrentMatchObject;
        heldMatchObject.SetHeld(true);
        selectionIndicator?.EnableIndicator(selectedTile);
    }
    
    public void ReleaseObject(bool animateReturn)
    {
        selectedTile?.SetSelected(false);
        selectedTile = null;
        heldMatchObject?.SetHeld(false);
        heldMatchObject = null;
        selectionIndicator?.DisableIndicator(animateReturn);
    }

    private void TrySwapHeldObject(Tile tile)
    {
        if (!gridManager.IsValidTile(tile))
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
            Match3GameManager.Instance.StartCoroutine(
                Match3GameManager.Instance.RunGameLogic(tile.GridPosition, selectedTile.GridPosition));
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
        
        var itemA = tileA.CurrentMatchObject;
        var itemB = tileB.CurrentMatchObject;
        
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

    public List<Tile> FindImmediateMatches(SOGridShape gridShape)
    {
        HashSet<Tile> matches = new HashSet<Tile>();

        // Find horizontal matches in grid
        for (var y = 0; y < gridShape.Grid.Height; y++)
        {
            for (var x = 0; x < gridShape.Grid.Width - 2; x++)
            {
                var tile1 = gridManager.GetTile(new Vector2Int(x, y));
                var tile2 = gridManager.GetTile(new Vector2Int(x + 1, y));
                var tile3 = gridManager.GetTile(new Vector2Int(x + 2, y));

                if (!tile1 || !tile2 || !tile3 || !tile1.HasObject || !tile2.HasObject || !tile3.HasObject) continue;

                if (tile1.CurrentMatchObject.ItemData == tile2.CurrentMatchObject.ItemData 
                    && tile2.CurrentMatchObject.ItemData == tile3.CurrentMatchObject.ItemData)
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
                
                if (tile1.CurrentMatchObject.ItemData == tile2.CurrentMatchObject.ItemData 
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

    public List<Tile> FindMatchesWithTile(Tile tile, SOGridShape gridShape)
    {
        if (!tile || !tile.HasObject) return new List<Tile>();
        
        HashSet<Tile> matches = new HashSet<Tile>();
        Vector2Int pos = tile.GridPosition;
        SOItemData itemData = tile.CurrentMatchObject.ItemData;
        
        // Check horizontal match
        List<Tile> horizontalMatches = new List<Tile> { tile };
        for (int x = pos.x - 1; x >= 0; x--)
        {
            var checkTile = gridManager.GetTile(new Vector2Int(x, pos.y));
            if (!gridManager.IsValidTile(checkTile) || !checkTile.HasObject || 
                checkTile.CurrentMatchObject.ItemData != itemData)
                break;
            horizontalMatches.Add(checkTile);
        }
        for (int x = pos.x + 1; x < gridShape.Grid.Width; x++)
        {
            var checkTile = gridManager.GetTile(new Vector2Int(x, pos.y));
            if (!gridManager.IsValidTile(checkTile) || !checkTile.HasObject || 
                checkTile.CurrentMatchObject.ItemData != itemData)
                break;
            horizontalMatches.Add(checkTile);
        }
        
        if (horizontalMatches.Count >= MinMatchCount)
        {
            foreach (var match in horizontalMatches)
                matches.Add(match);
        }
        
        // Check vertical match
        List<Tile> verticalMatches = new List<Tile> { tile };
        for (int y = pos.y - 1; y >= 0; y--)
        {
            var checkTile = gridManager.GetTile(new Vector2Int(pos.x, y));
            if (!gridManager.IsValidTile(checkTile) || !checkTile.HasObject || 
                checkTile.CurrentMatchObject.ItemData != itemData)
                break;
            verticalMatches.Add(checkTile);
        }
        for (int y = pos.y + 1; y < gridShape.Grid.Height; y++)
        {
            var checkTile = gridManager.GetTile(new Vector2Int(pos.x, y));
            if (!gridManager.IsValidTile(checkTile) || !checkTile.HasObject || 
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

    public List<Tile> FindPossibleMatches(SOGridShape gridShape)
    {
        HashSet<Tile> possibleMatchTiles = new HashSet<Tile>();
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

                var currentItemData = tile.CurrentMatchObject.ItemData;
                var neighborItemData = neighborTile.CurrentMatchObject.ItemData;
        
                if (WouldCreateMatch(neighborPos, currentItemData, gridShape) || 
                    WouldCreateMatch(tile.GridPosition, neighborItemData, gridShape))
                {
                    possibleMatchTiles.Add(tile);
                    possibleMatchTiles.Add(neighborTile);
                }
            }
        }

        return new List<Tile>(possibleMatchTiles);
    }

    public bool WouldCreateMatch(Vector2Int position, SOItemData itemData, SOGridShape gridShape)
    {
        int horizontalCount = 1;
        for (int x = position.x - 1; x >= 0; x--)
        {
            var checkTile = gridManager.GetTile(new Vector2Int(x, position.y));
            if (!gridManager.IsValidTile(checkTile) || !checkTile.HasObject || 
                checkTile.CurrentMatchObject.ItemData != itemData)
                break;
            horizontalCount++;
        }
        for (int x = position.x + 1; x < gridShape.Grid.Width; x++)
        {
            var checkTile = gridManager.GetTile(new Vector2Int(x, position.y));
            if (!gridManager.IsValidTile(checkTile) || !checkTile.HasObject || 
                checkTile.CurrentMatchObject.ItemData != itemData)
                break;
            horizontalCount++;
        }
        
        if (horizontalCount >= MinMatchCount) return true;
        
        int verticalCount = 1;
        for (int y = position.y - 1; y >= 0; y--)
        {
            var checkTile = gridManager.GetTile(new Vector2Int(position.x, y));
            if (!gridManager.IsValidTile(checkTile) || !checkTile.HasObject || 
                checkTile.CurrentMatchObject.ItemData != itemData)
                break;
            verticalCount++;
        }
        for (int y = position.y + 1; y < gridShape.Grid.Height; y++)
        {
            var checkTile = gridManager.GetTile(new Vector2Int(position.x, y));
            if (!gridManager.IsValidTile(checkTile) || !checkTile.HasObject || 
                checkTile.CurrentMatchObject.ItemData != itemData)
                break;
            verticalCount++;
        }
        
        return verticalCount >= MinMatchCount;
    }

    public bool WouldCreateMatchDuringPopulation(Vector2Int position, SOItemData itemData, 
        Dictionary<Tile, SOItemData> tilesToPopulate, SOGridShape gridShape)
    {
        // Check horizontal
        int horizontalCount = 1;
        for (int x = position.x - 1; x >= 0; x--)
        {
            var checkTile = gridManager.GetTile(new Vector2Int(x, position.y));
            if (!gridManager.IsValidTile(checkTile)) break;
            
            SOItemData checkItemData = null;
            if (checkTile.HasObject)
                checkItemData = checkTile.CurrentMatchObject.ItemData;
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
                checkItemData = checkTile.CurrentMatchObject.ItemData;
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
                checkItemData = checkTile.CurrentMatchObject.ItemData;
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
                checkItemData = checkTile.CurrentMatchObject.ItemData;
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

    public IEnumerator HandleMatches(List<Tile> matches)
    {
        foreach (var match in matches)
        {
            match.CurrentMatchObject.MatchFound();
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
        
        var tilesToPopulate = new Dictionary<Tile, SOItemData>();
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

    public MatchObject CreateMatchObject(SOItemData itemData, Tile tile)
    {
        if (!gridManager.IsValidTile(tile)) return null;

        var topMostTile = gridManager.GetTile(new Vector2Int(tile.GridPosition.x, 0));
        
        var spawnPosition = new Vector3(tile.transform.position.x,
            topMostTile.transform.position.y + topMostTile.transform.localScale.y,
            tile.transform.position.z);
        
        var item = Instantiate(matchObjectPrefab, spawnPosition, Quaternion.identity, matchObjectsParent);
        
        item.Initialize(itemData);
        tile.SetCurrentItem(item);
        item.SetCurrentTile(tile);
        
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
        Dictionary<Tile, SOItemData> tilesToPopulate, 
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