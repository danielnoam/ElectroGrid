using System.Collections.Generic;
using System.Linq;
using UnityEngine;




public class Match3GridManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Tile tilePrefab;
    [SerializeField] private Transform tilesParent;
    
    
    
    
    private readonly Dictionary<Vector2Int, Tile> _tiles = new Dictionary<Vector2Int, Tile>();
    public IReadOnlyDictionary<Vector2Int, Tile> Tiles => _tiles;

    
    
    public void CreateGrid(SOGridShape gridShape)
    {
        if (!gridShape || !tilePrefab) return;

        DestroyGrid();

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

    public void DestroyGrid()
    {
        ClearAllTileObjects();
        DestroyTiles();
    }

    private Tile CreateTile(Vector3 position, Vector2Int gridPos, bool isActive)
    {
        var tile = Instantiate(tilePrefab, position, Quaternion.identity, tilesParent);
        tile.Initialize(gridPos, isActive);
        return tile;
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

    private void ClearAllTileObjects()
    {
        foreach (var tile in _tiles.Values)
        {
            if (tile)
            {
                tile.SetCurrentItem(null);
            }
        }
    }




    #region Helper Methods

    public Tile GetTile(Vector2Int position)
    {
        _tiles.TryGetValue(position, out Tile tile);
        return tile;
    }

    public bool IsValidTile(Tile tile)
    {
        return tile && tile.IsActive;
    }

    public Tile GetRandomValidTile()
    {
        var validTiles = _tiles.Values.Where(IsValidTile).ToList();
        return validTiles.Count > 0 ? validTiles[UnityEngine.Random.Range(0, validTiles.Count)] : null;
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

    #endregion
}