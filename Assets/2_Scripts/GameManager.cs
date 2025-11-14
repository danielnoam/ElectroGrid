using System;
using DNExtensions;
using DNExtensions.Button;
using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-1000)]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Main Menu")]
    [SerializeField] private SceneField mainMenu;
    
    [Header("Clicker")]
    [SerializeField] private SceneField clickerScene;
    
    [Header("Match3")]
    [SerializeField] private SceneField match3Scene;
    [SerializeField] private SceneField match3LevelsScene;
    [SerializeField] private SOMatch3Level[] match3Levels = Array.Empty<SOMatch3Level>();
    
    
    [Separator]
    [SerializeField, ReadOnly] private int currentPopcorns;
    [SerializeField, ReadOnly] private int totalPopcornsCollected;
    [SerializeField, ReadOnly] private SOMatch3Level selectedMatch3Level;
    
    
    public SOMatch3Level[] Match3Levels => match3Levels;
    public SOMatch3Level SelectedMatch3Level => selectedMatch3Level;
    public event Action OnPopcornsCollected;




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


        if (Application.platform == RuntimePlatform.Android)
        {
            Application.targetFrameRate = 120;
        }
    }
    
    private void Start()
    {
        currentPopcorns = 0;
        totalPopcornsCollected = 0;
    }
    
    public void AddPopcorn()
    {
        currentPopcorns++;
        totalPopcornsCollected++;
        OnPopcornsCollected?.Invoke();
    }
    
    public void SelectMatch3Level(SOMatch3Level level)
    {
        selectedMatch3Level = level;
    }
    
    [Button(ButtonPlayMode.OnlyWhenPlaying)]
    public void LoadMatch3Scene()
    {
        match3Scene?.LoadScene();
    }
    
    [Button(ButtonPlayMode.OnlyWhenPlaying)]
    public void LoadMatch3LevelsScene()
    {
        match3LevelsScene?.LoadScene();
    }
    
    [Button(ButtonPlayMode.OnlyWhenPlaying)]
    public void LoadClickerScene()
    {
        clickerScene?.LoadScene();
    }

    [Button(ButtonPlayMode.OnlyWhenPlaying)]
    public void LoadMainMenuScene()
    {
        mainMenu?.LoadScene();
    }

}