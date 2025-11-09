using System;
using System.Collections.Generic;
using DNExtensions;
using DNExtensions.Button;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public CameraManager Instance { get; private set; }
    
    [Header("Match3")]
    [SerializeField] private float gridPadding = 0.5f; 
    
    [Header("References")]
    [SerializeField] private Camera cam;
    
    
    [Separator]
    [SerializeField, ReadOnly] private Vector2Int gridSize = new Vector2Int(10,10);
    private Match3GameManager _match3GameManager;



    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
    }

    private void Start()
    {
        SubscribeToMatch3Manager();
    }

    private void OnEnable()
    {
        SubscribeToMatch3Manager();
    }

    private void SubscribeToMatch3Manager()
    {
        if (!_match3GameManager) _match3GameManager = Match3GameManager.Instance;
        if (_match3GameManager)
        {
            _match3GameManager.LevelStarted -= OnLevelStarted;
            _match3GameManager.LevelStarted += OnLevelStarted;
        }
    }

    private void OnDisable()
    {
        if (_match3GameManager)
        {
            _match3GameManager.LevelStarted -= OnLevelStarted;
        }

    }




    private void OnLevelStarted(SOMatch3Level level, List<Match3Objective> arg2, List<Match3LoseCondition> arg3)
    {
        if (!level) return;
        var gridSize = new Vector2Int(level.GridShape.Grid.Width, level.GridShape.Grid.Height);
        UpdateGridSize(gridSize);
    }

    private void UpdateGridSize(Vector2Int gridSize)
    {
        this.gridSize = gridSize;
        FitCameraToGrid();
    }

    [Button(ButtonPlayMode.OnlyWhenPlaying)]
    private void FitCameraToGrid()
    {
        if (!cam) return;
        
        float screenAspect = (float)Screen.width / Screen.height;
        float gridAspect = gridSize.x / gridSize.y;

        if (screenAspect >= gridAspect)
        {
            cam.orthographicSize = (gridSize.y / 2f) + gridPadding;
        }
        else
        {
            float targetHeight =  gridSize.x / screenAspect;
            cam.orthographicSize = (targetHeight / 2f) + gridPadding;
        }
    }
}
