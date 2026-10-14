using DNExtensions.Systems.ObjectPooling;
using PrimeTween;
using UnityEngine;

[SelectionBase]
public class Match3MatchableObject : Match3SwappableObject
{
    private static readonly int EmissionMask = Shader.PropertyToID("_Emission_Mask");
    private static readonly int FillId = Shader.PropertyToID("_Fill");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

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

    public override bool IsMatchable => true;
    public bool HasFuse => _fused;
    public int FuseMoves => _fuseMoves;

    public override void Initialize(SOItemData data, Match3GridHandler gridHandler)
    {
        base.Initialize(data, gridHandler);

        ClearFuse();
        if (data) UpdateEmissionMask(data.EmissionMask);
    }

    public void SetFuse(int moves)
    {
        _fused = true;
        _fuseMoves = Mathf.Max(1, moves);
        _fuseTotal = _fuseMoves;
        EnsureFuseRing();
        _fuseRing.gameObject.SetActive(true);
        _fuseRing.transform.localScale = Vector3.one * fuseRingSize;
        RefreshFuse();
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

    public override void DestroyWithAnimation()
    {
        if (_fused)
        {
            PlayFuseBurst(_fuseColor);
            ClearFuse();
            Match3GameManager.Instance?.NotifyFuseDefused(this);
        }

        base.DestroyWithAnimation();
    }

    public override void OnPoolReturn()
    {
        base.OnPoolReturn();
        ClearFuse();
    }

    protected override void OnMatched()
    {
        Match3EffectManager.Instance?.CreateMatchBackgroundParticle(transform.position, _itemData);
    }

    protected override void SpawnDestroyParticle()
    {
        if (!destroyParticle) return;

        var particleGo = ObjectPooler.GetObjectFromPool(destroyParticle.gameObject, transform.position, Quaternion.identity);
        var poolableParticle = particleGo.GetComponent<PoolableParticleSystem>();
        var mainModule = poolableParticle.particle.main;
        var textureSheetModule = poolableParticle.particle.textureSheetAnimation;
        mainModule.startColor = itemRenderer.color;
        textureSheetModule.SetSprite(0, itemRenderer.sprite);
        poolableParticle.Play(transform.position);
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

    private void UpdateEmissionMask(Texture2D emissionMask)
    {
        itemRenderer.material.SetTexture(EmissionMask, emissionMask);
    }
}
