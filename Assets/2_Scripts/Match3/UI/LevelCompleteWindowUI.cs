using System;
using DNExtensions;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelCompleteWindowUI : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private TweenSettings levelCompleteTweenSettings;
    
    [Header("References")]
    [SerializeField] private Transform levelCompleteStatsParent;
    [SerializeField] private TextMeshProUGUI levelCompleteTitle;
    [SerializeField] private Button levelButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private SOAudioEvent levelCompleteWinSfx;
    [SerializeField] private SOAudioEvent levelCompleteFailSfx;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private Match3UIElement match3UIElementPrefab;
    
    private Match3GameManager _match3Manager;
    private CanvasGroup _levelCompleteWindow;
    private RectTransform _rectTransform;
    private Vector2 _defaultSize;
    private Sequence _toggleSequence;


    private void Awake()
    {
        _levelCompleteWindow = GetComponent<CanvasGroup>();
        _rectTransform = GetComponent<RectTransform>();
        _defaultSize = _rectTransform.sizeDelta;
        _rectTransform.sizeDelta = new Vector2(_defaultSize.x, 0);
        _levelCompleteWindow.alpha = 0f;
        _levelCompleteWindow.interactable = false;
        _levelCompleteWindow.blocksRaycasts = false;
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
        Toggle(false);
    }

    private void OnLevelComplete(Match3LevelData levelData)
    {
        ShowLevelComplete(levelData);
        Toggle(true);
    }

    private void OnLevelFailed(Match3LevelData levelData)
    {
        ShowLevelFailed(levelData);
        Toggle(true);
    }

    private void SetupButtons()
    {
        quitButton.onClick.RemoveAllListeners();
        quitButton.onClick.AddListener(() =>
        {
            Toggle(false);
            _toggleSequence.ChainCallback(() =>
            {
                GameManager.Instance?.MainMenu.LoadScene();
            });
        });
    }

    private void ShowLevelComplete(Match3LevelData levelData)
    {
        if (levelData == null) return;
        
        levelCompleteWinSfx?.Play(audioSource);
        
        UpdateLevelButton(true);
        UpdateLevelCompleteStats(levelData);
        levelCompleteTitle.text = $"{levelData.Level.LevelName} Complete!";
    }

    private void ShowLevelFailed(Match3LevelData levelData)
    {
        if (levelData == null) return;
        
        levelCompleteFailSfx.Play(audioSource);
        
        UpdateLevelButton(false);
        UpdateLevelCompleteStats(levelData);
        levelCompleteTitle.text = $"{levelData.Level.LevelName} Failed!";
    }

    private void Toggle(bool show)
    {
        if (_toggleSequence.isAlive) return;
        if (!_levelCompleteWindow || !_rectTransform) return;

        var startSize = show ? new Vector2(_rectTransform.sizeDelta.x, 0f) : _defaultSize;
        var endSize = show ? _defaultSize : new Vector2(_rectTransform.sizeDelta.x, 0f);
        var startAlpha = show ? 0f : 1f;
        var endAlpha = show ? 1f : 0f;
        
        if (show) _levelCompleteWindow.alpha = 1f;
        _rectTransform.sizeDelta = startSize;
        levelCompleteTitle.alpha = startAlpha;
        
        _toggleSequence = Sequence.Create(useUnscaledTime: true)
            .Group(Tween.UISizeDelta(_rectTransform, endSize, levelCompleteTweenSettings))
            .Group(Tween.Alpha(levelCompleteTitle, endAlpha, levelCompleteTweenSettings.duration * 0.8f))
            .ChainCallback(() => 
            { 
                _levelCompleteWindow.alpha = show ? 1f : 0f;
                _levelCompleteWindow.interactable = show;
                _levelCompleteWindow.blocksRaycasts = show; 
            });
    }

    private void UpdateLevelCompleteStats(Match3LevelData levelData)
    {
        if (!levelCompleteStatsParent) return;

        foreach (Transform child in levelCompleteStatsParent)
        {
            Destroy(child.gameObject);
        }

        var matchedMadeElement = Instantiate(match3UIElementPrefab, levelCompleteStatsParent);
        matchedMadeElement.Setup(null, $"Matches Made: {levelData.MatchesMade}");
        matchedMadeElement.gameObject.name = "MatchesMade";
        
        var movesMadeElement = Instantiate(match3UIElementPrefab, levelCompleteStatsParent);
        movesMadeElement.Setup(null, $"Moves Made: {levelData.MovesMade}");
        movesMadeElement.gameObject.name = "MovesMade";
    }

    private void UpdateLevelButton(bool won)
    {
        if (!levelButton) return;
        
        levelButton.onClick.RemoveAllListeners();
        var levelButtonText = levelButton.GetComponentInChildren<TextMeshProUGUI>();
        
        if (won)
        {
            levelButtonText.text = "Next Level";
            levelButton.onClick.AddListener(OnNextLevelPressed);
        }
        else
        {
            levelButtonText.text = "Try Again";
            levelButton.onClick.AddListener(OnRetryPressed);
        }
    }

    private void OnNextLevelPressed()
    {
        Toggle(false);
        _toggleSequence.ChainCallback(() =>
        {
            _match3Manager.SetNextLevel();
        });
    }

    private void OnRetryPressed()
    {
        Toggle(false);
        _toggleSequence.ChainCallback(() =>
        {
            _match3Manager.RestartLevel();
        });
    }
}