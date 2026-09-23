using System.Collections.Generic;
using UnityEngine;

[SelectionBase]
public class Match3HelperObject : Match3SwappableObject
{
    private Match3GameManager _gameManager;

    public override bool IsMatchable => false;

    public override void Initialize(SOItemData data, Match3GridHandler gridHandler)
    {
        base.Initialize(data, gridHandler);

        _gameManager = Match3GameManager.Instance;
        if (_gameManager)
        {
            _gameManager.MatchesMade -= OnMatchesMade;
            _gameManager.MatchesMade += OnMatchesMade;
        }
    }

    private void OnMatchesMade(List<Match3Tile> matches)
    {
        if (IsAdjacentToAnyMatch(matches)) MatchFound();
    }

    protected override void OnMatched()
    {
        Match3EffectManager.Instance?.CreateHelperBackgroundParticle(transform.position);
        _gameManager?.NotifyHelperObjectDestroyed(this);
    }

    public override void OnPoolReturn()
    {
        base.OnPoolReturn();

        if (_gameManager) _gameManager.MatchesMade -= OnMatchesMade;
    }
}
