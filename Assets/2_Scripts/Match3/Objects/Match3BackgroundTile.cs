using System;
using DNExtensions.ObjectPooling;
using PrimeTween;
using UnityEngine;

public class Match3BackgroundTile : MonoBehaviour, IPooledObject
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
