using System;
using DNExtensions.Systems.ObjectPooling;
using PrimeTween;
using UnityEngine;

public class Match3BackgroundTile : MonoBehaviour, IPoolable
{
    [Header("Settings")]
    [SerializeField] private Color inactiveTileColor = new Color(0.1f, 0.1f, 0.1f, 0.1f);
    [SerializeField] private float squashScaleAmount = 0.7f;
    [SerializeField] private float squashDuration = 0.2f;
    
    [Header("References")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Transform squashTransform;
    
    private Vector3 _baseScale;
    private Sequence _squashSequence;
    private bool _isSquashed;
    
    public SpriteRenderer SpriteRenderer => spriteRenderer;
    public Color InactiveTileColor => inactiveTileColor;

    
    
    private void Awake()
    {
        _baseScale = squashTransform.localScale;
    }

    private void OnDestroy()
    {
        _squashSequence.Stop();
    }

    public void SquashTile(int delayMultiplier = 0)
    {
        _squashSequence.Stop();
        
        _squashSequence = Sequence.Create();
        _squashSequence.Group(Tween.Scale(squashTransform, _baseScale * squashScaleAmount, squashDuration * 0.3f, Ease.Linear, startDelay: 0.02f * delayMultiplier));
        _squashSequence.Chain(Tween.Scale(squashTransform, _baseScale, squashDuration * 0.7f, Ease.OutSine));
    }

    public float SquashDuration => squashDuration;

    /// <summary>
    /// Sets the squash to a point in time along the same curve as <see cref="SquashTile"/>, for a caller that
    /// drives many tiles from one clock instead of giving each its own tween. Outside the squash it rests at
    /// full size, and a tile already resting is not written again.
    /// </summary>
    public void EvaluateSquash(float elapsed)
    {
        float squashDownTime = squashDuration * 0.3f;
        float factor;

        if (elapsed <= 0f || elapsed >= squashDuration)
        {
            if (!_isSquashed) return;
            factor = 1f;
        }
        else if (elapsed < squashDownTime)
        {
            factor = Mathf.Lerp(1f, squashScaleAmount, elapsed / squashDownTime);
        }
        else
        {
            float t = (elapsed - squashDownTime) / (squashDuration - squashDownTime);
            factor = Mathf.Lerp(squashScaleAmount, 1f, Mathf.Sin(t * Mathf.PI * 0.5f));
        }

        _isSquashed = factor < 1f;
        squashTransform.localScale = _baseScale * factor;
    }

    public void OnPoolGet()
    {
        
    }

    public void OnPoolReturn()
    {
        _squashSequence.Stop();
        _baseScale = squashTransform.localScale;
    }

    public void OnPoolRecycle()
    {

    }
}
