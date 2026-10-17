using System;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Shared settings panel. Behaves like InformationWindowUI so it can sit in the main menu and in
/// the Match3 scene unchanged, pausing gameplay while it is open.
/// </summary>
public class SettingsWindowUI : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private TweenSettings windowTweenSettings;

    [Header("Controls")]
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private Toggle hapticsToggle;
    [SerializeField] private Toggle rumbleToggle;
    [Tooltip("Shown on PC, and on phones only while a gamepad is connected")]
    [SerializeField] private GameObject rumbleRow;
    [SerializeField] private Toggle screenShakeToggle;
    [SerializeField] private Toggle tiltToggle;
    [SerializeField] private Toggle highFrameRateToggle;
    [Tooltip("Hidden on displays that cannot refresh faster than 60Hz")]
    [SerializeField] private GameObject highFrameRateRow;
    [SerializeField] private TMP_Dropdown languageDropdown;

    [Header("Reset Progress")]
    [SerializeField] private Button resetProgressButton;
    [SerializeField] private TextMeshProUGUI resetProgressLabel;
    [Tooltip("How long the confirm state stays armed before reverting")]
    [SerializeField, Min(1f)] private float resetConfirmTimeout = 3f;

    [Header("References")]
    [SerializeField] private Button backButton;
    [SerializeField] private RectTransform windowRectTransform;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TextMeshProUGUI versionText;

    private CanvasGroup _canvasGroup;
    private CanvasGroup _buttonsGroup;
    private RectTransform _backButtonRectTransform;
    private Vector2 _defaultSize;
    private Sequence _toggleSequence;
    private float _backgroundStartAlpha;
    private bool _applyingValues;
    private Action _onClosed;
    private Coroutine _versionRestore;
    private float _resetConfirmExpiry;
    private readonly System.Collections.Generic.List<UnityEngine.Localization.Locale> _languages = new System.Collections.Generic.List<UnityEngine.Localization.Locale>();

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        _buttonsGroup = WindowButtons.FadeGroup(backButton);

        if (backButton)
        {
            _backButtonRectTransform = backButton.GetComponent<RectTransform>();
        }

        if (windowRectTransform)
        {
            _defaultSize = windowRectTransform.sizeDelta;
            windowRectTransform.sizeDelta = new Vector2(_defaultSize.x, 0f);
        }

        if (titleText) titleText.alpha = 0f;
        if (versionText) versionText.text = $"v{Application.version}";
        AlignVersion();

        if (versionText)
        {
            versionText.raycastTarget = true;
            var versionRelay = PointerClickRelay.On(versionText);
            versionRelay.Clicked -= OnVersionClicked;
            versionRelay.Clicked += OnVersionClicked;
        }

        if (backgroundImage)
        {
            _backgroundStartAlpha = backgroundImage.color.a;
            backgroundImage.color = new Color(backgroundImage.color.r, backgroundImage.color.g, backgroundImage.color.b, 0f);
        }

        if (_canvasGroup)
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }
    }

    public void Initialize()
    {
        SetupControls();
        PullValuesFromSettings();

        L10n.LanguageChanged -= OnLanguageChanged;
        L10n.LanguageChanged += OnLanguageChanged;
    }

    private void OnDestroy()
    {
        L10n.LanguageChanged -= OnLanguageChanged;
        if (GameUpdater.Instance) GameUpdater.Instance.StateChanged -= OnUpdaterStateChanged;
    }

    private void OnVersionClicked(UnityEngine.EventSystems.PointerEventData eventData)
    {
        var updater = GameUpdater.Instance;
        if (!updater || !updater.CheckAgain()) return;

        CameraManager.Instance?.ShakeCamera(0.1f);
        updater.StateChanged -= OnUpdaterStateChanged;
        updater.StateChanged += OnUpdaterStateChanged;
        OnUpdaterStateChanged();
    }

    private System.Collections.IEnumerator RestoreVersionText()
    {
        yield return new WaitForSecondsRealtime(3f);
        if (versionText) versionText.text = $"v{Application.version}";
        _versionRestore = null;
    }

    private void OnUpdaterStateChanged()
    {
        var updater = GameUpdater.Instance;
        if (!updater || !versionText) return;

        string status = updater.CurrentState switch
        {
            GameUpdater.State.Checking => L10n.Get("settings.version.checking"),
            GameUpdater.State.UpToDate => L10n.Get("settings.version.uptodate"),
            GameUpdater.State.UpdateAvailable => L10n.Get("settings.version.available", ("latest", updater.LatestVersion)),
            _ => L10n.Get("settings.version.failed")
        };
        versionText.text = status;
        if (_versionRestore != null) StopCoroutine(_versionRestore);
        _versionRestore = null;

        if (updater.CurrentState == GameUpdater.State.Checking) return;

        updater.StateChanged -= OnUpdaterStateChanged;
        _versionRestore = StartCoroutine(RestoreVersionText());
        if (updater.CurrentState == GameUpdater.State.UpdateAvailable)
        {
            var offer = FindAnyObjectByType<UpdateWindowUI>(FindObjectsInactive.Include);
            if (offer) offer.OfferAgain();
        }
    }

    private void OnLanguageChanged()
    {
        RefreshLanguageLabel();
        if (resetProgressLabel) resetProgressLabel.text = L10n.Get(_resetConfirmExpiry > 0f ? "settings.reset.confirm" : "settings.reset");
        AlignVersion();
    }

    // The footer row mirrors for right-to-left languages, but the version reads left to right, so it gets its
    // alignment from the language rather than from its text
    private void AlignVersion()
    {
        if (versionText) versionText.horizontalAlignment = L10n.IsRightToLeft ? HorizontalAlignmentOptions.Left : HorizontalAlignmentOptions.Right;
    }

    /// <summary>Fills the language dropdown with each language's own name and selects the current one.</summary>
    private void RefreshLanguageLabel()
    {
        if (!languageDropdown) return;

        _languages.Clear();
        _languages.AddRange(L10n.PlayerLanguages());
        var options = new System.Collections.Generic.List<TMP_Dropdown.OptionData>();
        foreach (var locale in _languages) options.Add(new TMP_Dropdown.OptionData(L10n.LanguageName(locale)));
        languageDropdown.ClearOptions();
        languageDropdown.AddOptions(options);
        languageDropdown.SetValueWithoutNotify(Mathf.Max(0, _languages.IndexOf(UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocale)));
        languageDropdown.RefreshShownValue();
    }

    private void OnLanguagePicked(int index)
    {
        if (index < 0 || index >= _languages.Count) return;

        CameraManager.Instance?.ShakeCamera(0.1f);
        L10n.SetLanguage(_languages[index]);
    }

    private void RefreshRumbleRow()
    {
        if (!rumbleRow) return;

        bool mobile = Application.isMobilePlatform;
        rumbleRow.SetActive(!mobile || UnityEngine.InputSystem.Gamepad.all.Count > 0);
    }

    public void Show(Action onClosed = null)
    {
        _onClosed = onClosed;
        DisarmReset();
        PullValuesFromSettings();
        RefreshLanguageLabel();
        RefreshRumbleRow();
        Toggle(true);
    }

    private void SetupControls()
    {
        if (backButton)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(Close);
        }

        if (backgroundImage)
        {
            var backgroundRelay = PointerClickRelay.On(backgroundImage);
            backgroundRelay.Clicked -= OnBackgroundClicked;
            backgroundRelay.Clicked += OnBackgroundClicked;
        }

        if (musicVolumeSlider)
        {
            musicVolumeSlider.onValueChanged.RemoveAllListeners();
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        }

        if (sfxVolumeSlider)
        {
            sfxVolumeSlider.onValueChanged.RemoveAllListeners();
            sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
        }

        if (hapticsToggle)
        {
            hapticsToggle.onValueChanged.RemoveAllListeners();
            hapticsToggle.onValueChanged.AddListener(OnHapticsChanged);
        }

        if (rumbleToggle)
        {
            rumbleToggle.onValueChanged.RemoveAllListeners();
            rumbleToggle.onValueChanged.AddListener(OnRumbleChanged);
        }

        if (screenShakeToggle)
        {
            screenShakeToggle.onValueChanged.RemoveAllListeners();
            screenShakeToggle.onValueChanged.AddListener(OnScreenShakeChanged);
        }

        if (tiltToggle)
        {
            tiltToggle.onValueChanged.RemoveAllListeners();
            tiltToggle.onValueChanged.AddListener(OnTiltChanged);
        }

        if (highFrameRateToggle)
        {
            var row = highFrameRateRow ? highFrameRateRow : highFrameRateToggle.gameObject;
            row.SetActive(GameManager.SupportsHighFrameRate);
            highFrameRateToggle.onValueChanged.RemoveAllListeners();
            highFrameRateToggle.onValueChanged.AddListener(OnHighFrameRateChanged);
        }

        if (languageDropdown)
        {
            languageDropdown.onValueChanged.RemoveAllListeners();
            languageDropdown.onValueChanged.AddListener(OnLanguagePicked);
        }

        if (resetProgressButton)
        {
            resetProgressButton.onClick.RemoveAllListeners();
            resetProgressButton.onClick.AddListener(OnResetProgressPressed);
        }
    }

    private void Update()
    {
        if (_resetConfirmExpiry <= 0f) return;

        // Unscaled, the window sets the global time scale to zero while it is open
        if (Time.unscaledTime < _resetConfirmExpiry) return;

        DisarmReset();
    }

    /// <summary>
    /// Two presses on one button rather than a separate confirm dialog, which keeps a destructive
    /// action behind a deliberate second tap without another prefab to build and wire.
    /// </summary>
    private void OnResetProgressPressed()
    {
        if (_resetConfirmExpiry <= 0f)
        {
            _resetConfirmExpiry = Time.unscaledTime + resetConfirmTimeout;
            if (resetProgressLabel) resetProgressLabel.text = L10n.Get("settings.reset.confirm");
            return;
        }

        DisarmReset();

        SaveManager.Instance?.ResetProgress();
        CameraManager.Instance?.ShakeCamera(0.5f);
    }

    private void DisarmReset()
    {
        _resetConfirmExpiry = 0f;
        if (resetProgressLabel) resetProgressLabel.text = L10n.Get("settings.reset");
    }

    private void PullValuesFromSettings()
    {
        var settings = SaveManager.Instance ? SaveManager.Instance.Settings : null;
        if (settings == null) return;

        // Guarded, assigning a slider value raises onValueChanged and would write straight back
        _applyingValues = true;

        if (musicVolumeSlider) musicVolumeSlider.SetValueWithoutNotify(settings.musicVolume);
        if (sfxVolumeSlider) sfxVolumeSlider.SetValueWithoutNotify(settings.sfxVolume);
        if (hapticsToggle) hapticsToggle.SetIsOnWithoutNotify(settings.hapticsEnabled);
        if (rumbleToggle) rumbleToggle.SetIsOnWithoutNotify(settings.rumbleEnabled);
        if (screenShakeToggle) screenShakeToggle.SetIsOnWithoutNotify(settings.screenShakeEnabled);
        if (tiltToggle) tiltToggle.SetIsOnWithoutNotify(settings.tiltEnabled);
        if (highFrameRateToggle) highFrameRateToggle.SetIsOnWithoutNotify(settings.highFrameRate);

        _applyingValues = false;
    }

    private void OnMusicVolumeChanged(float value)
    {
        if (_applyingValues) return;

        AudioManager.Instance?.SetMusicVolume(value);
        if (SaveManager.Instance) SaveManager.Instance.Settings.musicVolume = value;
    }

    private void OnSfxVolumeChanged(float value)
    {
        if (_applyingValues) return;

        AudioManager.Instance?.SetSfxVolume(value);
        if (SaveManager.Instance) SaveManager.Instance.Settings.sfxVolume = value;
    }

    private void OnHapticsChanged(bool value)
    {
        if (_applyingValues) return;

        if (SaveManager.Instance) SaveManager.Instance.Settings.hapticsEnabled = value;
        if (value) MobileHaptics.Vibrate(50);
    }

    private void OnRumbleChanged(bool value)
    {
        if (_applyingValues) return;

        if (SaveManager.Instance) SaveManager.Instance.Settings.rumbleEnabled = value;
        if (value) MobileHaptics.Play(Haptic.ComboFull);
    }

    private void OnScreenShakeChanged(bool value)
    {
        if (_applyingValues) return;

        if (SaveManager.Instance) SaveManager.Instance.Settings.screenShakeEnabled = value;
        if (value) CameraManager.Instance?.ShakeCamera(0.2f);
    }

    private void OnTiltChanged(bool value)
    {
        if (_applyingValues) return;

        if (SaveManager.Instance) SaveManager.Instance.Settings.tiltEnabled = value;
    }

    private void OnHighFrameRateChanged(bool value)
    {
        if (_applyingValues) return;

        if (SaveManager.Instance) SaveManager.Instance.Settings.highFrameRate = value;
        GameManager.ApplyFrameRate();
    }

    private void OnBackgroundClicked(PointerEventData eventData)
    {
        var title = titleText ? titleText.rectTransform : null;
        if (!PointerClickRelay.IsOutside(eventData, windowRectTransform, title, _backButtonRectTransform)) return;
        if (!PointerClickRelay.Press(backButton, eventData)) Close();
    }

    private void Close()
    {
        CameraManager.Instance?.ShakeCamera(0.1f);
        DisarmReset();

        // Written once on close rather than on every slider frame, which would hit the disk continuously
        SaveManager.Instance?.Save();

        Toggle(false);

        var onClosed = _onClosed;
        _onClosed = null;
        onClosed?.Invoke();
    }

    public void Toggle(bool show)
    {
        if (!_canvasGroup || !windowRectTransform) return;

        _toggleSequence.Stop();

        var startSize = show ? new Vector2(windowRectTransform.sizeDelta.x, 0f) : _defaultSize;
        var endSize = show ? _defaultSize : new Vector2(windowRectTransform.sizeDelta.x, 0f);
        var titleStartAlpha = show ? 0f : 1f;
        var titleEndAlpha = show ? 1f : 0f;
        var backgroundStartAlpha = show ? 0f : _backgroundStartAlpha;
        var backgroundEndAlpha = show ? _backgroundStartAlpha : 0f;
        var endTimeScale = show ? 0f : 1f;

        windowRectTransform.sizeDelta = startSize;
        if (titleText) titleText.alpha = titleStartAlpha;
        if (_buttonsGroup) _buttonsGroup.alpha = titleStartAlpha;
        if (backgroundImage) backgroundImage.color = new Color(backgroundImage.color.r, backgroundImage.color.g, backgroundImage.color.b, backgroundStartAlpha);

        if (show) _canvasGroup.alpha = 1f;
        _canvasGroup.blocksRaycasts = show;
        if (!show) _canvasGroup.interactable = false;
        if (!show) GameManager.Instance?.TogglePause(false, false);

        _toggleSequence = Sequence.Create(useUnscaledTime: true)
            .Group(Tween.UISizeDelta(windowRectTransform, endSize, windowTweenSettings));
        if (!Mathf.Approximately(Time.timeScale, endTimeScale)) _toggleSequence.Group(Tween.GlobalTimeScale(endTimeScale, windowTweenSettings));

        if (titleText) _toggleSequence.Group(Tween.Alpha(titleText, titleEndAlpha, windowTweenSettings.duration * 0.8f));
        if (_buttonsGroup) _toggleSequence.Group(Tween.Alpha(_buttonsGroup, titleEndAlpha, windowTweenSettings.duration * 0.8f));
        if (backgroundImage) _toggleSequence.Group(Tween.Alpha(backgroundImage, backgroundEndAlpha, windowTweenSettings.duration * 0.8f));

        _toggleSequence.ChainCallback(() =>
        {
            if (show) GameManager.Instance?.TogglePause(true, false);
            if (!show) _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = show;
            _canvasGroup.blocksRaycasts = show;
        });
    }
}
