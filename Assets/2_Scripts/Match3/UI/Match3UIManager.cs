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
    }
}