using System.Collections.Generic;
using UnityEngine;

/// <summary>Shows each tutorial card once, the first time its mechanic actually appears in play.</summary>
public class Match3TutorialPresenter : MonoBehaviour
{
    private Match3GameManager _gameManager;
    private InformationWindowUI _informationWindow;
    private TopBarUI _topBarUI;
    private BottomBarUI _bottomBarUI;

    private readonly List<SOMatch3Tutorial> _queue = new List<SOMatch3Tutorial>();
    private readonly List<SOMatch3Tutorial> _presented = new List<SOMatch3Tutorial>();
    private SOMatch3Tutorial _showing;

    public void Initialize(Match3GameManager gameManager, InformationWindowUI informationWindow, TopBarUI topBarUI, BottomBarUI bottomBarUI)
    {
        Unsubscribe();

        _gameManager = gameManager;
        _informationWindow = informationWindow;
        _topBarUI = topBarUI;
        _bottomBarUI = bottomBarUI;

        Subscribe();
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }

    private void Subscribe()
    {
        if (!_gameManager) return;

        _gameManager.LevelStarted += OnLevelStarted;
        _gameManager.LineBreakMade += OnLineBreakMade;

        if (_gameManager.GridHandler) _gameManager.GridHandler.HelperSpawned += OnHelperSpawned;
    }

    private void Unsubscribe()
    {
        if (!_gameManager) return;

        _gameManager.LevelStarted -= OnLevelStarted;
        _gameManager.LineBreakMade -= OnLineBreakMade;

        if (_gameManager.GridHandler) _gameManager.GridHandler.HelperSpawned -= OnHelperSpawned;
    }

    private void Update()
    {
        if (_showing || _queue.Count == 0 || !_informationWindow) return;

        // Only interrupt once the board is idle, opening mid cascade would hand input back at the wrong moment
        var playHandler = _gameManager ? _gameManager.PlayHandler : null;
        if (!playHandler || !playHandler.CanInteract) return;

        Present(_queue[0]);
    }

    private void OnLevelStarted(Match3LevelData levelData)
    {
        _queue.Clear();

        // Insurance, a close callback that never arrived would otherwise block every later card
        _showing = null;

        Enqueue(Match3TutorialTrigger.AnyLevelStart);

        if (levelData?.Level == null) return;

        if (levelData.Level.CountObjectsOfType(Match3TileObjectType.Obstacle) > 0)
        {
            Enqueue(Match3TutorialTrigger.LevelHasObstacles);
        }

        if (levelData.Level.CountObjectsOfType(Match3TileObjectType.Bottom) > 0)
        {
            Enqueue(Match3TutorialTrigger.LevelHasBottomObjects);
        }
    }

    private void OnHelperSpawned(Match3HelperObject helper)
    {
        Enqueue(Match3TutorialTrigger.HelperSpawned);
    }

    private void OnLineBreakMade(List<int> rows, List<int> columns)
    {
        Enqueue(Match3TutorialTrigger.LineBreakMade);
    }

    private void Enqueue(Match3TutorialTrigger trigger)
    {
        var tutorials = GameManager.Instance ? GameManager.Instance.Match3GeneralTutorials : null;
        if (tutorials == null) return;

        foreach (var tutorial in tutorials)
        {
            if (!tutorial || !tutorial.ShowContextually || tutorial.Trigger != trigger) continue;
            if (tutorial == _showing || _queue.Contains(tutorial) || _presented.Contains(tutorial)) continue;
            if (SaveManager.Instance && SaveManager.Instance.HasSeenTutorial(tutorial)) continue;

            _queue.Add(tutorial);
        }
    }

    private void Present(SOMatch3Tutorial tutorial)
    {
        _queue.Remove(tutorial);
        _showing = tutorial;

        // Marked up front, so a card is never shown twice if the player quits before closing it
        _presented.Add(tutorial);
        SaveManager.Instance?.MarkTutorialSeen(tutorial);

        _topBarUI?.Toggle(false);
        _bottomBarUI?.Toggle(false);

        _informationWindow.ShowTutorials(new[] { tutorial }, () => _showing = null);
    }
}
