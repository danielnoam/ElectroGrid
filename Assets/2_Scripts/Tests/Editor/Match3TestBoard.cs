using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// Builds a real grid handler, play handler and tiles from a text picture, without a scene or the pooler.
/// </summary>
/// <remarks>
/// Rows are written top first, like the board looks on screen, so the last row is y = 0.
/// A letter is a matchable piece (same letter, same item), '.' an empty active tile,
/// '#' a cell with no tile, '*' an obstacle and '_' a bottom object.
/// </remarks>
internal sealed class Match3TestBoard : IDisposable
{
    private readonly List<Object> _created = new List<Object>();
    private readonly Dictionary<char, SOItemData> _items = new Dictionary<char, SOItemData>();
    private readonly Dictionary<Vector2Int, Match3Tile> _tiles;

    public Match3GameManager GameManager { get; }
    public Match3GridHandler GridHandler { get; }
    public Match3PlayHandler PlayHandler { get; }
    public SOGridShape GridShape { get; }

    public Match3TestBoard(params string[] rows)
    {
        int height = rows.Length;
        int width = rows[0].Length;

        var root = Track(new GameObject("Match3TestBoard"));
        GameManager = root.AddComponent<Match3GameManager>();
        GridHandler = root.AddComponent<Match3GridHandler>();
        PlayHandler = root.AddComponent<Match3PlayHandler>();

        TestUtils.SetField(GridHandler, "match3GameManager", GameManager);
        TestUtils.SetField(PlayHandler, "gameManager", GameManager);
        TestUtils.SetField(PlayHandler, "gridHandler", GridHandler);

        GridShape = Track(ScriptableObject.CreateInstance<SOGridShape>());
        TestUtils.SetField(GridShape, "grid", new Grid(width, height));

        _tiles = TestUtils.GetField<Dictionary<Vector2Int, Match3Tile>>(GridHandler, "_tiles");

        for (int row = 0; row < height; row++)
        {
            if (rows[row].Length != width) throw new ArgumentException($"Row {row} is not {width} wide");

            int y = height - 1 - row;
            for (int x = 0; x < width; x++)
            {
                char cell = rows[row][x];
                if (cell == '#')
                {
                    GridShape.Grid.SetCellActive(x, y, false);
                    continue;
                }

                var tile = CreateTile(new Vector2Int(x, y));
                switch (cell)
                {
                    case '.': break;
                    case '*': Place<Match3ObstacleObject>(tile, null); break;
                    case '_': Place<Match3BottomObject>(tile, null); break;
                    default: Place<Match3MatchableObject>(tile, Item(cell)); break;
                }
            }
        }
    }

    public Match3Tile Tile(int x, int y) => GridHandler.GetTile(new Vector2Int(x, y));

    /// <summary>The item a letter stands for, shared by every piece using that letter.</summary>
    public SOItemData Item(char letter)
    {
        if (_items.TryGetValue(letter, out var item)) return item;

        item = Track(ScriptableObject.CreateInstance<SOItemData>());
        item.name = letter.ToString();
        _items.Add(letter, item);
        return item;
    }

    public void Dispose()
    {
        for (int i = _created.Count - 1; i >= 0; i--)
        {
            if (_created[i]) Object.DestroyImmediate(_created[i]);
        }
        _created.Clear();
    }

    private Match3Tile CreateTile(Vector2Int position)
    {
        var tile = Track(new GameObject($"Tile ({position.x},{position.y})")).AddComponent<Match3Tile>();
        TestUtils.SetField(tile, "gridPosition", position);
        TestUtils.SetField(tile, "isActive", true);
        _tiles.Add(position, tile);
        return tile;
    }

    private void Place<T>(Match3Tile tile, SOItemData item) where T : Match3Object
    {
        var obj = Track(new GameObject(typeof(T).Name)).AddComponent<T>();
        TestUtils.SetField(obj, "_itemData", item);
        TestUtils.SetField(obj, "_currentTile", tile);
        tile.SetCurrentItem(obj);
    }

    private T Track<T>(T obj) where T : Object
    {
        _created.Add(obj);
        return obj;
    }
}
