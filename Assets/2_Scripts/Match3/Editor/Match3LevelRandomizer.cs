using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Random = System.Random;

/// <summary>
/// Places tile objects at random while honouring the rules in <see cref="Match3LevelValidation"/>,
/// so a roll cannot produce a layout the validator would then reject.
/// </summary>
internal static class Match3LevelRandomizer
{
    public struct Options
    {
        public int ObstacleCount;
        public int BottomCount;
        public int Seed;
        public int MinObstacleSpacing;
        public bool KeepObstaclesFromUnderBottoms;
        public bool RandomizeObstacles;
        public bool RandomizeBottoms;
    }

    public static void Randomize(SerializedProperty tileObjectsProp, Grid grid, Options options)
    {
        if (tileObjectsProp == null || grid == null) return;

        int width = grid.Width;
        int height = grid.Height;
        var random = new Random(options.Seed);

        var types = new Match3TileObjectType[width * height];
        for (int i = 0; i < types.Length && i < tileObjectsProp.arraySize; i++)
        {
            types[i] = (Match3TileObjectType)tileObjectsProp.GetArrayElementAtIndex(i).enumValueIndex;
        }

        // Only clear what is being rerolled, so one type can be rolled without disturbing the other
        for (int i = 0; i < types.Length; i++)
        {
            if (options.RandomizeObstacles && types[i] == Match3TileObjectType.Obstacle) types[i] = Match3TileObjectType.Matchable;
            if (options.RandomizeBottoms && types[i] == Match3TileObjectType.Bottom) types[i] = Match3TileObjectType.Matchable;
        }

        // Bottoms first, they are far more constrained than obstacles
        if (options.RandomizeBottoms) PlaceBottoms(types, grid, options, random);
        if (options.RandomizeObstacles) PlaceObstacles(types, grid, options, random);

        for (int i = 0; i < types.Length && i < tileObjectsProp.arraySize; i++)
        {
            tileObjectsProp.GetArrayElementAtIndex(i).enumValueIndex = (int)types[i];
        }
    }

    private static void PlaceBottoms(Match3TileObjectType[] types, Grid grid, Options options, Random random)
    {
        var candidates = new List<int>();

        for (int x = 0; x < grid.Width; x++)
        {
            if (!Match3LevelValidation.IsColumnBottomEligible(grid, x)) continue;

            // Row 0 is excluded, a Square Star there scores the instant the level starts
            for (int y = 1; y < grid.Height; y++)
            {
                if (!grid.IsCellActive(x, y)) continue;
                if (types[y * grid.Width + x] != Match3TileObjectType.Matchable) continue;

                candidates.Add(y * grid.Width + x);
            }
        }

        // Weighted toward higher rows so a Square Star has some distance to fall
        Shuffle(candidates, random);
        candidates.Sort((a, b) =>
        {
            int rowA = a / grid.Width;
            int rowB = b / grid.Width;
            return rowB.CompareTo(rowA);
        });

        int placed = 0;
        var usedColumns = new HashSet<int>();

        // One per column first, spread them out before doubling up anywhere
        for (int pass = 0; pass < 2 && placed < options.BottomCount; pass++)
        {
            foreach (int index in candidates)
            {
                if (placed >= options.BottomCount) break;
                if (types[index] != Match3TileObjectType.Matchable) continue;

                int column = index % grid.Width;
                if (pass == 0 && !usedColumns.Add(column)) continue;

                types[index] = Match3TileObjectType.Bottom;
                placed++;
            }
        }

        if (placed < options.BottomCount)
        {
            Debug.LogWarning($"Only placed {placed} of {options.BottomCount} Square Stars, the grid has no more eligible cells.");
        }
    }

    private static void PlaceObstacles(Match3TileObjectType[] types, Grid grid, Options options, Random random)
    {
        var candidates = new List<int>();

        for (int y = 0; y < grid.Height; y++)
        {
            for (int x = 0; x < grid.Width; x++)
            {
                if (!grid.IsCellActive(x, y)) continue;

                int index = y * grid.Width + x;
                if (types[index] != Match3TileObjectType.Matchable) continue;
                if (options.KeepObstaclesFromUnderBottoms && IsUnderABottom(types, grid, x, y)) continue;

                candidates.Add(index);
            }
        }

        Shuffle(candidates, random);

        int placed = 0;
        foreach (int index in candidates)
        {
            if (placed >= options.ObstacleCount) break;
            if (options.MinObstacleSpacing > 0 && HasObstacleWithin(types, grid, index, options.MinObstacleSpacing)) continue;

            types[index] = Match3TileObjectType.Obstacle;
            placed++;
        }

        // Spacing is a preference, not a rule, so a crowded grid falls back to ignoring it
        if (placed < options.ObstacleCount)
        {
            foreach (int index in candidates)
            {
                if (placed >= options.ObstacleCount) break;
                if (types[index] != Match3TileObjectType.Matchable) continue;

                types[index] = Match3TileObjectType.Obstacle;
                placed++;
            }
        }

        if (placed < options.ObstacleCount)
        {
            Debug.LogWarning($"Only placed {placed} of {options.ObstacleCount} Double Stars, the grid has no more free cells.");
        }
    }

    private static bool IsUnderABottom(Match3TileObjectType[] types, Grid grid, int x, int y)
    {
        for (int above = y + 1; above < grid.Height; above++)
        {
            if (!grid.IsCellActive(x, above)) continue;
            if (types[above * grid.Width + x] == Match3TileObjectType.Bottom) return true;
        }

        return false;
    }

    private static bool HasObstacleWithin(Match3TileObjectType[] types, Grid grid, int index, int spacing)
    {
        int originX = index % grid.Width;
        int originY = index / grid.Width;

        for (int y = originY - spacing; y <= originY + spacing; y++)
        {
            for (int x = originX - spacing; x <= originX + spacing; x++)
            {
                if (x < 0 || x >= grid.Width || y < 0 || y >= grid.Height) continue;
                if (types[y * grid.Width + x] == Match3TileObjectType.Obstacle) return true;
            }
        }

        return false;
    }

    private static void Shuffle(List<int> values, Random random)
    {
        for (int i = values.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (values[i], values[j]) = (values[j], values[i]);
        }
    }
}
