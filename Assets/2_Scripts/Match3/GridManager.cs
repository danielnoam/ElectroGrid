using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using DNExtensions;
using DNExtensions.Button;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }
    
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
    [SerializeField] private ChanceList<SOItemData> items = new ChanceList<SOItemData>();
    
    [Space(25f)][Separator("Debug")]
    [SerializeField] private bool drawGrid = true;
    [SerializeField] private bool drawOnlyInactive;
    [SerializeField] private Color activeColor = Color.green;
    [SerializeField] private Color inactiveColor = Color.white;
    [SerializeField] private GUIStyle labelStyle = new GUIStyle();
    [SerializeField, ReadOnly] private Tile selectedTile;
    [SerializeField, ReadOnly] private MatchObject heldMatchObject;
    
    private enum PopulationDirection { TopToBottom, BottomToTop, LeftToRight, RightToLeft }
    private readonly Dictionary<Vector2Int, Tile> _tiles = new Dictionary<Vector2Int, Tile>();
    private Coroutine _gridPopulationRoutine;
    private bool _isPopulating;

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
        
        CreateGird();
    }


    #region Objects Creation
    
    private void StartPopulatingGrid(float duration, PopulationDirection direction)
    {
        _isPopulating = true;
        if (_gridPopulationRoutine != null) StopCoroutine(_gridPopulationRoutine);
        _gridPopulationRoutine = StartCoroutine(PopulateGrid(duration, direction));

    }

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
        _isPopulating = false;
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
                    if (_tiles.TryGetValue(new Vector2Int(x, y), out var tile) && tile.IsActive)
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
                    if (_tiles.TryGetValue(new Vector2Int(x, y), out var tile) && tile.IsActive)
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
                    if (_tiles.TryGetValue(new Vector2Int(x, y), out var tile) && tile.IsActive)
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
                    if (_tiles.TryGetValue(new Vector2Int(x, y), out var tile) && tile.IsActive)
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
    
    _isPopulating = false;
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
        
        StartPopulatingGrid(populationDuration, populationDirection);
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
        foreach (Transform child in itemsParent)
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

    public void SelectItemInTile(Tile tile, out bool wasSelected)
    {
        if (!tile || tile == selectedTile || !tile.IsActive || _isPopulating)
        {
            wasSelected = false;
            return;
        }

        if (heldMatchObject)
        {
            SwapItems(tile.GridPosition, selectedTile.GridPosition);
            ReleaseItem();
            wasSelected = false;
            return;
        }

        selectedTile = tile;
        heldMatchObject = selectedTile.CurrentMatchObject;
        heldMatchObject.SetHeld(true);
        mouseIndicator.EnableIndicator(selectedTile);
        wasSelected = true;
    }
    
    private void ReleaseItem()
    {
        selectedTile?.SetSelected(false);
        selectedTile = null;
        heldMatchObject?.SetHeld(false);
        heldMatchObject = null;
        mouseIndicator.DisableIndicator();
    }
    
    public void SwapItems(Vector2Int posA, Vector2Int posB)
    {
        var tileA = GetTile(posA);
        var tileB = GetTile(posB);
        
        if (!tileA || !tileB || !tileA.IsActive || !tileB.IsActive) return;
        
        var itemA = tileA.CurrentMatchObject;
        var itemB = tileB.CurrentMatchObject;
        
        tileA.SetCurrentItem(itemB);
        tileB.SetCurrentItem(itemA);
        
        if (itemA) itemA.SetCurrentTile(tileB);
        if (itemB) itemB.SetCurrentTile(tileA);
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

    #endregion

    
    
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!gridShape || !drawGrid) return;
    
        var grid = gridShape.Grid;

        for (int x = 0; x < grid.Width; x++)
        {
            for (int y = 0; y < grid.Height; y++)
            {
                var tileState = grid.IsTileActive(x, y);
                
                if (drawOnlyInactive && tileState) continue;
                
                Vector3 tilePosition = gridOffset + new Vector3(
                    x * (tileSize.x + tileOffset.x), 
                    (grid.Height - 1 - y) * (tileSize.y + tileOffset.y), 
                    0
                );
                
                Gizmos.color = tileState ? activeColor : inactiveColor;
                Gizmos.DrawWireCube(tilePosition, tileSize);
                Handles.Label(tilePosition, $"{x},{y}", labelStyle);
            }
        }
    }
#endif
}