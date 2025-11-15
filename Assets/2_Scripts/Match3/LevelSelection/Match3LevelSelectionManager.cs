using System.Text;
using DNExtensions.MenuSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Match3LevelSelectionManager : MonoBehaviour
{
    [Header("Level Buttons")]
    [SerializeField] private Transform buttonsHolder;
    [SerializeField] private Button levelButtonPrefab;
    
    [Header("Level Info Window")]
    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private TextMeshProUGUI levelTitleText;
    [SerializeField] private TextMeshProUGUI levelInfoText;
    [SerializeField] private Button levelStartButton;
    
    [Header("References")]
    [SerializeField] private Button backButton;
    [SerializeField] private AudioSource audioSource;
    
    private SOMatch3Level _selectedLevel;

    private void Start()
    {
        GameManager.Instance?.SelectMatch3Level(null);
        CreateLevelButtons();
        UpdateLevelInfo();
        SetupStartButton();
        SetupBackButton();
    }

    private void SetupStartButton()
    {
        if (!levelStartButton) return;
        
        SelectableAnimator selectableAnimator = levelStartButton.GetComponent<SelectableAnimator>();
        selectableAnimator.audioSource = audioSource;
        
        levelStartButton.onClick.RemoveAllListeners();
        levelStartButton.onClick.AddListener(OnStartButtonClicked);
    }

    private void SetupBackButton()
    {
        if (!backButton) return;
        
        SelectableAnimator selectableAnimator = backButton.GetComponent<SelectableAnimator>();
        selectableAnimator.audioSource = audioSource;
        
        backButton.onClick.RemoveAllListeners();
        backButton.onClick.AddListener(() =>
        {
            GameManager.Instance?.LoadMainMenuScene();
        });
        
    }

    private void OnStartButtonClicked()
    {
        if (!_selectedLevel || !GameManager.Instance) return;
        
        GameManager.Instance.SelectMatch3Level(_selectedLevel);
        GameManager.Instance.LoadMatch3Scene();
    }

    private void CreateLevelButtons()
    {
        if (!GameManager.Instance || !buttonsHolder || !levelButtonPrefab) return;

        SOMatch3Level[] levels = GameManager.Instance.Match3Levels;
        if (levels == null || levels.Length == 0) return;

        foreach (Transform child in buttonsHolder)
        {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < levels.Length; i++)
        {
            SOMatch3Level level = levels[i];
            if (!level) continue;

            Button levelButton = Instantiate(levelButtonPrefab, buttonsHolder);
            int levelIndex = i;

            TextMeshProUGUI buttonText = levelButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText)
            {
                buttonText.text = (levelIndex + 1).ToString();
            }
            
            SelectableAnimator selectableAnimator = levelButton.GetComponent<SelectableAnimator>();
            selectableAnimator.audioSource = audioSource;

            levelButton.onClick.AddListener(() => OnLevelButtonClicked(level));
        }
    }

    private void OnLevelButtonClicked(SOMatch3Level level)
    {
        _selectedLevel = _selectedLevel == level ? null : level;

        UpdateLevelInfo();
    }
    
    private void UpdateLevelInfo()
    {
        if (!_selectedLevel)
        {
            if (levelTitleText) levelTitleText.text = "Select a Level";
            if (levelInfoText) levelInfoText.text = "Press any of the buttons bellow to select a level";
            if (levelStartButton) levelStartButton.interactable = false;
            return;
        }

        if (levelTitleText) levelTitleText.text = _selectedLevel.LevelName;
        if (levelInfoText) levelInfoText.text = GenerateLevelInfo(_selectedLevel);
        if (levelStartButton) levelStartButton.interactable = true;
    }

    private string GenerateLevelInfo(SOMatch3Level level)
    {
        StringBuilder info = new StringBuilder();

        info.AppendLine($"Grid: {level.GridShape.Grid.Width}x{level.GridShape.Grid.Height}");
        info.AppendLine();

        if (level.Objectives is { Count: > 0 })
        {
            info.AppendLine("Objectives:");
            foreach (var objective in level.Objectives)
            {
                if (objective != null)
                {
                    info.AppendLine($"• {objective.GetDescription()}");
                }
            }
            info.AppendLine();
        }

        if (level.LoseConditions is { Count: > 0 })
        {
            info.AppendLine("Lose Conditions:");
            foreach (var condition in level.LoseConditions)
            {
                if (condition != null)
                {
                    info.AppendLine($"• {condition.GetDescription()}");
                }
            }
        }

        return info.ToString();
    }
}