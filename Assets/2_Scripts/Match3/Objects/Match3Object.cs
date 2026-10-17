using System.Collections.Generic;
using DNExtensions.Systems.AudioLibrary;
using DNExtensions.Systems.ObjectPooling;
using PrimeTween;
using UnityEngine;

public abstract class Match3Object : MonoBehaviour, IPoolable
{
    private static readonly int FillId = Shader.PropertyToID("_Fill");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    [Header("Settings")]
    [SerializeField] protected float swapDuration = 0.2f;
    [SerializeField] protected float destroyDuration = 0.2f;
    [SerializeField] protected float destroyScaleMultiplier = 1.2f;
    
    [Header("References")]
    [SerializeField] protected SpriteRenderer itemRenderer;
    [SerializeField, AudioLibraryID] protected string spawnSfx;
    [SerializeField, AudioLibraryID] protected string destroySfx;
    [SerializeField] protected PoolableParticleSystem destroyParticle;

    protected Vector3 _baseScale;
    protected Match3Tile _currentTile;
    protected SOItemData _itemData;
    protected Sequence _movementSequence;
    private readonly List<(SpriteRenderer renderer, Color color)> _greyedRenderers = new();
    private Tween _greyScaleTween;
    protected Match3GridHandler _gridHandler;
    protected bool _beingDestroyed;

    [Header("Fuse")]
    [SerializeField] private Material fuseRingMaterial;
    [SerializeField] private Sprite fuseRingSprite;
    [SerializeField, Min(0.1f)] private float fuseRingSize = 1.5f;
    [SerializeField, ColorUsage(true, true)] private Color fuseFullColor = new Color(2f, 1.1f, 0.3f, 1f);
    [SerializeField, ColorUsage(true, true)] private Color fuseLowColor = new Color(3f, 0.15f, 0.35f, 1f);
    [SerializeField] private Match3FuseBurst fuseBurstPrefab;

    private SpriteRenderer _fuseRing;
    private MaterialPropertyBlock _fuseBlock;
    private Color _fuseColor;
    private int _fuseTotal;
    private Sequence _fusePulse;
    private bool _fused;
    private int _fuseMoves;
    
    public SOItemData ItemData => _itemData;
    public Match3Tile CurrentTile => _currentTile;
    public Sprite Sprite => itemRenderer.sprite;
    public Color Color => itemRenderer.color;
    
    public abstract bool IsSwappable { get; }
    public abstract bool IsMatchable { get; }
    public abstract bool IsMovable { get; }
    public abstract bool IsAffectedBySpecialMatches { get; }
    public bool HasFuse => _fused;
    public int FuseMoves => _fuseMoves;

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
        ClearFuse();
        
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
    public virtual void SetHeld(bool held)
    {
        if (_beingDestroyed) return;
        
    }

    public virtual void SetCurrentTile(Match3Tile match3Tile)
    {
        _currentTile = match3Tile;
    }
    
    public virtual void DestroyWithAnimation()
    {
        DefuseOnDestroy();
        _beingDestroyed = true;
        
        var destroySequence = Sequence.Create();
        destroySequence.Group(Tween.Scale(transform, _baseScale * destroyScaleMultiplier, destroyDuration, Ease.OutBack));
        destroySequence.InsertCallback(destroyDuration * 0.5f, () =>
        {
            MobileHaptics.Play(Haptic.Pop);
            CameraManager.Instance?.ShakeCamera(0.2f);
            AudioLibrary.Play(destroySfx);

            SpawnDestroyParticle();
        });
        destroySequence.ChainCallback(() => { ObjectPooler.ReturnObjectToPool(gameObject); });
    }

    public void SetFuse(int moves)
    {
        _fused = true;
        _fuseMoves = Mathf.Max(1, moves);
        _fuseTotal = _fuseMoves;
        EnsureFuseRing();
        _fuseRing.gameObject.SetActive(true);
        RefreshFuse();

        _fusePulse.Stop();
        _fuseRing.transform.localScale = Vector3.zero;
        _fusePulse = Sequence.Create().Chain(Tween.Scale(_fuseRing.transform, Vector3.one * fuseRingSize, 0.45f, Ease.OutBack));
    }

    /// <summary>Burns one move off the fuse. True when it has just run out.</summary>
    public bool TickFuse()
    {
        if (!_fused || _fuseMoves <= 0) return false;

        _fuseMoves--;
        RefreshFuse();

        _fusePulse.Stop();
        float ringPunch = _fuseMoves <= 2 ? 1.25f : 1.1f;
        _fusePulse = Sequence.Create()
            .Chain(Tween.Scale(_fuseRing.transform, Vector3.one * fuseRingSize * ringPunch, 0.12f, Ease.OutBack))
            .Chain(Tween.Scale(_fuseRing.transform, Vector3.one * fuseRingSize, 0.18f, Ease.InQuad));

        return _fuseMoves <= 0;
    }

    /// <summary>A lit fuse goes out with the object that carries it: the ring bursts and the Defuse goal counts it.</summary>
    protected void DefuseOnDestroy()
    {
        if (!_fused) return;

        PlayFuseBurst(_fuseColor);
        ClearFuse();
        Match3GameManager.Instance?.NotifyFuseDefused(this);
    }

