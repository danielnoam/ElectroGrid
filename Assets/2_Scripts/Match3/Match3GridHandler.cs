using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Match3GridHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Match3Tile match3TilePrefab;
    [SerializeField] private Transform tilesParent;
    [SerializeField] private Match3Object match3ObjectPrefab;
    [SerializeField] private Transform matchObjectsParent;
    
    private readonly Dictionary<Vector2Int, Match3Tile> _tiles = new Dictionary<Vector2Int, Match3Tile>();
    private SOGridShape _currentGridShape;
    
    public IReadOnlyDictionary<Vector2Int, Match3Tile> Tiles => _tiles;
    public SOGridShape GridShape => _currentGridShape;

    
    
    public void CreateGrid(SOGridShape gridShape)
    {
        if (!gridShape || !match3TilePrefab) return;

        DestroyGrid();
        
        _currentGridShape = gridShape;
        var grid = gridShape.Grid;

        for (int x = 0; x < grid.Width; x++)
        {
            for (int y = 0; y < grid.Height; y++)
            {
                Vector2Int tileGridPosition = new Vector2Int(x, y);
                Vector3 tileWorldPosition = grid.GetTileWorldPosition(x, y);
                bool tileState = grid.IsTileActive(x, y);

                var tile = CreateTile(tileWorldPosition, tileGridPosition, tileState);
                _tiles.Add(tileGridPosition, tile);
            }
        }
    }
    
    private Match3Tile CreateTile(Vector3 position, Vector2Int gridPos, bool isActive)
    {
        var tile = Instantiate(match3TilePrefab, position, Quaternion.identity, tilesParent);
        tile.Initialize(gridPos, isActive);
        return tile;
    }
    
    
    public Match3Object CreateMatchObject(SOItemData itemData, Match3Tile match3Tile)
    {
        if (!IsValidTile(match3Tile)) return null;

        var topMostTile = GetTile(new Vector2Int(match3Tile.GridPosition.x, 0));
        
        var spawnPosition = new Vector3(match3Tile.transform.localPosition.x,
            topMostTile.transform.localPosition.y + topMostTile.transform.localScale.y,
            match3Tile.transform.localPosition.z);
        
        var item = Instantiate(match3ObjectPrefab, spawnPosition, Quaternion.identity, matchObjectsParent);
        
        item.Initialize(itemData);
        match3Tile.SetCurrentItem(item);
        item.SetCurrentTile(match3Tile);
        
        return item;
    }
    

    public void DestroyGrid()
    {
        DestroyAllObjects();
        DestroyTiles();
    }

    
    private void DestroyTiles()
    {
        foreach (var tile in _tiles.Values)
        {
            if (tile)
            {
                if (Application.isEditor)
                {
                    DestroyImmediate(tile.gameObject);
                }
                else
                {
                    Destroy(tile.gameObject);
                }
            }
        }

        if (Application.isEditor)
        {
            while (tilesParent.childCount > 0)
            {
                DestroyImmediate(tilesParent.GetChild(0).gameObject);
            }
        }
        else
        {
            foreach (Transform child in tilesParent)
            {
                Destroy(child.gameObject);
            }
        }

        _tiles.Clear();
    }
    
    public void DestroyAllObjects()
    {
        foreach (var tile in _tiles.Values)
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

    

    

    public Match3Tile GetTile(Vector2Int position)
    {
        _tiles.TryGetValue(position, out Match3Tile tile);
        return tile;
    }

    public bool IsValidTile(Match3Tile match3Tile)
    {
        return match3Tile && match3Tile.IsActive;
    }

    public Match3Tile GetRandomValidTile()
    {
        var validTiles = _tiles.Values.Where(IsValidTile).ToList();
        return validTiles.Count > 0 ? validTiles[Random.Range(0, validTiles.Count)] : null;
    }

    public int GetActiveTileCount()
    {
        return _tiles.Values.Count(tile => tile.IsActive);
    }

    public Vector2Int GetGridDimensions()
    {
        if (_tiles.Count == 0) return Vector2Int.zero;

        int maxX = 0, maxY = 0;
        foreach (var pos in _tiles.Keys)
        {
            if (pos.x > maxX) maxX = pos.x;
            if (pos.y > maxY) maxY = pos.y;
        }

        return new Vector2Int(maxX, maxY);
    }


    
    
    private void OnDrawGizmos()
    {
        GridShape?.Grid?.DrawGrid();
    }
}