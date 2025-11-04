using UnityEngine;

[System.Serializable]
public class Grid
{
    [SerializeField] private int width;
    [SerializeField] private int height;
    [SerializeField] private bool[] tiles; 

    public int Width => width;
    public int Height => height;
    public int TotalTilesCount => width * height;
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
        this.width = width;
        this.height = height;
        InitializeTiles();
    }

    private void InitializeTiles()
    {
        tiles = new bool[width * height];
        for (int i = 0; i < tiles.Length; i++)
        {
            tiles[i] = true;
        }
    }

    public bool IsTileActive(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
            return false;

        return tiles[y * width + x];
    }

    public void SetTileActive(int x, int y, bool active)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
            return;

        tiles[y * width + x] = active;
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
        if (x < 0 || x >= width || y < 0 || y >= height)
            return;

        tiles[y * width + x] = !tiles[y * width + x];
    }

    public void Resize(int newWidth, int newHeight)
    {
        bool[] newTiles = new bool[newWidth * newHeight];
        
        for (int y = 0; y < Mathf.Min(height, newHeight); y++)
        {
            for (int x = 0; x < Mathf.Min(width, newWidth); x++)
            {
                newTiles[y * newWidth + x] = tiles[y * width + x];
            }
        }

        // Fill new tiles with active state
        for (int y = 0; y < newHeight; y++)
        {
            for (int x = 0; x < newWidth; x++)
            {
                if (x >= width || y >= height)
                {
                    newTiles[y * newWidth + x] = true;
                }
            }
        }

        width = newWidth;
        height = newHeight;
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

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
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
}