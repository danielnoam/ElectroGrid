using System;
using System.Collections.Generic;
using DNExtensions.Utilities;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class Match3ScoreSettings
{
    [Tooltip("Points for every piece cleared, by a match or a line break")]
    [Min(0)] public int pointsPerPiece = 10;
    [Tooltip("Extra points for each piece a single match has beyond the minimum match size, so a match of 5 earns two of these")]
    [Min(0)] public int pointsPerExtraPiece = 20;

    [Header("Bonus Points")]
    [Min(0)] public int plusBonus = 100;
    [Min(0), FormerlySerializedAs("squareStarBonus")] public int plugBonus = 500;
    [Min(0), FormerlySerializedAs("doubleStarBonus")] public int starBonus = 300;

    [Header("Combo")]
    [Tooltip("Added to the multiplier for each combo step after the first. At 1 the multiplier is the combo count, so x3 triples every point")]
    [Min(0)] public float comboMultiplierPerStep = 1f;
    [Tooltip("The multiplier never goes above this")]
    [Min(1)] public float maxComboMultiplier = 10f;
}

[Serializable]
public class SurvivalTimeSettings
{
    [Tooltip("Seconds on the clock when a run starts")]
    [Min(1)] public float startTime = 45f;

    [Header("Time Gains")]
    [Min(0)] public float plusTime = 5f;
    [Min(0)] public float fullComboTime = 0f;

    [Tooltip("Every time gain is multiplied by this, read at the minutes played. Falling off is what makes every run end")]
    public AnimationCurve gainByMinute = AnimationCurve.Linear(0f, 1f, 5f, 0.4f);
}

[Serializable]
public class SurvivalObstacleSettings
{
    [Header("Plugs")]
    [Tooltip("Chance, per board refill, that one new piece is a Plug instead. Only once a Plugs milestone is reached")]
    [Range(0f, 100f), FormerlySerializedAs("squareStarChance")] public float plugChance = 15f;
    [Min(0), FormerlySerializedAs("maxSquareStars")] public int maxPlugs = 2;
    [Tooltip("Plugs only appear this high up the board or above (0 is the bottom row, 1 the top), so they have a way to fall")]
    [Range(0f, 1f), FormerlySerializedAs("squareStarMinHeight")] public float plugMinHeight = 0.5f;
    [Tooltip("Seconds added when a Plug reaches the bottom")]
    [Min(0)] public float plugTime = 3f;

    [Header("Stars")]
    [Tooltip("Chance, per board refill, that one new piece is a Star instead. Only once a Stars milestone is reached")]
    [Range(0f, 100f), FormerlySerializedAs("doubleStarChance")] public float starChance = 8f;
    [Min(0), FormerlySerializedAs("maxDoubleStars")] public int maxStars = 2;
    [Tooltip("Seconds added when a Star breaks")]
    [Min(0)] public float starTime = 2f;

    [Header("Viruses")]
    [Tooltip("Chance, per board refill, that one new piece is a virus instead. Only once a Viruses milestone is reached")]
    [Range(0f, 100f)] public float virusChance = 6f;
    [Tooltip("No new virus spawns while this many are on the board. Spreading can still go past it")]
    [Min(0)] public int maxViruses = 2;

    [Header("Fuses")]
    [Tooltip("Chance, per board refill, that one new piece carries a fuse. Only once a Fuses milestone is reached")]
    [Range(0f, 100f)] public float fuseChance = 4f;
    [Min(0)] public int maxFuses = 1;
    [Tooltip("Moves before a fuse blows and ends the run")]
    [Min(1)] public int fuseMoves = 10;
}

public enum SurvivalUnlock
{
    Piece,
    Viruses,
    Fuses,
    Plugs,
    Stars
}

[Serializable]
public class SurvivalMilestone
{
    [Tooltip("The run score that reaches this milestone")]
    [Min(0)] public int score;
    [Tooltip("What starts appearing once the run reaches the score")]
    public SurvivalUnlock unlock;
    [Tooltip("The piece that joins the board, as likely as one of the board pieces")]
    [ShowIf(nameof(unlock), SurvivalUnlock.Piece)] public SOItemData piece;
}

[CreateAssetMenu(fileName = "SurvivalMode", menuName = "Scriptable Objects/Survival Mode")]
public class SOSurvivalMode : ScriptableObject
{
    [Tooltip("The board: its grid shape, piece colours and any Stars placed on it. Its objectives and lose conditions are ignored")]
    [SerializeField] private SOMatch3Level board;

    [SerializeField] private Match3ScoreSettings score = new Match3ScoreSettings();
    [SerializeField] private SurvivalTimeSettings time = new SurvivalTimeSettings();
    [SerializeField, FormerlySerializedAs("bonuses")] private SurvivalObstacleSettings obstacles = new SurvivalObstacleSettings();
    [Tooltip("Scores that unlock something new during a run: a piece, viruses or fuses")]
    [SerializeField] private List<SurvivalMilestone> milestones = new List<SurvivalMilestone>();

    public SOMatch3Level Board => board;
    public Match3ScoreSettings Score => score;
    public SurvivalTimeSettings Time => time;
    public SurvivalObstacleSettings Obstacles => obstacles;
    public IReadOnlyList<SurvivalMilestone> Milestones => milestones;

    public bool HasMilestone(SurvivalUnlock unlock)
    {
        return milestones != null && milestones.Exists(milestone => milestone != null && milestone.unlock == unlock);
    }
}
