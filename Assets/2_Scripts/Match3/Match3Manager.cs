using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using DNExtensions;
using DNExtensions.Button;
using UnityEngine.InputSystem;

public class Match3Manager : MonoBehaviour
{
    public static Match3Manager Instance { get; private set; }
    
    [Header("Level")]
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
    [SerializeField, ReadOnly] private int movesMade;
    [SerializeField, ReadOnly] private Tile hoveredTile;
    [SerializeField, ReadOnly] private Tile selectedTile;
    [SerializeField, ReadOnly] private MatchObject heldMatchObject;


    private SOGridShape GridShape => level ? level.GridShape : defaultGridShape;
    
    
    private readonly Dictionary<Vector2Int, Tile> _tiles = new Dictionary<Vector2Int, Tile>();
    private Coroutine _gridPopulationRoutine;
    private bool _canSelectTiles;
    private Camera _camera;
    
    

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
        if (!_canSelectTiles || !heldMatchObject) return;

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
        if (!_canSelectTiles) return;

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

        movesMade = level.AllowedMoves;
        CreateGird();
    }

    private void CheckMovesLeft()
    {
        if (movesMade <= 0)
        {
            _canSelectTiles = false;
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
        
        float duration = level.PopulationDuration;
        PopulationDirection direction = level.PopulationDirection;
        ChanceList<SOItemData> matchObjects = level.MatchObjects;
        

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
            _canSelectTiles = false;
            yield break;
        }

        // Get grid dimensions
        int maxX = 0, maxY = 0;
        foreach (var pos in _tiles.Keys)
        {
            if (pos.x > maxX) maxX = pos.x;
            if (pos.y > maxY) maxY = pos.y;
        }

        switch (direction)
        {
            case PopulationDirection.TopToBottom:
                for (int y = 0; y <= maxY; y++)
                {
                    for (int x = 0; x <= maxX; x++)
                    {
                        if (_tiles.TryGetValue(new Vector2Int(x, y), out var tile) && tile.IsActive && !tile.HasObject)
                        {
                            var randomItemData = level.MatchObjects.GetRandomItem();
                            if (randomItemData)
                            {
                                CreateMatchObject(randomItemData, tile);
                                yield return new WaitForSeconds(duration / totalActiveTiles);
                            }
                        }
                    }
                }
                break;
                
            case PopulationDirection.BottomToTop:
                for (int y = maxY; y >= 0; y--)
                {
                    for (int x = 0; x <= maxX; x++)
                    {
                        if (_tiles.TryGetValue(new Vector2Int(x, y), out var tile) && tile.IsActive && !tile.HasObject)
                        {
                            var randomItemData = matchObjects.GetRandomItem();
                            if (randomItemData)
                            {
                                CreateMatchObject(randomItemData, tile);
                                yield return new WaitForSeconds(duration / totalActiveTiles);
                            }
                        }
                    }
                }
                break;
                
            case PopulationDirection.RightToLeft:
                for (int x = 0; x <= maxX; x++)
                {
                    for (int y = 0; y <= maxY; y++)
                    {
                        if (_tiles.TryGetValue(new Vector2Int(x, y), out var tile) && tile.IsActive && !tile.HasObject)
                        {
                            var randomItemData = matchObjects.GetRandomItem();
                            if (randomItemData)
                            {
                                CreateMatchObject(randomItemData, tile);
                                yield return new WaitForSeconds(duration / totalActiveTiles);
                            }
                        }
                    }
                }
                break;
                
            case PopulationDirection.LeftToRight:
                for (int x = maxX; x >= 0; x--)
                {
                    for (int y = 0; y <= maxY; y++)
                    {
                        if (_tiles.TryGetValue(new Vector2Int(x, y), out var tile) && tile.IsActive && !tile.HasObject)
                        {
                            var randomItemData = matchObjects.GetRandomItem();
                            if (randomItemData)
                            {
                                CreateMatchObject(randomItemData, tile);
                                yield return new WaitForSeconds(duration / totalActiveTiles);
                            }
                        }
                    }
                }
                break;
        }
        
        _canSelectTiles = true;
    }
    
    
    private MatchObject CreateMatchObject(SOItemData itemData, Tile tile)
    {
        if (!tile || !tile.IsActive) return null;

        var item = Instantiate(matchObjectPrefab, tile.transform.position, Quaternion.identity, matchObjectsParent);
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

        _canSelectTiles = false;
        ClearGrid();
        
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
    


    private void ClearGrid()
    {
        if (_gridPopulationRoutine != null) StopCoroutine(_gridPopulationRoutine);
        _gridPopulationRoutine = null;
        
        
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
        if (!_canSelectTiles) return;
        
        Vector2 mousePos = inputReader.MousePosition;
        Vector2 worldPos = _camera.ScreenToWorldPoint(mousePos);
    
        RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);
    
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
            movesMade -= 1;
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
        mouseIndicator.EnableIndicator(selectedTile);
    }
    
    private void ReleaseObject(bool animateReturn)
    {
        selectedTile?.SetSelected(false);
        selectedTile = null;
        heldMatchObject?.SetHeld(false);
        heldMatchObject = null;
        mouseIndicator.DisableIndicator(animateReturn);
    }
    
    private IEnumerator RunGameLogic(Vector2Int posA, Vector2Int posB)
    {
        _canSelectTiles = false;
        hoveredTile?.SetHovered(false);
        
        // Swap items
        yield return StartCoroutine(SwapObjects(posA, posB));
        
        // Check for matches only in nearby tiles if there is non 
        List<Tile> matchesWithTile = new List<Tile>();
        matchesWithTile.AddRange(FindMatchesWithTile(GetTile(posA)));
        matchesWithTile.AddRange(FindMatchesWithTile(GetTile(posB)));
        matchesWithTile = matchesWithTile.Distinct().ToList();

        if (matchesWithTile.Count == 0)
        {
            yield return StartCoroutine(SwapObjects(posB, posA));
            CheckMovesLeft();
            yield return null;
        }
        else
        {
            yield return StartCoroutine(HandleMatches(matchesWithTile));
        }
        
        // Find matches
        List<Tile> matches = FindMatches();
        if (matches.Count == 0) yield return null;
        
        // Handle matches
        yield return StartCoroutine(HandleMatches(matches));
        
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
        yield return new WaitForSeconds(0.3f);
    }
    

    private IEnumerator HandleMatches(List<Tile> matches)
    {
        foreach (var match in matches)
        {
            match.CurrentMatchObject.MatchFound();
            match.SetCurrentItem(null);
            yield return new WaitForSeconds(0.1f);
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
                            yield return new WaitForSeconds(0.1f);
                            break;
                        }
                    }
                }
            }
        }
    }


    private List<Tile> FindMatches()
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
        
        if (verticalMatches.Count >= 3)
        {
            foreach (var match in verticalMatches)
                matches.Add(match);
        }
        
        return new List<Tile>(matches);
    }
    

    #endregion


    #region Helper

    public Tile GetTile(Vector2Int position)
    {
        _tiles.TryGetValue(position, out Tile tile);
        return tile;
    }
    
    public MatchObject GetItem(Vector2Int position)
    {
        var tile = GetTile(position);
        return tile ? tile.CurrentMatchObject : null;
    }
    
    public bool IsValidTile(Tile tile)
    {
        return tile && tile.IsActive;
    }
    
    public bool IsPositionsTouching(Vector2Int positionA, Vector2Int positionB)
    {
        int deltaX = Mathf.Abs(positionA.x - positionB.x);
        int deltaY = Mathf.Abs(positionA.y - positionB.y);
        
        return (deltaX == 1 && deltaY == 0) || (deltaX == 0 && deltaY == 1);
    }
    

    #endregion

    
    
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        GridShape?.Grid?.DrawGrid();
    }


#endif
}