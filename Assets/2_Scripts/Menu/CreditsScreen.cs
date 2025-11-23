using System;
using System.Text;
using DNExtensions;
using DNExtensions.MenuSystem;
using DNExtensions.VFXManager;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CreditsScreen : MenuScreen
{
    
    [Header("References")]
    [SerializeField] private MenuManager menuManager;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private Button backButton;
    

    private void Start()
    {
        SetupButtons();
    }

    protected override Tween GetShowPositionTween()
    {
        Vector3 startPosition = contentOriginalPosition - (Vector3.right * 1000f);
        return Tween.UIAnchoredPosition(contentContainer, startPosition, contentOriginalPosition, showTweenSettings);
    }

    protected override Tween GetHidePositionTween()
    {
        Vector3 endPosition = contentOriginalPosition - (Vector3.right * 1000f);
        return Tween.UIAnchoredPosition(contentContainer, contentOriginalPosition, endPosition, hideTweenSettings);
    }
    


    private void SetupButtons()
    {

        if (backButton)
        {
            SelectableAnimator selectableAnimator = backButton.GetComponent<SelectableAnimator>();
            if (selectableAnimator && audioSource) selectableAnimator.audioSource = audioSource;
            
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(() =>
            {
                CameraManager.Instance?.ShakeCamera(0.5f);
                menuManager?.ShowMainMenu();
            });
        }
    }

    
}