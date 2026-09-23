#if UNITY_EDITOR
using System.Collections.Generic;

/// <summary>
/// Authoring checks for a level. The rules here are also what the randomiser filters against,
/// so a generated layout cannot break a rule the validator would then complain about.
/// </summary>
internal static class Match3LevelValidation
{
    public enum Severity
    {
        Error,
        Warning
    }

    public readonly struct Issue
    {
        public readonly Severity Severity;
        public readonly string Message;

        public Issue(Severity severity, string message)
        {
            Severity = severity;
            Message = message;
        }
    }

    /// <summary>
    /// A bottom object can only score in a column whose lowest active cell is row 0. Gravity moves
    /// objects to the lowest tile that exists, tiles only exist on active cells, and reaching the
    /// bottom is tested as y &lt;= 0 — so on a silhouette grid a column that starts higher is a trap.
    /// </summary>
    public static bool IsColumnBottomEligible(Grid grid, int x)
    {
        if (grid == null || x < 0 || x >= grid.Width) return false;

        for (int y = 0; y < grid.Height; y++)
        {
            if (grid.IsCellActive(x, y)) return y == 0;
        }

        return false;
    }

    /// <summary>Null when the level is clean. Used for the status dot next to each level in the browser.</summary>
    public static Severity? WorstSeverity(SOMatch3Level level)
    {
        Severity? worst = null;

        foreach (var issue in Validate(level))
        {
            if (issue.Severity == Severity.Error) return Severity.Error;
            worst = Severity.Warning;
        }

        return worst;
    }

    public static List<Issue> Validate(SOMatch3Level level)
    {
        var issues = new List<Issue>();
        if (!level) return issues;

        if (!level.GridShape || level.GridShape.Grid == null)
        {
            issues.Add(new Issue(Severity.Error, "No Grid Shape assigned."));
            return issues;
        }

        Grid grid = level.GridShape.Grid;
        var tileObjects = level.TileObjects;

        if (tileObjects == null || tileObjects.Length != grid.Width * grid.Height)
        {
            issues.Add(new Issue(Severity.Error, "Tile data does not match the grid shape. Repaint or reopen the level to rebuild it."));
            return issues;
        }

        ValidateObjectives(level, issues);
        ValidateMatchObjects(level, issues);
        ValidateTilePlacement(level, grid, issues);
        ValidateCounts(level, grid, issues);

        return issues;
    }

    private static void ValidateObjectives(SOMatch3Level level, List<Issue> issues)
    {
        int realObjectives = 0;
        var seenExclusiveTypes = new HashSet<System.Type>();

        foreach (var objective in level.Objectives)
        {
            if (objective == null)
            {
                issues.Add(new Issue(Severity.Warning, "An objective slot is empty."));
                continue;
            }

            realObjectives++;

            // AllowOnlyOneObjectiveOfThisType has always been declared but never enforced anywhere
            if (objective.AllowOnlyOneObjectiveOfThisType && !seenExclusiveTypes.Add(objective.GetType()))
            {
                issues.Add(new Issue(Severity.Warning, $"More than one {objective.GetType().Name}. They will compete for the same row in the top bar."));
            }
        }

        if (realObjectives == 0)
        {
            issues.Add(new Issue(Severity.Error, "No objectives. The level can never be completed."));
        }

        foreach (var condition in level.LoseConditions)
        {
            if (condition == null) issues.Add(new Issue(Severity.Warning, "A lose condition slot is empty."));
        }
    }

    private static void ValidateMatchObjects(SOMatch3Level level, List<Issue> issues)
    {
        int count = level.MatchObjects?.Count ?? 0;

        if (count == 0)
        {
            issues.Add(new Issue(Severity.Error, "No match objects. The grid cannot be populated."));
            return;
        }

        if (count < 3)
        {
            issues.Add(new Issue(Severity.Warning, $"Only {count} match object type(s). Matches will be almost unavoidable."));
        }

        for (int i = 0; i < count; i++)
        {
            if (!level.MatchObjects[i]) issues.Add(new Issue(Severity.Warning, $"Match object {i} is empty."));
        }
    }

    private static void ValidateTilePlacement(SOMatch3Level level, Grid grid, List<Issue> issues)
    {
        for (int y = 0; y < grid.Height; y++)
        {
            for (int x = 0; x < grid.Width; x++)
            {
                if (!level.TileHasObjectType(x, y, Match3TileObjectType.Bottom)) continue;

                if (y == 0)
                {
                    issues.Add(new Issue(Severity.Error, $"Square Star at ({x},{y}) is on the bottom row and scores the moment the level starts."));
                    continue;
                }

                if (!IsColumnBottomEligible(grid, x))
                {
                    issues.Add(new Issue(Severity.Error, $"Square Star at ({x},{y}) is in a column that never reaches row 0, so it can never be collected."));
                    continue;
                }

                for (int below = y - 1; below >= 0; below--)
                {
                    if (!grid.IsCellActive(x, below)) continue;

                    if (level.TileHasObjectType(x, below, Match3TileObjectType.Obstacle))
                    {
                        issues.Add(new Issue(Severity.Warning, $"Square Star at ({x},{y}) is blocked by a Double Star at ({x},{below}) until that is destroyed."));
                    }

                    break;
                }
            }
        }
    }

    private static void ValidateCounts(SOMatch3Level level, Grid grid, List<Issue> issues)
    {
        int obstacles = level.CountObjectsOfType(Match3TileObjectType.Obstacle);
        int bottoms = level.CountObjectsOfType(Match3TileObjectType.Bottom);
        int matchable = grid.ActiveCellCount - obstacles - bottoms;

        if (matchable <= 0)
        {
            issues.Add(new Issue(Severity.Error, "No cells left for matchable pieces."));
        }
        else if (matchable < 9)
        {
            issues.Add(new Issue(Severity.Warning, $"Only {matchable} cells for matchable pieces. The board may not find enough possible matches."));
        }

        int obstaclesNeeded = 0;
        int bottomsNeeded = 0;

        foreach (var objective in level.Objectives)
        {
            if (objective is DestroyObstaclesObjective destroyObstacles) obstaclesNeeded += destroyObstacles.RequiredAmount;
            else if (objective is ReachBottomObjective reachBottom) bottomsNeeded += reachBottom.RequiredAmount;
        }

        if (obstaclesNeeded > obstacles)
        {
            issues.Add(new Issue(Severity.Error, $"Objectives need {obstaclesNeeded} Double Stars but only {obstacles} are placed."));
        }

        if (bottomsNeeded > bottoms)
        {
            issues.Add(new Issue(Severity.Error, $"Objectives need {bottomsNeeded} Square Stars but only {bottoms} are placed."));
        }
    }
}
#endif
