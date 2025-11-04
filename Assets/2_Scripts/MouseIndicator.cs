using System;
using UnityEngine;

public class MouseIndicator : MonoBehaviour
{

    
    [SerializeField] private SpriteRenderer spriteRenderer;
    private Camera _camera;


    private void Awake()
    {
        _camera = Camera.main;
    }

    public void SetSprite(Sprite sprite)
    {
        spriteRenderer.sprite = sprite;
    }

    private void Update()
    {
        if (_camera)
        {
            Vector3 mousePosition = _camera.ScreenToWorldPoint(Input.mousePosition);
            mousePosition.z = transform.position.z;
            transform.position = mousePosition;
        }
    }
}
