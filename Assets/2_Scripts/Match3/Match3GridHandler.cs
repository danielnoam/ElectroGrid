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
    
    private readonly Dictionary<Vector2Int, Match3Tile> _tiles = new Dictionary<Vector2Int, Match3Tile>();
    
    public IReadOnlyDictionary<Vector2Int, Match3Tile> Tiles => _tiles;
    public SOGridShape GridShape => match3GameManager.GridShape;
    public Grid Grid => GridShape.Grid;
    
    
    public event Action GridDestroyed;
    
    
    public void CreateGrid(SOGridShape gridShape)
    {
        if (!gridShape || !match3TilePrefab) return;

        DestroyGrid();
        

        for (int x = 0; x < Grid.Width; x++)
        {
            for (int y = 0; y < Grid.Height; y++)
            {
                Vector2Int tileGridPosition = new Vector2Int(x, y);
                Vector3 tileWorldPosition = Grid.GetCellWorldPosition(x, y);
                bool tileState = Grid.IsCellActive(x, y);

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
        
        var spawnPosition = Grid.GetCellWorldPosition(match3Tile.GridPosition.x, Grid.Height);
        var itemGo = ObjectPooler.GetObjectFromPool(match3ObjectPrefab.gameObject, spawnPosition, Quaternion.identity);
        var item = itemGo.GetComponent<Match3Object>();
        
        item.Initialize(itemData, this);
        match3Tile.SetCurrentItem(item);
        item.SetCurrentTile(match3Tile);

        return item;
    }
    

    private void DestroyGrid()
    {
        GridDestroyed?.Invoke();
        
        foreach (var tile in _tiles.Values)
        {
            if (tile)
            {
                tile.SetCurrentItem(null);
            }
        }
        
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
    
    private void OnDrawGizmos()
    {
        Grid?.DrawGrid();
    }
}