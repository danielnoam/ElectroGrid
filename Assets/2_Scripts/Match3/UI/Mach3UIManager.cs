using DNExtensions.VFXManager;
using PrimeTween;
using UnityEngine;

public class Mach3UIManager : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private TopBarUI topBarUI;
    [SerializeField] private BottomBarUI bottomBarUI;
    [SerializeField] private LevelCompleteWindowUI levelCompleteWindowUI;
    
    [Header("References")]
    [SerializeField] private Match3GameManager match3Manager;
    [SerializeField] private Match3EffectManager match3EffectManager;

    private void Awake()
    {
        topBarUI.Initialize();
        bottomBarUI.Initialize();
        levelCompleteWindowUI.Initialize();
    }

    private void OnEnable()
    {
        match3Manager.LevelStarted += OnLevelStarted;
        match3Manager.LevelComplete += OnLevelComplete;
        match3Manager.LevelFailed += OnLevelFailed;
    }

    private void OnDisable()
    {
        match3Manager.LevelStarted -= OnLevelStarted;
        match3Manager.LevelComplete -= OnLevelComplete;
        match3Manager.LevelFailed -= OnLevelFailed;
    }

    private void Update()
    {
        topBarUI.UpdateUIElements();
    }

    private void OnLevelComplete(Match3LevelData levelData)
    {
        levelCompleteWindowUI.ShowLevelComplete(levelData, () =>
        {
            levelCompleteWindowUI.AnimateLevelCompleteWindow(false);
            levelCompleteWindowUI.InsertCallback(0.5f, () =>
            {
                match3Manager.SetNextLevel();
            });
        });
        
        topBarUI.AnimateTopBar(false);
        bottomBarUI.AnimateBottomBar(false);
        levelCompleteWindowUI.AnimateLevelCompleteWindow(true);
    }

    private void OnLevelFailed(Match3LevelData levelData)
    {
        levelCompleteWindowUI.ShowLevelFailed(levelData, () =>
        {
            levelCompleteWindowUI.AnimateLevelCompleteWindow(false);
            levelCompleteWindowUI.InsertCallback(0.5f, () =>
            {
                match3Manager.RestartLevel();
            });
        });
        
        topBarUI.AnimateTopBar(false);
        bottomBarUI.AnimateBottomBar(false);
        levelCompleteWindowUI.AnimateLevelCompleteWindow(true);
    }

    private void OnLevelStarted(Match3LevelData levelData)
    {
        topBarUI.SetupLevel(levelData);
        topBarUI.AnimateTopBar(true);
        bottomBarUI.AnimateBottomBar(true);
        levelCompleteWindowUI.AnimateLevelCompleteWindow(false);
    }
}