using PrimeTween;
using UnityEngine;

public class Match3BottomObject : Match3Object
{
    private Match3GameManager _gameManager;
    
    public override bool IsSwappable => false;
    public override bool IsMatchable => false;
    public override bool IsMovable => true;

    public override void Initialize(SOItemData data, Match3GridHandler gridHandler)
    {
        base.Initialize(data, gridHandler);
        
        _gameManager = Match3GameManager.Instance;
    }

    public override void SetCurrentTile(Match3Tile match3Tile)
    {
        bool spawning = !_currentTile;
        _currentTile = match3Tile;
        
        _movementSequence.Stop();
        
        var endPosition = new Vector3(_currentTile.transform.localPosition.x, _currentTile.transform.localPosition.y, transform.localPosition.z);
        var ease = spawning ? Ease.OutQuad : Ease.Linear;
        
        _movementSequence = Sequence.Create();
        _movementSequence.Group(Tween.LocalPosition(transform, endPosition, swapDuration, ease));
        _movementSequence.ChainCallback(() =>
        {
            if (spawning)
            {
                if (spawnSfx) spawnSfx.Play(audioSource);
            }
            
            CheckIfReachedBottom();
        });
    }

    private void CheckIfReachedBottom()
    {
        if (IsTouchingEndOfGrid())
        {
            ReachedBottom();
        }
    }

    private void ReachedBottom()
    {
        _currentTile?.SetCurrentItem(null);
        _gameManager?.NotifyBottomObjectReached(this);
        DestroyWithAnimation();
    }
}