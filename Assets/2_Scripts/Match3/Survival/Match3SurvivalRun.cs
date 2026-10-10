using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One Survival run: its score, its clock and when bonus Stars appear. Plain C#, so the scoring rules can be tested
/// without a scene; Match3GameManager feeds it what happens on the board.
/// </summary>
public class Match3SurvivalRun
{
    public readonly SOSurvivalMode Mode;
    public readonly SurvivalScoreObjective ScoreDisplay;
    public readonly SurvivalTimeLimit Timer;

    public int Score { get; private set; }
    public int BestCombo { get; private set; }
    public float Elapsed { get; private set; }
    public int PreviousBest { get; set; }
    public bool IsNewBest { get; set; }

    public event Action<int> ScoreGained;
    public event Action<float> TimeGained;

    private Match3ScoreSettings ScoreSettings => Mode.Score;
    private SurvivalTimeSettings TimeSettings => Mode.Time;
    private SurvivalBonusSettings BonusSettings => Mode.Bonuses;

    public Match3SurvivalRun(SOSurvivalMode mode)
    {
        Mode = mode;
        ScoreDisplay = new SurvivalScoreObjective(mode.ScoreIcon);
        Timer = new SurvivalTimeLimit(mode.Time.startTime, mode.TimeIcon);
        ScoreDisplay.Setup();
        Timer.Setup();
    }

    public void Tick(float deltaTime)
    {
        Elapsed += deltaTime;
    }

    public static float ComboMultiplier(Match3ScoreSettings settings, int comboCount)
    {
        float multiplier = 1f + settings.comboMultiplierPerStep * Mathf.Max(0, comboCount - 1);
        return Mathf.Clamp(multiplier, 1f, settings.maxComboMultiplier);
    }

    public static int MatchPoints(Match3ScoreSettings settings, IReadOnlyList<int> groupSizes, int minMatchCount)
    {
        int points = 0;
        foreach (int size in groupSizes)
        {
            points += size * settings.pointsPerPiece;
            points += Mathf.Max(0, size - minMatchCount) * settings.pointsPerExtraPiece;
        }

        return points;
    }

    /// <summary>Splits a set of matched tiles into the separate matches it holds, by touching tiles of the same colour.</summary>
    public static List<int> MatchGroupSizes(List<Match3Tile> matches)
    {
        var sizes = new List<int>();
        var remaining = new HashSet<Match3Tile>(matches);
        var byPosition = new Dictionary<Vector2Int, Match3Tile>();
        foreach (var tile in matches)
        {
            if (tile) byPosition[tile.GridPosition] = tile;
        }

        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        var stack = new Stack<Match3Tile>();

        foreach (var start in matches)
        {
            if (!start || !remaining.Remove(start)) continue;

            int size = 0;
            stack.Push(start);
            while (stack.Count > 0)
            {
                var tile = stack.Pop();
                size++;
                var item = ItemOf(tile);

                foreach (var direction in directions)
                {
                    if (!byPosition.TryGetValue(tile.GridPosition + direction, out var neighbour)) continue;
                    if (!remaining.Contains(neighbour) || ItemOf(neighbour) != item) continue;

                    remaining.Remove(neighbour);
                    stack.Push(neighbour);
                }
            }

            sizes.Add(size);
        }

        return sizes;
    }

    private static SOItemData ItemOf(Match3Tile tile)
    {
        return tile && tile.CurrentMatch3Object is Match3MatchableObject matchable ? matchable.ItemData : null;
    }

    public void OnMatches(List<Match3Tile> matches, int minMatchCount, int comboCount)
    {
        if (matches == null || matches.Count == 0) return;

        AddPoints(MatchPoints(ScoreSettings, MatchGroupSizes(matches), minMatchCount), comboCount);
    }

    public void OnLineBreakPieces(int pieces, int comboCount)
    {
        AddPoints(pieces * ScoreSettings.pointsPerPiece, comboCount);
    }

    public void OnPlusDestroyed(int comboCount)
    {
        AddPoints(ScoreSettings.plusBonus, comboCount);
        AddTime(TimeSettings.plusTime);
    }

    public void OnSquareStarReached(int comboCount)
    {
        AddPoints(ScoreSettings.squareStarBonus, comboCount);
        AddTime(TimeSettings.squareStarTime);
    }

    public void OnDoubleStarBroken(int comboCount)
    {
        AddPoints(ScoreSettings.doubleStarBonus, comboCount);
        AddTime(TimeSettings.doubleStarTime);
    }

    public void OnComboFilled()
    {
        AddTime(TimeSettings.fullComboTime);
    }

    public void OnComboStep(int comboCount)
    {
        if (comboCount > BestCombo) BestCombo = comboCount;
    }

    public float TimeGainMultiplier()
    {
        var curve = TimeSettings.gainByMinute;
        if (curve == null || curve.length == 0) return 1f;

        return Mathf.Max(0f, curve.Evaluate(Elapsed / 60f));
    }

    private void AddPoints(int points, int comboCount)
    {
        if (points <= 0) return;

        int gained = Mathf.RoundToInt(points * ComboMultiplier(ScoreSettings, comboCount));
        Score += gained;
        ScoreDisplay.SetScore(Score);
        ScoreGained?.Invoke(gained);
    }

    private void AddTime(float seconds)
    {
        float gained = seconds * TimeGainMultiplier();
        if (gained <= 0f) return;

        Timer.AddTime(gained);
        TimeGained?.Invoke(gained);
    }

    /// <summary>
    /// Which bonus, if any, should replace one piece of this refill. Square Stars need a column that reaches the bottom
    /// row and a starting cell high enough to fall from.
    /// </summary>
    public (Match3Tile tile, Match3TileObjectType type) PickBonus(ICollection<Match3Tile> refillTiles, Match3GridHandler grid, int squareStarsOnBoard, int doubleStarsOnBoard)
    {
        if (refillTiles == null || refillTiles.Count == 0 || !grid) return (null, Match3TileObjectType.Matchable);

        bool trySquare = squareStarsOnBoard < BonusSettings.maxSquareStars && UnityEngine.Random.Range(0f, 100f) < BonusSettings.squareStarChance;
        if (trySquare)
        {
            int minRow = Mathf.CeilToInt((grid.Grid.Height - 1) * BonusSettings.squareStarMinHeight);
            var candidates = new List<Match3Tile>();
            foreach (var tile in refillTiles)
            {
                if (tile.GridPosition.y >= minRow && tile.GridPosition.y > 0 && grid.HasCatcher(tile.GridPosition.x)) candidates.Add(tile);
            }

            if (candidates.Count > 0) return (candidates[UnityEngine.Random.Range(0, candidates.Count)], Match3TileObjectType.Bottom);
        }

        bool tryDouble = doubleStarsOnBoard < BonusSettings.maxDoubleStars && UnityEngine.Random.Range(0f, 100f) < BonusSettings.doubleStarChance;
        if (tryDouble)
        {
            var candidates = new List<Match3Tile>(refillTiles);
            return (candidates[UnityEngine.Random.Range(0, candidates.Count)], Match3TileObjectType.Obstacle);
        }

        return (null, Match3TileObjectType.Matchable);
    }
}
