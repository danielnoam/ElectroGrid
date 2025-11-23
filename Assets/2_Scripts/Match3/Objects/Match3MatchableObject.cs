using DNExtensions;
using PrimeTween;
using UnityEngine;

[SelectionBase]
public class Match3MatchableObject : Match3Object
{
    private static readonly int EmissionMask = Shader.PropertyToID("_Emission_Mask");

    [Header("Matchable Settings")]
    [SerializeField] private float heldDuration = 0.2f;
    [SerializeField] private float heldScaleMultiplier = 0.8f;
    [SerializeField] private Color heldColor;
    [SerializeField] private SOAudioEvent swapSfx;

    private Color _baseColor;
    private bool _held;
    
    public override bool IsSwappable => true;
    public override bool IsMatchable => true;
    public override bool IsMovable => true;
    public override bool IsAffectedBySpecialMatches => true;
    
    

    protected override void Awake()
    {
        base.Awake();
        if (itemRenderer) _baseColor = itemRenderer.color;
    }

    public override void Initialize(SOItemData data, Match3GridHandler gridHandler)
    {
        base.Initialize(data, gridHandler);
        
        if (data)
        {
            _baseColor = data.Color;
            UpdateEmissionMask(data.EmissionMask);
        }
        _held = false;
        UpdateVisuals();
    }

    public override void SetCurrentTile(Match3Tile match3Tile)
    {
        bool spawning = !_currentTile;
        _currentTile = match3Tile;
        
        _movementSequence.Stop();
        
        var endPosition = new Vector3(_currentTile.transform.localPosition.x, _currentTile.transform.localPosition.y, transform.localPosition.z);
        var ease = spawning ? Ease.OutQuad : Ease.Linear;
        
        _movementSequence = Sequence.Create();
        _movementSequence.Group(Tween.LocalPosition(transform, endPosition, swapDuration, ease));
        _movementSequence.ChainCallback(() =>
        {
            if (spawning)
            {
                if (spawnSfx) spawnSfx.Play(audioSource);
                _currentTile?.SquashTile();
            }
            else
            {
                if (swapSfx) swapSfx.Play(audioSource);
            }
        });
        
        var duration = 0.15f;
        _movementSequence.Group(Tween.Scale(transform, _baseScale * 0.7f, duration * 0.3f, Ease.Linear));
        _movementSequence.Chain(Tween.Scale(transform, _baseScale, duration * 0.7f, Ease.OutSine));

    }
    
    
    public override void SetHeld(bool held)
    {
        if (_beingDestroyed) return;
        
        _held = held;
        UpdateVisuals();
    }

    public void MatchFound()
    {
        Match3EffectManager.Instance?.CreateMatchBackgroundParticle(transform.position, _itemData);
        _currentTile?.PunchTile();
        _currentTile?.SetCurrentItem(null);
        DestroyWithAnimation();
    }
    
    private void UpdateVisuals()
    {
        if (_beingDestroyed || !itemRenderer) return;
        
        itemRenderer.color = _held ? heldColor : _baseColor;
        var endScale = _held ? _baseScale * heldScaleMultiplier : _baseScale;
        if (transform.localScale != endScale) Tween.Scale(transform, endScale, heldDuration, Ease.OutBack);
    }

    private void UpdateEmissionMask(Texture2D emissionMask)
    {
        itemRenderer.material.SetTexture(EmissionMask, emissionMask);
        
    }
}