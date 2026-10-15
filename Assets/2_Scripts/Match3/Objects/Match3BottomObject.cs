using DNExtensions.Utilities.Button;
using DNExtensions.Systems.ObjectPooling;
using DNExtensions.Systems.AudioLibrary;
using PrimeTween;
using UnityEngine;

[SelectionBase]
public class Match3BottomObject : Match3Object
{
    private static readonly int EmissionMaskId = Shader.PropertyToID("_Emission_Mask");

    [Header("Plug")]
    [SerializeField] private Texture2D emissionMask;
    [Tooltip("Seconds between two plug-in dips")]
    [SerializeField, Min(0.1f)] private float bobPeriod = 1.6f;
    [Tooltip("How far the plug dips down, in world units")]
    [SerializeField, Min(0f)] private float bobDistance = 0.08f;
    [Tooltip("Share of the period the dip takes, the rest of the time it rests")]
    [SerializeField, Range(0.05f, 1f)] private float bobShare = 0.18f;
    [SerializeField, Min(0f)] private float bobRandomOffset = 1.6f;
    [SerializeField] private ParticleSystem pinSparks;
    [SerializeField, Min(0)] private int sparksPerDip = 3;

    private Match3GameManager _gameManager;
    private float _bobTimeOffset;
    private Vector3 _spriteBasePosition;
    private bool _sparkedThisDip;
    
    public override bool IsSwappable => false;
    public override bool IsMatchable => false;
    public override bool IsMovable => true;
    public override bool IsAffectedBySpecialMatches => false;
    

    protected override void Awake()
    {
        base.Awake();

        if (!itemRenderer) return;
        _spriteBasePosition = itemRenderer.transform.localPosition;
        if (emissionMask) itemRenderer.material.SetTexture(EmissionMaskId, emissionMask);
    }

    private void Update()
    {
        if (!_currentTile || _beingDestroyed || !itemRenderer) return;

        float phase = Mathf.Repeat((Time.time + _bobTimeOffset) / bobPeriod, 1f);
        float dip = phase < bobShare ? Mathf.Sin(phase / bobShare * Mathf.PI) : 0f;
        itemRenderer.transform.localPosition = _spriteBasePosition + Vector3.down * (bobDistance * dip);

        bool atBottom = phase >= bobShare * 0.5f && phase < bobShare;
        if (atBottom && !_sparkedThisDip)
        {
            _sparkedThisDip = true;
            if (pinSparks && sparksPerDip > 0) pinSparks.Emit(sparksPerDip);
        }
        else if (!atBottom)
        {
            _sparkedThisDip = false;
        }
    }

    public override void Initialize(SOItemData data, Match3GridHandler gridHandler)
    {
        base.Initialize(data, gridHandler);
        
        _bobTimeOffset = Random.Range(0f, bobRandomOffset);
        transform.localRotation = Quaternion.identity;
        if (itemRenderer) itemRenderer.transform.localPosition = _spriteBasePosition;
        _gameManager = Match3GameManager.Instance;
    }

    public override void SetCurrentTile(Match3Tile match3Tile)
    {
        bool spawning = !_currentTile;
        _currentTile = match3Tile;
        
        _movementSequence.Stop();
        _movementSequence = Sequence.Create();
        
        var endPosition = new Vector3(_currentTile.transform.localPosition.x, _currentTile.transform.localPosition.y, transform.localPosition.z);
        
        if (spawning)
        {
            transform.localPosition = endPosition;
            transform.localScale = Vector3.zero;
            _movementSequence.Group(Tween.Scale(transform, _baseScale, 0.5f, Ease.OutBack, startDelay: 0.5f));
            _movementSequence.ChainCallback(() => { AudioLibrary.Play(spawnSfx); });
        }
        else
        {
            _movementSequence.Group(Tween.LocalPosition(transform, endPosition, swapDuration, Ease.OutQuad));
        }
        
        CheckIfReachedBottom();
    }
    
    public override void DestroyWithAnimation()
    {
        DefuseOnDestroy();
        _beingDestroyed = true;

        var bellowCellPosition = _gridHandler.Grid.GetCellWorldPosition(_currentTile.GridPosition.x, -1);
        var endPosition = new Vector3(bellowCellPosition.x, bellowCellPosition.y, transform.localPosition.z);
        AudioLibrary.Play(destroySfx);
        
        _movementSequence.Stop();
        _movementSequence = Sequence.Create();
        var duration = swapDuration * 3f;
        
        _movementSequence.Group(Tween.LocalPosition(transform, endPosition, duration, Ease.OutQuart));
        _movementSequence.Group(Tween.Scale(transform, _baseScale * 0.75f, duration/1.75f, Ease.OutQuart, startDelay: duration/3));
        _movementSequence.InsertCallback(duration * 0.5f, () => {
            
            MobileHaptics.Vibrate(50);
            SpawnDestroyParticle();
        });

        int column = _currentTile.GridPosition.x;
        _movementSequence.ChainCallback(() =>
        {
            var socket = _gridHandler ? _gridHandler.GetTile(new Vector2Int(column, -1)) : null;
            if (socket) socket.PlugIn();
            CloseCatcherIfColumnDone(column);
            ObjectPooler.ReturnObjectToPool(gameObject);
        });
    }

    private void CloseCatcherIfColumnDone(int column)
    {
        if (!_gridHandler || _gridHandler.Grid == null) return;
        if (_gameManager && _gameManager.IsSurvival) return;

        for (int y = 0; y < _gridHandler.Grid.Height; y++)
        {
            var tile = _gridHandler.GetTile(new Vector2Int(column, y));
            if (tile && tile.CurrentMatch3Object is Match3BottomObject) return;
        }

        var catcher = _gridHandler.GetTile(new Vector2Int(column, -1));
        if (catcher) catcher.CloseTrash();
    }

    private void CheckIfReachedBottom()
    {
        if (IsTouchingEndOfGrid())
        {
            ReachedBottom();
        }
    }

    [Button(ButtonPlayMode.OnlyWhenPlaying)]
    private void ReachedBottom()
    {
        _currentTile?.SetCurrentItem(null);
        _gameManager?.NotifyBottomObjectReached(this);
        DestroyWithAnimation();
    }
}