    /// <summary>The fuse ran out: the ring bursts in red.</summary>
    public void BlowFuse()
    {
        if (!_fuseRing) return;

        PlayFuseBurst(fuseLowColor * 1.5f);
        _fusePulse.Stop();
        _fuseRing.gameObject.SetActive(false);
        SpawnDestroyParticle();
    }

    private void PlayFuseBurst(Color color)
    {
        if (!_fuseRing || !_fuseRing.gameObject.activeInHierarchy) return;

        Match3FuseBurst.Play(fuseBurstPrefab, _fuseRing, color);
    }

    private void ClearFuse()
    {
        _fused = false;
        _fuseMoves = 0;
        _fusePulse.Stop();
        if (_fuseRing) _fuseRing.gameObject.SetActive(false);
    }

    private void SetFuseRing(float fill, Color color)
    {
        if (!_fuseRing) return;

        _fuseColor = color;
        _fuseBlock ??= new MaterialPropertyBlock();
        _fuseRing.GetPropertyBlock(_fuseBlock);
        _fuseBlock.SetFloat(FillId, fill);
        _fuseBlock.SetColor(ColorId, color);
        _fuseRing.SetPropertyBlock(_fuseBlock);
    }

    private void RefreshFuse()
    {
        float ratio = _fuseTotal > 0 ? (float)_fuseMoves / _fuseTotal : 0f;
        SetFuseRing(ratio, Color.Lerp(fuseLowColor, fuseFullColor, ratio));
    }

    private void EnsureFuseRing()
    {
        if (_fuseRing) return;

        var ringObject = new GameObject("FuseRing");
        ringObject.transform.SetParent(transform, false);
        ringObject.transform.localPosition = new Vector3(0f, 0f, -0.005f);
        _fuseRing = ringObject.AddComponent<SpriteRenderer>();
        _fuseRing.sprite = fuseRingSprite ? fuseRingSprite : Resources.Load<Sprite>("Sprites/ShapeSquare");
        _fuseRing.sharedMaterial = fuseRingMaterial ? fuseRingMaterial : Resources.Load<Material>("Materials/FuseRing");
        if (itemRenderer)
        {
            _fuseRing.sortingLayerID = itemRenderer.sortingLayerID;
            _fuseRing.sortingOrder = itemRenderer.sortingOrder + 2;
        }
    }

    protected virtual void SpawnDestroyParticle()
    {
        if (!destroyParticle) return;

        var particleGo = ObjectPooler.GetObjectFromPool(destroyParticle.gameObject, transform.position, Quaternion.identity);
        var poolableParticle = particleGo.GetComponent<PoolableParticleSystem>();
        poolableParticle.Play(transform.position);
    }

    protected bool IsAdjacentToAnyMatch(List<Match3Tile> matches)
    {
        if (_beingDestroyed || !_currentTile || !_currentTile.IsActive) return false;

        foreach (var match in matches)
        {
            if (_gridHandler.AreTilesNeighbours(match, _currentTile)) return true;
        }

        return false;
    }

    protected bool IsTouchingEndOfGrid()
    {
        if (!_currentTile) return false;

        return _currentTile.GridPosition.y <= 0;
    }
    
    public virtual void OnPoolGet()
    {
    }

    /// <summary>Fades every sprite on the object to its grey value, for the lose ending. Undone when the object goes back to the pool.</summary>
    public void GreyOut(float delay, float duration, float brightness)
    {
        foreach (var spriteRenderer in GetComponentsInChildren<SpriteRenderer>())
        {
            var color = spriteRenderer.color;
            _greyedRenderers.Add((spriteRenderer, color));
            float grey = (color.r * 0.299f + color.g * 0.587f + color.b * 0.114f) * brightness;
            var sequence = Sequence.Create();
            if (delay > 0f) sequence.ChainDelay(delay);
            sequence.Chain(Tween.Color(spriteRenderer, color * 1.6f, 0.05f))
                .Chain(Tween.Alpha(spriteRenderer, color.a * 0.2f, 0.04f))
                .Chain(Tween.Color(spriteRenderer, new Color(grey, grey, grey, color.a), duration, Ease.OutQuad));
        }

        _greyScaleTween = Tween.Scale(transform, transform.localScale * 0.82f, duration + 0.09f, Ease.OutBack, startDelay: delay + 0.05f);
    }

    private void RestoreGreyedRenderers()
    {
        foreach (var (spriteRenderer, color) in _greyedRenderers)
        {
            if (!spriteRenderer) continue;

            Tween.StopAll(spriteRenderer);
            spriteRenderer.color = color;
        }

        _greyedRenderers.Clear();
        _greyScaleTween.Stop();
    }

    public virtual void OnPoolReturn()
    {
        RestoreGreyedRenderers();
        ClearFuse();
        if (_gridHandler) _gridHandler.GridDestroyed -= OnGridDestroyed;
    }

    public virtual void OnPoolRecycle()
    {
    }
    
}