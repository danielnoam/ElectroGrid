using System;
using PrimeTween;
using UnityEngine;

public abstract class MenuScreen : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] protected CanvasGroup canvasGroup;
    [SerializeField] protected RectTransform contentContainer;
    [SerializeField] protected TweenSettings showTweenSettings;
    [SerializeField] protected TweenSettings hideTweenSettings;
    
    protected Vector3 contentOriginalPosition;
    protected Vector3 contentOriginalScale;
    protected Sequence animationSequence;

    protected virtual void Awake()
    {
        if (contentContainer)
        {
            contentOriginalScale = contentContainer.localScale;
            contentOriginalPosition = contentContainer.anchoredPosition3D;
        }
    }

    public virtual void Show(bool animated = true, Action onComplete = null)
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
        
        animationSequence = Sequence.Create()
            .Group(Tween.Alpha(canvasGroup, 1f, showTweenSettings))
            .Group(GetShowPositionTween())
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

    public virtual void ShowInitial(bool animated = true, Action onComplete = null)
    {
        Show(animated, onComplete);
    }

    public virtual void Hide(bool animated = true, Action onComplete = null)
    {
        if (canvasGroup)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (!animated)
        {
            HideInstant();
            onComplete?.Invoke();
            return;
        }

        animationSequence.Stop();
        
        animationSequence = Sequence.Create()
            .Group(Tween.Alpha(canvasGroup, 0f, hideTweenSettings))
            .Group(GetHidePositionTween())
            .ChainCallback(() =>
            {
                gameObject.SetActive(false);
                onComplete?.Invoke();
            });
    }

    public virtual void SetInteractable(bool interactable)
    {
        if (canvasGroup)
        {
            canvasGroup.interactable = interactable;
        }
    }

    protected virtual void ShowInstant()
    {
        if (canvasGroup)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        if (contentContainer)
        {
            contentContainer.localScale = contentOriginalScale;
            contentContainer.anchoredPosition3D = contentOriginalPosition;
        }
    }

    protected virtual void HideInstant()
    {
        if (canvasGroup) canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }

    protected abstract Tween GetShowPositionTween();
    protected abstract Tween GetHidePositionTween();
}