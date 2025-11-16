using DNExtensions;
using DNExtensions.ObjectPooling;
using PrimeTween;
using UnityEngine;

public abstract class Match3Object : MonoBehaviour, IPooledObject
{
    [Header("Settings")]
    [SerializeField] protected float swapDuration = 0.2f;
    [SerializeField] protected float destroyDuration = 0.2f;
    [SerializeField] protected float destroyScaleMultiplier = 1.2f;
    
    [Header("References")]
    [SerializeField] protected AudioSource audioSource;
    [SerializeField] protected SpriteRenderer itemRenderer;
    [SerializeField] protected SOAudioEvent spawnSfx;
    [SerializeField] protected SOAudioEvent destroySfx;
    [SerializeField] protected OneShotParticle destroyParticle;

    protected Vector3 _baseScale;
    protected Match3Tile _currentTile;
    protected SOItemData _itemData;
    protected Sequence _movementSequence;
    protected Match3GridHandler _gridHandler;
    protected bool _beingDestroyed;
    
    public SOItemData ItemData => _itemData;
    public Match3Tile CurrentTile => _currentTile;
    
    public abstract bool IsSwappable { get; }
    public abstract bool IsMatchable { get; }
    public abstract bool IsMovable { get; }

    protected virtual void OnDestroy()
    {
        _movementSequence.Stop();
    }

    protected virtual void Awake()
    {
        _baseScale = transform.localScale;
    }
    
    protected virtual void OnGridDestroyed()
    {
        if (_beingDestroyed) return;
        _movementSequence.Stop();
        _beingDestroyed = true;
        _currentTile = null;
        transform.localScale = _baseScale;
        ObjectPooler.ReturnObjectToPool(gameObject);
    }

    public virtual void Initialize(SOItemData data, Match3GridHandler gridHandler)
    {
        _currentTile = null;
        _itemData = data;
        _beingDestroyed = false;
        
        _gridHandler = gridHandler;
        _gridHandler.GridDestroyed -= OnGridDestroyed;
        _gridHandler.GridDestroyed += OnGridDestroyed;

        transform.localScale = _baseScale;
        if (itemRenderer && _itemData)
        {
            itemRenderer.sprite = _itemData.Sprite;
            itemRenderer.color = _itemData.Color;
        }
        gameObject.name = _itemData ? $"{GetType().Name} ({_itemData.Label})" : GetType().Name;
    }

    public virtual void SetCurrentTile(Match3Tile match3Tile)
    {
        _currentTile = match3Tile;
    }
    
    public virtual void DestroyWithAnimation()
    {
        _beingDestroyed = true;
        
        var destroySequence = Sequence.Create();
        destroySequence.Group(Tween.Scale(transform, _baseScale * destroyScaleMultiplier, destroyDuration, Ease.OutBack));
        destroySequence.InsertCallback(destroyDuration * 0.5f, () =>
        {
            MobileHaptics.Vibrate(50);
            destroySfx?.PlayAtPoint(transform.position);
            
            if (destroyParticle)
            {
                var particleGo = ObjectPooler.GetObjectFromPool(destroyParticle.gameObject, transform.position, Quaternion.identity);
                var oneShotParticle = particleGo.GetComponent<OneShotParticle>();
                var mainModule = oneShotParticle.particle.main;
                var textureSheetModule = oneShotParticle.particle.textureSheetAnimation;
                mainModule.startColor = itemRenderer.color;
                textureSheetModule.SetSprite(0, itemRenderer.sprite);
                oneShotParticle.Play(transform.position);
            }
        });
        destroySequence.ChainCallback(() => { ObjectPooler.ReturnObjectToPool(gameObject); });
    }
    
    protected bool IsTouchingEndOfGrid()
    {
        if (!_currentTile) return false;

        return _currentTile.GridPosition.y <= 0;
    }
    
    public virtual void OnPoolGet()
    {
    }

    public virtual void OnPoolReturn()
    {
        if (_gridHandler) _gridHandler.GridDestroyed -= OnGridDestroyed;
    }

    public virtual void OnPoolRecycle()
    {
    }
    
}