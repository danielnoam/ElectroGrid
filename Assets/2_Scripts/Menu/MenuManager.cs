using System;
using System.Collections.Generic;
using DNExtensions;
using DNExtensions.Button;
using DNExtensions.VFXManager;
using UnityEngine;
using UnityEngine.VFX;
using VFXManager = DNExtensions.VFXManager.VFXManager;

public class MenuManager : MonoBehaviour
{
    [Header("Screens")]
    [SerializeField] private MainMenuScreen mainMenuScreen;
    [SerializeField] private Match3LevelSelectionScreen match3LevelSelectionScreen;
    
    [Header("Background")]
    [SerializeField] private Transform backgroundParent;
    [SerializeField] private SpriteRenderer backgroundTilePrefab;
    [SerializeField] private Grid grid = new Grid();
    
    [Header("VFX")]
    [SerializeField] private SOVFEffectsSequence startLevelEffect;
    [SerializeField] private SOVFEffectsSequence endLevelEffect;
    
    private IMenuScreen _currentScreen;
    private readonly List<SpriteRenderer> _backgroundTiles = new List<SpriteRenderer>();
    private readonly Dictionary<Type, IMenuScreen> _screens = new Dictionary<Type, IMenuScreen>();

    public SOVFEffectsSequence StartLevelEffect => startLevelEffect;
    public SOVFEffectsSequence EndLevelEffect => endLevelEffect;
    
    private void Awake()
    {
        _screens.Clear();
        if (mainMenuScreen) _screens[typeof(MainMenuScreen)] = mainMenuScreen;
        if (match3LevelSelectionScreen) _screens[typeof(Match3LevelSelectionScreen)] = match3LevelSelectionScreen;
    }
    
    private void Start()
    {
        VFXManager.Instance?.PlayVFX(startLevelEffect);
        HideAllScreensImmediate();
        ShowScreen<MainMenuScreen>();
    }
    
    private void ShowScreen<T>(bool animated = true, Action onComplete = null) where T : IMenuScreen
    {
        if (!_screens.TryGetValue(typeof(T), out IMenuScreen screen))
        {
            Debug.LogError($"Screen of type {typeof(T).Name} not found!");
            return;
        }

        if (_currentScreen != null)
        {
            _currentScreen.Hide(animated, () =>
            {
                screen.Show(animated, onComplete);
                _currentScreen = screen;
            });
        }
        else
        {
            screen.Show(animated, onComplete);
            _currentScreen = screen;
        }
    }

    public void ShowMainMenu(bool animated = true)
    {
        ShowScreen<MainMenuScreen>(animated);
    }

    public void ShowMatch3LevelSelection(bool animated = true)
    {
        ShowScreen<Match3LevelSelectionScreen>(animated);
    }

    private void HideAllScreensImmediate()
    {
        foreach (var screen in _screens.Values)
        {
            screen.Hide(false);
        }
        
        _currentScreen = null;
    }
    
    [Button]
    private void CreateBackground()
    {

        if (grid == null || !backgroundTilePrefab || !backgroundParent) return;
        
        _backgroundTiles.Clear();
        foreach (Transform child in backgroundParent)
        {
            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
                DestroyImmediate(child.gameObject);
            }
        }
        
        // Create background tiles
        for (int x = 0; x < grid.Width; x++)
        {
            for (int y = 0; y < grid.Height; y++)
            {
                var position = grid.GetCellWorldPosition(x, y);
                var tile = Instantiate(backgroundTilePrefab, position, Quaternion.identity,backgroundParent);
                _backgroundTiles.Add(tile);
            }
        }
            
        // Set alpha of tiles based on distance from the middle of the grid
        foreach  (var tile in _backgroundTiles)
        {
            var tileGridPosition = grid.GetCell(tile.transform.position);
            var centerOfGrid = new Vector2(grid.Width/2, grid.Height/2);
                
            float distanceFromCenter = Vector2.Distance(tileGridPosition, centerOfGrid);
            
            float maxFadeDistance = 13;
            float normalizedDistance = Mathf.Clamp01(distanceFromCenter / maxFadeDistance);
            normalizedDistance = Mathf.Pow(normalizedDistance, 4f);

            Color baseColor = tile.color;
            Color color = tile.color;
            color.a = Mathf.Lerp(0f, baseColor.a, normalizedDistance);
            tile.color = color;
        }
    }


    private void OnDrawGizmos()
    {
        grid?.DrawGrid();
    }
}