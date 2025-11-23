using System.Collections.Generic;
using DNExtensions;
using DNExtensions.ObjectPooling;
using PrimeTween;
using UnityEngine;

[SelectionBase]
public class Match3HelperObject : Match3Object
{

    [Header("Held Settings")]
    [SerializeField] private float heldDuration = 0.2f;
    [SerializeField] private float heldScaleMultiplier = 0.8f;
    [SerializeField] private Color heldColor;
    [SerializeField] private SOAudioEvent swapSfx;

    private Match3GameManager _gameManager;
    private Color _baseColor;
    private bool _held;
    
    public override bool IsSwappable => true;
    public override bool IsMatchable => false;
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
        
        _gameManager = Match3GameManager.Instance;
        if (_gameManager)
        {
            _gameManager.MatchesMade -= OnMatchesMade;
            _gameManager.MatchesMade += OnMatchesMade;
        }
        
        _held = false;
        UpdateVisuals();
    }

    private void OnMatchesMade(List<Match3Tile> matches)
    {
        if (_beingDestroyed || !_currentTile || !_currentTile.IsActive) return;
        
        foreach (var match in matches)
        {
            if (_gridHandler.AreTilesNeighbours(match, _currentTile))
            {
                MatchFound();
                return;
            }
        }
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
        if (_beingDestroyed || !IsSwappable) return;
        
        _held = held;
        UpdateVisuals();
    }

    private void MatchFound()
    {
        Match3EffectManager.Instance?.CreateHelperBackgroundParticle(transform.position);
        _gameManager?.NotifyHelperObjectDestroyed(this);
        _currentTile?.PunchTile();
        _currentTile?.SetCurrentItem(null);
        DestroyWithAnimation();
    }
    
    public override void DestroyWithAnimation()
    {
        _beingDestroyed = true;
        
        var destroySequence = Sequence.Create();
        destroySequence.Group(Tween.Scale(transform, _baseScale * destroyScaleMultiplier, destroyDuration, Ease.OutBack));
        destroySequence.InsertCallback(destroyDuration * 0.5f, () =>
        {
            MobileHaptics.Vibrate(50);
            CameraManager.Instance?.ShakeCamera(0.2f);
            destroySfx?.PlayAtPoint(transform.position);
            
            if (destroyParticle)
            {
                var particleGo = ObjectPooler.GetObjectFromPool(destroyParticle.gameObject, transform.position, Quaternion.identity);
                var particle = particleGo.GetComponent<OneShotParticle>();
                particle.Play(transform.position);
            }
        });
        destroySequence.ChainCallback(() => { ObjectPooler.ReturnObjectToPool(gameObject); });
    }
    private void UpdateVisuals()
    {
        if (_beingDestroyed || !itemRenderer) return;
        
        itemRenderer.color = _held ? heldColor : _baseColor;
        var endScale = _held ? _baseScale * heldScaleMultiplier : _baseScale;
        if (transform.localScale != endScale) Tween.Scale(transform, endScale, heldDuration, Ease.OutBack);
    }
    
}