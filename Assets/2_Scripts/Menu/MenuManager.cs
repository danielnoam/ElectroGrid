using System;
using System.Collections.Generic;
using DNExtensions;
using DNExtensions.VFXManager;
using UnityEngine;
using VFXManager = DNExtensions.VFXManager.VFXManager;

public class MenuManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MainMenuScreen mainMenuScreen;
    [SerializeField] private Match3LevelSelectionScreen match3LevelSelectionScreen;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private SOAudioEvent screenSwitchSfx;
    [SerializeField] private SOVFEffectsSequence gameStartEffect;
    [SerializeField] private SOVFEffectsSequence startLevelEffect;
    [SerializeField] private SOVFEffectsSequence endLevelEffect;
    [SerializeField] private BackgroundManager backgroundManager;
    
    private IMenuScreen _currentScreen;
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
        VFXManager.Instance?.PlayVFX(gameStartEffect);
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
        
        screenSwitchSfx?.Play(audioSource);
        
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
}