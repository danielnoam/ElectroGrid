using System;
using DNExtensions.MenuSystem;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuScreen : MenuScreen
{
    [Header("References")]
    [SerializeField] private Button match3Button;
    [SerializeField] private Button creditsButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private MenuManager menuManager;
    [SerializeField] private InformationWindowUI informationWindowUI;
    [SerializeField] private Button infoButton;
    [SerializeField] private Button muteButton;
    [SerializeField] private Image muteButtonImage;
    [SerializeField] private Sprite mutedSprite;
    [SerializeField] private Sprite unmutedSprite;

    private void Start()
    {
        SetupButtons();
        informationWindowUI?.Initialize();
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
            SelectableAnimator selectableAnimator = match3Button.GetComponent<SelectableAnimator>();
            if (selectableAnimator && audioSource) selectableAnimator.audioSource = audioSource;
            
            match3Button.onClick.RemoveAllListeners();
            match3Button.onClick.AddListener(() =>
            {
                CameraManager.Instance?.ShakeCamera(0.5f);
                menuManager?.ShowMatch3LevelSelection();
            });
        }
        
        if (quitButton)
        {
            SelectableAnimator selectableAnimator = quitButton.GetComponent<SelectableAnimator>();
            if (selectableAnimator && audioSource) selectableAnimator.audioSource = audioSource;
            
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
        }

        if (creditsButton)
        {
            SelectableAnimator selectableAnimator = creditsButton.GetComponent<SelectableAnimator>();
            if (selectableAnimator && audioSource) selectableAnimator.audioSource = audioSource;

            creditsButton.onClick.RemoveAllListeners();
            creditsButton.onClick.AddListener(() =>
            {
                CameraManager.Instance?.ShakeCamera(0.1f);
                menuManager?.ShowCredits();
            });
        }

        if (muteButtonImage)
        {
            muteButtonImage.sprite = AudioManager.Instance.IsMuted ? mutedSprite : unmutedSprite;
    
            muteButton.onClick.RemoveAllListeners();
            muteButton.onClick.AddListener(() =>
            {
                CameraManager.Instance.ShakeCamera(0.1f);
                AudioManager.Instance.ToggleAudio();
                muteButtonImage.sprite = AudioManager.Instance.IsMuted ? mutedSprite : unmutedSprite;
            });
        }

        if (infoButton)
        {
            infoButton.onClick.RemoveAllListeners();
            infoButton.onClick.AddListener(() =>
            {
                CameraManager.Instance.ShakeCamera(0.1f);
                informationWindowUI?.Toggle(true);
            });
        }
    }
    
}