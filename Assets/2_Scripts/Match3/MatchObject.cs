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
    
    public SOItemData ItemData => _itemData;
    public Tile CurrentTile => _currentTile;
    public Vector2Int GridPosition => _currentTile ? _currentTile.GridPosition : Vector2Int.zero;
    

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
        
        if (_currentTile)
        {
            transform.position = new Vector3(_currentTile.transform.position.x,_currentTile.transform.position.y,transform.position.z);
        }
    }
    
    public void SetHeld(bool held)
    {
        _held = held;
        UpdateVisuals();
    }
    
    
    private void UpdateVisuals()
    {
        itemLabel.text = _itemData.Label;
        itemRenderer.sprite = _itemData.Sprite;
        itemRenderer.color = _held ? heldColor : _baseColor;
        transform.localScale = _held ? _baseScale * heldScaleMultiplier : _baseScale;
        gameObject.name = _itemData ? $"Item ({_itemData.Label})" : "Item (Empty)";
        
    }
}