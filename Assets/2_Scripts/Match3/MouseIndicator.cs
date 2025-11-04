using System;
using UnityEngine;

public class MouseIndicator : MonoBehaviour
{

    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private SpriteRenderer spriteRenderer;
    
    
    private Camera _camera;
    private bool _enabled;
    private Tile _tile;


    private void Awake()
    {
        _camera = Camera.main;
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
        enabled = true;
    }
    
    public void DisableIndicator()
    {
        lineRenderer.SetPosition(0, Vector3.zero);
        lineRenderer.SetPosition(1, Vector3.zero);
        _tile = null;
        spriteRenderer.sprite = null;
        enabled = false;
    }



    private void UpdateIndicatorPosition()
    {
        if (!_camera || !enabled || !_tile) return;
        
        
        Vector3 mousePosition = _camera.ScreenToWorldPoint(Input.mousePosition);
        mousePosition.z = spriteRenderer.transform.position.z;
        spriteRenderer.transform.position = mousePosition;
            
        lineRenderer.SetPosition(0, mousePosition);
        lineRenderer.SetPosition(1, _tile.transform.position);
    }
}
