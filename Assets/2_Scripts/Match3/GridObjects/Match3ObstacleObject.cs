using System.Collections.Generic;
using PrimeTween;
using UnityEngine;

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
        Tween.Scale(transform, _baseScale, 0.5f, Ease.OutBack, startDelay: 0.5f);
    }

    public void TakeDamage(int damage = 1)
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