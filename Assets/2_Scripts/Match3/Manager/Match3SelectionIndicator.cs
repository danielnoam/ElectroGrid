using DNExtensions;
using PrimeTween;
using UnityEngine;

public class Match3SelectionIndicator : MonoBehaviour
{

    private static readonly int EmissionMask = Shader.PropertyToID("_Emission_Mask");
    
    [Header("References")]
    [SerializeField] private Match3PlayHandler match3PlayHandler;
    [SerializeField] private Match3GameManager match3GameManager;
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private SpriteRenderer spriteRenderer;

    
    [Separator]
    [SerializeField, ReadOnly] private Match3Tile hoveredMatch3Tile;
    
    private bool _enabled;
    private Vector3 _baseSpriteScale;
    private TouchInputReader _inputReader;
    private Match3Tile _pressedMatch3Tile;
    private Camera _camera;
    private Sequence _animationSequence;



    private void Awake()
    {
        _camera = Camera.main;
        _baseSpriteScale = spriteRenderer.transform.localScale;
        spriteRenderer.transform.localScale = Vector3.zero;
        lineRenderer.positionCount = 2;
    }
    
    private void Start()
    {
        if (!_inputReader)
        {
            _inputReader = TouchInputReader.Instance;
        }
    }
    
    private void Update()
    {
        UpdateIndicatorPosition();
        UpdateHoveredTile();
    }
    

    public void EnableIndicator(Match3Tile match3Tile)
    {
        if (!match3Tile) return;
        
        _animationSequence.Stop();
        
        _pressedMatch3Tile = match3Tile;
        spriteRenderer.sprite = _pressedMatch3Tile.CurrentMatch3Object.Sprite;
        if (_pressedMatch3Tile.CurrentMatch3Object.ItemData)
        {
            spriteRenderer.color = _pressedMatch3Tile.CurrentMatch3Object.ItemData.Color;
            UpdateEmissionMask(_pressedMatch3Tile.CurrentMatch3Object.ItemData.EmissionMask);
        }
        else
        {
            spriteRenderer.color = Color.white;
            UpdateEmissionMask(null);
        }
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
            var endPosition = _pressedMatch3Tile ? _pressedMatch3Tile.transform.position : spriteRenderer.transform.position;
            endPosition.z = spriteRenderer.transform.position.z;
            
            _animationSequence = Sequence.Create()
                .Group(Tween.Scale(spriteRenderer.transform, Vector3.zero, 0.3f, Ease.InOutSine))
                .Group(Tween.Position(spriteRenderer.transform, endPosition, 0.3f, Ease.InOutSine))
                .OnComplete(() =>
                {

                    _pressedMatch3Tile = null;
                    spriteRenderer.sprite = null;
                });
        }
        else
        {
            if (spriteRenderer.transform.localScale != Vector3.zero) Tween.Scale(spriteRenderer.transform, Vector3.zero, 0.2f, Ease.OutBack);
            _pressedMatch3Tile = null;
            spriteRenderer.sprite = null;
        }

    }
    
    public void ResetHoveredTile()
    {
        hoveredMatch3Tile?.SetHovered(false);
        hoveredMatch3Tile = null;
    }
    
    private void UpdateIndicatorPosition()
    {
        if (!_camera || !_enabled || !_pressedMatch3Tile) return;
        
        
        Vector3 mousePosition = _camera.ScreenToWorldPoint(_inputReader.MousePosition);
        mousePosition.z = spriteRenderer.transform.position.z;
        spriteRenderer.transform.position = mousePosition;
            
        lineRenderer.SetPosition(0, mousePosition);
        lineRenderer.SetPosition(1, _pressedMatch3Tile.transform.position);
    }
    
    private void UpdateHoveredTile()
    {
        if (!_camera || !match3PlayHandler.CanInteract || !_inputReader || _inputReader.IsCurrentDeviceTouchscreen) return;
        
        Vector2 mousePos = _inputReader.MousePosition;
        Vector2 worldPos = _camera.ScreenToWorldPoint(mousePos);
    
        RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);
        
        Match3Tile newHoveredMatch3Tile = null;
        if (hit.collider)
        {
            newHoveredMatch3Tile = hit.collider.GetComponent<Match3Tile>();
        }

        if (newHoveredMatch3Tile != hoveredMatch3Tile)
        {
            hoveredMatch3Tile?.SetHovered(false);
            newHoveredMatch3Tile?.SetHovered(true);
            hoveredMatch3Tile = newHoveredMatch3Tile;
        }
    }
    
    private void UpdateEmissionMask(Texture2D emissionMask)
    {
        spriteRenderer.material.SetTexture(EmissionMask, emissionMask);
    }
    

}
