using System;
using System.Text;
using DNExtensions;
using DNExtensions.MenuSystem;
using DNExtensions.VFXManager;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelSelectionScreen : MenuScreen
{
    [Header("Level Info Window")]
    [SerializeField, MinMaxRange(25f, 35f)] private RangedFloat cellSize = 35f;
    [SerializeField] private float cellSpacing = 2.5f;
    [SerializeField] private int maxGridWidthForLargeSize = 8;
    [SerializeField] private Color activeCellColor = new Color(0.3f, 0.7f, 0.3f);
    [SerializeField] private Color inactiveCellColor = new Color(0.4f, 0.4f, 0.4f);
    [SerializeField] private string lockedLevelLabel = "?";
    
    [Header("References")]
    [SerializeField] private Transform buttonsHolder;
    [SerializeField] private Button levelButtonPrefab;
    [SerializeField] private TextMeshProUGUI levelTitleText;
    [SerializeField] private TextMeshProUGUI levelInfoText;
    [SerializeField] private Button levelStartButton;
    [SerializeField] private RectTransform gridContainer;
    [SerializeField] private Image gridCellPrefab;
    [SerializeField] private MenuManager menuManager;
    [SerializeField] private Button backButton;
    [SerializeField] private AudioSource audioSource;

    private SOMatch3Level _selectedLevel;

    private void Start()
    {
        GameManager.Instance?.SelectMatch3Level(null);
        CreateLevelButtons();
        UpdateLevelInfo();
        SetupButtons();

        if (SaveManager.Instance) SaveManager.Instance.SaveReset += OnSaveReset;
    }

    private void OnDestroy()
    {
        if (SaveManager.Instance) SaveManager.Instance.SaveReset -= OnSaveReset;
    }

    private void OnSaveReset()
    {
        _selectedLevel = null;
        GameManager.Instance?.SelectMatch3Level(null);
        CreateLevelButtons();
        UpdateLevelInfo();
    }

    protected override Tween GetShowPositionTween()
    {
        Vector3 startPosition = contentOriginalPosition - (Vector3.right * 1000f);
        return Tween.UIAnchoredPosition(contentContainer, startPosition, contentOriginalPosition, showTweenSettings);
    }

    protected override Tween GetHidePositionTween()
    {
        Vector3 endPosition = contentOriginalPosition - (Vector3.right * 1000f);
        return Tween.UIAnchoredPosition(contentContainer, contentOriginalPosition, endPosition, hideTweenSettings);
    }
    


    private void SetupButtons()
    {
        if (levelStartButton)
        {
            SelectableAnimator selectableAnimator = levelStartButton.GetComponent<SelectableAnimator>();
            if (selectableAnimator && audioSource) selectableAnimator.audioSource = audioSource;

            levelStartButton.onClick.RemoveAllListeners();
            levelStartButton.onClick.AddListener(OnStartButtonClicked);
        }

        if (backButton)
        {
            SelectableAnimator selectableAnimator = backButton.GetComponent<SelectableAnimator>();
            if (selectableAnimator && audioSource) selectableAnimator.audioSource = audioSource;
            
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(OnBackButtonClicked);
        }
    }

    private void OnStartButtonClicked()
    {
        if (!_selectedLevel || !GameManager.Instance) return;
        

        var vfxDuration = VFXManager.Instance.PlayVFX(menuManager.EndLevelEffect);
        CameraManager.Instance?.ShakeCamera(vfxDuration);
        GameManager.Instance.SelectMatch3Level(_selectedLevel);
        
        
        HideByFade(4, () => { GameManager.Instance.Match3Scene.LoadScene(); });
    }

    private void OnBackButtonClicked()
    {
        CameraManager.Instance?.ShakeCamera(0.5f);
        menuManager?.ShowMainMenu();
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
            bool unlocked = !SaveManager.Instance || SaveManager.Instance.IsLevelUnlocked(levelIndex);

            TextMeshProUGUI buttonText = levelButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText)
            {
                buttonText.text = unlocked ? (levelIndex + 1).ToString() : lockedLevelLabel;
            }

            SelectableAnimator selectableAnimator = levelButton.GetComponent<SelectableAnimator>();
            if (selectableAnimator && audioSource) selectableAnimator.audioSource = audioSource;

            levelButton.interactable = unlocked;
            if (!unlocked) continue;

            levelButton.onClick.AddListener(() => OnLevelButtonClicked(level));
        }
    }

    private void OnLevelButtonClicked(SOMatch3Level level)
    {
        CameraManager.Instance?.ShakeCamera(0.1f);
        _selectedLevel = _selectedLevel == level ? null : level;
        UpdateLevelInfo();
    }
    
    private void UpdateLevelInfo()
    {
        ClearGrid();
        
        if (!_selectedLevel)
        {
            if (levelTitleText) levelTitleText.text = "Select a Level";
            if (levelInfoText) levelInfoText.text = "Press any of the buttons below to select a level";
            if (levelStartButton) levelStartButton.interactable = false;
            return;
        }

        if (levelTitleText) levelTitleText.text = _selectedLevel.LevelName;
        if (levelInfoText) levelInfoText.text = GenerateLevelInfo(_selectedLevel);
        if (levelStartButton) levelStartButton.interactable = true;
        
        DrawGrid(_selectedLevel.GridShape.Grid);
    }

    private string GenerateLevelInfo(SOMatch3Level level)
    {
        StringBuilder info = new StringBuilder();
        
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

        AppendBestStats(info, level);

        return info.ToString();
    }

    private void AppendBestStats(StringBuilder info, SOMatch3Level level)
    {
        var record = SaveManager.Instance ? SaveManager.Instance.GetRecord(level) : null;
        if (record is not { completed: true }) return;

        int minutes = Mathf.FloorToInt(record.bestTime / 60f);
        int seconds = Mathf.FloorToInt(record.bestTime % 60f);

        info.AppendLine();
        info.AppendLine("Best:");
        info.AppendLine($"• Moves: {record.bestMoves}");
        info.AppendLine($"• Time: {minutes:00}:{seconds:00}");
        info.AppendLine($"• Pieces Cleared: {record.bestPiecesCleared}");
    }

    private void DrawGrid(Grid grid)
    {
        if (!gridContainer || !gridCellPrefab || grid == null) return;

        int width = grid.Width;
        int height = grid.Height;

        // Use max size if grid width is below threshold, otherwise use min size
        float actualCellSize = width <= maxGridWidthForLargeSize ? cellSize.maxValue : cellSize.minValue;

        float totalWidth = (width * actualCellSize) + ((width - 1) * cellSpacing);
        float totalHeight = (height * actualCellSize) + ((height - 1) * cellSpacing);

        gridContainer.sizeDelta = new Vector2(totalWidth, totalHeight);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                CreateCell(x, y, grid, actualCellSize);
            }
        }
    }

    private void CreateCell(int x, int y, Grid grid, float actualCellSize)
    {
        Image cell = Instantiate(gridCellPrefab, gridContainer);
        cell.name = $"Cell ({x},{y})";
    
        RectTransform cellRect = cell.rectTransform;
        cellRect.sizeDelta = new Vector2(actualCellSize, actualCellSize);

        float posX = (x * (actualCellSize + cellSpacing)) - (gridContainer.sizeDelta.x / 2f) + (actualCellSize / 2f);
        float posY = (y * (actualCellSize + cellSpacing)) - (gridContainer.sizeDelta.y / 2f) + (actualCellSize / 2f);
    
        cellRect.anchoredPosition = new Vector2(posX, posY);

        bool isActive = grid.IsCellActive(x, y);
        cell.color = isActive ? activeCellColor : inactiveCellColor;
    }

    private void ClearGrid()
    {
        if (!gridContainer) return;

        foreach (Transform child in gridContainer)
        {
            Destroy(child.gameObject);
        }
    }
}