using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using DNExtensions;
using DNExtensions.Button;
using UnityEngine.InputSystem;

public class Match3Manager : MonoBehaviour
{
    public static Match3Manager Instance { get; private set; }
    
    [Header("Grid Settings")]
    [SerializeField] private SOGridShape gridShape;
    [SerializeField] private Vector3 gridOffset = Vector3.zero;
    [SerializeField] private Vector2 tileSize = Vector2.one;
    [SerializeField] private Vector2 tileOffset = Vector2.zero;
    
    [Header("Population Settings")]
    [SerializeField] private PopulationDirection populationDirection = PopulationDirection.TopToBottom;
    [SerializeField, Min(1)] private float populationDuration = 1f;
    
    [Header("References")]
    [SerializeField] private Tile tilePrefab;
    [SerializeField] private MatchObject matchObjectPrefab;
    [SerializeField] private Transform matchObjectsParent;
    [SerializeField] private Transform tilesParent;
    [SerializeField] private MouseIndicator mouseIndicator;
    [SerializeField] private Match3InputReader inputReader;
    [SerializeField] private ChanceList<SOItemData> items = new ChanceList<SOItemData>();
    
    [Separator]
    [SerializeField, ReadOnly] private Tile hoveredTile;
    [SerializeField, ReadOnly] private Tile selectedTile;
    [SerializeField, ReadOnly] private MatchObject heldMatchObject;
    
