using DNExtensions;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelCompleteWindowUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Transform levelCompleteStatsParent;
    [SerializeField] private CanvasGroup levelCompleteWindow;
    [SerializeField] private TextMeshProUGUI levelCompleteTitle;
    [SerializeField] private Button levelButton;
    [SerializeField] private Button quitButton;
    
    [Header("Audio")]
    [SerializeField] private SOAudioEvent levelCompleteWinSfx;
    [SerializeField] private SOAudioEvent levelCompleteFailSfx;
    [SerializeField] private AudioSource audioSource;
    
    [Header("Prefab")]
    [SerializeField] private Match3UIElement match3UIElementPrefab;
    
    [Header("Animation Settings")]
    [SerializeField] private TweenSettings levelCompleteTweenSettings;
    
    private RectTransform _levelCompleteWindowRectTransform;
    private Vector2 _levelCompleteWindowDefaultSize;
    private Sequence _levelCompleteSequence;

    public void Initialize()
    {
        _levelCompleteWindowRectTransform = levelCompleteWindow.GetComponent<RectTransform>();
        _levelCompleteWindowDefaultSize = _levelCompleteWindowRectTransform.sizeDelta;
        _levelCompleteWindowRectTransform.sizeDelta = new Vector2(_levelCompleteWindowDefaultSize.x, 0);
        levelCompleteWindow.alpha = 0f;
        levelCompleteWindow.interactable = false;
        levelCompleteWindow.blocksRaycasts = false;
        
        SetupButtons();
    }

    private void SetupButtons()
    {
        quitButton.onClick.RemoveAllListeners();
        quitButton.onClick.AddListener(() =>
        {
            AnimateLevelCompleteWindow(false);
            ChainCallback(() =>
            {
                GameManager.Instance?.MainMenu.LoadScene();
            });
        });
    }

    public void ShowLevelComplete(Match3LevelData levelData, System.Action onNextLevel)
    {
        if (levelData == null) return;
        
        levelCompleteWinSfx.Play(audioSource);
        
        UpdateLevelButton(true, onNextLevel);
        UpdateLevelCompleteStats(levelData);
        levelCompleteTitle.text = $"{levelData.Level.LevelName} Complete!";
    }

    public void ShowLevelFailed(Match3LevelData levelData, System.Action onRetry)
    {
        if (levelData == null) return;
        
        levelCompleteFailSfx.Play(audioSource);
        
        UpdateLevelButton(false, onRetry);
        UpdateLevelCompleteStats(levelData);
        levelCompleteTitle.text = $"{levelData.Level.LevelName} Failed!";
    }

    public void AnimateLevelCompleteWindow(bool show)
    {
        if (_levelCompleteSequence.isAlive) return;
        if (!levelCompleteWindow || !_levelCompleteWindowRectTransform) return;

        var startSize = show ? new Vector2(_levelCompleteWindowRectTransform.sizeDelta.x, 0f) : _levelCompleteWindowDefaultSize;
        var endSize = show ? _levelCompleteWindowDefaultSize : new Vector2(_levelCompleteWindowRectTransform.sizeDelta.x, 0f);
        
        if (show) levelCompleteWindow.alpha = 1f;
        _levelCompleteWindowRectTransform.sizeDelta = startSize;
        
        _levelCompleteSequence = Sequence.Create()
            .Group(Tween.UISizeDelta(_levelCompleteWindowRectTransform, endSize, levelCompleteTweenSettings))
            .Group(Tween.Alpha(levelCompleteTitle, show ? 1f : 0f, levelCompleteTweenSettings.duration * 0.8f))
            .ChainCallback(() => 
            { 
                levelCompleteWindow.alpha = show ? 1f : 0f;
                levelCompleteWindow.interactable = show;
                levelCompleteWindow.blocksRaycasts = show; 
            });
    }

    public void ChainCallback(System.Action callback)
    {
        if (_levelCompleteSequence.isAlive)
        {
            _levelCompleteSequence.ChainCallback(callback);
        }
        else
        {
            callback?.Invoke();
        }
    }

    public void InsertCallback(float duration, System.Action callback)
    {
        if (_levelCompleteSequence.isAlive)
        {
            _levelCompleteSequence.InsertCallback(duration, callback);
        }
        else
        {
            callback?.Invoke();
        }
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

    private void UpdateLevelButton(bool won, System.Action onButtonPress)
    {
        if (!levelButton) return;
        
        levelButton.onClick.RemoveAllListeners();
        var levelButtonText = levelButton.GetComponentInChildren<TextMeshProUGUI>();
        
        if (won)
        {
            levelButtonText.text = "Next Level";
            levelButton.onClick.AddListener(() => onButtonPress?.Invoke());
        }
        else
        {
            levelButtonText.text = "Try Again";
            levelButton.onClick.AddListener(() => onButtonPress?.Invoke());
        }
    }
}