using UnityEngine;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{

    [Header("References")]
    [SerializeField] private Button clickerButton;
    [SerializeField] private Button match3Button;
    [SerializeField] private Button quitButton;
    
    
    
    private void OnEnable()
    {
        clickerButton.onClick.RemoveAllListeners();
        clickerButton.onClick.AddListener(() =>
        {
            GameManager.Instance?.LoadClickerScene();
        });
        
        match3Button.onClick.RemoveAllListeners();
        match3Button.onClick.AddListener(() =>
        {
            GameManager.Instance?.LoadMatch3LevelsScene();
        });
        
        quitButton.onClick.RemoveAllListeners();
        quitButton.onClick.AddListener(() =>
        {
#if UNITY_EDITOR
            if (Application.isEditor)
            {
                UnityEditor.EditorApplication.isPlaying = false;
                return;
            }     
#endif

            
            Application.Quit();
        });
    }
    
    
    
}
