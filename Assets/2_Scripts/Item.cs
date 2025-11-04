using TMPro;
using UnityEngine;

public class Item : MonoBehaviour
{
    [Header("Item Data")]
    [SerializeField] private SOItemData itemData;
    
    [Header("References")]
    [SerializeField] private TextMeshPro itemLabel;
    [SerializeField] private SpriteRenderer itemRenderer;
    
    private Tile _currentTile;
    
    public SOItemData ItemData => itemData;
    public Tile CurrentTile => _currentTile;
    public Vector2Int GridPosition => _currentTile != null ? _currentTile.GridPosition : Vector2Int.zero;

    public void Initialize(SOItemData data)
    {
        itemData = data;
        UpdateVisuals();
    }

    public void SetCurrentTile(Tile tile)
    {
        _currentTile = tile;
        
        if (_currentTile != null)
        {
            transform.position = new Vector3(_currentTile.transform.position.x,_currentTile.transform.position.y,transform.position.z);
        }
    }
    
    
    private void UpdateVisuals()
    {
        if (itemData == null) return;

        if (itemLabel != null)
        {
            itemLabel.text = itemData.Label;
        }

        if (itemRenderer != null && itemData.Sprite != null)
        {
            itemRenderer.sprite = itemData.Sprite;
        }

        gameObject.name = itemData != null ? $"Item ({itemData.Label})" : "Item (Empty)";
    }
}