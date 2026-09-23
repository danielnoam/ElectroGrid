using System;
using DNExtensions.Systems.VFXManager;
using DNExtensions.Utilities.SerializableSelector;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Scales the VFXManager fullscreen image. The level transitions open and close the black
/// overlay horizontally with this. DNExtensions dropped it in favour of wipes, which slide
/// the image instead, so it lives here now.
/// </summary>
[Serializable]
[SerializableSelectorName("Scale", "Fullscreen")]
public class SetFullscreenScale : EffectBase
{
    [SerializeField] private Vector3 startScale = Vector3.one;
    [SerializeField] private Vector3 endScale = Vector3.one;
    [SerializeField] private Ease ease = Ease.Linear;

    private Image _image;
    private Sequence _sequence;

    protected override bool Initialize()
    {
        if (!base.Initialize()) return false;

        _image = VFXManager.Instance.FullScreenImage;
        if (!_image)
        {
            Debug.LogWarning("FullScreenImage not found in VFXManager. SetFullscreenScale effect will not play.", VFXManager.Instance);
            return false;
        }

        return true;
    }

    protected override void OnPlayEffect(float sequenceDuration)
    {
        if (_sequence.isAlive) _sequence.Stop();

        _sequence = Sequence.Create(useUnscaledTime: true)
            .Group(Tween.Scale(_image.rectTransform, startScale, endScale, GetEffectDuration(sequenceDuration), ease, startDelay: GetStartDelay(sequenceDuration)));
    }

    protected override void OnResetEffect()
    {
        if (_sequence.isAlive) _sequence.Stop();
        if (_image) _image.rectTransform.localScale = VFXManager.Instance.DefaultFullScreenImage.LocalScale;
    }
}
