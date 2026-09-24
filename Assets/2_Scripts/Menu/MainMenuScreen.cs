using System;
using DNExtensions.Systems.VFXManager;
using UnityEngine.Serialization;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuScreen : MenuScreen
{
    [Header("References")]
    [SerializeField] private Button match3Button;
    [SerializeField] private Button creditsButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private MenuManager menuManager;
    [SerializeField] private InformationWindowUI informationWindowUI;
    [SerializeField] private Button infoButton;
    [FormerlySerializedAs("muteButton")]
    [SerializeField] private Button settingsButton;
    [SerializeField] private SettingsWindowUI settingsWindowUI;
    [SerializeField] private Button continueButton;

    private void Start()
    {
        SetupButtons();
        informationWindowUI?.Initialize();
        settingsWindowUI?.Initialize();

        if (SaveManager.Instance) SaveManager.Instance.SaveReset += SetupContinueButton;
    }

    private void OnDestroy()
    {
        if (SaveManager.Instance) SaveManager.Instance.SaveReset -= SetupContinueButton;
    }

    private void SetupContinueButton()
    {
        if (!continueButton) return;

        var level = ResolveLastPlayedLevel();

        // Nothing to continue into on a fresh save, so the button is hidden rather than shown disabled
        continueButton.gameObject.SetActive(level);
        if (!level) return;

        continueButton.onClick.RemoveAllListeners();
        continueButton.onClick.AddListener(() =>
        {
            // Same exit as starting a level from level select
            var vfxDuration = VFXManager.Instance ? VFXManager.Instance.PlaySequence(menuManager.EndLevelEffect) : 0.5f;
            CameraManager.Instance?.ShakeCamera(vfxDuration);
            GameManager.Instance.SelectMatch3Level(level);

            HideByFade(4, () => { GameManager.Instance.Match3Scene.LoadScene(); });
        });
    }

    private SOMatch3Level ResolveLastPlayedLevel()
    {
        if (!SaveManager.Instance || !GameManager.Instance) return null;

        string lastPlayed = SaveManager.Instance.LastPlayedLevel;
        if (string.IsNullOrEmpty(lastPlayed)) return null;

        foreach (var level in GameManager.Instance.Match3Levels)
        {
            if (level && level.name == lastPlayed) return level;
        }

        return null;
    }

    protected override Tween GetShowPositionTween()
    {
        Vector3 startPosition = contentOriginalPosition - (Vector3.left * 1000f);
        return Tween.UIAnchoredPosition(contentContainer, startPosition, contentOriginalPosition, showTweenSettings);
    }

    protected override Tween GetHidePositionTween()
    {
        Vector3 endPosition = contentOriginalPosition - (Vector3.left * 1000f);
        return Tween.UIAnchoredPosition(contentContainer, contentOriginalPosition, endPosition, hideTweenSettings);
    }

    public override void ShowInitial(bool animated = true, Action onComplete = null)
    {
        gameObject.SetActive(true);
        
        if (!animated)
        {
            ShowInstant();
            onComplete?.Invoke();
            return;
        }

        animationSequence.Stop();
        
        if (canvasGroup) canvasGroup.alpha = 0f;
        
        Vector3 startPosition = contentOriginalPosition + (Vector3.up * 1000f);
        
        animationSequence = Sequence.Create()
            .Group(Tween.Alpha(canvasGroup, 1f, showTweenSettings))
            .Group(Tween.UIAnchoredPosition(contentContainer, startPosition, contentOriginalPosition, showTweenSettings))
            .ChainCallback(() =>
            {
                if (canvasGroup)
                {
                    canvasGroup.interactable = true;
                    canvasGroup.blocksRaycasts = true;
                }
                onComplete?.Invoke();
            });
    }

    private void SetupButtons()
    {
        if (match3Button)
        {
            match3Button.onClick.RemoveAllListeners();
            match3Button.onClick.AddListener(() =>
            {
                CameraManager.Instance?.ShakeCamera(0.5f);
                menuManager?.ShowMatch3LevelSelection();
            });
        }
        
        if (quitButton)
        {
            quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(() =>
            {
                #if UNITY_EDITOR
                if (Application.isEditor)
                {
                    UnityEditor.EditorApplication.isPlaying = false;
                    return;
                }
                #endif
                Application.Quit();
            });
            
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                quitButton.gameObject.SetActive(false);
            }
        }

        if (creditsButton)
        {
            creditsButton.onClick.RemoveAllListeners();
            creditsButton.onClick.AddListener(() =>
            {
                CameraManager.Instance?.ShakeCamera(0.1f);
                menuManager?.ShowCredits();
                FirebaseManager.Instance?.LogCreditsClicked();
            });
        }

        if (settingsButton)
        {
            settingsButton.onClick.RemoveAllListeners();
            settingsButton.onClick.AddListener(() =>
            {
                CameraManager.Instance.ShakeCamera(0.1f);
                settingsWindowUI?.Show();
            });
        }

        SetupContinueButton();

        if (infoButton)
        {
            infoButton.onClick.RemoveAllListeners();
            infoButton.onClick.AddListener(() =>
            {
                CameraManager.Instance.ShakeCamera(0.1f);
                informationWindowUI?.ShowAllTutorials();
                FirebaseManager.Instance?.LogInformationClicked();
            });
        }
    }
    
}