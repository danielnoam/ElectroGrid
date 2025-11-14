using System;
using DNExtensions.MenuSystem;
using UnityEngine;

public class SelectableHaptics : MonoBehaviour
{
    
    [Header("Settings")]
    [Tooltip("Duration of the haptic feedback in milliseconds")]
    [SerializeField, Min(1)] private long duration = 50;
    [SerializeField] private bool playOnSubmit = true;
    [SerializeField] private bool playOnSelect;
    [SerializeField] private bool playOnDeselect;
    
    [Header("References")]
    [SerializeField] private SelectableAnimator selectableAnimator;


    private void OnEnable()
    {
        if (!selectableAnimator) return;
        
        if (playOnSubmit) selectableAnimator.OnSubmitEvent += PlayHaptics;
        if (playOnSelect) selectableAnimator.OnSubmitEvent += PlayHaptics;
        if (playOnDeselect) selectableAnimator.OnSubmitEvent += PlayHaptics;
    }


    private void OnDisable()
    {
        if (!selectableAnimator) return;
        
        selectableAnimator.OnSubmitEvent -= PlayHaptics;

    }

    private void PlayHaptics()
    {
        MobileHaptics.Vibrate(duration);
    }
}