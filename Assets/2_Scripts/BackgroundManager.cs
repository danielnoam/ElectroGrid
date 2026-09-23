using System.Collections.Generic;
using DNExtensions.Utilities;
using DNExtensions.Utilities.Button;
using UnityEngine;
using PrimeTween;

public class BackgroundManager : MonoBehaviour
{
    [Header("Mouse Interaction")]
    [SerializeField] private bool mouseInteractionEffect = true;
    [SerializeField] private float maxScaleMultiplier = 1f;
    [SerializeField] private float minScaleMultiplier = 0.8f;
    [SerializeField] private float effectRadius = 3f;
    [SerializeField] private AnimationCurve zOffsetCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Pulse Settings")]
    [SerializeField] private bool enablePulse = true;
    [SerializeField] private float pulseInterval = 1.5f;
    
    [Header("Background Settings")]
    [SerializeField] private float maxFadeDistance = 13f;
    [SerializeField] private Transform backgroundParent;
    [SerializeField] private Match3BackgroundTile backgroundTilePrefab;
    [SerializeField] private Grid grid = new Grid();
    [SerializeField, ReadOnly] private List<Match3BackgroundTile> backgroundTiles = new List<Match3BackgroundTile>();
    
    private Camera _camera;
    private TouchInputReader _inputReader;
    private float _pulseTimer;
    private Vector2 _lastPointerPosition = Vector2.positiveInfinity;

    private void Awake()
    {
        _camera = Camera.main;
    }

    private void Start()
    {
        if (!_inputReader)
        {
            _inputReader = TouchInputReader.Instance;
        }
        
        _pulseTimer = pulseInterval;
    }

    private void Update()
    {
        UpdateTiles();
        
        if (enablePulse)
        {
            UpdatePulse();
        }
    }

    private void UpdatePulse()
    {
        _pulseTimer -= Time.deltaTime;
        
        if (_pulseTimer <= 0)
        {
            _pulseTimer = pulseInterval;
            PulseFromCenter();
        }
    }

    private void PulseFromCenter()
    {
        if (backgroundTiles.Count == 0) return;
        
        var centerOfGrid = new Vector2(grid.Width / 2f, grid.Height / 2f);

        foreach (var tile in backgroundTiles)
        {
            if (!tile) continue;
            
            var tileGridPosition = grid.GetCell(tile.transform.position);
            float distanceFromCenter = Vector2.Distance(tileGridPosition, centerOfGrid);
            int delayMultiplier = Mathf.RoundToInt(distanceFromCenter);
            
            tile.SquashTile(delayMultiplier);
        }
    }

    private void UpdateTiles()
    {
        if (!mouseInteractionEffect || !_camera || !_inputReader || backgroundTiles.Count == 0) return;

        // The pointer only moves while a finger is down, so without this the whole grid is
        // recalculated and rewritten every frame to produce exactly the same picture
        Vector2 pointerPosition = _inputReader.MousePosition;
        if (pointerPosition == _lastPointerPosition) return;
        _lastPointerPosition = pointerPosition;

        Vector3 pointerWorldPosition = _camera.ScreenToWorldPoint(pointerPosition);
        pointerWorldPosition.z = 0;

        foreach (var tile in backgroundTiles)
        {
            if (!tile) continue;

            float distance = Vector2.Distance(pointerWorldPosition, tile.transform.position);
            var scale = Vector3.one * CalculateScaleMultiplier(distance);

            // Assigning an identical scale still dirties the transform, so only write real changes
            if (tile.transform.localScale != scale) tile.transform.localScale = scale;
        }
    }

    private float CalculateScaleMultiplier(float distance)
    {
        if (distance > effectRadius)
        {
            return 1f;
        }

        float normalizedDistance = distance / effectRadius;
        float curveValue = zOffsetCurve.Evaluate(normalizedDistance);
        return Mathf.Lerp(minScaleMultiplier, maxScaleMultiplier, curveValue);
    }
    
    [Button]
    private void ClearBackground()
    {
        var tilesToClear = new List<Match3BackgroundTile>(backgroundTiles);
    
        foreach (Match3BackgroundTile tile in tilesToClear)
        {
            if (!tile) continue;
        
            if (Application.isPlaying)
            {
                Destroy(tile.gameObject);
            }
            else
            {
                DestroyImmediate(tile.gameObject);
            }
        }
    
        backgroundTiles.Clear();
    }
    
    [Button]
    public void CreateBackground()
    {
        if (grid == null || !backgroundTilePrefab || !backgroundParent) return;
        
        ClearBackground();
        GenerateTiles();
        ApplyFadeEffect();
    }

    private void GenerateTiles()
    {
        for (int x = 0; x < grid.Width; x++)
        {
            for (int y = 0; y < grid.Height; y++)
            {
                var position = grid.GetCellWorldPosition(x, y);
                var tile = Instantiate(backgroundTilePrefab, position, Quaternion.identity, backgroundParent);
                backgroundTiles.Add(tile);
            }
        }
    }

    private void ApplyFadeEffect()
    {
        if (maxFadeDistance <= 0) return;
        
        var centerOfGrid = new Vector2(grid.Width / 2f, grid.Height / 2f);

        foreach (var tile in backgroundTiles)
        {
            var tileGridPosition = grid.GetCell(tile.transform.position);
            float distanceFromCenter = Vector2.Distance(tileGridPosition, centerOfGrid);
            
            float normalizedDistance = Mathf.Clamp01(distanceFromCenter / maxFadeDistance);
            normalizedDistance = Mathf.Pow(normalizedDistance, 2.75f);

            Color color = tile.SpriteRenderer.color;
            Color baseColor = tile.SpriteRenderer.color;
            color.a = Mathf.Lerp(0f, baseColor.a, normalizedDistance);
            tile.SpriteRenderer.color = color;
        }
    }
}