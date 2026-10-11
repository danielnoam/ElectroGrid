using System;
using System.Text;
using DNExtensions.Utilities;
using DNExtensions.Systems.VFXManager;
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

    [Header("Level Buttons")]
    [SerializeField] private Sprite lockSprite;
    [SerializeField] private Color nextLevelGlowColor = new Color(1f, 0.7f, 0.1f, 1f);

    [Header("Circuit Layout")]
    [SerializeField, Min(1)] private int circuitColumns = 4;
    [SerializeField] private float circuitCellSize = 150f;
    [SerializeField] private float circuitGap = 60f;
    [SerializeField] private float circuitPadding = 40f;
    [SerializeField] private float wireThickness = 10f;
    [SerializeField] private Color litWireColor = new Color(0.85f, 0.3f, 1f, 1f);
    [SerializeField] private Color darkWireColor = new Color(1f, 1f, 1f, 0.12f);
    [SerializeField] private ScrollRect levelsScroll;
    [Tooltip("Space kept between the bottom of the level list and the top of the Back button")]
    [SerializeField] private float listBottomMargin = 40f;

    private float _cellSize;
    private float _cellGap;
    
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

    private SOMatch3Level _selectedLevel;
    private Tween _nextLevelPulse;

    private void Start()
    {
        L10n.LanguageChanged += UpdateLevelInfo;
        if (SaveManager.Instance) SaveManager.Instance.SaveReset += OnSaveReset;

        GameManager.Instance?.SelectMatch3Level(null);
        CreateLevelButtons();
        _selectedLevel = NextLevel();
        UpdateLevelInfo();
        SetupButtons();
    }

    public override void Show(bool animated = true, Action onComplete = null)
    {
        if (!_selectedLevel) _selectedLevel = NextLevel();
        UpdateLevelInfo();
        base.Show(animated, onComplete);
    }

    /// <summary>The first unlocked level not yet completed, or the last unlocked one when everything is done.</summary>
    private SOMatch3Level NextLevel()
    {
        var levels = GameManager.Instance ? GameManager.Instance.Match3Levels : null;
        if (levels == null) return null;

        SOMatch3Level lastUnlocked = null;
        for (int i = 0; i < levels.Length; i++)
        {
            var level = levels[i];
            if (!level) continue;
            if (SaveManager.Instance && !SaveManager.Instance.IsLevelUnlocked(i)) break;

            lastUnlocked = level;
            var record = SaveManager.Instance ? SaveManager.Instance.GetRecord(level) : null;
            if (record is not { completed: true }) return level;
        }

        return lastUnlocked;
    }

    private void OnDestroy()
    {
        _nextLevelPulse.Stop();
        if (SaveManager.Instance) SaveManager.Instance.SaveReset -= OnSaveReset;
        L10n.LanguageChanged -= UpdateLevelInfo;
    }

    private void OnSaveReset()
    {
        GameManager.Instance?.SelectMatch3Level(null);
        CreateLevelButtons();
        _selectedLevel = NextLevel();
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
            levelStartButton.onClick.RemoveAllListeners();
            levelStartButton.onClick.AddListener(OnStartButtonClicked);
        }

        if (backButton)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(OnBackButtonClicked);
        }
    }

    private void OnStartButtonClicked()
    {
        if (!_selectedLevel || !GameManager.Instance) return;
        

        var vfxDuration = VFXManager.Instance.PlaySequence(menuManager.EndLevelEffect);
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

        _nextLevelPulse.Stop();
        var nextLevel = NextLevel();
        var nodes = new System.Collections.Generic.List<(RectTransform rect, bool unlocked)>();
        int nextIndex = 0;

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

            levelButton.interactable = unlocked;
            nodes.Add(((RectTransform)levelButton.transform, unlocked));
            if (level == nextLevel) nextIndex = nodes.Count - 1;
            DecorateLevelButton(levelButton, buttonText, level, unlocked, level == nextLevel);
            if (!unlocked) continue;

            levelButton.onClick.AddListener(() => OnLevelButtonClicked(level));
        }

        LayoutCircuit(nodes, nextIndex);
    }

    private Vector2 CircuitPosition(int index, float width)
    {
        int row = index / circuitColumns;
        int column = index % circuitColumns;
        if (row % 2 == 1) column = circuitColumns - 1 - column;

        float gridWidth = circuitColumns * _cellSize + (circuitColumns - 1) * _cellGap;
        float left = (width - gridWidth) * 0.5f;
        float x = left + column * (_cellSize + _cellGap) + _cellSize * 0.5f;
        float y = -(circuitPadding + row * (_cellSize + _cellGap) + _cellSize * 0.5f);
        return new Vector2(x, y);
    }

    /// <summary>
    /// Lays the level buttons out as a circuit: a snake through rows of <see cref="circuitColumns"/>, wired together.
    /// Wires up to the next level are lit. Adding levels just adds rows, the scroll view takes care of the rest.
    /// </summary>
    private void LayoutCircuit(System.Collections.Generic.List<(RectTransform rect, bool unlocked)> nodes, int nextIndex)
    {
        var holder = (RectTransform)buttonsHolder;
        foreach (var group in holder.GetComponents<LayoutGroup>()) group.enabled = false;
        foreach (var fitter in holder.GetComponents<ContentSizeFitter>()) fitter.enabled = false;

        FitListAboveBackButton();

        holder.anchorMin = new Vector2(0f, 1f);
        holder.anchorMax = new Vector2(1f, 1f);
        holder.pivot = new Vector2(0.5f, 1f);
        holder.anchoredPosition = Vector2.zero;
        holder.sizeDelta = new Vector2(0f, holder.sizeDelta.y);
        Canvas.ForceUpdateCanvases();

        float width = holder.rect.width;
        float gapRatio = circuitCellSize > 0f ? circuitGap / circuitCellSize : 0.4f;
        float fitCell = (width - circuitPadding * 2f) / (circuitColumns + (circuitColumns - 1) * gapRatio);
        _cellSize = Mathf.Min(circuitCellSize, fitCell);
        _cellGap = _cellSize * gapRatio;

        int rows = Mathf.CeilToInt(nodes.Count / (float)circuitColumns);
        holder.sizeDelta = new Vector2(0f, circuitPadding * 2 + rows * _cellSize + Mathf.Max(0, rows - 1) * _cellGap);

        var wires = new GameObject("Wires", typeof(RectTransform));
        var wiresRect = (RectTransform)wires.transform;
        wiresRect.SetParent(holder, false);
        wiresRect.SetAsFirstSibling();
        wiresRect.anchorMin = new Vector2(0f, 1f);
        wiresRect.anchorMax = new Vector2(0f, 1f);
        wiresRect.pivot = new Vector2(0f, 1f);
        wiresRect.anchoredPosition = Vector2.zero;
        wiresRect.sizeDelta = Vector2.zero;

        for (int i = 0; i < nodes.Count; i++)
        {
            var rect = nodes[i].rect;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(_cellSize, _cellSize);
            rect.anchoredPosition = CircuitPosition(i, width);

            if (i == 0) continue;

            var from = CircuitPosition(i - 1, width);
            var to = rect.anchoredPosition;
            var wire = new GameObject($"Wire {i}", typeof(RectTransform), typeof(Image));
            var wireRect = (RectTransform)wire.transform;
            wireRect.SetParent(wiresRect, false);
            wireRect.anchorMin = wireRect.anchorMax = new Vector2(0f, 1f);
            wireRect.pivot = new Vector2(0.5f, 0.5f);
            wireRect.anchoredPosition = (from + to) * 0.5f;
            bool horizontal = Mathf.Approximately(from.y, to.y);
            float length = horizontal ? Mathf.Abs(to.x - from.x) : Mathf.Abs(to.y - from.y);
            wireRect.sizeDelta = horizontal ? new Vector2(length, wireThickness) : new Vector2(wireThickness, length);
            var wireImage = wire.GetComponent<Image>();
            wireImage.raycastTarget = false;
            wireImage.color = nodes[i].unlocked ? litWireColor : darkWireColor;
        }

        if (levelsScroll && rows > 0)
        {
            Canvas.ForceUpdateCanvases();
            float viewport = levelsScroll.viewport ? levelsScroll.viewport.rect.height : ((RectTransform)levelsScroll.transform).rect.height;
            float contentHeight = holder.sizeDelta.y;
            bool fits = contentHeight <= viewport;
            levelsScroll.vertical = !fits;
            if (fits)
            {
                levelsScroll.StopMovement();
                holder.anchoredPosition = new Vector2(0f, -(viewport - contentHeight) * 0.5f);
                return;
            }

            float target = -CircuitPosition(nextIndex, width).y - viewport * 0.5f;
            float scrollable = Mathf.Max(0f, contentHeight - viewport);
            levelsScroll.verticalNormalizedPosition = scrollable > 0f ? 1f - Mathf.Clamp01(target / scrollable) : 1f;
        }
    }

    /// <summary>Stretches the level list down to just above the Back button, whatever the screen's aspect ratio.</summary>
    private void FitListAboveBackButton()
    {
        if (!levelsScroll || !backButton) return;

        Canvas.ForceUpdateCanvases();
        var window = (RectTransform)levelsScroll.transform;
        var back = (RectTransform)backButton.transform;
        var windowCorners = new Vector3[4];
        var backCorners = new Vector3[4];
        window.GetWorldCorners(windowCorners);
        back.GetWorldCorners(backCorners);

        float scale = window.lossyScale.y;
        if (scale <= 0f) return;

        float top = windowCorners[1].y;
        float bottom = backCorners[1].y + listBottomMargin * scale;
        float newHeight = (top - bottom) / scale;
        if (newHeight < 200f) return;

        float oldHeight = window.rect.height;
        window.sizeDelta = new Vector2(window.sizeDelta.x, window.sizeDelta.y + (newHeight - oldHeight));
        window.anchoredPosition += new Vector2(0f, (oldHeight - newHeight) * (1f - window.pivot.y));
    }

    private void DecorateLevelButton(Button levelButton, TextMeshProUGUI buttonText, SOMatch3Level level, bool unlocked, bool isNext)
    {
        if (!unlocked)
        {
            if (!lockSprite) return;

            if (buttonText) buttonText.text = string.Empty;
            var lockIcon = new GameObject("Lock", typeof(RectTransform), typeof(Image));
            lockIcon.transform.SetParent(levelButton.transform, false);
            ((RectTransform)lockIcon.transform).sizeDelta = new Vector2(72f, 72f);
            var lockImage = lockIcon.GetComponent<Image>();
            lockImage.sprite = lockSprite;
            lockImage.preserveAspect = true;
            lockImage.raycastTarget = false;
            lockImage.color = new Color(1f, 1f, 1f, 0.35f);
            return;
        }

        if (isNext)
        {
            var buttonImage = levelButton.GetComponent<Image>();
            var glow = new GameObject("NextGlow", typeof(RectTransform), typeof(Image));
            glow.transform.SetParent(levelButton.transform, false);
            glow.transform.SetAsFirstSibling();
            var glowRect = (RectTransform)glow.transform;
            glowRect.anchorMin = Vector2.zero;
            glowRect.anchorMax = Vector2.one;
            glowRect.offsetMin = Vector2.zero;
            glowRect.offsetMax = Vector2.zero;
            var glowImage = glow.GetComponent<Image>();
            glowImage.sprite = buttonImage ? buttonImage.sprite : null;
            glowImage.type = buttonImage ? buttonImage.type : Image.Type.Simple;
            glowImage.raycastTarget = false;
            var glowColor = nextLevelGlowColor;
            glowColor.a = 0.15f;
            glowImage.color = glowColor;
            _nextLevelPulse = Tween.Alpha(glowImage, 0.5f, 0.8f, Ease.InOutSine, cycles: -1, cycleMode: CycleMode.Yoyo);
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
            if (levelTitleText) levelTitleText.text = L10n.Get("levelselect.none.title");
            if (levelInfoText) levelInfoText.text = L10n.Get("levelselect.none.info");
            if (levelStartButton) levelStartButton.interactable = false;
            return;
        }

        if (levelTitleText) levelTitleText.text = _selectedLevel.DisplayName;
        if (levelInfoText) levelInfoText.text = GenerateLevelInfo(_selectedLevel);
        if (levelStartButton) levelStartButton.interactable = true;
        
        DrawGrid(_selectedLevel.GridShape.Grid);
    }

    private string GenerateLevelInfo(SOMatch3Level level)
    {
        StringBuilder info = new StringBuilder();
        
        if (level.Objectives is { Count: > 0 })
        {
            info.AppendLine(L10n.Get("levelselect.objectives"));
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
            info.AppendLine(L10n.Get("levelselect.loseconditions"));
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
        info.AppendLine(L10n.Get("levelselect.best"));
        info.AppendLine($"• {L10n.Get("levelselect.best.moves", ("moves", record.bestMoves))}");
        info.AppendLine($"• {L10n.Get("levelselect.best.time", ("minutes", minutes), ("seconds", seconds))}");
        info.AppendLine($"• {L10n.Get("levelselect.best.pieces", ("pieces", record.bestPiecesCleared))}");
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