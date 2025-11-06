using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif


[System.Serializable]
public class Grid
{
    [SerializeField] private Vector2Int gridSize;
    [SerializeField] private Vector3 tileSize;
    [SerializeField] private Vector3 tileSpacing;
    [SerializeField] private bool[] tiles; 
    
    

    public int Width => gridSize.x;
    public int Height => gridSize.y;
    public Vector3 TileSize => tileSize;
    public Vector3 TileSpacing => tileSpacing;
    public int TotalTilesCount => Width * Height;
    public int ActiveTilesCount
    {
        get
        {
            int count = 0;
            for (int i = 0; i < tiles.Length; i++)
            {
                if (tiles[i]) count++;
            }
            return count;
        }
    }
    
    public bool[] Tiles => tiles;

    public Grid(int width = 8, int height = 8)
    {
        gridSize = new Vector2Int(width, height);
        tileSize = new Vector3(1f, 1f, 0);
        tileSpacing = new Vector3(0.1f, 0.1f, 0);
        InitializeTiles();
    }

    private void InitializeTiles()
    {
        tiles = new bool[TotalTilesCount];
        for (int i = 0; i < tiles.Length; i++)
        {
            tiles[i] = true;
        }
    }

    public bool IsTileActive(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return false;

        return tiles[y * Width + x];
    }

    public void SetTileActive(int x, int y, bool active)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return;

        tiles[y * Width + x] = active;
    }

    public void ActivateTile(int x, int y)
    {
        SetTileActive(x, y, true);
    }

    public void DeactivateTile(int x, int y)
    {
        SetTileActive(x, y, false);
    }

    public void ToggleTile(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return;

        tiles[y * Width + x] = !tiles[y * Width + x];
    }

    public void Resize(int newWidth, int newHeight)
    {
        bool[] newTiles = new bool[newWidth * newHeight];
        
        for (int y = 0; y < Mathf.Min(Height, newHeight); y++)
        {
            for (int x = 0; x < Mathf.Min(Width, newWidth); x++)
            {
                newTiles[y * newWidth + x] = tiles[y * Width + x];
            }
        }

        // Fill new tiles with active state
        for (int y = 0; y < newHeight; y++)
        {
            for (int x = 0; x < newWidth; x++)
            {
                if (x >= Width || y >= Height)
                {
                    newTiles[y * newWidth + x] = true;
                }
            }
        }
        
        gridSize = new Vector2Int(newWidth, newHeight);
        tiles = newTiles;
    }

    public void ActivateAll()
    {
        for (int i = 0; i < tiles.Length; i++)
        {
            tiles[i] = true;
        }
    }

    public void DeactivateAll()
    {
        for (int i = 0; i < tiles.Length; i++)
        {
            tiles[i] = false;
        }
    }

    public void InvertAll()
    {
        for (int i = 0; i < tiles.Length; i++)
        {
            tiles[i] = !tiles[i];
        }
    }

    public Vector2Int[] GetActiveTilesPositions()
    {
        int count = ActiveTilesCount;
        Vector2Int[] activeTiles = new Vector2Int[count];
        int index = 0;

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                if (IsTileActive(x, y))
                {
                    activeTiles[index] = new Vector2Int(x, y);
                    index++;
                }
            }
        }

        return activeTiles;
    }
    
    public Vector2Int GetPositionInGird(Vector3 position)
    {
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                Vector3 tilePosition = GetTileWorldPosition(x, y);

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
    
    public Vector3 GetTileWorldPosition(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height) return Vector3.zero;
    
        // Calculate the total size of the grid including spacing
        float totalGridWidth = (Width * tileSize.x) + ((Width - 1) * tileSpacing.x);
        float totalGridHeight = (Height * tileSize.y) + ((Height - 1) * tileSpacing.y);
    
        // Calculate offset to center the grid at (0,0,0)
        float offsetX = -totalGridWidth / 2f + tileSize.x / 2f;
        float offsetY = -totalGridHeight / 2f + tileSize.y / 2f;
    
        // Calculate tile position with spacing
        float posX = x * (tileSize.x + tileSpacing.x);
        float posY = (Height - 1 - y) * (tileSize.y + tileSpacing.y);
        float posZ = (x + y) * (tileSize.z + tileSpacing.z);
    
        return new Vector3(
            posX + offsetX,
            posY + offsetY,
            posZ
        );
    }
    
    
    public void DrawGrid()
    {
        var labelStyle = new GUIStyle()
        {

            fontSize = 12,
            normal = { textColor = Color.white },
            alignment = TextAnchor.MiddleCenter
        };

        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                var tileState = IsTileActive(x, y);
                var tilePosition = GetTileWorldPosition(x, y);
                
                Gizmos.color = tileState ? Color.green : Color.white;
                Gizmos.DrawWireCube(tilePosition, tileSize);
                
                #if UNITY_EDITOR
                Handles.Label(tilePosition, $"{x},{y}", labelStyle);
                #endif
            }
        }
    }
    
}