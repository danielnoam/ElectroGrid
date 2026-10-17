using System;
using System.Collections.Generic;
using DNExtensions.Utilities;
using DNExtensions.Systems.VFXManager;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelSelectionScreen : MenuScreen
{
    [Header("Board Preview")]
    [Tooltip("The area above the levels where the selected level's board is drawn, sized to fit like the board in game")]
    [SerializeField] private RectTransform shapePreviewArea;
    [Tooltip("The board tile prefab: the preview copies its sprite, material and sorting so it glows like the real board")]
    [SerializeField] private Match3Tile previewTilePrefab;
    [SerializeField] private Color previewTileColor = new Color(0.72f, 0f, 1f, 1f);
    [Tooltip("Gap between cells, as a fraction of a cell")]
    [SerializeField, Range(0f, 0.5f)] private float previewCellGap = 0.1f;
    [SerializeField, Min(0f)] private float previewPopDuration = 0.25f;
    [SerializeField, Min(0f)] private float previewPopStagger = 0.012f;
    [SerializeField] private string lockedLevelLabel = "?";

    [Header("Level Buttons")]
    [SerializeField] private Sprite lockSprite;
    [SerializeField] private Color nextLevelGlowColor = new Color(1f, 0.7f, 0.1f, 1f);
    [SerializeField] private Color selectedOutlineColor = new Color(0.85f, 0.3f, 1f, 1f);
    [SerializeField] private float selectedOutlineWidth = 10f;

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
    [Tooltip("How fast the right stick or Page Up and Page Down scroll the level list, in pixels per second")]
    [SerializeField, Min(0f)] private float stickScrollSpeed = 1600f;

    private float _cellSize;
    private float _cellGap;
    
    [Header("References")]
    [SerializeField] private Transform buttonsHolder;
    [SerializeField] private Button levelButtonPrefab;
    [SerializeField] private Button levelStartButton;
    [SerializeField] private MenuManager menuManager;
    [SerializeField] private Button backButton;

    private SOMatch3Level _selectedLevel;
    private Tween _nextLevelPulse;
    private readonly System.Collections.Generic.Dictionary<SOMatch3Level, Button> _levelButtons = new System.Collections.Generic.Dictionary<SOMatch3Level, Button>();
    private Image _selectedOutline;
    private Transform _previewRoot;
    private readonly List<SpriteRenderer> _previewTiles = new List<SpriteRenderer>();
    private int _previewUsed;
    private TextMeshProUGUI _startLabel;
    private UnityEngine.Localization.Components.LocalizeStringEvent _startLabelEvent;
    private bool _previewOpen;

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
        base.Show(animated, () =>
        {
            _previewOpen = true;
            DrawPreview(_selectedLevel);
            onComplete?.Invoke();
        });
    }

    public override void Hide(bool animated = true, Action onComplete = null)
    {
        _previewOpen = false;
        ClearPreview(true);
        base.Hide(animated, onComplete);
    }

    public override void HideByFade(float durationMultiplier, Action onComplete = null)
    {
        _previewOpen = false;
        ClearPreview(true);
        base.HideByFade(durationMultiplier, onComplete);
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

    private UnityEngine.InputSystem.InputAction _scrollAction;

    /// <summary>The right stick (or Page Up and Page Down) scrolls the level list; the left stick still moves the selection.</summary>
    private void Update()
    {
        if (!levelsScroll || !levelsScroll.vertical || !levelsScroll.content || ControllerFocus.TopScope != GetComponent<FocusScope>()) return;

        _scrollAction ??= ControllerFocus.FindUiAction("Scroll");
        if (_scrollAction == null) return;

        float input = _scrollAction.ReadValue<Vector2>().y;
        if (Mathf.Abs(input) < 0.2f) return;

        float viewport = levelsScroll.viewport ? levelsScroll.viewport.rect.height : ((RectTransform)levelsScroll.transform).rect.height;
        float scrollable = levelsScroll.content.rect.height - viewport;
        if (scrollable <= 0f) return;

        levelsScroll.StopMovement();
        levelsScroll.verticalNormalizedPosition = Mathf.Clamp01(levelsScroll.verticalNormalizedPosition + input * stickScrollSpeed * Time.unscaledDeltaTime / scrollable);
    }

    private void OnDestroy()
    {
        _nextLevelPulse.Stop();
        ClearPreview(false);
        if (_previewRoot) Destroy(_previewRoot.gameObject);
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
        _levelButtons.Clear();
        _selectedOutline = null;

        _nextLevelPulse.Stop();
        var nextLevel = NextLevel();
        var nodes = new System.Collections.Generic.List<(RectTransform rect, bool unlocked)>();
        int nextIndex = 0;

        for (int i = 0; i < levels.Length; i++)
        {
            SOMatch3Level level = levels[i];
            if (!level) continue;

            Button levelButton = Instantiate(levelButtonPrefab, buttonsHolder);
            levelButton.gameObject.AddComponent<ScrollRectDragRelay>();
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
            _levelButtons[level] = levelButton;
        }

        LayoutCircuit(nodes, nextIndex);
        UpdateSelectedOutline();
    }

    private void UpdateSelectedOutline()
    {
        if (!_selectedLevel || !_levelButtons.TryGetValue(_selectedLevel, out var button) || !button)
        {
            if (_selectedOutline) _selectedOutline.gameObject.SetActive(false);
            return;
        }

        var buttonRect = (RectTransform)button.transform;
        if (!_selectedOutline)
        {
            var outline = new GameObject("SelectedOutline", typeof(RectTransform), typeof(Image));
            _selectedOutline = outline.GetComponent<Image>();
            _selectedOutline.raycastTarget = false;
        }

        var buttonImage = button.GetComponent<Image>();
        _selectedOutline.sprite = buttonImage ? buttonImage.sprite : null;
        _selectedOutline.type = buttonImage ? buttonImage.type : Image.Type.Simple;
        _selectedOutline.color = selectedOutlineColor;

        var outlineRect = (RectTransform)_selectedOutline.transform;
        outlineRect.SetParent(buttonRect.parent, false);
        outlineRect.SetAsLastSibling();
        outlineRect.SetSiblingIndex(buttonRect.GetSiblingIndex());
        outlineRect.anchorMin = buttonRect.anchorMin;
        outlineRect.anchorMax = buttonRect.anchorMax;
        outlineRect.pivot = buttonRect.pivot;
        outlineRect.anchoredPosition = buttonRect.anchoredPosition;
        outlineRect.sizeDelta = buttonRect.sizeDelta + Vector2.one * (selectedOutlineWidth * 2f);
        _selectedOutline.gameObject.SetActive(true);
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

    /// <summary>Stretches the level list down to just above the Start button, whatever the screen's aspect ratio.</summary>
    private void FitListAboveBackButton()
    {
        if (!levelsScroll || !backButton) return;

        Canvas.ForceUpdateCanvases();
        var window = (RectTransform)levelsScroll.transform;
        var back = levelStartButton && levelStartButton.gameObject.activeInHierarchy ? (RectTransform)levelStartButton.transform : (RectTransform)backButton.transform;
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
        if (_selectedLevel == level && ControllerFocus.ControllerActive)
        {
            OnStartButtonClicked();
            return;
        }

        CameraManager.Instance?.ShakeCamera(0.1f);
        _selectedLevel = _selectedLevel == level ? null : level;
        UpdateLevelInfo();
    }
    
    private void UpdateLevelInfo()
    {
        UpdateSelectedOutline();
        var scope = GetComponent<FocusScope>();
        if (scope) scope.FirstSelected = _selectedLevel && _levelButtons.TryGetValue(_selectedLevel, out var selectedButton) ? selectedButton : levelStartButton;
        if (levelStartButton) levelStartButton.interactable = _selectedLevel;
        UpdateStartLabel();
        if (_previewOpen && isActiveAndEnabled) DrawPreview(_selectedLevel);
    }

    private void UpdateStartLabel()
    {
        if (!levelStartButton) return;

        if (!_startLabel)
        {
            _startLabel = levelStartButton.GetComponentInChildren<TextMeshProUGUI>(true);
            _startLabelEvent = _startLabel ? _startLabel.GetComponent<UnityEngine.Localization.Components.LocalizeStringEvent>() : null;
            if (_startLabelEvent) _startLabelEvent.enabled = false;
        }
        if (!_startLabel) return;

        int number = _selectedLevel && GameManager.Instance ? Array.IndexOf(GameManager.Instance.Match3Levels, _selectedLevel) + 1 : 0;
        if (number > 0) _startLabel.text = L10n.Get("levelselect.play", ("level", number));
        else if (_startLabelEvent) _startLabel.text = L10n.Get(_startLabelEvent.StringReference);
    }

    private void DrawPreview(SOMatch3Level level)
    {
        ClearPreview(false);
        if (!level || !level.GridShape || level.GridShape.Grid == null || !shapePreviewArea || !previewTilePrefab) return;

        var camera = Camera.main;
        var canvas = shapePreviewArea.GetComponentInParent<Canvas>();
        if (!camera || !canvas) return;

        var uiCamera = canvas.rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.rootCanvas.worldCamera;
        var corners = new Vector3[4];
        shapePreviewArea.GetWorldCorners(corners);
        Vector3 ToWorld(Vector3 corner)
        {
            var point = camera.ScreenToWorldPoint(RectTransformUtility.WorldToScreenPoint(uiCamera, corner));
            point.z = 0f;
            return point;
        }

        var bottomLeft = ToWorld(corners[0]);
        var topRight = ToWorld(corners[2]);
        var grid = level.GridShape.Grid;
        float step = Mathf.Min((topRight.x - bottomLeft.x) / grid.Width, (topRight.y - bottomLeft.y) / grid.Height);
        float cell = step / (1f + previewCellGap);
        var center = (bottomLeft + topRight) * 0.5f;
        var origin = center - new Vector3((grid.Width - 1) * step * 0.5f, (grid.Height - 1) * step * 0.5f, 0f);

        if (!_previewRoot) _previewRoot = new GameObject("Level Preview").transform;
        var tileSource = previewTilePrefab.GetComponentInChildren<SpriteRenderer>(true);

        for (int y = 0; y < grid.Height; y++)
        {
            for (int x = 0; x < grid.Width; x++)
            {
                if (!grid.IsCellActive(x, y)) continue;

                var position = origin + new Vector3(x * step, y * step, 0f);
                float delay = (x + grid.Height - 1 - y) * previewPopStagger;
                PreviewSprite("Tile", tileSource, tileSource.sprite, previewTileColor, position, cell, 0, delay);
            }
        }
    }

    private void PreviewSprite(string name, SpriteRenderer source, Sprite sprite, Color color, Vector3 position, float size, int orderOffset, float delay)
    {
        if (!sprite) return;

        SpriteRenderer renderer;
        if (_previewUsed < _previewTiles.Count)
        {
            renderer = _previewTiles[_previewUsed];
        }
        else
        {
            renderer = new GameObject(name).AddComponent<SpriteRenderer>();
            renderer.transform.SetParent(_previewRoot, false);
            _previewTiles.Add(renderer);
        }
        _previewUsed++;

        var spriteObject = renderer.gameObject;
        spriteObject.SetActive(true);
        spriteObject.transform.position = position;
        renderer.sprite = sprite;
        renderer.color = color;
        if (source)
        {
            renderer.sharedMaterial = source.sharedMaterial;
            renderer.sortingLayerID = source.sortingLayerID;
            renderer.sortingOrder = source.sortingOrder + orderOffset;
        }

        float spriteSize = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
        var scale = Vector3.one * (spriteSize > 0f ? size / spriteSize : 1f);
        spriteObject.transform.localScale = Vector3.zero;
        Tween.Scale(spriteObject.transform, scale, previewPopDuration, Ease.OutBack, startDelay: delay);
    }

    /// <summary>Hides the preview tiles, fading them out or at once. They are kept and reused by the next preview.</summary>
    private void ClearPreview(bool fade)
    {
        _previewUsed = 0;
        foreach (var renderer in _previewTiles)
        {
            if (!renderer) continue;

            Tween.StopAll(renderer.transform);
            Tween.StopAll(renderer);
            if (!renderer.gameObject.activeSelf) continue;

            if (fade)
            {
                var tile = renderer;
                Tween.Alpha(tile, 0f, 0.2f).OnComplete(() => { if (tile && tile.color.a <= 0.01f) tile.gameObject.SetActive(false); });
            }
            else
            {
                renderer.gameObject.SetActive(false);
            }
        }
    }

}