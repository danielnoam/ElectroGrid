using System;
using System.Collections;
using System.Collections.Generic;
using DNExtensions;
using UnityEngine;
using Firebase;
using Firebase.Analytics;
using Firebase.RemoteConfig;

public class FirebaseManager : MonoBehaviour
{
    public static FirebaseManager Instance { get; private set; }

    [Header("Firebase State")]
    [SerializeField, ReadOnly] private bool firebaseInitialized;
    [SerializeField, ReadOnly] private float screenShakeIntensityMultiplier = 1.0f;
    [SerializeField, ReadOnly] private bool hapticsEnabled = true;
    [SerializeField, ReadOnly] private int globalMoveBonus;

    public float ScreenShakeIntensityMultiplier => screenShakeIntensityMultiplier;
    public bool HapticsEnabled => hapticsEnabled;
    public int GlobalMoveBonus => globalMoveBonus;
    public bool FirebaseReady => firebaseInitialized;

    public event Action OnFirebaseInitialized;
    
    private readonly List<string> _debugLogs = new List<string>();
    private Vector2 _scrollPosition;
    private const int MaxLogs = 30;

    private void Awake()
    {
        if (Instance && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        InitializeFirebase();
    }
    
    
    #region Initialization

    private async void InitializeFirebase()
    {
        FirebaseApp.LogLevel = LogLevel.Error;
        
        var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();

        if (dependencyStatus == DependencyStatus.Available)
        {
            StartCoroutine(SetupFirebase());
        }
    }

    private IEnumerator SetupFirebase()
    {
        FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
        FirebaseAnalytics.LogEvent(FirebaseAnalytics.EventAppOpen);

        yield return StartCoroutine(InitializeRemoteConfig());

        firebaseInitialized = true;
        OnFirebaseInitialized?.Invoke();
    }

    private IEnumerator InitializeRemoteConfig()
    {
        var defaults = new Dictionary<string, object>
        {
            { "screen_shake_intensity", 1.0 },
            { "enable_haptics", true },
            { "global_move_bonus", 0 },
        };
    
        var defaultsTask = FirebaseRemoteConfig.DefaultInstance.SetDefaultsAsync(defaults);
        yield return new WaitUntil(() => defaultsTask.IsCompleted);
        
        // Production clients are throttled to a handful of fetches per hour, so only bypass the cache in development builds
        var cacheExpiration = Debug.isDebugBuild ? TimeSpan.Zero : TimeSpan.FromHours(12);
        var fetchTask = FirebaseRemoteConfig.DefaultInstance.FetchAsync(cacheExpiration);

        yield return new WaitUntil(() => fetchTask.IsCompleted);
        
        if (fetchTask.IsFaulted || fetchTask.IsCanceled)
        {
            Debug.LogWarning("Firebase Remote Config Fetch Failed/Canceled. Using defaults.");
        }
        else
        {
            var activateTask = FirebaseRemoteConfig.DefaultInstance.ActivateAsync();
            yield return new WaitUntil(() => activateTask.IsCompleted);
        }
        
        LoadRemoteConfigValues();
    }

    private void LoadRemoteConfigValues()
    {
        screenShakeIntensityMultiplier = (float)GetRemoteDouble("screen_shake_intensity");
        hapticsEnabled = GetRemoteBool("enable_haptics");
        globalMoveBonus = (int)GetRemoteLong("global_move_bonus");
    }

    private double GetRemoteDouble(string key) => 
        FirebaseRemoteConfig.DefaultInstance.GetValue(key).DoubleValue;

    private bool GetRemoteBool(string key) => 
        FirebaseRemoteConfig.DefaultInstance.GetValue(key).BooleanValue;

    private long GetRemoteLong(string key) => 
        FirebaseRemoteConfig.DefaultInstance.GetValue(key).LongValue;

    #endregion

    #region Analytics Events

    public void LogLevelStarted(Match3LevelData levelData)
    {
        if (!firebaseInitialized) return;

        FirebaseAnalytics.LogEvent(
            FirebaseAnalytics.EventLevelStart,
            FirebaseAnalytics.ParameterLevelName, levelData.Level.LevelName
        );
    }

    public void LogLevelCompleted(Match3LevelData levelData)
    {
        if (!firebaseInitialized) return;

        FirebaseAnalytics.LogEvent(
            FirebaseAnalytics.EventLevelEnd,
            new Parameter[] {
                new(FirebaseAnalytics.ParameterLevelName, levelData.Level.LevelName),
                new(FirebaseAnalytics.ParameterSuccess, 1),
                // Wire name kept as-is so the existing Firebase series stays continuous after the field rename
                new("matches_made", levelData.PiecesCleared),
                new("moves_made", levelData.MovesMade),
                new("time_spent_seconds", (int)levelData.TimeSpent)
            }
        );
    }

    public void LogLevelFailed(Match3LevelData levelData)
    {
        if (!firebaseInitialized) return;

        FirebaseAnalytics.LogEvent(
            FirebaseAnalytics.EventLevelEnd,
            new Parameter[] {
                new(FirebaseAnalytics.ParameterLevelName, levelData.Level.LevelName),
                new(FirebaseAnalytics.ParameterSuccess, 0),
                // Wire name kept as-is so the existing Firebase series stays continuous after the field rename
                new("matches_made", levelData.PiecesCleared),
                new("moves_made", levelData.MovesMade),
                new("time_spent_seconds", (int)levelData.TimeSpent)
            }
        );

    }

    public void LogLineBreak()
    {
        FirebaseAnalytics.LogEvent("Line_Break");
    }
    
    public void LogCreditsClicked()
    {
        FirebaseAnalytics.LogEvent("Credits_Clicked");
    }
    
    public void LogInformationClicked()
    {
        FirebaseAnalytics.LogEvent("Information_Clicked");
    }

    #endregion
}