using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif


[System.Serializable]
public class Grid
{
    public Vector2Int size;
    public Vector3 origin;
    public Vector3 cellSize;
    public Vector3 cellSpacing;
    public bool[] cells;
    [SerializeReference] public CoordinateConverter coordinateConverter;
    [SerializeField, HideInInspector] private bool editorFoldout = true;
    
    public int Width => size.x;
    public int Height => size.y;
    public int TotalCellCount => Width * Height;
    public int ActiveCellCount
    {
        get
        {
            int count = 0;
            for (int i = 0; i < cells.Length; i++)
            {
                if (cells[i]) count++;
            }
            return count;
        }
    }
    



    
    public Grid(int width = 8, int height = 8)
    {
        size = new Vector2Int(width, height);
        origin =  Vector3.zero;
        cellSize = new Vector3(1f, 1f, 0);
        cellSpacing = new Vector3(0.1f, 0.1f, 0);
        coordinateConverter = new VerticalConvertor();
        
        InitializeCells();
    }
    
    public Grid(int width, int height, Vector3 origin, Vector3 cellSize, Vector3 cellSpacing, CoordinateConverter coordinateConverter)
    {
        size = new Vector2Int(width, height);
        this.origin = origin;
        this.cellSize = cellSize;
        this.cellSpacing = cellSpacing;
        this.coordinateConverter = coordinateConverter ?? new VerticalConvertor();
        InitializeCells();
    }

    private void InitializeCells()
    {
        cells = new bool[TotalCellCount];
        for (int i = 0; i < cells.Length; i++)
        {
            cells[i] = true;
        }
    }
    public void SetCellActive(int x, int y, bool active)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return;

        cells[y * Width + x] = active;
    }

    public void ActivateCell(int x, int y)
    {
        SetCellActive(x, y, true);
    }

    public void DeactivateTile(int x, int y)
    {
        SetCellActive(x, y, false);
    }

    public void ToggleCell(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return;

        cells[y * Width + x] = !cells[y * Width + x];
    }

    public void Resize(int newWidth, int newHeight)
    {
        bool[] newCells = new bool[newWidth * newHeight];
        
        for (int y = 0; y < Mathf.Min(Height, newHeight); y++)
        {
            for (int x = 0; x < Mathf.Min(Width, newWidth); x++)
            {
                newCells[y * newWidth + x] = cells[y * Width + x];
            }
        }

        // Fill new cells with active state
        for (int y = 0; y < newHeight; y++)
        {
            for (int x = 0; x < newWidth; x++)
            {
                if (x >= Width || y >= Height)
                {
                    newCells[y * newWidth + x] = true;
                }
            }
        }
        
        size = new Vector2Int(newWidth, newHeight);
        cells = newCells;
    }

    public void ActivateAll()
    {
        for (int i = 0; i < cells.Length; i++)
        {
            cells[i] = true;
        }
    }

    public void DeactivateAll()
    {
        for (int i = 0; i < cells.Length; i++)
        {
            cells[i] = false;
        }
    }

    public void InvertAll()
    {
        for (int i = 0; i < cells.Length; i++)
        {
            cells[i] = !cells[i];
        }
    }

    public Vector2Int[] GetActiveCellsPositions()
    {
        int count = ActiveCellCount;
        Vector2Int[] activeTiles = new Vector2Int[count];
        int index = 0;

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                if (IsCellActive(x, y))
                {
                    activeTiles[index] = new Vector2Int(x, y);
                    index++;
                }
            }
        }

        return activeTiles;
    }
    
    public Vector2Int GetCell(Vector3 position)
    {
        Vector2Int gridPos = coordinateConverter.WorldToGrid(position, size, cellSize, cellSpacing, origin);
        
        if (gridPos.x < 0 || gridPos.x >= Width || gridPos.y < 0 || gridPos.y >= Height)
        {
            return new Vector2Int(-1, -1);
        }
    
        return gridPos;
    }
    
    public Vector3 GetCellWorldPosition(int x, int y)
    {
        return coordinateConverter.GridToWorldCenter(
            new Vector2Int(x, y), 
            size, 
            cellSize, 
            cellSpacing, 
            origin
        );
    }

    public Vector3 GetCellWorldPosition(Vector2Int position)
    {
        
        return coordinateConverter.GridToWorldCenter(
            position, 
            size, 
            cellSize, 
            cellSpacing, 
            origin
        );
    }
    
    public Vector2Int GetNeighboringCell(Vector2Int tile, Vector2Int direction)
    {
        Vector2Int neighboringTile = tile + direction;

        if (neighboringTile.x < 0 || neighboringTile.x >= Width || neighboringTile.y < 0 || neighboringTile.y >= Height)
        {
            return new Vector2Int(-1, -1);
        }

        return neighboringTile;
    }
    
    public bool IsTouchingEdge(Vector2Int tile, out List<Vector2Int> directions)
    {
        directions = new List<Vector2Int>();

        // Main directions
        if (tile.x == 0)
        {
            directions.Add(Vector2Int.left);
        }
        if (tile.x == Width - 1)
        {
            directions.Add(Vector2Int.right);
        }
        if (tile.y == 0)
        {
            directions.Add(Vector2Int.down);
        }
        if (tile.y == Height - 1)
        {
            directions.Add(Vector2Int.up);
        }
        
        // Diagonals
        if (tile.x == Width - 1 && tile.y == Height - 1)
        {
            directions.Add(new Vector2Int(1,1));
        }
        if (tile.x == Width - 1 && tile.y == 0)
        {
            directions.Add(new Vector2Int(1, -1));
        }
        if (tile.x == 0 && tile.y == Height - 1)
        {
            directions.Add(new Vector2Int(-1, 1));
        }
        if (tile is { x: 0, y: 0 })
        {
            directions.Add(new Vector2Int(-1, -1));
        }

        return directions.Count > 0;

    }
    
    public bool AreCellsNeighbors(Vector2Int tile1, Vector2Int tile2)
    {
        Vector2Int difference = tile1 - tile2;
        int manhattanDistance = Mathf.Abs(difference.x) + Mathf.Abs(difference.y);
    
        return manhattanDistance == 1;
    }
    
    
    public bool IsCellActive(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return false;

        return cells[y * Width + x];
    }

    
    
    public void DrawGrid()
    {
        coordinateConverter ??= new VerticalConvertor();
        
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
                var tileState = IsCellActive(x, y);
                var tilePosition = GetCellWorldPosition(x, y);
            
                Gizmos.color = tileState ? Color.green : Color.white;
                
                Gizmos.matrix = Matrix4x4.TRS(tilePosition, Quaternion.LookRotation(coordinateConverter.Forward), Vector3.one);
                Gizmos.DrawWireCube(Vector3.zero, cellSize);
                Gizmos.matrix = Matrix4x4.identity;
            
                #if UNITY_EDITOR
                Handles.Label(tilePosition, $"{x},{y}", labelStyle);
                #endif
            }
        }
    }
}