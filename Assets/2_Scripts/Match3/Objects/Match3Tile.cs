using System;
using DNExtensions.Utilities;
using DNExtensions.Systems.ObjectPooling;
using PrimeTween;
using UnityEngine;

[SelectionBase]
public class Match3Tile : MonoBehaviour, IPoolable
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
    
    [Header("Line Break Pulse")]
    [SerializeField] private float lineBreakPulseDuration = 0.45f;
    [SerializeField, Range(0f, 1f)] private float lineBreakPulseAlpha = 0.8f;
    
    [Header("Glitch")]
    [SerializeField] private Material glitchMaterial;
    [SerializeField] private Sprite glitchSprite;
    [SerializeField] private PoolableParticleSystem glitchClearParticle;
    [SerializeField, Min(0.01f)] private float glitchClearDuration = 0.3f;
    [SerializeField, Min(0.01f)] private float glitchSize = 0.98f;

    [Header("Squash Settings")]
    [SerializeField] private float squashScaleAmount = 0.7f;
    [SerializeField] private float squashDuration = 0.15f;
    
    [Header("References")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Transform trashSprite;

    [Separator]
    [SerializeField, ReadOnly] private Vector2Int gridPosition;
    [SerializeField, ReadOnly] private bool isActive;
    
    private Match3GameManager _match3GameManager;
    private Match3GridHandler _match3GridHandler;
    private Match3Object _currentMatch3Object;
    private bool _isSelected;
    private bool _isHovered;
    private Vector3 _baseScale;
    private Vector3 _baseTrashScale;
    private Sequence _pulseSequence;
    private Tween _trashTween;
    private Sequence _colorSequence;
    private bool _glitched;
    private SpriteRenderer _glitchOverlay;
    private MaterialPropertyBlock _glitchBlock;
    private Sequence _glitchSequence;
    
    public Vector2Int GridPosition => gridPosition;
    public Match3Object CurrentMatch3Object => _currentMatch3Object;
    public bool CanSelect => isActive && _currentMatch3Object && !_isSelected && _currentMatch3Object.IsSwappable;
    public bool IsActive => isActive;
    public bool HasObject => isActive && _currentMatch3Object;
    public bool IsGlitched => _glitched;


    private void Awake()
    {
        _baseTrashScale = trashSprite.localScale;
        _baseScale = transform.localScale;
    }

    public void Initialize(Match3GameManager match3GameManager, Vector2Int position, bool active)
    {
        _match3GameManager = match3GameManager;
        _match3GridHandler = _match3GameManager.GridHandler;
        _match3GridHandler.GridDestroyed -= OnGridDestroyed;
        _match3GridHandler.GridDestroyed += OnGridDestroyed;
        
        
        transform.localScale = _baseScale;
        trashSprite.localScale = _baseTrashScale;
        gameObject.name = $"Tile ({position.x},{position.y})";
        gridPosition = position;
        _isSelected = false;
        _isHovered = false;
        isActive = active;
        HideGlitch();
        ToggleTrash(false);
        
        UpdateVisuals();
    }
    
    public void InitializeAsTrash(Match3GameManager match3GameManager, Vector2Int position)
    {
        _match3GameManager = match3GameManager;
        _match3GridHandler = _match3GameManager.GridHandler;
        _match3GridHandler.GridDestroyed -= OnGridDestroyed;
        _match3GridHandler.GridDestroyed += OnGridDestroyed;
        
        transform.localScale = _baseScale;
        trashSprite.localScale = _baseTrashScale;
        gameObject.name = $"Tile ({position.x},{position.y})";
        gridPosition = position;
        _isSelected = false;
        _isHovered = false;
        isActive = false;   
        HideGlitch();
        ToggleTrash(true);
        
        UpdateVisuals();
    }

    private void OnGridDestroyed()
    {
        ObjectPooler.ReturnObjectToPool(gameObject);
    }
    
    private void ToggleTrash(bool state)
    {
        trashSprite.gameObject.SetActive(state);
        
        if (state)
        {
            _trashTween.Stop();
            trashSprite.localScale = new Vector3(_baseTrashScale.x, 0f, _baseTrashScale.z);
        }
    }

    /// <summary>Unfolds the catch box under the board. Catch boxes start shut and open once the board is ready.</summary>
    public void OpenTrash(float delay = 0f)
    {
        if (!trashSprite.gameObject.activeSelf) return;

        _trashTween.Stop();
        _trashTween = Tween.ScaleY(trashSprite, _baseTrashScale.y, 0.35f, Ease.OutBack, startDelay: delay);
    }

    /// <summary>Folds the catch box under the board shut, once the last Plug for its column has dropped in.</summary>
    public void CloseTrash()
    {
        if (!trashSprite.gameObject.activeSelf) return;

        _trashTween.Stop();
        _trashTween = Tween.ScaleY(trashSprite, 0f, 0.35f, Ease.InBack);
    }

    /// <summary>A Plug just landed in this socket, so it gives a short jolt.</summary>
    public void PlugIn()
    {
        if (!trashSprite.gameObject.activeSelf) return;

        _trashTween.Stop();
        trashSprite.localScale = _baseTrashScale;
        _trashTween = Tween.PunchScale(trashSprite, new Vector3(0.25f, -0.2f, 0f), 0.3f, frequency: 6);
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
            spriteRenderer.sprite = inactiveSprite;
            spriteRenderer.color = inactiveTileColor;
        }
    }
    
    public void SetGlitch(bool glitched)
    {
        bool wasGlitched = _glitched;
        _glitched = glitched && isActive;

        if (_glitched)
        {
            ShowGlitch();
            return;
        }

        if (wasGlitched) ClearGlitchWithEffect();
        else HideGlitch();
    }

    private void ShowGlitch()
    {
        EnsureGlitchOverlay();
        _glitchSequence.Stop();
        _glitchOverlay.gameObject.SetActive(true);
        _glitchOverlay.transform.localScale = Vector3.one * glitchSize;
        SetGlitchProperties(UnityEngine.Random.value * 100f, 1f);
    }

    private void HideGlitch()
    {
        _glitched = false;
        _glitchSequence.Stop();
        if (_glitchOverlay) _glitchOverlay.gameObject.SetActive(false);
    }

    private void ClearGlitchWithEffect()
    {
        if (!_glitchOverlay)
        {
            HideGlitch();
            return;
        }

        if (glitchClearParticle)
        {
            var particleGo = ObjectPooler.GetObjectFromPool(glitchClearParticle.gameObject, transform.position, Quaternion.identity);
            particleGo.GetComponent<PoolableParticleSystem>().Play(transform.position);
        }

        _glitchSequence.Stop();
        _glitchSequence = Sequence.Create()
            .Group(Tween.Scale(_glitchOverlay.transform, Vector3.one * glitchSize * 1.35f, glitchClearDuration, Ease.OutQuad))
            .Group(Tween.Custom(1f, 0f, glitchClearDuration, value => SetGlitchProperties(null, value), Ease.InQuad))
            .ChainCallback(() =>
            {
                if (_glitchOverlay) _glitchOverlay.gameObject.SetActive(false);
            });
    }

    private void SetGlitchProperties(float? seed, float intensity)
    {
        _glitchBlock ??= new MaterialPropertyBlock();
        _glitchOverlay.GetPropertyBlock(_glitchBlock);
        if (seed.HasValue) _glitchBlock.SetFloat("_Seed", seed.Value);
        _glitchBlock.SetFloat("_Intensity", intensity);
        _glitchOverlay.SetPropertyBlock(_glitchBlock);
    }

    private void EnsureGlitchOverlay()
    {
        if (_glitchOverlay) return;

        var overlay = new GameObject("Glitch");
        overlay.transform.SetParent(transform, false);
        overlay.transform.localPosition = new Vector3(0f, 0f, -0.01f);
        _glitchOverlay = overlay.AddComponent<SpriteRenderer>();
        _glitchOverlay.sprite = glitchSprite ? glitchSprite : Resources.Load<Sprite>("Sprites/ShapeSquare");
        _glitchOverlay.sharedMaterial = glitchMaterial ? glitchMaterial : Resources.Load<Material>("Materials/Glitch");
        _glitchOverlay.sortingLayerID = SortingLayer.NameToID("Object");
        _glitchOverlay.sortingOrder = 10;
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
    
    /// <summary>Flashes the cell in the colour of the piece a line break just destroyed on it, then fades back.</summary>
    /// <summary>The lose ending: the tile flickers once, then dims to grey. Undone by the next visual update.</summary>
    public void PowerDown(float delay, float duration, float brightness)
    {
        if (!isActive || !spriteRenderer) return;

        var color = spriteRenderer.color;
        float grey = (color.r * 0.299f + color.g * 0.587f + color.b * 0.114f) * brightness;
        Tween.StopAll(spriteRenderer);
        var sequence = Sequence.Create();
        if (delay > 0f) sequence.ChainDelay(delay);
        sequence.Chain(Tween.Alpha(spriteRenderer, color.a * 0.25f, 0.04f))
            .Chain(Tween.Alpha(spriteRenderer, color.a, 0.04f))
            .Chain(Tween.Color(spriteRenderer, new Color(grey, grey, grey, color.a), duration));
    }

    public void PulseColor(Color color)
    {
        if (!isActive || !spriteRenderer) return;

        _colorSequence.Stop();
        UpdateVisuals();
        var restColor = spriteRenderer.color;
        var pulseColor = new Color(color.r, color.g, color.b, Mathf.Max(restColor.a, lineBreakPulseAlpha));

        _colorSequence = Sequence.Create()
            .Chain(Tween.Color(spriteRenderer, pulseColor, lineBreakPulseDuration * 0.2f, Ease.OutQuad))
            .Chain(Tween.Color(spriteRenderer, restColor, lineBreakPulseDuration * 0.8f, Ease.InQuad))
            .ChainCallback(UpdateVisuals);
    }
    
    public void SquashTile()
    {
        if (!isActive) return;
        
        _pulseSequence = Sequence.Create();
        _pulseSequence.Group(Tween.Scale(transform, _baseScale * squashScaleAmount, squashDuration * 0.3f, Ease.Linear));
        _pulseSequence.Chain(Tween.Scale(transform, _baseScale, squashDuration * 0.7f, Ease.OutSine));
    }
    
    public void PunchTile(float multiplier = 1f)
    {
        PunchTileTo(punchScaleAmount * multiplier);
    }

    /// <summary>Punches the tile to an exact multiple of its size, then back.</summary>
    public void PunchTileTo(float scale)
    {
        if (!isActive) return;
        
        _pulseSequence = Sequence.Create();
        _pulseSequence.Group(Tween.Scale(transform, _baseScale * scale, punchDuration/2, Ease.OutElastic));
        _pulseSequence.Chain(Tween.Scale(transform, _baseScale, punchDuration/2, Ease.InQuad));
    }

    public void OnPoolGet()
    {
    }

    public void OnPoolReturn()
    {
        _pulseSequence.Stop();
        _colorSequence.Stop();
        _trashTween.Stop();
        _glitchSequence.Stop();
        if (_match3GridHandler) _match3GridHandler.GridDestroyed -= OnGridDestroyed;
    }

    public void OnPoolRecycle()
    {
    }
}