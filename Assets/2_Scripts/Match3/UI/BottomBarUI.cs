using DNExtensions.VFXManager;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

public class BottomBarUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private RectTransform bottomBar;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button restartButton;
    
    [Header("Animation Settings")]
    [SerializeField] private TweenSettings bottomBarTweenSettings;
    
    [Header("References")]
    [SerializeField] private Match3GameManager match3Manager;
    [SerializeField] private Match3EffectManager match3EffectManager;
    [SerializeField] private TopBarUI topBarUI;
    
    private float _bottomBarDefaultYPosition;
    private Sequence _bottomBarSequence;

    public void Initialize()
    {
        _bottomBarDefaultYPosition = bottomBar.anchoredPosition.y;
        bottomBar.anchoredPosition = new Vector2(bottomBar.anchoredPosition.x, -bottomBar.sizeDelta.y);
        
        SetupButtons();
    }

    private void SetupButtons()
    {
        quitButton.onClick.RemoveAllListeners();
        quitButton.onClick.AddListener(() =>
        {
            if (VFXManager.Instance)
            {
                AnimateBottomBar(false);
                topBarUI.AnimateTopBar(false);
                var quitSequence = Sequence.Create();
                quitSequence.ChainDelay(VFXManager.Instance.PlayVFX(match3EffectManager.EndLevelSequence));
                quitSequence.ChainCallback(() => GameManager.Instance?.MainMenu.LoadScene());
            }
            else
            {
                GameManager.Instance?.MainMenu.LoadScene();
            }
        });
        
        restartButton.onClick.RemoveAllListeners();
        restartButton.onClick.AddListener(() =>
        {
            if (VFXManager.Instance)
            {
                AnimateBottomBar(false);
                topBarUI.AnimateTopBar(false);
                var quitSequence = Sequence.Create();
                quitSequence.ChainDelay(VFXManager.Instance.PlayVFX(match3EffectManager.EndLevelSequence));
                quitSequence.ChainCallback(() => match3Manager.StartNewGame());
            }
            else
            {
                match3Manager.StartNewGame();
            }
        });
    }

    public void AnimateBottomBar(bool show)
    {
        if (!bottomBar) return;

        _bottomBarSequence.Stop();
        var endYPosition = show ? _bottomBarDefaultYPosition : -bottomBar.sizeDelta.y;
        _bottomBarSequence = Sequence.Create()
            .Group(Tween.UIAnchoredPositionY(bottomBar, endYPosition, bottomBarTweenSettings));
    }
}