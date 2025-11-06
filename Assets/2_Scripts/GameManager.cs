using System;
using DNExtensions;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    
    [SerializeField, ReadOnly] private int currentPopcorns;
    [SerializeField, ReadOnly] private int totalPopcornsCollected;
    
    public event Action OnPopcornsCollected;




    private void Awake()
    {
        if (Instance && Instance != this)
        {
            Destroy(this);
            return;
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
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
}
