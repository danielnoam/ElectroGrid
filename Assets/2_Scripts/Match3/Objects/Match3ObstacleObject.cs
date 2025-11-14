using System.Collections.Generic;
using PrimeTween;
using UnityEngine;


[SelectionBase]
public class Match3ObstacleObject : Match3Object
{
    [Header("Obstacle Settings")]
    [SerializeField] private int maxHealth = 1;

    private Match3GameManager _gameManager;
    private int _currentHealth;
    
    public override bool IsSwappable => false;
    public override bool IsMatchable => false;
    public override bool IsMovable => false;
    public int CurrentHealth => _currentHealth;
    
    

    public override void Initialize(SOItemData data, Match3GridHandler gridHandler)
    {
        base.Initialize(data, gridHandler);
        
        _currentHealth = maxHealth;
        
        _gameManager = Match3GameManager.Instance;
        if (_gameManager)
        {
            _gameManager.MatchesMade -= OnMatchesMade;
            _gameManager.MatchesMade += OnMatchesMade;
        }
        
        transform.localScale = Vector3.zero;
    }

    public override void SetCurrentTile(Match3Tile match3Tile)
    {
        bool spawning = !_currentTile;
        _currentTile = match3Tile;
        
        _movementSequence.Stop();
        _movementSequence = Sequence.Create();
        
        if (spawning)
        {
            _movementSequence.Group(Tween.Scale(transform, _baseScale, 0.5f, Ease.OutBack, startDelay: 0.5f));
            _movementSequence.ChainCallback(() => { spawnSfx?.Play(audioSource); });
        }
        else
        {
            var endPosition = new Vector3(_currentTile.transform.localPosition.x, _currentTile.transform.localPosition.y, transform.localPosition.z);
            
            _movementSequence.Group(Tween.Scale(transform, Vector3.zero, swapDuration/2, Ease.OutBack, startDelay: 0.5f));
            _movementSequence.ChainCallback(() => { transform.localPosition = endPosition; });
            _movementSequence.Group(Tween.Scale(transform,_baseScale, swapDuration/2, Ease.OutBack, startDelay: 0.5f));
        }
    }

    private void TakeDamage(int damage = 1)
    {
        if (_beingDestroyed) return;
        
        _currentHealth -= damage;
        
        if (_currentHealth <= 0)
        {
            BreakObstacle();
        }
        else
        {
            var damageSequence = Sequence.Create();
            damageSequence.Group(Tween.Scale(transform, _baseScale * 1.3f, 0.1f, Ease.OutBack));
            damageSequence.Chain(Tween.Scale(transform, _baseScale, 0.1f, Ease.OutBack));
        }
    }

    private void BreakObstacle()
    {
        _currentTile?.SetCurrentItem(null);
        _gameManager?.NotifyObstacleBroke(this);
        DestroyWithAnimation();
    }

    private void OnMatchesMade(List<Match3Tile> matches)
    {
        if (_beingDestroyed || !_currentTile || !_currentTile.IsActive) return;
        
        foreach (var match in matches)
        {
            if (_gridHandler.AreTilesNeighbours(match, _currentTile))
            {
                TakeDamage();
                return;
            }
        }
    }

    public override void OnPoolReturn()
    {
        base.OnPoolReturn();
        
        if (_gameManager) _gameManager.MatchesMade -= OnMatchesMade;
    }
}