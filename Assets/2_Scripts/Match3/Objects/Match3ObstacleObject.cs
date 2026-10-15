
using System.Collections.Generic;
using DNExtensions.Systems.AudioLibrary;
using DNExtensions.Systems.ObjectPooling;
using PrimeTween;
using UnityEngine;


[SelectionBase]
public class Match3ObstacleObject : Match3Object
{
    [Header("Obstacle Settings")]
    [SerializeField] private int maxHealth = 1;
    [Tooltip("The sprite for each health left: element 0 at 1 health, element 1 at 2, and so on")]
    [SerializeField] private Sprite[] healthSprites;
    [Tooltip("Played when the Star takes a hit but survives, a lighter version of the break particle")]
    [SerializeField] private PoolableParticleSystem hitParticle;
    [SerializeField] private float pulseEffectSpeed = 2f;
    [SerializeField] private float pulseEffectMinScale = 0.8f;
    [SerializeField] private float pulseEffectMaxScale = 1.2f;
    [SerializeField] private float pulseRandomOffset = 1f;
    

    private Match3GameManager _gameManager;
    private int _currentHealth;
    private Sequence _hitSequence;
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
        if (_currentTile && !_hitSequence.isAlive)
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
        RefreshHealthSprite();

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
            _movementSequence.ChainCallback(() => { AudioLibrary.Play(spawnSfx); });
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
            _hitSequence.Stop();
            _hitSequence = Sequence.Create();
            var damageSequence = _hitSequence;
            damageSequence.Group(Tween.Scale(transform, _baseScale * destroyScaleMultiplier, destroyDuration, Ease.OutBack));
            damageSequence.InsertCallback(destroyDuration * 0.5f, () =>
            {
                RefreshHealthSprite();
                MobileHaptics.Vibrate(30);
                CameraManager.Instance?.ShakeCamera(0.1f);
                AudioLibrary.Play(destroySfx);
                if (hitParticle)
                {
                    var particle = ObjectPooler.GetObjectFromPool(hitParticle.gameObject, transform.position, Quaternion.identity);
                    particle.GetComponent<PoolableParticleSystem>().Play(transform.position);
                }
            });
            damageSequence.Chain(Tween.Scale(transform, _baseScale, 0.15f, Ease.OutBack));
        }
    }
    private void RefreshHealthSprite()
    {
        if (!itemRenderer || healthSprites == null || healthSprites.Length == 0) return;

        var sprite = healthSprites[Mathf.Clamp(_currentHealth - 1, 0, healthSprites.Length - 1)];
        if (sprite) itemRenderer.sprite = sprite;
    }

    private void OnMatchesMade(List<Match3Tile> matches)
    {
        if (IsAdjacentToAnyMatch(matches)) TakeDamage();
    }

    public override void DestroyWithAnimation()
    {
        _hitSequence.Stop();
        _currentTile?.SetCurrentItem(null);
        _gameManager?.NotifyObstacleBroke(this);

        base.DestroyWithAnimation();
    }

    public override void OnPoolReturn()
    {
        base.OnPoolReturn();
        _hitSequence.Stop();
        
        if (_gameManager) _gameManager.MatchesMade -= OnMatchesMade;
    }
}