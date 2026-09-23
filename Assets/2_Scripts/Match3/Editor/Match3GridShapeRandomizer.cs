#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

/// <summary>
/// Generates board silhouettes. Raw noise reads as static and plays badly, so the output is
/// smoothed, optionally mirrored, and then cleaned of cells that cannot take part in a match.
/// </summary>
internal static class Match3GridShapeRandomizer
{
    public enum SymmetryMode
    {
        None,
        Horizontal,
        Vertical,
        Both
    }

    public struct Options
    {
        public int Width;
        public int Height;
        public int Seed;
        public float Density;
        public SymmetryMode Symmetry;
        public int SmoothingPasses;
        public bool RemoveIsolatedCells;
        public bool KeepLargestRegionOnly;
    }

    public static bool[] Generate(Options options)
    {
        int width = Mathf.Max(1, options.Width);
        int height = Mathf.Max(1, options.Height);

        var random = new Random(options.Seed);
        var cells = new bool[width * height];

        for (int i = 0; i < cells.Length; i++)
        {
            cells[i] = random.NextDouble() < options.Density;
        }

        for (int pass = 0; pass < options.SmoothingPasses; pass++)
        {
            cells = Smooth(cells, width, height);
        }

        ApplySymmetry(cells, width, height, options.Symmetry);

        if (options.KeepLargestRegionOnly) KeepLargestRegion(cells, width, height);

        // Must run last: symmetry and region trimming can both strand a cell on its own
        if (options.RemoveIsolatedCells) RemoveIsolated(cells, width, height);

        return cells;
    }

    private static bool[] Smooth(bool[] cells, int width, int height)
    {
        var result = new bool[cells.Length];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int neighbours = CountNeighbours(cells, width, height, x, y, includeDiagonals: true);
                int index = y * width + x;

                // Standard cellular smoothing: crowded cells fill in, lonely cells drop out
                result[index] = neighbours > 4 || (cells[index] && neighbours == 4);
            }
        }

        return result;
    }

    private static void ApplySymmetry(bool[] cells, int width, int height, SymmetryMode mode)
    {
        if (mode == SymmetryMode.None) return;

        if (mode is SymmetryMode.Horizontal or SymmetryMode.Both)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width / 2; x++)
                {
                    cells[y * width + (width - 1 - x)] = cells[y * width + x];
                }
            }
        }

        if (mode is SymmetryMode.Vertical or SymmetryMode.Both)
        {
            for (int y = 0; y < height / 2; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    cells[(height - 1 - y) * width + x] = cells[y * width + x];
                }
            }
        }
    }

    private static void RemoveIsolated(bool[] cells, int width, int height)
    {
        var toRemove = new List<int>();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                if (cells[index] && CountNeighbours(cells, width, height, x, y) == 0) toRemove.Add(index);
            }
        }

        foreach (int index in toRemove) cells[index] = false;
    }

    private static void KeepLargestRegion(bool[] cells, int width, int height)
    {
        var visited = new bool[cells.Length];
        List<int> largest = null;

        for (int i = 0; i < cells.Length; i++)
        {
            if (!cells[i] || visited[i]) continue;

            var region = FloodFill(cells, visited, width, height, i);
            if (largest == null || region.Count > largest.Count) largest = region;
        }

        if (largest == null) return;

        var keep = new HashSet<int>(largest);
        for (int i = 0; i < cells.Length; i++)
        {
            if (cells[i] && !keep.Contains(i)) cells[i] = false;
        }
    }

    private static List<int> FloodFill(bool[] cells, bool[] visited, int width, int height, int start)
    {
        var region = new List<int>();
        var stack = new Stack<int>();

        stack.Push(start);
        visited[start] = true;

        while (stack.Count > 0)
        {
            int index = stack.Pop();
            region.Add(index);

            int x = index % width;
            int y = index / width;

            TryPush(cells, visited, stack, width, height, x - 1, y);
            TryPush(cells, visited, stack, width, height, x + 1, y);
            TryPush(cells, visited, stack, width, height, x, y - 1);
            TryPush(cells, visited, stack, width, height, x, y + 1);
        }

        return region;
    }

    private static void TryPush(bool[] cells, bool[] visited, Stack<int> stack, int width, int height, int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height) return;

        int index = y * width + x;
        if (!cells[index] || visited[index]) return;

        visited[index] = true;
        stack.Push(index);
    }

    private static int CountNeighbours(bool[] cells, int width, int height, int x, int y, bool includeDiagonals = false)
    {
        int count = 0;

        for (int offsetY = -1; offsetY <= 1; offsetY++)
        {
            for (int offsetX = -1; offsetX <= 1; offsetX++)
            {
                if (offsetX == 0 && offsetY == 0) continue;
                if (!includeDiagonals && offsetX != 0 && offsetY != 0) continue;

                int neighbourX = x + offsetX;
                int neighbourY = y + offsetY;
                if (neighbourX < 0 || neighbourX >= width || neighbourY < 0 || neighbourY >= height) continue;

                if (cells[neighbourY * width + neighbourX]) count++;
            }
        }

        return count;
    }
}
#endif
