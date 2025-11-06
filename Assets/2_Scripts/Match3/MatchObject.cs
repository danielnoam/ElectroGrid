using PrimeTween;
using TMPro;
using UnityEngine;

public class MatchObject : MonoBehaviour
{
    
    [Header("Settings")]
    [SerializeField] private Color heldColor;
    [SerializeField] private float heldScaleMultiplier = 0.8f;
    
    [Header("References")]
    [SerializeField] private TextMeshPro itemLabel;
    [SerializeField] private SpriteRenderer itemRenderer;

    private Color _baseColor;
    private Vector3 _baseScale;
    private Tile _currentTile;
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

    public void SetCurrentTile(Tile tile)
    {
        _currentTile = tile;
        
        var endPosition = new Vector3(_currentTile.transform.position.x,_currentTile.transform.position.y,transform.position.z);
        Tween.LocalPosition(transform, endPosition, 0.2f);
    }
    
    public void SetHeld(bool held)
    {
        _held = held;
        UpdateVisuals();
    }

    public void MatchFound()
    {
        _beingDestroyed = true;
        GameManager.Instance?.AddPopcorn();
        Tween.Scale(transform, _baseScale * 1.2f, 0.1f, Ease.OutBack);
        Destroy(gameObject, 0.1f);
    }
    
    
    private void UpdateVisuals()
    {
        if (_beingDestroyed) return;
        
        itemLabel.text = _itemData.Label;
        itemRenderer.sprite = _itemData.Sprite;
        itemRenderer.color = _held ? heldColor : _baseColor;
        gameObject.name = _itemData ? $"Item ({_itemData.Label})" : "Item (Empty)";
        
        var endScale = _held ? _baseScale * heldScaleMultiplier : _baseScale;
        if (transform.localScale != endScale) Tween.Scale(transform, endScale, 0.2f, Ease.OutBack);
    }
}