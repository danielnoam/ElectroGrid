using System;
using DNExtensions;
using DNExtensions.ObjectPooling;
using PrimeTween;
using UnityEngine;

[SelectionBase]
public class Match3Tile : MonoBehaviour, IPooledObject
{
    [Header("Settings")]
    [SerializeField] private Color selectedTileColor = new Color(0f, 1f, 0f, 1f);
    [SerializeField] private Color hoverTileColor = new Color(0f, 1f, 0f, 0.5f);
    [SerializeField] private Color activeTileColor = new Color(0f, 1f, 0f, 0.1f);
    [SerializeField] private Color inactiveTileColor = new Color(0.1f, 0.1f, 0.1f, 0.1f);
    [SerializeField] private Sprite activeSprite;
    [SerializeField] private Sprite inactiveSprite;
    
    [Header("Punch Settings")]
    [SerializeField] private float punchScaleAmount = 1.1f;
    [SerializeField] private float punchDuration = 0.2f;
    
    [Header("Squash Settings")]
    [SerializeField] private float squashScaleAmount = 0.7f;
    [SerializeField] private float squashDuration = 0.15f;
    
    [Header("References")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private GameObject trashObject;

    [Separator]
    [SerializeField, ReadOnly] private Vector2Int gridPosition;
    [SerializeField, ReadOnly] private bool isActive;
    
    private Match3GameManager _match3GameManager;
    private Match3GridHandler _match3GridHandler;
    private Match3Object _currentMatch3Object;
    private bool _isSelected;
    private bool _isHovered;
    private Vector3 _baseScale;
    private Sequence _pulseSequence;
    
    public Vector2Int GridPosition => gridPosition;
    public Match3Object CurrentMatch3Object => _currentMatch3Object;
    public bool CanSelect => isActive && _currentMatch3Object && !_isSelected && _currentMatch3Object.IsSwappable;
    public bool IsActive => isActive;
    public bool HasObject => isActive && _currentMatch3Object;


    private void Awake()
    {
        _baseScale = transform.localScale;
    }

    public void Initialize(Match3GameManager match3GameManager, Vector2Int position, bool active)
    {
        _match3GameManager = match3GameManager;
        _match3GridHandler = _match3GameManager.GridHandler;
        _match3GridHandler.GridDestroyed -= OnGridDestroyed;
        _match3GridHandler.GridDestroyed += OnGridDestroyed;
        
        transform.localScale = _baseScale;
        gameObject.name = $"Tile ({position.x},{position.y})";
        gridPosition = position;
        _isSelected = false;
        _isHovered = false;
        isActive = active;
        trashObject.SetActive(false);
        
        UpdateVisuals();
    }
    
    public void InitializeAsTrash(Match3GameManager match3GameManager, Vector2Int position)
    {
        _match3GameManager = match3GameManager;
        _match3GridHandler = _match3GameManager.GridHandler;
        _match3GridHandler.GridDestroyed -= OnGridDestroyed;
        _match3GridHandler.GridDestroyed += OnGridDestroyed;
        
        transform.localScale = _baseScale;
        gameObject.name = $"Tile ({position.x},{position.y})";
        gridPosition = position;
        _isSelected = false;
        _isHovered = false;
        isActive = false;
        trashObject.SetActive(true);
        
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
        if (!spriteRenderer) return;

        if (isActive)
        {
            if (_isSelected)
            {
                spriteRenderer.color = selectedTileColor;
            }
            else if (_isHovered)
            {
                spriteRenderer.color = hoverTileColor;
            }
            else
            {
                spriteRenderer.color = activeTileColor;
            }
            
            spriteRenderer.sprite = activeSprite;
        }
        else
        {
            spriteRenderer.color = inactiveTileColor;
            spriteRenderer.sprite = inactiveSprite;
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
    
    public void SquashTile()
    {
        if (!isActive) return;
        
        var duration = 0.15f;
        _pulseSequence = Sequence.Create();
        _pulseSequence.Group(Tween.Scale(transform, _baseScale * squashScaleAmount, squashDuration * 0.3f, Ease.Linear));
        _pulseSequence.Chain(Tween.Scale(transform, _baseScale, squashDuration * 0.7f, Ease.OutSine));
    }
    
    public void PunchTile()
    {
        if (!isActive) return;
        
        _pulseSequence = Sequence.Create();
        _pulseSequence.Group(Tween.Scale(transform, _baseScale * punchScaleAmount, punchDuration/2, Ease.OutElastic));
        _pulseSequence.Chain(Tween.Scale(transform, _baseScale, punchDuration/2, Ease.InQuad));
    }

    public void OnPoolGet()
    {
    }

    public void OnPoolReturn()
    {
        _pulseSequence.Stop();
        if (_match3GridHandler) _match3GridHandler.GridDestroyed -= OnGridDestroyed;
    }

    public void OnPoolRecycle()
    {
    }
}