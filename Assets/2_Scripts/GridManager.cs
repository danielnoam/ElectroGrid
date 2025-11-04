using System;
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
    
    [Header("References")]
    [SerializeField] private Tile tilePrefab;
    [SerializeField] private Item itemPrefab;
    [SerializeField] private Transform itemsParent;
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
    [SerializeField, ReadOnly] private Item heldItem;
    
    private readonly Dictionary<Vector2Int, Tile> _tiles = new Dictionary<Vector2Int, Tile>();


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
        
        UpdateGrid();
    }




    #region Setup

    private Tile CreateTile(Vector3 position, Vector2Int gridPos, bool isActive)
    {
        var tile = Instantiate(tilePrefab, position, Quaternion.identity, tilesParent);
        tile.transform.localScale = new Vector3(tileSize.x, tileSize.y, 1f);
        tile.Initialize(gridPos, isActive);
        return tile;
    }

    private Item CreateItem(SOItemData itemData, Tile tile)
    {
        if (tile == null || !tile.IsActive)
            return null;

        var item = Instantiate(itemPrefab, new Vector3(tile.transform.position.x,tile.transform.position.y, itemsParent.position.z), Quaternion.identity, itemsParent);
        item.transform.localScale = new Vector3(tileSize.x, tileSize.y, 1f);
        item.Initialize(itemData);
        
        tile.SetCurrentItem(item);
        item.SetCurrentTile(tile);
        
        return item;
    }

    
    [Button]
    private void UpdateGrid()
    {
        if (!gridShape || !tilePrefab || !itemPrefab) return;

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
        
        foreach (var kvp in _tiles)
        {
            var tile = kvp.Value;
            if (tile.IsActive)
            {
                var randomItemData = items.GetRandomItem();
                if (randomItemData != null)
                {
                    CreateItem(randomItemData, tile);
                }
            }
        }
    }
    
    [Button]
    private void ClearGrid()
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
        while (itemsParent.childCount > 0)
        {
            DestroyImmediate(itemsParent.GetChild(0).gameObject);
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

    
    public void SelectItemInTile(Tile tile, out bool wasSelected)
    {
        if (!tile || tile == selectedTile)
        {
            wasSelected = false;
            return;
        }

        if (heldItem)
        {
            SwapItems(tile.GridPosition, selectedTile.GridPosition);
            ReleaseItem();
            wasSelected = false;
            return;
        }

        selectedTile = tile;
        heldItem = selectedTile.CurrentItem;
        mouseIndicator.SetSprite(heldItem.ItemData.Sprite);
        wasSelected = true;
    }
    
    private void ReleaseItem()
    {
        selectedTile?.SetSelected(false);
        selectedTile = null;
        heldItem = null;
        mouseIndicator.SetSprite(null);
    }
    
    public Tile GetTile(Vector2Int position)
    {
        _tiles.TryGetValue(position, out Tile tile);
        return tile;
    }
    
    public Item GetItem(Vector2Int position)
    {
        var tile = GetTile(position);
        return tile != null ? tile.CurrentItem : null;
    }
    
    public bool IsValidPosition(Vector2Int position)
    {
        var tile = GetTile(position);
        return tile != null && tile.IsActive;
    }
    
    public void SwapItems(Vector2Int posA, Vector2Int posB)
    {
        var tileA = GetTile(posA);
        var tileB = GetTile(posB);
        
        if (tileA == null || tileB == null || !tileA.IsActive || !tileB.IsActive)
            return;
        
        var itemA = tileA.CurrentItem;
        var itemB = tileB.CurrentItem;
        
        tileA.SetCurrentItem(itemB);
        tileB.SetCurrentItem(itemA);
        
        if (itemA != null) itemA.SetCurrentTile(tileB);
        if (itemB != null) itemB.SetCurrentTile(tileA);
    }
    
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