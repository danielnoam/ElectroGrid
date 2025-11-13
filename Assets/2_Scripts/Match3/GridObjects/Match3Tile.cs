using DNExtensions;
using DNExtensions.ObjectPooling;
using UnityEngine;

public class Match3Tile : MonoBehaviour, IPooledObject
{
    [Header("Settings")]
    [SerializeField] private Color selectedTileColor = new Color(0f, 1f, 0f, 1f);
    [SerializeField] private Color hoverTileColor = new Color(0f, 1f, 0f, 0.5f);
    [SerializeField] private Color activeTileColor = new Color(0f, 1f, 0f, 0.1f);
    [SerializeField] private Color inactiveTileColor = new Color(0.1f, 0.1f, 0.1f, 0.1f);
    
    [Header("References")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private SpriteRenderer tileRenderer;

    [Separator]
    [SerializeField, ReadOnly] private Vector2Int gridPosition;
    [SerializeField, ReadOnly] private bool isActive;
    
    private Match3GameManager _match3GameManager;
    private Match3GridHandler _match3GridHandler;
    private Match3Object _currentMatch3Object;
    private bool _isSelected;
    private bool _isHovered;
    
    public Vector2Int GridPosition => gridPosition;
    public Match3Object CurrentMatch3Object => _currentMatch3Object;
    public bool CanSelect => isActive && _currentMatch3Object && !_isSelected && _currentMatch3Object.IsSwappable;
    public bool IsActive => isActive;
    public bool HasObject => isActive && _currentMatch3Object;

    public void Initialize(Match3GameManager match3GameManager, Vector2Int position, bool active)
    {
        _match3GameManager = match3GameManager;
        _match3GridHandler = _match3GameManager.GridHandler;
        _match3GridHandler.GridDestroyed -= OnGridDestroyed;
        _match3GridHandler.GridDestroyed += OnGridDestroyed;
        
        gameObject.name = $"Tile ({position.x},{position.y})";
        gridPosition = position;
        _isSelected = false;
        _isHovered = false;
        isActive = active;
        
        UpdateVisuals();
    }

    private void OnGridDestroyed()
    {
        ObjectPooler.ReturnObjectToPool(gameObject);
    }

    public void SetCurrentItem(Match3Object match3Object)
    {
        _isSelected = false;
        _isHovered = false;
        _currentMatch3Object = match3Object;
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

    public void OnPoolGet()
    {
    }

    public void OnPoolReturn()
    {
        if (_match3GridHandler) _match3GridHandler.GridDestroyed -= OnGridDestroyed;
    }

    public void OnPoolRecycle()
    {
    }
}