    private enum PopulationDirection { TopToBottom, BottomToTop, LeftToRight, RightToLeft }
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
        CreateGird();
    }

    private void OnEnable()
    {
        inputReader.OnSelect += OnSelect;
    }
    
    private void OnDisable()
    {
        inputReader.OnSelect -= OnSelect;
    }

    private void OnSelect(InputAction.CallbackContext callbackContext)
    {
        
        Vector3 worldPos = _camera.ScreenToWorldPoint(inputReader.MousePosition);
        worldPos.z = 0;
    
        var gridPos = GetGridPosition(worldPos);
        var tile = GetTile(gridPos);
        SelectTile(tile);
    }
    
    private void Update()
    {
        UpdateHoveredTile();
    }


    

    #region Objects Creation
    

    private IEnumerator PopulateGrid(float duration, PopulationDirection direction)
    {
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
                            var randomItemData = items.GetRandomItem();
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
                            var randomItemData = items.GetRandomItem();
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
                            var randomItemData = items.GetRandomItem();
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
                            var randomItemData = items.GetRandomItem();
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

        var item = Instantiate(matchObjectPrefab, new Vector3(tile.transform.position.x,tile.transform.position.y, matchObjectsParent.position.z), Quaternion.identity, matchObjectsParent);
        item.transform.localScale = new Vector3(tileSize.x, tileSize.y, 1f);
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
        tile.transform.localScale = new Vector3(tileSize.x, tileSize.y, 1f);
        tile.Initialize(gridPos, isActive);
        return tile;
    }
    
    
    [Button]
    private void CreateGird()
    {
        if (!gridShape || !tilePrefab || !matchObjectPrefab) return;

        _canSelectTiles = false;
        ClearGrid();
        
        var grid = gridShape.Grid;
        
        for (int x = 0; x < grid.Width; x++)
        {
            for (int y = 0; y < grid.Height; y++)
            {
                var tileState = grid.IsTileActive(x, y);
                Vector2Int gridPos = new Vector2Int(x, y);
                
                Vector3 tilePosition = gridOffset + new Vector3(
                    x * (tileSize.x + tileOffset.x), 
                    (grid.Height - 1 - y) * (tileSize.y + tileOffset.y), 
                    0
                );

                var tile = CreateTile(tilePosition, gridPos, tileState);
                _tiles.Add(gridPos, tile);
            }
        }
        
        StartCoroutine(PopulateGrid(populationDuration, populationDirection));
    }
    

    [Button]
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
    
    private void SelectTile(Tile tile)
    {
        if (!tile || !_canSelectTiles)
        {
            return;
        }

        if (tile == selectedTile || !tile.IsActive)
        {
            ReleaseItem();
            return;
        }

        if (heldMatchObject)
        {
            StartCoroutine(RunGameLogic(tile.GridPosition, selectedTile.GridPosition));
            return;
        }

        selectedTile = tile;
        heldMatchObject = selectedTile.CurrentMatchObject;
        heldMatchObject.SetHeld(true);
        mouseIndicator.EnableIndicator(selectedTile);
    }
    
    private void ReleaseItem()
    {
        selectedTile?.SetSelected(false);
        selectedTile = null;
        heldMatchObject?.SetHeld(false);
        heldMatchObject = null;
        mouseIndicator.DisableIndicator();
    }
    
    private IEnumerator RunGameLogic(Vector2Int posA, Vector2Int posB)
    {
        _canSelectTiles = false;
        hoveredTile?.SetHovered(false);
        
        // Swap items
        yield return StartCoroutine(SwapObjects(posA, posB));
        
        // Check for matches
        List<Tile> matches = FindMatches();
        if (matches.Count == 0)  yield return null;
        
        // Handle matches
        yield return StartCoroutine(HandleMatches(matches));
        
        // Make objects fall
        yield return StartCoroutine(MoveObjects());
        
        
        // Repopulate grid
        yield return StartCoroutine(PopulateGrid(populationDuration, populationDirection));

    }
    
    private IEnumerator SwapObjects(Vector2Int posA, Vector2Int posB)
    {
        var tileA = GetTile(posA);
        var tileB = GetTile(posB);
        
        if (!tileA || !tileB || !tileA.IsActive || !tileB.IsActive) yield return null;
        
        var itemA = tileA.CurrentMatchObject;
        var itemB = tileB.CurrentMatchObject;
        
        tileA.SetCurrentItem(itemB);
        tileB.SetCurrentItem(itemA);
        
        itemA.SetCurrentTile(tileB);
        itemB.SetCurrentTile(tileA);

        ReleaseItem();
        yield return new WaitForSeconds(1f);
    }
    

    private IEnumerator HandleMatches(List<Tile> matches)
    {
        foreach (var match in matches)
        {
            match.CurrentMatchObject.DestroyObject();
            match.SetCurrentItem(null);
            yield return new WaitForSeconds(0.1f);
        }
    }

    private IEnumerator MoveObjects()
    {
        for (var x = 0; x < gridShape.Grid.Width; x++)
        {
            for (var y = gridShape.Grid.Height - 1; y >= 0; y--)
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
        for (var y = 0; y < gridShape.Grid.Height; y++)
        {
            for (var x = 0; x < gridShape.Grid.Width - 2; x++)
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
        for (var x = 0; x < gridShape.Grid.Width; x++)
        {
            for (var y = 0; y < gridShape.Grid.Height - 2; y++)
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
        return tile != null ? tile.CurrentMatchObject : null;
    }
    
    public bool IsValidPosition(Vector2Int position)
    {
        var tile = GetTile(position);
        return tile != null && tile.IsActive;
    }

    public Vector2Int GetGridPosition(Vector3 position)
    {
        var grid = gridShape.Grid;
        for (int x = 0; x < grid.Width; x++)
        {
            for (int y = 0; y < grid.Height; y++)
            {
                Vector3 tilePosition = gridOffset + new Vector3(
                    x * (tileSize.x + tileOffset.x), 
                    (grid.Height - 1 - y) * (tileSize.y + tileOffset.y), 
                    0
                );

                Rect tileRect = new Rect(
                    tilePosition.x - tileSize.x / 2,
                    tilePosition.y - tileSize.y / 2,
                    tileSize.x,
                    tileSize.y
                );

                if (tileRect.Contains(new Vector2(position.x, position.y)))
                {
                    return new Vector2Int(x, y);
                }
            }
        }

        return new Vector2Int(-1, -1);
    }

    #endregion

    
    
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!gridShape) return;
    
        var grid = gridShape.Grid;
        var labelStyle = new GUIStyle()
        {

            fontSize = 12,
            normal = { textColor = Color.white },
            alignment = TextAnchor.MiddleCenter
        };

        for (int x = 0; x < grid.Width; x++)
        {
            for (int y = 0; y < grid.Height; y++)
            {
                var tileState = grid.IsTileActive(x, y);
                
                Vector3 tilePosition = gridOffset + new Vector3(
                    x * (tileSize.x + tileOffset.x), 
                    (grid.Height - 1 - y) * (tileSize.y + tileOffset.y), 
                    0
                );
                
                Gizmos.color = tileState ? Color.green : Color.white;
                Gizmos.DrawWireCube(tilePosition, tileSize);
                Handles.Label(tilePosition, $"{x},{y}", labelStyle);
            }
        }
    }
#endif
}