using System;
using System.Collections.Generic;
using System.Text;
using DNExtensions.Systems.VFXManager;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

/// <summary>Survival's own menu screen, laid out like level select: the mode and its Play button on top, the player's best runs below.</summary>
public class SurvivalScreen : MenuScreen
{
    [Header("References")]
    [SerializeField] private MenuManager menuManager;
    [SerializeField] private TextMeshProUGUI bestScoreText;
    [SerializeField] private TextMeshProUGUI infoText;
    [SerializeField] private Button playButton;
    [SerializeField] private Button backButton;
    [SerializeField] private RectTransform runsParent;
    [SerializeField] private SurvivalRunRowUI rowTemplate;
    [SerializeField] private TextMeshProUGUI emptyText;
    [Tooltip("Its text is shown as the rules on this screen")]
    [SerializeField] private SOMatch3Tutorial rulesCard;

    private readonly List<SurvivalRunRowUI> _rows = new List<SurvivalRunRowUI>();

    protected override void Awake()
    {
        base.Awake();
        if (rowTemplate) rowTemplate.gameObject.SetActive(false);
    }

    private void Start()
    {
        L10n.LanguageChanged += Refresh;
        if (SaveManager.Instance) SaveManager.Instance.SaveReset += Refresh;

        SetupButtons();
        Refresh();
    }

    private void OnDestroy()
    {
        if (SaveManager.Instance) SaveManager.Instance.SaveReset -= Refresh;
        L10n.LanguageChanged -= Refresh;
    }

    public override void Show(bool animated = true, Action onComplete = null)
    {
        Refresh();
        base.Show(animated, onComplete);
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
        if (playButton)
        {
            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(OnPlayClicked);
        }

        if (backButton)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(OnBackClicked);
        }
    }

    private void OnPlayClicked()
    {
        if (!GameManager.Instance || !GameManager.Instance.SurvivalMode) return;

        var vfxDuration = VFXManager.Instance ? VFXManager.Instance.PlaySequence(menuManager.EndLevelEffect) : 0.5f;
        CameraManager.Instance?.ShakeCamera(vfxDuration);
        GameManager.Instance.SelectSurvival();

        HideByFade(4, () => { GameManager.Instance.Match3Scene.LoadScene(); });
    }

    private void OnBackClicked()
    {
        CameraManager.Instance?.ShakeCamera(0.5f);
        menuManager?.ShowMainMenu();
    }

    private void Refresh()
    {
        var save = SaveManager.Instance;
        var runs = save ? save.SurvivalRuns : null;
        int count = runs?.Count ?? 0;

        if (playButton) playButton.interactable = GameManager.Instance && GameManager.Instance.SurvivalMode;

        if (bestScoreText) bestScoreText.text = L10n.Get("survival.result.best", ("best", save ? save.SurvivalBestScore : 0));

        if (infoText)
        {
            var info = new StringBuilder();
            if (rulesCard)
            {
                info.AppendLine(rulesCard.TutorialText);
                info.AppendLine();
            }
            info.Append(L10n.Get("survival.runsplayed", ("runs", save ? save.Data.survivalRunsPlayed : 0)));
            infoText.text = info.ToString();
        }

        if (!runsParent || !rowTemplate) return;

        while (_rows.Count < count)
        {
            var row = Instantiate(rowTemplate, runsParent);
            row.name = $"Run {_rows.Count + 1}";
            _rows.Add(row);
        }

        var locale = LocalizationSettings.SelectedLocale;
        var culture = locale ? locale.Identifier.CultureInfo : null;
        for (int i = 0; i < _rows.Count; i++)
        {
            bool used = i < count;
            _rows[i].gameObject.SetActive(used);
            if (used) _rows[i].Set(i + 1, runs[i], culture);
        }

        if (emptyText)
        {
            emptyText.text = L10n.Get("survival.noruns");
            emptyText.gameObject.SetActive(count == 0);
            emptyText.transform.SetAsLastSibling();
        }
    }
}
