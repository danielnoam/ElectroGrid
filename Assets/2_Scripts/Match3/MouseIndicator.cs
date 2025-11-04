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
        
        _tile = tile;
        spriteRenderer.sprite = _tile.CurrentMatchObject.ItemData.Sprite;
        Tween.Scale(spriteRenderer.transform, _baseScale, 0.2f, Ease.OutBack);
        
        enabled = true;
    }
    
    public void DisableIndicator()
    {
        lineRenderer.SetPosition(0, Vector3.zero);
        lineRenderer.SetPosition(1, Vector3.zero);
        Tween.Scale(spriteRenderer.transform, Vector3.zero, 0.2f, Ease.OutBack);
        _tile = null;
        spriteRenderer.sprite = null;
        
        
        enabled = false;
    }



    private void UpdateIndicatorPosition()
    {
        if (!_camera || !enabled || !_tile) return;
        
        
        Vector3 mousePosition = _camera.ScreenToWorldPoint(inputReader.MousePosition);
        mousePosition.z = spriteRenderer.transform.position.z;
        spriteRenderer.transform.position = mousePosition;
            
        lineRenderer.SetPosition(0, mousePosition);
        lineRenderer.SetPosition(1, _tile.transform.position);
    }
}
