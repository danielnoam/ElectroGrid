using System;
using PrimeTween;
using UnityEngine;

public class MouseIndicator : MonoBehaviour
{

    [SerializeField] private Match3InputReader inputReader;
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private Vector3 _baseScale;
    private Tile _tile;
    private Camera _camera;
    private bool _enabled;
    private Sequence _sequence;



    private void Awake()
    {
        _camera = Camera.main;
        _baseScale = spriteRenderer.transform.localScale;
        spriteRenderer.transform.localScale = Vector3.zero;
        lineRenderer.positionCount = 2;
    }
    
    private void Update()
    {
        UpdateIndicatorPosition();
    }
    

    public void EnableIndicator(Tile tile)
    {
        if (!tile) return;
        
        _sequence.Stop();
        
        _tile = tile;
        spriteRenderer.sprite = _tile.CurrentMatchObject.ItemData.Sprite;
        if (spriteRenderer.transform.localScale != _baseScale) Tween.Scale(spriteRenderer.transform, _baseScale, 0.2f, Ease.OutBack);
        
        _enabled = true;
    }
    
    public void DisableIndicator(bool animateReturn = false)
    {
        if (!_enabled) return;
        
        _sequence.Stop();
        
        _enabled = false;
        lineRenderer.SetPosition(0, Vector3.zero);
        lineRenderer.SetPosition(1, Vector3.zero);
        
        if (animateReturn)
        {
            var endPosition = _tile ? _tile.transform.position : spriteRenderer.transform.position;
            endPosition.z = spriteRenderer.transform.position.z;
            
            _sequence = Sequence.Create()
                .Group(Tween.Scale(spriteRenderer.transform, Vector3.zero, 0.3f, Ease.InOutSine))
                .Group(Tween.Position(spriteRenderer.transform, endPosition, 0.3f, Ease.InOutSine))
                .OnComplete(() =>
                {

                    _tile = null;
                    spriteRenderer.sprite = null;
                });
        }
        else
        {
            if (spriteRenderer.transform.localScale != Vector3.zero) Tween.Scale(spriteRenderer.transform, Vector3.zero, 0.2f, Ease.OutBack);
            _tile = null;
            spriteRenderer.sprite = null;
        }

    }



    private void UpdateIndicatorPosition()
    {
        if (!_camera || !_enabled || !_tile) return;
        
        
        Vector3 mousePosition = _camera.ScreenToWorldPoint(inputReader.MousePosition);
        mousePosition.z = spriteRenderer.transform.position.z;
        spriteRenderer.transform.position = mousePosition;
            
        lineRenderer.SetPosition(0, mousePosition);
        lineRenderer.SetPosition(1, _tile.transform.position);
    }
}
