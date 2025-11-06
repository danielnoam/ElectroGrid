using System;
using DNExtensions;
using DNExtensions.Button;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Scenes")]
    [SerializeField] private SceneField clickerScene;
    [SerializeField] private SceneField match3Scene;
    
    [Separator]
    [SerializeField, ReadOnly] private int currentPopcorns;
    [SerializeField, ReadOnly] private int totalPopcornsCollected;
    
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
            Application.targetFrameRate = 60;
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
    
    [Button(ButtonPlayMode.OnlyWhenPlaying)]
    public void LoadMatch3Scene()
    {
        match3Scene?.LoadScene();
    }
    
    [Button(ButtonPlayMode.OnlyWhenPlaying)]
    public void LoadClickerScene()
    {
        clickerScene?.LoadScene();
    }
    
}
