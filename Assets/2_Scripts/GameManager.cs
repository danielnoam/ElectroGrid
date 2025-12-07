using System;
using DNExtensions;
using DNExtensions.Button;
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
        if (Application.platform == RuntimePlatform.Android)
        {
            Application.targetFrameRate = 120;
        }
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