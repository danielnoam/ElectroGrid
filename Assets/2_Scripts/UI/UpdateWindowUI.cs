using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Offers the update <see cref="GameUpdater"/> found, then walks through download and install in place. Built from
/// the settings window so it opens and closes the same way, pausing the game while it is up.
/// </summary>
public class UpdateWindowUI : MonoBehaviour
{
    private const int MaxNotesLength = 700;

    [Header("Settings")]
    [SerializeField] private TweenSettings windowTweenSettings;
    [Tooltip("Wait after the menu opens before offering the update, so it does not cover the intro")]
    [SerializeField, Min(0f)] private float offerDelay = 1.5f;

    [Header("Content")]
    [SerializeField] private TextMeshProUGUI bodyText;
    [SerializeField] private Slider progressSlider;
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("References")]
    [SerializeField] private Button laterButton;
    [SerializeField] private Button actionButton;
    [SerializeField] private TextMeshProUGUI actionButtonLabel;
    [SerializeField] private RectTransform buttonsRectTransform;
    [SerializeField] private RectTransform windowRectTransform;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Image backgroundImage;

    private CanvasGroup _canvasGroup;
    private Vector2 _defaultSize;
    private Sequence _toggleSequence;
    private float _buttonsDefaultYPosition;
    private float _backgroundStartAlpha;
    private bool _isOpen;
    private bool _offered;
    private float _offerTime;

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();

        if (buttonsRectTransform && windowRectTransform)
        {
            _buttonsDefaultYPosition = buttonsRectTransform.anchoredPosition.y;
            buttonsRectTransform.anchoredPosition = new Vector2(buttonsRectTransform.anchoredPosition.x, -windowRectTransform.sizeDelta.y);
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

        if (laterButton)
        {
            laterButton.onClick.RemoveAllListeners();
            laterButton.onClick.AddListener(Close);
        }

        if (actionButton)
        {
            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(OnActionPressed);
        }
    }

    private void Start()
    {
        _offerTime = Time.unscaledTime + offerDelay;
        if (GameUpdater.Instance) GameUpdater.Instance.StateChanged += Refresh;
    }

    private void OnDestroy()
    {
        _toggleSequence.Stop();
        if (GameUpdater.Instance) GameUpdater.Instance.StateChanged -= Refresh;
    }

    private void Update()
    {
        var updater = GameUpdater.Instance;
        if (!updater || _offered || _isOpen) return;
        if (updater.CurrentState != GameUpdater.State.UpdateAvailable || Time.unscaledTime < _offerTime) return;

        // Offered once per launch; Later means ask again next time the game starts
        _offered = true;
        Refresh();
        Toggle(true);
    }

    private void Refresh()
    {
        var updater = GameUpdater.Instance;
        if (!updater || !_isOpen && updater.CurrentState != GameUpdater.State.UpdateAvailable) return;

        string version = updater.LatestVersion;
        bool canInstall = GameUpdater.CanInstallOnThisPlatform && updater.PlatformAsset != null;

        if (titleText) titleText.text = "Update Available";
        if (bodyText) bodyText.text = $"Version {version} is out. You have {Application.version}.\n\n{Notes(updater.LatestRelease?.body)}";

        bool downloading = updater.CurrentState == GameUpdater.State.Downloading;
        if (progressSlider)
        {
            progressSlider.gameObject.SetActive(downloading || updater.CurrentState == GameUpdater.State.ReadyToInstall);
            progressSlider.SetValueWithoutNotify(updater.DownloadProgress);
        }

        switch (updater.CurrentState)
        {
            case GameUpdater.State.UpdateAvailable:
                SetStatus(canInstall ? $"Download size {FormatSize(updater.PlatformAsset.size)}." : "Get it from the release page.");
                SetAction(canInstall ? "Update" : "Open Page", true);
                break;

            case GameUpdater.State.Downloading:
                long total = updater.PlatformAsset?.size ?? 0;
                SetStatus(total > 0 ? $"Downloading {FormatSize((long)(total * updater.DownloadProgress))} of {FormatSize(total)}" : "Downloading...");
                SetAction("Update", false);
                break;

            case GameUpdater.State.ReadyToInstall:
                SetStatus(Application.platform == RuntimePlatform.Android ? "Downloaded. Tap Install to finish." : "Downloaded. The game restarts to finish.");
                SetAction(Application.platform == RuntimePlatform.Android ? "Install" : "Restart", true);
                break;

            case GameUpdater.State.Failed:
                SetStatus(updater.ErrorMessage);
                SetAction("Open Page", true);
                break;
        }
    }

    private void OnActionPressed()
    {
        var updater = GameUpdater.Instance;
        if (!updater) return;

        CameraManager.Instance?.ShakeCamera(0.1f);

        bool canInstall = GameUpdater.CanInstallOnThisPlatform && updater.PlatformAsset != null;

        switch (updater.CurrentState)
        {
            case GameUpdater.State.UpdateAvailable when canInstall:
                updater.StartDownload();
                break;

            case GameUpdater.State.ReadyToInstall:
                var result = updater.Install();
                if (result.Outcome != UpdateInstaller.Outcome.Failed) SetStatus(result.Message);
                break;

            default:
                updater.OpenReleasePage();
                break;
        }
    }

    private void Close()
    {
        CameraManager.Instance?.ShakeCamera(0.1f);
        Toggle(false);
    }

    private void SetStatus(string message)
    {
        if (statusText) statusText.text = message;
    }

    private void SetAction(string label, bool interactable)
    {
        if (actionButtonLabel) actionButtonLabel.text = label;
        if (actionButton) actionButton.interactable = interactable;
    }

    /// <summary>Release notes are a commit list, which can be long; the window shows the start and the page has the rest.</summary>
    private static string Notes(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return string.Empty;

        body = body.Trim();
        return body.Length <= MaxNotesLength ? body : body.Substring(0, MaxNotesLength).TrimEnd() + "\n...";
    }

    private static string FormatSize(long bytes)
    {
        return bytes >= 1024 * 1024 ? $"{bytes / (1024f * 1024f):0.0} MB" : $"{bytes / 1024f:0} KB";
    }

    public void Toggle(bool show)
    {
        if (!_canvasGroup || !windowRectTransform) return;

        _isOpen = show;
        _toggleSequence.Stop();

        var startSize = show ? new Vector2(windowRectTransform.sizeDelta.x, 0f) : _defaultSize;
        var endSize = show ? _defaultSize : new Vector2(windowRectTransform.sizeDelta.x, 0f);
        var startYPosition = show ? -_defaultSize.y : _buttonsDefaultYPosition;
        var endYPosition = show ? _buttonsDefaultYPosition : -_defaultSize.y;
        var titleStartAlpha = show ? 0f : 1f;
        var titleEndAlpha = show ? 1f : 0f;
        var backgroundStartAlpha = show ? 0f : _backgroundStartAlpha;
        var backgroundEndAlpha = show ? _backgroundStartAlpha : 0f;
        var endTimeScale = show ? 0f : 1f;

        windowRectTransform.sizeDelta = startSize;
        if (buttonsRectTransform) buttonsRectTransform.anchoredPosition = new Vector2(buttonsRectTransform.anchoredPosition.x, startYPosition);
        if (titleText) titleText.alpha = titleStartAlpha;
        if (backgroundImage) backgroundImage.color = new Color(backgroundImage.color.r, backgroundImage.color.g, backgroundImage.color.b, backgroundStartAlpha);

        if (show) _canvasGroup.alpha = 1f;
        if (!show) GameManager.Instance?.TogglePause(false, false);

        _toggleSequence = Sequence.Create(useUnscaledTime: true)
            .Group(Tween.UISizeDelta(windowRectTransform, endSize, windowTweenSettings))
            .Group(Tween.GlobalTimeScale(endTimeScale, windowTweenSettings));

        if (titleText) _toggleSequence.Group(Tween.Alpha(titleText, titleEndAlpha, windowTweenSettings.duration * 0.8f));
        if (backgroundImage) _toggleSequence.Group(Tween.Alpha(backgroundImage, backgroundEndAlpha, windowTweenSettings.duration * 0.8f));
        if (buttonsRectTransform) _toggleSequence.Group(Tween.UIAnchoredPositionY(buttonsRectTransform, endYPosition, windowTweenSettings));

        _toggleSequence.ChainCallback(() =>
        {
            if (show) GameManager.Instance?.TogglePause(true, false);
            if (!show) _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = show;
            _canvasGroup.blocksRaycasts = show;
        });
    }
}
