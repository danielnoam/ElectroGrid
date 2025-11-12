using System;
using DNExtensions;
using DNExtensions.ObjectPooling;
using PrimeTween;
using TMPro;
using UnityEngine;
using Object = System.Object;

public class Match3Object : MonoBehaviour, IPooledObject
{
    
    [Header("Settings")]
    [SerializeField] private float swapDuration = 0.2f;
    [SerializeField] private float destroyDuration = 0.2f;
    [SerializeField] private float destroyScaleMultiplier = 1.2f;
    [SerializeField] private float heldDuration = 0.2f;
    [SerializeField] private float heldScaleMultiplier = 0.8f;
    [SerializeField] private Color heldColor;
    
    [Header("References")]
    [SerializeField] private TextMeshPro itemLabel;
    [SerializeField] private SpriteRenderer itemRenderer;
    [SerializeField] private SOAudioEvent popSfx;
    [SerializeField] private SOAudioEvent swapSfx;
    [SerializeField] private SOAudioEvent spawnSfx;
    [SerializeField] private OneShotParticle popParticle;

    private Color _baseColor;
    private Vector3 _baseScale;
    private Match3Tile _currentMatch3Tile;
    private SOItemData _itemData;
    private bool _held;
    private bool _beingDestroyed;
    private Sequence _movementSequence;
    private Match3GridHandler _gridHandler;
    
    public SOItemData ItemData => _itemData;


    private void OnDestroy()
    {
        _movementSequence.Stop();
    }

    private void Awake()
    {
        _baseScale = transform.localScale;
        _baseColor = itemRenderer.color;
    }
    
    private void OnGridDestroyed()
    {
        if (_beingDestroyed) return;
        ObjectPooler.ReturnObjectToPool(gameObject);
    }


    public void Initialize(SOItemData data, Match3GridHandler gridHandler)
    {
        _gridHandler = gridHandler;
        _gridHandler.GridDestroyed -= OnGridDestroyed;
        _gridHandler.GridDestroyed += OnGridDestroyed;
        
        _held = false;
        _beingDestroyed = false;
        _itemData = data;

        itemLabel.text = _itemData.Label;
        itemRenderer.sprite = _itemData.Sprite;
        transform.localScale = _baseScale;
        gameObject.name = _itemData ? $"Item ({_itemData.Label})" : "Item (Empty)";
        
        UpdateVisuals();
    }


    public void SetCurrentTile(Match3Tile match3Tile)
    {
        bool spawning = !_currentMatch3Tile;
        _currentMatch3Tile = match3Tile;
        
        _movementSequence.Stop();
        
        var endPosition = new Vector3(_currentMatch3Tile.transform.localPosition.x, _currentMatch3Tile.transform.localPosition.y, transform.localPosition.z);
        
        _movementSequence = Sequence.Create()
            .Group(Tween.LocalPosition(transform, endPosition, swapDuration)
            .OnComplete(() =>
            {
                if (spawning)
                {
                    spawnSfx.Play(_currentMatch3Tile.AudioSource);
                }
                else
                {
                    swapSfx.Play(_currentMatch3Tile.AudioSource);
                }
            }));
    }
    
    public void SetHeld(bool held)
    {
        if (_beingDestroyed) return;
        
        _held = held;
        UpdateVisuals();
    }

    public void MatchFound()
    {
        _beingDestroyed = true;
        MobileHaptics.Vibrate(50);
        popSfx.Play(_currentMatch3Tile.AudioSource);
        var particleGo = ObjectPooler.GetObjectFromPool(popParticle.gameObject, transform.position, Quaternion.identity);
        var particle = particleGo.GetComponent<OneShotParticle>();
        particle.Play(transform.position);
        
        
        var popSequence = Sequence.Create();
        popSequence.Group(Tween.Scale(transform, _baseScale * destroyScaleMultiplier, destroyDuration, Ease.OutBack));
        popSequence.ChainCallback(() => ObjectPooler.ReturnObjectToPool(gameObject));
    }
    
    
    private void UpdateVisuals()
    {
        if (_beingDestroyed) return;
        
        itemRenderer.color = _held ? heldColor : _baseColor;
        var endScale = _held ? _baseScale * heldScaleMultiplier : _baseScale;
        if (transform.localScale != endScale) Tween.Scale(transform, endScale, heldDuration, Ease.OutBack);
    }
    
    

    public void OnPoolGet()
    {

    }

    public void OnPoolReturn()
    {
        if (_gridHandler) _gridHandler.GridDestroyed -= OnGridDestroyed;
        _movementSequence.Stop();
        _beingDestroyed = true;
        transform.localScale = _baseScale;
        _currentMatch3Tile = null;
    }

    public void OnPoolRecycle()
    {

    }
}