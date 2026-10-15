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

    public static bool IsColumnBottomEligible(Grid grid, int x) => Match3GridHandler.IsColumnBottomEligible(grid, x);

    /// <summary>Null when the list is empty. Used for the status dot next to each level in the browser.</summary>
    public static Severity? WorstSeverity(List<Issue> issues)
    {
        Severity? worst = null;

        foreach (var issue in issues)
        {
            if (issue.Severity == Severity.Error) return Severity.Error;
            worst = Severity.Warning;
        }

        return worst;
    }

    /// <summary>How many Stars and Plugs the level's objectives ask for in total.</summary>
    public static void GetRequiredCounts(SOMatch3Level level, out int obstaclesNeeded, out int bottomsNeeded)
    {
        obstaclesNeeded = 0;
        bottomsNeeded = 0;

        if (!level || level.Objectives == null) return;

        foreach (var objective in level.Objectives)
        {
            if (objective is DestroyObstaclesObjective destroyObstacles) obstaclesNeeded += destroyObstacles.RequiredAmount;
            else if (objective is ReachBottomObjective reachBottom) bottomsNeeded += reachBottom.RequiredAmount;
        }
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
        ValidatePlacedPieces(level, grid, issues);

        return issues;
    }

    private static void ValidatePlacedPieces(SOMatch3Level level, Grid grid, List<Issue> issues)
    {
        for (int y = 0; y < grid.Height; y++)
        {
            for (int x = 0; x < grid.Width; x++)
            {
                var piece = level.PlacedPiece(x, y);
                if (!piece) continue;

                bool row = x + 2 < grid.Width && level.PlacedPiece(x + 1, y) == piece && level.PlacedPiece(x + 2, y) == piece;
                bool column = y + 2 < grid.Height && level.PlacedPiece(x, y + 1) == piece && level.PlacedPiece(x, y + 2) == piece;
                if (row || column)
                {
                    issues.Add(new Issue(Severity.Error, $"Three placed {piece.Label} pieces in a line at ({x}, {y}) match at once. The board can't start with a match."));
                    return;
                }
            }
        }
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

        bool hasLoseCondition = level.LoseConditions.Exists(condition => condition != null);
        if (level.LoseConditions.Exists(condition => condition is MoveLimit) && level.LoseConditions.Exists(condition => condition is TimeLimit))
        {
            issues.Add(new Issue(Severity.Warning, "Both a move limit and a time limit. Levels use one or the other."));
        }
        if (!level.ComboBar && level.Objectives.Exists(objective => objective is ReachScoreObjective))
        {
            issues.Add(new Issue(Severity.Warning, "Reach Score without the combo bar. Score levels use the combo to multiply points."));
        }

        if (!hasLoseCondition && level.Objectives.Exists(objective => objective is ReachScoreObjective))
        {
            issues.Add(new Issue(Severity.Warning, "Reach Score with no lose condition. Any score is reached eventually, so the level cannot be lost."));
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
                    issues.Add(new Issue(Severity.Error, $"Plug at ({x},{y}) is on the bottom row and scores the moment the level starts."));
                    continue;
                }

                if (!IsColumnBottomEligible(grid, x))
                {
                    issues.Add(new Issue(Severity.Error, $"Plug at ({x},{y}) is in a column that never reaches row 0, so it can never be collected."));
                    continue;
                }
            }
        }
    }

    private static void ValidateCounts(SOMatch3Level level, Grid grid, List<Issue> issues)
    {
        int obstacles = level.CountObjectsOfType(Match3TileObjectType.Obstacle);
        int bottoms = level.CountObjectsOfType(Match3TileObjectType.Bottom);
        int matchable = grid.ActiveCellCount - obstacles - bottoms - level.CountObjectsOfType(Match3TileObjectType.Virus) - level.CountObjectsOfType(Match3TileObjectType.Surge);

        if (matchable <= 0)
        {
            issues.Add(new Issue(Severity.Error, "No cells left for matchable pieces."));
        }
        else if (matchable < 9)
        {
            issues.Add(new Issue(Severity.Warning, $"Only {matchable} cells for matchable pieces. The board may not find enough possible matches."));
        }

        GetRequiredCounts(level, out int obstaclesNeeded, out int bottomsNeeded);

        int viruses = level.CountObjectsOfType(Match3TileObjectType.Virus);
        int fuses = level.CountFuses();
        int glitches = level.CountGlitches();

        foreach (var objective in level.Objectives)
        {
            if (objective is ClearVirusesObjective && viruses == 0)
                issues.Add(new Issue(Severity.Error, "Clear Viruses with no viruses placed. The goal would be done before the first move."));
            if (objective is DefuseObjective defuse && defuse.RequiredAmount != fuses)
                issues.Add(new Issue(Severity.Error, $"Objectives need {defuse.RequiredAmount} fuses but {fuses} are placed."));
            if (objective is ClearGlitchesObjective clear && clear.RequiredAmount != glitches)
                issues.Add(new Issue(Severity.Error, $"Objectives need {clear.RequiredAmount} glitches but {glitches} are painted."));
        }

        var starFuses = level.StarFuseTiles;
        if (starFuses != null)
        {
            for (int i = 0; i < starFuses.Length && i < grid.Width * grid.Height; i++)
            {
                int x = i % grid.Width, y = i / grid.Width;
                if (starFuses[i] && grid.IsCellActive(x, y) && !level.HasStarFuse(x, y))
                    issues.Add(new Issue(Severity.Warning, $"Star fuse at ({x},{y}) has no Star or Plug under it, so it does nothing."));
            }
        }

        if (fuses > 0 && !level.Objectives.Exists(objective => objective is DefuseObjective))
            issues.Add(new Issue(Severity.Warning, "Fuses but no Defuse objective. The fuses can still blow and lose the level."));

        if (level.ConveyorRows != null)
        {
            foreach (var conveyor in level.ConveyorRows)
            {
                if (conveyor == null) continue;
                if (conveyor.row < 0 || conveyor.row >= grid.Height)
                {
                    issues.Add(new Issue(Severity.Error, $"Conveyor row {conveyor.row} is outside the grid."));
                    continue;
                }

                int cells = 0;
                for (int x = 0; x < grid.Width; x++) if (grid.IsCellActive(x, conveyor.row)) cells++;
                if (cells < 2) issues.Add(new Issue(Severity.Warning, $"Conveyor row {conveyor.row} has fewer than 2 cells, so nothing moves."));
            }
        }

        if (obstaclesNeeded > 0 && obstaclesNeeded != obstacles)
        {
            issues.Add(new Issue(Severity.Error, $"Objectives need {obstaclesNeeded} Stars but {obstacles} are placed."));
        }

        if (bottomsNeeded > 0 && bottomsNeeded != bottoms)
        {
            issues.Add(new Issue(Severity.Error, $"Objectives need {bottomsNeeded} Plugs but {bottoms} are placed."));
        }
    }
}
