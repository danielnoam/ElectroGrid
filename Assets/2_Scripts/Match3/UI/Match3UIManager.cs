using UnityEngine;

public class Match3UIManager : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private TopBarUI topBarUI;
    [SerializeField] private BottomBarUI bottomBarUI;
    [SerializeField] private LevelCompleteWindowUI levelCompleteWindowUI;
    [SerializeField] private InformationWindowUI informationWindowUI;

    [Header("References")]
    [SerializeField] private Match3GameManager match3Manager;

    private void Awake()
    {
        topBarUI.Initialize(match3Manager);
        bottomBarUI.Initialize(match3Manager);
        levelCompleteWindowUI.Initialize(match3Manager);
        informationWindowUI.Initialize();

        // Added here rather than placed in the scene, so it needs no serialized wiring of its own
        var tutorialPresenter = gameObject.AddComponent<Match3TutorialPresenter>();
        tutorialPresenter.Initialize(match3Manager, informationWindowUI, topBarUI, bottomBarUI);
    }
}
