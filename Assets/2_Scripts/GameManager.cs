using System;
using DNExtensions.Utilities;
using DNExtensions.Utilities.CustomFields;
using DNExtensions.Utilities.Button;
using PrimeTween;
using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-1000)]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Main Menu")]
    [SerializeField] private SceneField mainMenu;
    
    [Header("Match3")]
    [SerializeField] private SceneField match3Scene;
    [SerializeField] private SOMatch3Level[] match3Levels = Array.Empty<SOMatch3Level>();
    [SerializeField] private SOMatch3Tutorial[] match3GeneralTutorials = Array.Empty<SOMatch3Tutorial>();
    
    [Separator]
    [SerializeField, ReadOnly] private SOMatch3Level selectedMatch3Level;
    
    public SceneField MainMenu => mainMenu;
    public SceneField Match3Scene => match3Scene;
    
    public SOMatch3Level[] Match3Levels => match3Levels;
    public SOMatch3Tutorial[] Match3GeneralTutorials => match3GeneralTutorials;
    public SOMatch3Level SelectedMatch3Level => selectedMatch3Level;

    public event Action<bool> PauseToggled;

    private void Awake()
    {
        if (Instance && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);

        PrimeTweenConfig.SetTweensCapacity(1600);
        ApplyFrameRate();
    }

    /// <summary>
    /// True when the display refreshes fast enough for the high frame rate option to mean anything.
    /// </summary>
    public static bool SupportsHighFrameRate => MaxRefreshRate > 61;

    /// <summary>
    /// The fastest refresh rate the display supports, not the one it is running at. Phones with 90/120Hz panels drop
    /// to 60Hz while an app asks for 60, so the current rate alone would hide the option that raises it.
    /// </summary>
    private static int MaxRefreshRate
    {
        get
        {
            double max = Screen.currentResolution.refreshRateRatio.value;
            foreach (var resolution in Screen.resolutions)
            {
                if (resolution.refreshRateRatio.value > max) max = resolution.refreshRateRatio.value;
            }

            return Mathf.RoundToInt((float)max);
        }
    }

    /// <summary>
    /// 60 by default, which matters for battery on long puzzle sessions. Players can opt into the
    /// panel's refresh rate, capped at 120, from the settings window.
    /// </summary>
    public static void ApplyFrameRate()
    {
        if (!Application.isMobilePlatform) return;

        var highFrameRate = SaveManager.Instance && SaveManager.Instance.Settings.highFrameRate && SupportsHighFrameRate;
        // On Android the target frame rate is also what asks the display to switch to its faster mode
        Application.targetFrameRate = highFrameRate ? Mathf.Min(120, MaxRefreshRate) : 60;
    }

    public void SelectMatch3Level(SOMatch3Level level)
    {
        selectedMatch3Level = level;
    }
    
    public void TogglePause(bool pause, bool affectTimeScale = true)
    {
        if (affectTimeScale) Time.timeScale = pause ? 0 : 1;
        PauseToggled?.Invoke(pause);
    }
}