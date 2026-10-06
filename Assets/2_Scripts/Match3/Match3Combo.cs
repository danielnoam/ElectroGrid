using System;
using UnityEngine;

[Serializable]
public class Match3ComboSettings
{
    [Tooltip("Bar fill from a swap that makes a match")]
    [Range(0f, 1f)] public float swapFill = 0.2f;
    [Tooltip("Bar fill from each cascade wave the swap sets off. Smaller than a swap, cascades are partly luck")]
    [Range(0f, 1f)] public float cascadeFill = 0.05f;
    [Tooltip("Seconds to make the next match before the combo resets, at combo 1")]
    [Min(0.1f)] public float startWindow = 4f;
    [Tooltip("The window is multiplied by this for every step, so a long combo needs quicker play")]
    [Range(0.1f, 1f)] public float windowShrink = 0.9f;
    [Tooltip("The window never gets shorter than this")]
    [Min(0.1f)] public float minWindow = 1.5f;

    [Header("Full Bar")]
    [Tooltip("Seconds added to a time limit when the bar fills")]
    [Min(0)] public float fullBarSeconds = 5f;
    [Tooltip("Moves added to a move limit when the bar fills")]
    [Min(0)] public int fullBarMoves = 2;
}

/// <summary>
/// Counts a run of matches made in quick succession and fills a bar from it. Plain C#, so normal levels and Endless
/// share it and it can be tested without a scene.
/// </summary>
/// <remarks>
/// A swap that matches is a step: it raises <see cref="Count"/> and adds a large fill. Each cascade wave it sets off adds
/// a small fill but no step. The countdown to the next match is paused while the board resolves, so a long cascade
/// animation can never reset a combo the player earned. A full bar raises <see cref="Filled"/> and starts over; what a
/// full bar does is up to whoever listens.
/// </remarks>
public class Match3Combo
{
    private readonly Match3ComboSettings _settings;

    public int Count { get; private set; }
    public float Fill { get; private set; }
    public float TimeLeft { get; private set; }
    public float Window { get; private set; }
    public bool Resolving { get; private set; }
    public bool Active => Count > 0;

    /// <summary>A swap made a match. Carries the new step count.</summary>
    public event Action<int> StepAdded;
    /// <summary>The bar changed, from a step or a cascade wave. Carries the fill, 0 to 1.</summary>
    public event Action<float> FillChanged;
    /// <summary>The bar reached full. Raised before it empties again.</summary>
    public event Action Filled;
    /// <summary>The window ran out, or the combo was cleared. Carries the step count it reached.</summary>
    public event Action<int> Reset;

    public Match3Combo(Match3ComboSettings settings)
    {
        _settings = settings ?? new Match3ComboSettings();
    }

    /// <summary>The board started resolving a swap; the countdown stops until <see cref="EndResolve"/>.</summary>
    public void BeginResolve() => Resolving = true;

    /// <summary>The board settled and the player can move again; the countdown restarts from a full window.</summary>
    public void EndResolve()
    {
        Resolving = false;
        if (Active) TimeLeft = Window;
    }

    public void AddSwapStep()
    {
        Count++;
        Window = Mathf.Max(_settings.minWindow, _settings.startWindow * Mathf.Pow(_settings.windowShrink, Count - 1));
        TimeLeft = Window;
        StepAdded?.Invoke(Count);
        AddFill(_settings.swapFill);
    }

    public void AddCascadeWave()
    {
        // A cascade with no swap behind it (a reshuffle, the opening board) is not the player's doing
        if (!Active) return;
        AddFill(_settings.cascadeFill);
    }

    /// <summary>Advances the countdown. Pass scaled time, so a paused game or a line break slow-down holds the combo too.</summary>
    public void Tick(float deltaTime)
    {
        if (!Active || Resolving) return;

        TimeLeft -= deltaTime;
        if (TimeLeft <= 0f) Clear();
    }

    /// <summary>Ends the combo, for example when the window runs out or the level ends.</summary>
    public void Clear()
    {
        if (!Active && Fill <= 0f) return;

        int reached = Count;
        Count = 0;
        Fill = 0f;
        TimeLeft = 0f;
        Window = 0f;
        Reset?.Invoke(reached);
        FillChanged?.Invoke(Fill);
    }

    private void AddFill(float amount)
    {
        if (amount <= 0f) return;

        Fill += amount;
        if (Fill >= 1f)
        {
            Fill = 1f;
            FillChanged?.Invoke(Fill);
            Filled?.Invoke();
            // The combo keeps going, only the bar starts over
            Fill = 0f;
        }
        FillChanged?.Invoke(Fill);
    }
}
