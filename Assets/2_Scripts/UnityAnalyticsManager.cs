using System;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.SceneManagement;

public class UnityAnalyticsManager : MonoBehaviour
{
    public static UnityAnalyticsManager Instance { get; private set; }


    private void Awake()
    {
        if (!Instance || Instance == this)
        {
            Instance = this;
            DontDestroyOnLoad(this);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }


    public void LogGameStarted()
    {
        Analytics.CustomEvent("GameStarted");
    }
    
    public void LogGameCompleted()
    {
        Analytics.CustomEvent("GameFinished");
    }
    
    public void LogGameFailed()
    {
        Analytics.CustomEvent("GameFailed");
    }
    
}
