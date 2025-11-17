using System;
using DNExtensions.MenuSystem;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuScreen : MonoBehaviour, IMenuScreen
{
    [Header("Animation")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform contentContainer;
    [SerializeField] private TweenSettings showTweenSettings;
    [SerializeField] private TweenSettings hideTweenSettings;
    
    [Header("References")]
    [SerializeField] private Button match3Button;
    [SerializeField] private Button quitButton;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private MenuManager menuManager;
    
    private Vector3 _contentOriginalPosition;
    private Vector3 _contentOriginalScale;
    private Sequence _animationSequence;

    private void Awake()
    {
        if (contentContainer)
        {
            _contentOriginalScale = contentContainer.localScale;
            _contentOriginalPosition = contentContainer.anchoredPosition3D;
        }
    }

    private void OnEnable()
    {
        SetupButtons();
    }

    public void Show(bool animated = true, Action onComplete = null)
    {
        gameObject.SetActive(true);
        
        if (!animated)
        {
            if (canvasGroup)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            if (contentContainer)
            {
                contentContainer.localScale = _contentOriginalScale;
                contentContainer.anchoredPosition3D = _contentOriginalPosition;
            }
            
            onComplete?.Invoke();
            return;
        }

        _animationSequence.Stop();
        
        if (canvasGroup) canvasGroup.alpha = 0f;
        
        _animationSequence = Sequence.Create()
            .Group(Tween.Alpha(canvasGroup, 1f, showTweenSettings))
            .Group(Tween.UIAnchoredPosition(contentContainer,_contentOriginalPosition - (Vector3.left * 1000f),_contentOriginalPosition, showTweenSettings))
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

    public void Hide(bool animated = true, Action onComplete = null)
    {
        if (canvasGroup)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (!animated)
        {
            if (canvasGroup) canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
            onComplete?.Invoke();
            return;
        }

        _animationSequence.Stop();
        
        _animationSequence = Sequence.Create()
            .Group(Tween.Alpha(canvasGroup, 0f, hideTweenSettings))
            .Group(Tween.UIAnchoredPosition(contentContainer, _contentOriginalPosition, _contentOriginalPosition - (Vector3.left * 1000f), hideTweenSettings))
            .ChainCallback(() =>
            {
                gameObject.SetActive(false);
                onComplete?.Invoke();
            });
    }

    public void SetInteractable(bool interactable)
    {
        if (canvasGroup)
        {
            canvasGroup.interactable = interactable;
        }
    }

    private void SetupButtons()
    {
        if (match3Button)
        {
            SelectableAnimator selectableAnimator = match3Button.GetComponent<SelectableAnimator>();
            if (selectableAnimator && audioSource) selectableAnimator.audioSource = audioSource;
            
            match3Button.onClick.RemoveAllListeners();
            match3Button.onClick.AddListener(OnMatch3ButtonClicked);
        }
        
        if (quitButton)
        {
            SelectableAnimator selectableAnimator = quitButton.GetComponent<SelectableAnimator>();
            if (selectableAnimator && audioSource) selectableAnimator.audioSource = audioSource;
            
            quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(OnQuitButtonClicked);
        }
    }

    private void OnMatch3ButtonClicked()
    {
        CameraManager.Instance?.ShakeCamera(0.5f);
        menuManager?.ShowMatch3LevelSelection();
    }

    private void OnQuitButtonClicked()
    {
#if UNITY_EDITOR
        if (Application.isEditor)
        {
            UnityEditor.EditorApplication.isPlaying = false;
            return;
        }
#endif
        Application.Quit();
    }
}