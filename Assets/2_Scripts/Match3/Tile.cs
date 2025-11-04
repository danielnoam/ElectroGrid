using System;
using UnityEngine;

public class Tile : MonoBehaviour
{
    [Header("Tile Properties")]
    [SerializeField] private Vector2Int gridPosition;
    [SerializeField] private bool isActive;
    
    [Header("References")]
    [SerializeField] private SpriteRenderer tileRenderer;
    [SerializeField] private Color selectedTileColor = new Color(0f, 1f, 0f, 1f);
    [SerializeField] private Color hoverTileColor = new Color(0f, 1f, 0f, 0.5f);
    [SerializeField] private Color activeTileColor = new Color(0f, 1f, 0f, 0.1f);
    [SerializeField] private Color inactiveTileColor = new Color(0.1f, 0.1f, 0.1f, 0.1f);
    
    private MatchObject _currentMatchObject;
    private bool _isSelected;
    private bool _isHovered;
    
    public Vector2Int GridPosition => gridPosition;
    public MatchObject CurrentMatchObject => _currentMatchObject;
    public bool CanSelect => isActive && _currentMatchObject && !_isSelected;
    public bool IsActive => isActive;
    public bool HasObject => isActive && _currentMatchObject;
    

    public void Initialize(Vector2Int position, bool active)
    {
        gameObject.name = $"Tile ({position.x},{position.y})";
        _isSelected = false;
        _isHovered = false;
        gridPosition = position;
        isActive = active;
        UpdateVisuals();
    }

    public void SetCurrentItem(MatchObject matchObject)
    {
        _isSelected = false;
        _isHovered = false;
        _currentMatchObject = matchObject;
    }

    private void UpdateVisuals()
    {
        if (!tileRenderer) return;

        if (isActive)
        {
            if (_isSelected)
            {
                tileRenderer.color = selectedTileColor;
            }
            else if (_isHovered)
            {
                tileRenderer.color = hoverTileColor;
            }
            else
            {
                tileRenderer.color = activeTileColor;
            }
        }
        else
        {

            tileRenderer.color = inactiveTileColor;
        }
    }
    
    
    public void SetHovered(bool hovered)
    {
        if (!CanSelect) return;
        
        _isHovered = hovered;
        UpdateVisuals();
    }
    
    
    public void SetSelected(bool selected)
    {
        _isSelected = selected;
        _isHovered = false;
        UpdateVisuals();
    }
    
    
    
}