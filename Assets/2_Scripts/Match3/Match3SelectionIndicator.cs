using System;
using DNExtensions;
using PrimeTween;
using UnityEngine;

public class Match3SelectionIndicator : MonoBehaviour
{

    [Header("References")]
    [SerializeField] private Match3PlayHandler match3PlayHandler;
    [SerializeField] private Match3GameManager match3GameManager;
    [SerializeField] private Match3InputReader inputReader;
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private SpriteRenderer spriteRenderer;

    
    [Separator]
    [SerializeField, ReadOnly] private Tile hoveredTile;
    
    private bool _enabled;
    private Vector3 _baseSpriteScale;
    private Tile _pressedTile;
    private Camera _camera;
    private Sequence _animationSequence;



    private void Awake()
    {
        _camera = Camera.main;
        _baseSpriteScale = spriteRenderer.transform.localScale;
        spriteRenderer.transform.localScale = Vector3.zero;
        lineRenderer.positionCount = 2;
    }
    
    private void Update()
    {
        UpdateIndicatorPosition();
        UpdateHoveredTile();
    }
    

    public void EnableIndicator(Tile tile)
    {
        if (!tile) return;
        
        _animationSequence.Stop();
        
        _pressedTile = tile;
        spriteRenderer.sprite = _pressedTile.CurrentMatchObject.ItemData.Sprite;
        if (spriteRenderer.transform.localScale != _baseSpriteScale) Tween.Scale(spriteRenderer.transform, _baseSpriteScale, 0.2f, Ease.OutBack);
        
        _enabled = true;
    }
    
    public void DisableIndicator(bool animateReturn = false)
    {
        if (!_enabled) return;
        
        _animationSequence.Stop();
        
        _enabled = false;
        lineRenderer.SetPosition(0, Vector3.zero);
        lineRenderer.SetPosition(1, Vector3.zero);
        
        if (animateReturn)
        {
            var endPosition = _pressedTile ? _pressedTile.transform.position : spriteRenderer.transform.position;
            endPosition.z = spriteRenderer.transform.position.z;
            
            _animationSequence = Sequence.Create()
                .Group(Tween.Scale(spriteRenderer.transform, Vector3.zero, 0.3f, Ease.InOutSine))
                .Group(Tween.Position(spriteRenderer.transform, endPosition, 0.3f, Ease.InOutSine))
                .OnComplete(() =>
                {

                    _pressedTile = null;
                    spriteRenderer.sprite = null;
                });
        }
        else
        {
            if (spriteRenderer.transform.localScale != Vector3.zero) Tween.Scale(spriteRenderer.transform, Vector3.zero, 0.2f, Ease.OutBack);
            _pressedTile = null;
            spriteRenderer.sprite = null;
        }

    }
    
    public void ResetHoveredTile()
    {
        hoveredTile?.SetHovered(false);
        hoveredTile = null;
    }
    
    private void UpdateIndicatorPosition()
    {
        if (!_camera || !_enabled || !_pressedTile) return;
        
        
        Vector3 mousePosition = _camera.ScreenToWorldPoint(inputReader.MousePosition);
        mousePosition.z = spriteRenderer.transform.position.z;
        spriteRenderer.transform.position = mousePosition;
            
        lineRenderer.SetPosition(0, mousePosition);
        lineRenderer.SetPosition(1, _pressedTile.transform.position);
    }
    
    private void UpdateHoveredTile()
    {
        if (!_camera || !match3PlayHandler.CanInteract || inputReader.IsCurrentDeviceTouchscreen) return;
        
        Vector2 mousePos = inputReader.MousePosition;
        Vector2 worldPos = _camera.ScreenToWorldPoint(mousePos);
    
        RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);
        
        Tile newHoveredTile = null;
        if (hit.collider)
        {
            newHoveredTile = hit.collider.GetComponent<Tile>();
        }

        if (newHoveredTile != hoveredTile)
        {
            hoveredTile?.SetHovered(false);
            newHoveredTile?.SetHovered(true);
            hoveredTile = newHoveredTile;
        }
    }
    

}
