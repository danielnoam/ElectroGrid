
using System.Collections.Generic;
using DNExtensions.ObjectPooling;
using PrimeTween;
using UnityEngine;


[SelectionBase]
public class Match3ObstacleObject : Match3Object
{
    [Header("Obstacle Settings")]
    [SerializeField] private int maxHealth = 1;
    [SerializeField] private float pulseEffectSpeed = 2f;
    [SerializeField] private float pulseEffectMinScale = 0.8f;
    [SerializeField] private float pulseEffectMaxScale = 1.2f;
    [SerializeField] private float pulseRandomOffset = 1f;
    

    private Match3GameManager _gameManager;
    private int _currentHealth;
    private Vector3 _baseSpriteScale;
    private float _pulseTimeOffset;
    
    public override bool IsSwappable => false;
    public override bool IsMatchable => false;
    public override bool IsMovable => false;
    public override bool IsAffectedBySpecialMatches => true;


    protected override void Awake()
    {
        base.Awake();
        
        _baseSpriteScale = itemRenderer.transform.localScale;
    }

    private void Update()
    {
        if (_currentTile)
        {
            float time = (Time.time + _pulseTimeOffset) * pulseEffectSpeed;
            float sinWave = Mathf.Sin(time);
        
            // Create a sharper pulse by squaring the sine wave (only keeps positive values as pulses)
            float pulse = Mathf.Max(0, sinWave);
            pulse = Mathf.Pow(pulse,25); // Square it for sharper peaks
        
            var pulseScale = Mathf.Lerp(pulseEffectMinScale, pulseEffectMaxScale, pulse);
            transform.localScale = _baseScale * pulseScale;
        }
    }


    public override void Initialize(SOItemData data, Match3GridHandler gridHandler)
    {
        base.Initialize(data, gridHandler);
        
        _currentHealth = maxHealth;
        
        _gameManager = Match3GameManager.Instance;
        if (_gameManager)
        {
            _gameManager.MatchesMade -= OnMatchesMade;
            _gameManager.MatchesMade += OnMatchesMade;
        }
        
        transform.localScale = Vector3.zero;
        itemRenderer.transform.localScale = _baseSpriteScale;
        _pulseTimeOffset = UnityEngine.Random.Range(0f, pulseRandomOffset);
    }

    public override void SetCurrentTile(Match3Tile match3Tile)
    {
        bool spawning = !_currentTile;
        _currentTile = match3Tile;
        
        _movementSequence.Stop();
        _movementSequence = Sequence.Create();
        
        if (spawning)
        {
            _movementSequence.Group(Tween.Scale(transform, _baseScale, 0.5f, Ease.OutBack, startDelay: 0.5f));
            _movementSequence.ChainCallback(() => { spawnSfx?.Play(audioSource); });
        }
        else
        {
            var endPosition = new Vector3(_currentTile.transform.localPosition.x, _currentTile.transform.localPosition.y, transform.localPosition.z);
            
            _movementSequence.Group(Tween.Scale(transform, Vector3.zero, swapDuration/2, Ease.OutBack, startDelay: 0.5f));
            _movementSequence.ChainCallback(() => { transform.localPosition = endPosition; });
            _movementSequence.Group(Tween.Scale(transform,_baseScale, swapDuration/2, Ease.OutBack, startDelay: 0.5f));
        }
    }

    private void TakeDamage(int damage = 1)
    {
        if (_beingDestroyed) return;
        
        _currentHealth -= damage;
        
        if (_currentHealth <= 0)
        {
            DestroyWithAnimation();
        }
        else
        {
            var damageSequence = Sequence.Create();
            damageSequence.Group(Tween.Scale(transform, _baseScale * 1.3f, 0.1f, Ease.OutBack));
            damageSequence.Chain(Tween.Scale(transform, _baseScale, 0.1f, Ease.OutBack));
        }
    }
    private void OnMatchesMade(List<Match3Tile> matches)
    {
        if (_beingDestroyed || !_currentTile || !_currentTile.IsActive) return;
        
        foreach (var match in matches)
        {
            if (_gridHandler.AreTilesNeighbours(match, _currentTile))
            {
                TakeDamage();
                return;
            }
        }
    }
    
    public override void DestroyWithAnimation()
    {
        _currentTile?.SetCurrentItem(null);
        _gameManager?.NotifyObstacleBroke(this);
        
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
                var oneShotParticle = particleGo.GetComponent<OneShotParticle>();
                oneShotParticle.Play(transform.position);
            }
        });
        destroySequence.ChainCallback(() => { ObjectPooler.ReturnObjectToPool(gameObject); });
    }

    public override void OnPoolReturn()
    {
        base.OnPoolReturn();
        
        if (_gameManager) _gameManager.MatchesMade -= OnMatchesMade;
    }
}