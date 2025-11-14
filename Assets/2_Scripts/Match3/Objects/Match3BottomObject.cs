using DNExtensions.Button;
using DNExtensions.ObjectPooling;
using PrimeTween;
using UnityEngine;

[SelectionBase]
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
        
        _movementSequence = Sequence.Create();
        _movementSequence.Group(Tween.LocalPosition(transform, endPosition, swapDuration, Ease.OutQuad));
        if (spawning) _movementSequence.ChainCallback(() => { spawnSfx?.Play(audioSource); });

        CheckIfReachedBottom();
    }
    
    protected override void DestroyWithAnimation()
    {
        _beingDestroyed = true;
        


        var bellowCellPosition = _gridHandler.Grid.GetCellWorldPosition(_currentTile.GridPosition.x, -1);
        var endPosition = new Vector3(bellowCellPosition.x, bellowCellPosition.y, transform.localPosition.z);
        if (destroySfx) destroySfx.Play(audioSource);
        
        _movementSequence.Stop();
        _movementSequence = Sequence.Create();
        var duration = swapDuration * 3f;
        
        _movementSequence.Group(Tween.LocalPosition(transform, endPosition, duration, Ease.OutQuart));
        _movementSequence.Group(Tween.Scale(transform, _baseScale * 0.75f, duration/1.75f, Ease.OutQuart, startDelay: duration/3));
        _movementSequence.InsertCallback(duration * 0.5f, () => {
            
            MobileHaptics.Vibrate(50);
            if (destroyParticle)
            {
                var particleGo = ObjectPooler.GetObjectFromPool(destroyParticle.gameObject, transform.position, Quaternion.identity);
                var particle = particleGo.GetComponent<OneShotParticle>();
                particle.Play(transform.position);
            }
        });

        _movementSequence.ChainCallback(() => { ObjectPooler.ReturnObjectToPool(gameObject); });
    }

    private void CheckIfReachedBottom()
    {
        if (IsTouchingEndOfGrid())
        {
            ReachedBottom();
        }
    }

    [Button(ButtonPlayMode.OnlyWhenPlaying)]
    private void ReachedBottom()
    {
        _currentTile?.SetCurrentItem(null);
        _gameManager?.NotifyBottomObjectReached(this);
        DestroyWithAnimation();
    }
}