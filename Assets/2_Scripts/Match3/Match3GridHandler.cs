using System;
using System.Collections.Generic;
using System.Linq;
using DNExtensions.ObjectPooling;
using UnityEngine;
using Random = UnityEngine.Random;

public class Match3GridHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Match3GameManager match3GameManager;
    [SerializeField] private Match3Tile match3TilePrefab;
    [SerializeField] private Transform tilesParent;
    [SerializeField] private Match3Object match3ObjectPrefab;
    [SerializeField] private SOGridShape defaultGridShape;
    
    private readonly Dictionary<Vector2Int, Match3Tile> _tiles = new Dictionary<Vector2Int, Match3Tile>();
    private SOGridShape _gridShape;
    
    public IReadOnlyDictionary<Vector2Int, Match3Tile> Tiles => _tiles;
    public Grid Grid => GridShape.Grid;
    public SOGridShape GridShape => _gridShape ? _gridShape : defaultGridShape;
    
    public event Action GridDestroyed;
    
    
    public void CreateGrid(SOMatch3Level level)
    {
        if (!level || !match3TilePrefab) return;
        
        _gridShape = level.GridShape;

        // Clear previous grid
        foreach (var tile in _tiles.Values)
        {
            if (tile)
            {
                tile.SetCurrentItem(null);
            }
        }
        _tiles.Clear();
        GridDestroyed?.Invoke();

        // Find BreakTilesObjective if it exists
        ClearLayersObjective breakTilesObjective = null;
        if (level.Objectives != null)
        {
            foreach (var objective in level.Objectives)
            {
                if (objective is ClearLayersObjective breakObj)
                {
                    breakTilesObjective = breakObj;
                    break;
                }
            }
        }

        // Create tiles
        for (int x = 0; x < Grid.Width; x++)
        {
            for (int y = 0; y < Grid.Height; y++)
            {
                Vector2Int tileGridPosition = new Vector2Int(x, y);
                Vector3 tileWorldPosition = Grid.GetCellWorldPosition(x, y);
                bool tileState = Grid.IsCellActive(x, y);
                bool breakableLayer = breakTilesObjective != null && breakTilesObjective.TileHasLayer(x, y);

                var tile = CreateTile(tileWorldPosition, tileGridPosition, tileState, breakableLayer ? 1 : 0);
                _tiles.Add(tileGridPosition, tile);
            }
        }
    }
    
    private Match3Tile CreateTile(Vector3 position, Vector2Int gridPos, bool isActive, int breakableLayerHealth)
    {
        var tileGo = ObjectPooler.GetObjectFromPool(match3TilePrefab.gameObject, position, Quaternion.identity);
        var tile = tileGo.GetComponent<Match3Tile>();
        tile.Initialize(match3GameManager, gridPos, isActive, breakableLayerHealth);
        
        return tile;
    }
    
    
    public Match3Object CreateMatchObject(SOItemData itemData, Match3Tile match3Tile)
    {
        if (!IsValidTile(match3Tile)) return null;
        
        var spawnPosition = Grid.GetCellWorldPosition(match3Tile.GridPosition.x, Grid.Height);
        var itemGo = ObjectPooler.GetObjectFromPool(match3ObjectPrefab.gameObject, spawnPosition, Quaternion.identity);
        var item = itemGo.GetComponent<Match3Object>();
        
        item.Initialize(itemData, this);
        match3Tile.SetCurrentItem(item);
        item.SetCurrentTile(match3Tile);

        return item;
    }

    

    public Match3Tile GetTile(Vector2Int position)
    {
        _tiles.TryGetValue(position, out Match3Tile tile);
        return tile;
    }

    public bool IsValidTile(Match3Tile match3Tile)
    {
        return match3Tile && match3Tile.IsActive;
    }
    
    public bool CanSelectTile(Match3Tile match3Tile)
    {
        return IsValidTile(match3Tile) && !match3Tile.HasLayer && match3Tile.HasObject;
    }

    public Match3Tile GetRandomValidTile()
    {
        var validTiles = _tiles.Values.Where(IsValidTile).ToList();
        return validTiles.Count > 0 ? validTiles[Random.Range(0, validTiles.Count)] : null;
    }
    
    public bool AreTilesNeighbours(Match3Tile tile1, Match3Tile tile2)
    {
        return Grid.AreCellsNeighbors(tile1.GridPosition, tile2.GridPosition);
    }
    

    public int GetActiveTileCount()
    {
        return _tiles.Values.Count(tile => tile.IsActive);
    }
    
    private void OnDrawGizmos()
    {
        Grid?.DrawGrid();
    }
}