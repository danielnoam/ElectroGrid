using System;
using System.Collections.Generic;
using DNExtensions.Button;
using DNExtensions.ObjectPooling;
using DNExtensions.VFXManager;
using UnityEngine;

public class Match3EffectManager : MonoBehaviour
{
    public static Match3EffectManager Instance { get; private set; }
    
    [Header("Effects")]
    [SerializeField] private SOVFEffectsSequence startLevelSequence;
    [SerializeField] private SOVFEffectsSequence endLevelSequence;
    [SerializeField] private OneShotParticle backgroundParticlePrefab;
    
    [Header("Mouse Interaction")]
    [SerializeField] private float maxScaleMultiplier = 1f;
    [SerializeField] private float minScaleMultiplier = 0.9f;
    [SerializeField] private float effectRadius = 3f;
    [SerializeField] private AnimationCurve effectOffsetCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("References")]
    [SerializeField] private Match3GameManager gameManager;
    [SerializeField] private Match3GridHandler gridHandler;
    [SerializeField] private Match3PlayHandler playHandler;
    [SerializeField] private Match3BackgroundTile backgroundTilePrefab;

    private Camera _camera;
    private TouchInputReader _inputReader;
    private readonly Dictionary<Vector2Int, Match3BackgroundTile> _backgroundTiles = new Dictionary<Vector2Int, Match3BackgroundTile>();
    
    public SOVFEffectsSequence StartLevelSequence => startLevelSequence;
    public SOVFEffectsSequence EndLevelSequence => endLevelSequence;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (!_camera)
        {
            _camera = Camera.main;
        }

        if (!_inputReader)
        {
            _inputReader = TouchInputReader.Instance;
        }
    }

    private void OnEnable()
    {
        if (gameManager)
        {
            gameManager.LevelStarted += OnLevelStarted;
            gameManager.LevelFailed += OnLevelEnded;
            gameManager.LevelComplete += OnLevelEnded;
            gameManager.MatchesMade += OnMatchesMade;
        }
        
        if (gridHandler)
        {
            gridHandler.GridCreated += OnGridCreated;
            gridHandler.GridDestroyed += OnGridDestroyed;
        }
    }

    private void OnDisable()
    {
        if (gameManager)
        {
            gameManager.LevelStarted -= OnLevelStarted;
            gameManager.LevelFailed -= OnLevelEnded;
            gameManager.LevelComplete -= OnLevelEnded;
            gameManager.MatchesMade -= OnMatchesMade;
        }
        
        if (gridHandler)
        {
            gridHandler.GridCreated -= OnGridCreated;
            gridHandler.GridDestroyed -= OnGridDestroyed;
        }
    }

    private void OnMatchesMade(List<Match3Tile> matchedTiles)
    {
        var backgroundTilesPositions = new List<Vector2Int>(_backgroundTiles.Keys);
        
        bool isVertical = false;
        bool isHorizontal = false;
        
        if (matchedTiles.Count >= 2)
        {
            Vector2Int? firstPos = null;
            Vector2Int? secondPos = null;
            
            for (int i = 0; i < matchedTiles.Count && secondPos == null; i++)
            {
                if (matchedTiles[i])
                {
                    if (firstPos == null)
                        firstPos = matchedTiles[i].GridPosition;
                    else
                        secondPos = matchedTiles[i].GridPosition;
                }
            }
            
            if (firstPos.HasValue && secondPos.HasValue)
            {
                isVertical = firstPos.Value.x == secondPos.Value.x;
                isHorizontal = firstPos.Value.y == secondPos.Value.y;
            }
        }
        
        for (int matchedTile = 0; matchedTile < matchedTiles.Count; matchedTile++)
        {
            if (!matchedTiles[matchedTile]) continue;

            Vector2Int matchedPos = matchedTiles[matchedTile].GridPosition;

            for (int backgroundTile = 0; backgroundTile < backgroundTilesPositions.Count; backgroundTile++)
            {
                Vector2Int bgPos = backgroundTilesPositions[backgroundTile];
                
                bool shouldAnimate = false;
                int distance = 0;
                
                if (isVertical && matchedPos.x == bgPos.x)
                {
                    shouldAnimate = true;
                    distance = Mathf.Abs(matchedPos.y - bgPos.y);
                }
                else if (isHorizontal && matchedPos.y == bgPos.y)
                {
                    shouldAnimate = true;
                    distance = Mathf.Abs(matchedPos.x - bgPos.x);
                }
                
                if (shouldAnimate)
                {
                    _backgroundTiles.TryGetValue(bgPos, out var tile);
                    if (tile)
                    {
                        tile.SquashTile(distance);
                    }
                }
            }
        }
    }


    private void Update()
    {
        UpdateTiles();
    }
    


    private void OnGridDestroyed()
    {
        var tilesToClear = new List<Match3BackgroundTile>(_backgroundTiles.Values);
    
        foreach (Match3BackgroundTile tile in tilesToClear)
        {
            if (!tile) continue;
            ObjectPooler.ReturnObjectToPool(tile.gameObject);
        }
    
        _backgroundTiles.Clear();
    }

    private void OnGridCreated(Grid grid)
    {
        if (!gridHandler) return;
        
        var mainTiles = gridHandler.Tiles;
        
        // Create background tiles in inactive cells
        for (int x = 0; x < grid.Width; x++)
        {
            for (int y = 0; y < grid.Height; y++)
            {
                Vector2Int tileGridPosition = new Vector2Int(x, y);
                Vector3 tileWorldPosition = grid.GetCellWorldPosition(x, y);
                bool tileState = grid.IsCellActive(x, y);

                if (!tileState && !mainTiles.ContainsKey(tileGridPosition))
                {
                    var tile = CreateBackgroundTile(tileWorldPosition);
                    _backgroundTiles.Add(tileGridPosition, tile);
                }
            }
        }
        
        
        // Create around each boundary tile another disabled tile
        foreach (var kvp in mainTiles)
        {
            var gridPosition = kvp.Key;
            
            if (grid.IsTouchingEdge(gridPosition, out var directions))
            {
                foreach (var direction in directions)
                {
                    var edgePosition = gridPosition + direction;
                    if (!mainTiles.ContainsKey(edgePosition) && !_backgroundTiles.ContainsKey(edgePosition))
                    {
                        var tileWorldPosition = grid.GetCellWorldPosition(edgePosition.x, edgePosition.y);
                        var tile = CreateBackgroundTile(tileWorldPosition);
                        _backgroundTiles.Add(edgePosition, tile);
                    }
                }
            }
        }
        
        // Create extra tiles above and below the grid
        for (int x = -1; x < grid.Width + 1; x++)
        {
            // Above the grid
            for (int y = grid.Height; y < grid.Height + 4; y++)
            {
                Vector2Int tileGridPosition = new Vector2Int(x, y);
                if (mainTiles.ContainsKey(tileGridPosition) || _backgroundTiles.ContainsKey(tileGridPosition)) 
                    continue;
                
                Vector3 tileWorldPosition = grid.GetCellWorldPosition(x, y);
                var tile = CreateBackgroundTile(tileWorldPosition);
                _backgroundTiles.Add(tileGridPosition, tile);
            }
            
            // Below the grid
            for (int y = -4; y < 0; y++)
            {
                Vector2Int tileGridPosition = new Vector2Int(x, y);
                if (mainTiles.ContainsKey(tileGridPosition) || _backgroundTiles.ContainsKey(tileGridPosition)) 
                    continue;
                
                Vector3 tileWorldPosition = grid.GetCellWorldPosition(x, y);
                var tile = CreateBackgroundTile(tileWorldPosition);
                _backgroundTiles.Add(tileGridPosition, tile);
            }
        }
        
        // Create extra tiles to the left and right of the grid
        for (int y = -1; y < grid.Height + 1; y++)
        {
            // To the left of the grid
            for (int x = -1; x < 0; x++)
            {
                Vector2Int tileGridPosition = new Vector2Int(x, y);
                if (mainTiles.ContainsKey(tileGridPosition) || _backgroundTiles.ContainsKey(tileGridPosition))
                    continue;

                Vector3 tileWorldPosition = grid.GetCellWorldPosition(x, y);
                var tile = CreateBackgroundTile(tileWorldPosition);
                _backgroundTiles.Add(tileGridPosition, tile);
            }

            // To the right of the grid
            for (int x = grid.Width; x < grid.Width + 1; x++)
            {
                Vector2Int tileGridPosition = new Vector2Int(x, y);
                if (mainTiles.ContainsKey(tileGridPosition) || _backgroundTiles.ContainsKey(tileGridPosition))
                    continue;

                Vector3 tileWorldPosition = grid.GetCellWorldPosition(x, y);
                var tile = CreateBackgroundTile(tileWorldPosition);
                _backgroundTiles.Add(tileGridPosition, tile);
            }
        }
        
        
        // Set the color of the tiles
        foreach (var backgroundTile in _backgroundTiles)
        {
            var gridPosition = backgroundTile.Key;
            var tile = backgroundTile.Value;
            
            Vector2 closestPointOnGrid = new Vector2(
                Mathf.Clamp(gridPosition.x, 0, grid.Width - 1),
                Mathf.Clamp(gridPosition.y, 0, grid.Height - 1)
            );
            
            float distanceFromGrid = Vector2.Distance(gridPosition, closestPointOnGrid);
            
            float maxFadeDistance = 5;
            float normalizedDistance = Mathf.Clamp01(distanceFromGrid / maxFadeDistance);
            normalizedDistance = Mathf.Pow(normalizedDistance, 0.25f);

            Color color = tile.InactiveTileColor;
            color.a = Mathf.Lerp(tile.InactiveTileColor.a, 0f, normalizedDistance);
            tile.SpriteRenderer.color = color;
        }
    }
    
    private void OnLevelStarted(Match3LevelData levelData)
    {
        VFXManager.Instance?.PlayVFX(startLevelSequence);
    }
    
    private void OnLevelEnded(Match3LevelData levelData)
    {
        VFXManager.Instance?.PlayVFX(endLevelSequence);
    }
    


    private void UpdateTiles()
    {
        if (!_camera || !_inputReader || _backgroundTiles.Count == 0) return;
    
        Vector2 mousePos = _inputReader.MousePosition;
        Vector3 mouseWorldPos = _camera.ScreenToWorldPoint(mousePos);
        mouseWorldPos.z = 0;

        foreach (var kvp in _backgroundTiles)
        {
            var tile = kvp.Value;
            if (!tile) continue;

            Vector3 tilePos = tile.transform.position;
            float distance = Vector2.Distance(new Vector2(mouseWorldPos.x, mouseWorldPos.y), new Vector2(tilePos.x, tilePos.y));
        
            float scaleMultiplier = CalculateScaleMultiplier(distance);
            tile.transform.localScale = Vector3.one * scaleMultiplier;
        }
    }
    
    private float CalculateScaleMultiplier(float distance)
    {
        if (distance > effectRadius)
        {
            return 1f;
        }

        float normalizedDistance = distance / effectRadius;
        float curveValue = effectOffsetCurve.Evaluate(normalizedDistance);
        return Mathf.Lerp( minScaleMultiplier, maxScaleMultiplier, curveValue);
    }
    
    private Match3BackgroundTile CreateBackgroundTile(Vector3 position)
    {
        var tileGo = ObjectPooler.GetObjectFromPool(backgroundTilePrefab.gameObject, position, Quaternion.identity);
        var tile = tileGo.GetComponent<Match3BackgroundTile>();
        
        return tile;
    }
    
    [Button]
    public void CreateShapeEffect(Vector3 position, SOItemData itemData)
    {
        if (!backgroundParticlePrefab || !itemData) return;
    
        var particle = ObjectPooler.GetObjectFromPool(backgroundParticlePrefab.gameObject, position, Quaternion.identity);
        var particleOneShot = particle.GetComponent<OneShotParticle>();
        var textureSheetModule = particleOneShot.particle.textureSheetAnimation;
        var colorOverLifetimeModule = particleOneShot.particle.colorOverLifetime;
    
        var startColor = itemData.Color;
        startColor.a = 0.3f;
        
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] 
            { 
                new GradientColorKey(startColor, 0.0f),   
                new GradientColorKey(startColor, 0.2f),  
                new GradientColorKey(Color.clear, 1.0f)   
            },
            new GradientAlphaKey[] 
            { 
                new GradientAlphaKey(startColor.a, 0.0f),      
                new GradientAlphaKey(startColor.a, 0.2f),  
                new GradientAlphaKey(0.0f, 1.0f)     
            }
        );
    
        colorOverLifetimeModule.color = new ParticleSystem.MinMaxGradient(gradient);
        textureSheetModule.SetSprite(0, itemData.Sprite);
        particleOneShot.Play();
    }

}