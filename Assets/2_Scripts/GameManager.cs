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
    
    
    [Separator]
    [SerializeField, ReadOnly] private SOMatch3Level selectedMatch3Level;
    
    
    public SceneField MainMenu => mainMenu;
    public SceneField Match3Scene => match3Scene;
    
    public SOMatch3Level[] Match3Levels => match3Levels;
    public SOMatch3Level SelectedMatch3Level => selectedMatch3Level;
    
    

    private void Awake()
    {
        if (Instance && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        PrimeTweenConfig.SetTweensCapacity(400);
        if (Application.platform == RuntimePlatform.Android)
        {
            Application.targetFrameRate = 120;
        }
    }
    
    

    public void SelectMatch3Level(SOMatch3Level level)
    {
        selectedMatch3Level = level;
    }
    
}