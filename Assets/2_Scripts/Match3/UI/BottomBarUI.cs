using System;
using DNExtensions.Systems.VFXManager;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

public class BottomBarUI : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private TweenSettings bottomBarTweenSettings;
    
    [Header("References")]
    [SerializeField] private Button quitButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Match3EffectManager match3EffectManager;
    [SerializeField] private TopBarUI topBarUI;
    
    private Match3GameManager _match3Manager;
    private RectTransform _rectTransform;
    private float _bottomBarDefaultYPosition;
    private Sequence _bottomBarSequence;


    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _bottomBarDefaultYPosition = _rectTransform.anchoredPosition.y;
        _rectTransform.anchoredPosition = new Vector2(_rectTransform.anchoredPosition.x, -_rectTransform.sizeDelta.y);
    }

    public void Initialize(Match3GameManager match3Manager)
    {
        _match3Manager = match3Manager;
        
        SetupButtons();
        SubscribeToEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    private void SubscribeToEvents()
    {
        if (_match3Manager == null) return;
        
        _match3Manager.LevelStarted += OnLevelStarted;
        _match3Manager.LevelComplete += OnLevelComplete;
        _match3Manager.LevelFailed += OnLevelFailed;
    }

    private void UnsubscribeFromEvents()
    {
        if (_match3Manager == null) return;
        
        _match3Manager.LevelStarted -= OnLevelStarted;
        _match3Manager.LevelComplete -= OnLevelComplete;
        _match3Manager.LevelFailed -= OnLevelFailed;
    }

    private void OnLevelStarted(Match3LevelData levelData)
    {
        Toggle(true);
    }

    private void OnLevelComplete(Match3LevelData levelData)
    {
        Toggle(false);
    }

    private void OnLevelFailed(Match3LevelData levelData)
    {
        Toggle(false);
    }

    private void SetupButtons()
    {
        quitButton.onClick.RemoveAllListeners();
        quitButton.onClick.AddListener(() =>
        {
            _match3Manager.LogLevelQuit();

            if (VFXManager.Instance)
            {
                CameraManager.Instance.ShakeCamera(0.1f);
                Toggle(false);
                topBarUI.Toggle(false);
                var quitSequence = Sequence.Create();
                quitSequence.ChainDelay(VFXManager.Instance.PlaySequence(match3EffectManager.EndLevelSequence));
                quitSequence.ChainCallback(() => GameManager.Instance?.MainMenu.LoadScene());
            }
            else
            {
                GameManager.Instance?.MainMenu.LoadScene();
            }
        });
        
        restartButton.onClick.RemoveAllListeners();
        restartButton.onClick.AddListener(() =>
        {
            if (VFXManager.Instance)
            {
                CameraManager.Instance.ShakeCamera(0.1f);
                Toggle(false);
                topBarUI.Toggle(false);
                var quitSequence = Sequence.Create();
                quitSequence.ChainDelay(VFXManager.Instance.PlaySequence(match3EffectManager.EndLevelSequence));
                quitSequence.ChainCallback(() => _match3Manager.StartNewGame());
            }
            else
            {
                _match3Manager.StartNewGame();
            }
        });
    }

    public void Toggle(bool show)
    {
        if (!_rectTransform) return;

        _bottomBarSequence.Stop();
        
        // Resolved on every toggle, the safe area can change between levels (rotation, simulator)
        float shownYPosition = SafeAreaMargin.ResolveDistance(_rectTransform, SafeAreaMargin.Edge.Bottom, _bottomBarDefaultYPosition);
        var startYPosition = show ? -_rectTransform.sizeDelta.y : shownYPosition;
        var endYPosition = show ? shownYPosition : -_rectTransform.sizeDelta.y;
        
        _rectTransform.anchoredPosition = new Vector2(_rectTransform.anchoredPosition.x, startYPosition);
        
        _bottomBarSequence = Sequence.Create(useUnscaledTime: true)
            .Group(Tween.UIAnchoredPositionY(_rectTransform, endYPosition, bottomBarTweenSettings));
    }
}