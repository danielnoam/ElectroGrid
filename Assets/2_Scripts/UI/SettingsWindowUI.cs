using System;
using PrimeTween;
using TMPro;
using UnityEngine;
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
    [SerializeField] private Toggle screenShakeToggle;

    [Header("Reset Progress")]
    [SerializeField] private Button resetProgressButton;
    [SerializeField] private TextMeshProUGUI resetProgressLabel;
    [SerializeField] private string resetLabel = "Reset Progress";
    [SerializeField] private string resetConfirmLabel = "Tap again to confirm";
    [Tooltip("How long the confirm state stays armed before reverting")]
    [SerializeField, Min(1f)] private float resetConfirmTimeout = 3f;

    [Header("References")]
    [SerializeField] private Button backButton;
    [SerializeField] private RectTransform windowRectTransform;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Image backgroundImage;

    private CanvasGroup _canvasGroup;
    private RectTransform _backButtonRectTransform;
    private Vector2 _defaultSize;
    private Sequence _toggleSequence;
    private float _backButtonDefaultYPosition;
    private float _backgroundStartAlpha;
    private bool _applyingValues;
    private Action _onClosed;
    private float _resetConfirmExpiry;

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();

        if (backButton)
        {
            _backButtonRectTransform = backButton.GetComponent<RectTransform>();
            _backButtonDefaultYPosition = _backButtonRectTransform.anchoredPosition.y;
            _backButtonRectTransform.anchoredPosition = new Vector2(_backButtonRectTransform.anchoredPosition.x, -windowRectTransform.sizeDelta.y);
        }

        if (windowRectTransform)
        {
            _defaultSize = windowRectTransform.sizeDelta;
            windowRectTransform.sizeDelta = new Vector2(_defaultSize.x, 0f);
        }

        if (titleText) titleText.alpha = 0f;

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
    }

    public void Show(Action onClosed = null)
    {
        _onClosed = onClosed;
        DisarmReset();
        PullValuesFromSettings();
        Toggle(true);
    }

    private void SetupControls()
    {
        if (backButton)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(Close);
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

        if (screenShakeToggle)
        {
            screenShakeToggle.onValueChanged.RemoveAllListeners();
            screenShakeToggle.onValueChanged.AddListener(OnScreenShakeChanged);
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
            if (resetProgressLabel) resetProgressLabel.text = resetConfirmLabel;
            return;
        }

        DisarmReset();

        SaveManager.Instance?.ResetProgress();
        CameraManager.Instance?.ShakeCamera(0.5f);
    }

    private void DisarmReset()
    {
        _resetConfirmExpiry = 0f;
        if (resetProgressLabel) resetProgressLabel.text = resetLabel;
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
        if (screenShakeToggle) screenShakeToggle.SetIsOnWithoutNotify(settings.screenShakeEnabled);

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

    private void OnScreenShakeChanged(bool value)
    {
        if (_applyingValues) return;

        if (SaveManager.Instance) SaveManager.Instance.Settings.screenShakeEnabled = value;
        if (value) CameraManager.Instance?.ShakeCamera(0.2f);
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
        var startYPosition = show ? -_defaultSize.y : _backButtonDefaultYPosition;
        var endYPosition = show ? _backButtonDefaultYPosition : -_defaultSize.y;
        var titleStartAlpha = show ? 0f : 1f;
        var titleEndAlpha = show ? 1f : 0f;
        var backgroundStartAlpha = show ? 0f : _backgroundStartAlpha;
        var backgroundEndAlpha = show ? _backgroundStartAlpha : 0f;
        var endTimeScale = show ? 0f : 1f;

        windowRectTransform.sizeDelta = startSize;
        if (_backButtonRectTransform) _backButtonRectTransform.anchoredPosition = new Vector2(_backButtonRectTransform.anchoredPosition.x, startYPosition);
        if (titleText) titleText.alpha = titleStartAlpha;
        if (backgroundImage) backgroundImage.color = new Color(backgroundImage.color.r, backgroundImage.color.g, backgroundImage.color.b, backgroundStartAlpha);

        if (show) _canvasGroup.alpha = 1f;
        if (!show) GameManager.Instance?.TogglePause(false, false);

        _toggleSequence = Sequence.Create(useUnscaledTime: true)
            .Group(Tween.UISizeDelta(windowRectTransform, endSize, windowTweenSettings))
            .Group(Tween.GlobalTimeScale(endTimeScale, windowTweenSettings));

        if (titleText) _toggleSequence.Group(Tween.Alpha(titleText, titleEndAlpha, windowTweenSettings.duration * 0.8f));
        if (backgroundImage) _toggleSequence.Group(Tween.Alpha(backgroundImage, backgroundEndAlpha, windowTweenSettings.duration * 0.8f));
        if (_backButtonRectTransform) _toggleSequence.Group(Tween.UIAnchoredPositionY(_backButtonRectTransform, endYPosition, windowTweenSettings));

        _toggleSequence.ChainCallback(() =>
        {
            if (show) GameManager.Instance?.TogglePause(true, false);
            if (!show) _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = show;
            _canvasGroup.blocksRaycasts = show;
        });
    }
}
