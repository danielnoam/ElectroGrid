using PrimeTween;
using TMPro;
using UnityEngine;

public class Match3Object : MonoBehaviour
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

    private Color _baseColor;
    private Vector3 _baseScale;
    private Match3Tile _currentMatch3Tile;
    private SOItemData _itemData;
    private bool _held;
    private bool _beingDestroyed;
    
    public SOItemData ItemData => _itemData;
    

    public void Initialize(SOItemData data)
    {
        _held = false;
        _itemData = data;
        _baseScale = transform.localScale;
        _baseColor = itemRenderer.color;
        UpdateVisuals();
    }

    public void SetCurrentTile(Match3Tile match3Tile)
    {
        _currentMatch3Tile = match3Tile;
        
        var endPosition = new Vector3(_currentMatch3Tile.transform.localPosition.x,_currentMatch3Tile.transform.localPosition.y,transform.localPosition.z);
        Tween.LocalPosition(transform, endPosition, swapDuration);
    }
    
    public void SetHeld(bool held)
    {
        _held = held;
        UpdateVisuals();
    }

    public void MatchFound()
    {
        _beingDestroyed = true;
        MobileHaptics.Vibrate(50);
        Tween.Scale(transform, _baseScale * destroyScaleMultiplier, destroyDuration, Ease.OutBack);
        Destroy(gameObject, destroyDuration);
    }
    
    
    private void UpdateVisuals()
    {
        if (_beingDestroyed) return;
        
        itemLabel.text = _itemData.Label;
        itemRenderer.sprite = _itemData.Sprite;
        itemRenderer.color = _held ? heldColor : _baseColor;
        gameObject.name = _itemData ? $"Item ({_itemData.Label})" : "Item (Empty)";
        
        var endScale = _held ? _baseScale * heldScaleMultiplier : _baseScale;
        if (transform.localScale != endScale) Tween.Scale(transform, endScale, heldDuration, Ease.OutBack);
    }
}