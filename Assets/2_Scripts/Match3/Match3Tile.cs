using System;
using System.Collections.Generic;
using DNExtensions;
using DNExtensions.ObjectPooling;
using PrimeTween;
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
    [SerializeField] private SpriteRenderer layerRenderer;
    [SerializeField] private OneShotParticle layerClearedParticle;
    [SerializeField] private SOAudioEvent layerClearedSfx;

    
    [Separator]
    [SerializeField, ReadOnly] private Vector2Int gridPosition;
    [SerializeField, ReadOnly] private bool isActive;
    [SerializeField, ReadOnly] private int layerHealth;
    
    private Match3GameManager _match3GameManager;
    private Match3GridHandler _match3GridHandler;
    private Match3Object _currentMatch3Object;
    private bool _isSelected;
    private bool _isHovered;
    private Vector3 _baseLayerScale;
    
    
    
    public Vector2Int GridPosition => gridPosition;
    public Match3Object CurrentMatch3Object => _currentMatch3Object;
    public AudioSource AudioSource => audioSource;
    public bool CanSelect => isActive && _currentMatch3Object && !_isSelected && !HasLayer;
    public bool IsActive => isActive;
    public bool HasObject => isActive && _currentMatch3Object;
    public bool HasLayer => layerHealth > 0;


    private void Awake()
    {
        _baseLayerScale = layerRenderer.transform.localScale;
    }

    public void Initialize(Match3GameManager match3GameManager, Vector2Int position, bool active, int layerHealth)
    {

        _match3GameManager = match3GameManager;
        _match3GameManager.MatchesMade -= OnMatchesMade;
        _match3GameManager.MatchesMade += OnMatchesMade;
        
        _match3GridHandler = _match3GameManager.GridHandler;
        _match3GridHandler.GridDestroyed -= OnGridDestroyed;
        _match3GridHandler.GridDestroyed += OnGridDestroyed;
        
        gameObject.name = $"Tile ({position.x},{position.y})";
        layerRenderer.transform.localScale = Vector3.zero;
        this.layerHealth = layerHealth;
        gridPosition = position;
        _isSelected = false;
        _isHovered = false;
        
        if (HasLayer)
        {
            Tween.Scale(layerRenderer.transform, _baseLayerScale, 0.5f, Ease.OutBack, startDelay:0.5f);
        }
        
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


    private void LowerLayerHealth()
    {
        if (!HasLayer) return;
        
        layerHealth--;
        if (layerHealth <= 0)
        {
            var effectSequence = Sequence.Create()
                .Group(Tween.Scale(layerRenderer.transform, _baseLayerScale,_baseLayerScale * 1.3f, 0.1f, Ease.OutBack))
                .ChainCallback(() =>
                {
                    var breakableParticleGo = ObjectPooler.GetObjectFromPool(layerClearedParticle.gameObject, transform.position);
                    breakableParticleGo.GetComponent<OneShotParticle>().Play();
                    layerClearedSfx?.Play(audioSource);
                    layerRenderer.transform.localScale = Vector3.zero;
                    _match3GameManager?.NotifyLayerBroke(this);
                });
        }
    }
    
    private void OnMatchesMade(List<Match3Tile> matches)
    {
        if (!HasLayer || !IsActive || !HasObject) return;
        
        foreach (var match in matches)
        {
            if (_match3GridHandler.AreTilesNeighbours(match, this))
            {
               LowerLayerHealth();
            }
        }
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