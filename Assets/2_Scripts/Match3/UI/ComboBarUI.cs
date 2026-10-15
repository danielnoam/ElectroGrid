using DNExtensions.Systems.AudioLibrary;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows the combo next to the level title: a counter on one side, a bar that fills on the other, and a thin line under
/// the bar that drains as the window to the next match runs out. Also plays the combo's feel: a sound that rises in
/// pitch per step and a shake that grows with it. On levels with the combo bar it stays on screen while there is no combo,
/// empty and showing x1.
/// </summary>
public class ComboBarUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image fillImage;
    [SerializeField] private Image timerImage;
    [SerializeField] private TextMeshProUGUI countText;

    [Header("Look")]
    [SerializeField, Min(0.01f)] private float fadeSpeed = 6f;
    [SerializeField, Min(0.01f)] private float fillSpeed = 4f;

    [Header("Feel")]
    [SerializeField, AudioLibraryID] private string stepSfx;
    [SerializeField, AudioLibraryID] private string filledSfx;
    [Tooltip("Pitch added to the step sound for every step after the first")]
    [SerializeField, Range(0f, 0.3f)] private float pitchPerStep = 0.08f;
    [SerializeField, Range(1f, 3f)] private float maxPitch = 2f;
    [SerializeField, Range(0f, 1f)] private float baseShake = 0.1f;
    [SerializeField, Range(0f, 0.2f)] private float shakePerStep = 0.04f;
    [SerializeField, Range(0f, 1f)] private float maxShake = 0.5f;
    [SerializeField, Range(0f, 1f)] private float filledShake = 0.6f;

    private Match3Combo _combo;
    private float _shownFill;

    private void Start()
    {
        if (canvasGroup) canvasGroup.alpha = 0f;
        SetCount(0);
        if (!Match3GameManager.Instance) return;

        _combo = Match3GameManager.Instance.Combo;
        _combo.StepAdded += OnStepAdded;
        _combo.Filled += OnFilled;
        _combo.Reset += OnReset;
    }

    private void OnDestroy()
    {
        if (_combo == null) return;

        _combo.StepAdded -= OnStepAdded;
        _combo.Filled -= OnFilled;
        _combo.Reset -= OnReset;
    }

    private void Update()
    {
        if (_combo == null) return;

        // Unscaled, the bar keeps animating behind a pause window even though the combo itself is frozen
        float dt = Time.unscaledDeltaTime;
        bool comboOn = Match3GameManager.Instance && Match3GameManager.Instance.ComboEnabled;
        if (canvasGroup) canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, comboOn ? 1f : 0f, fadeSpeed * dt);

        _shownFill = Mathf.MoveTowards(_shownFill, _combo.Fill, fillSpeed * dt);
        if (fillImage) fillImage.fillAmount = _shownFill;
        if (timerImage) timerImage.fillAmount = _combo.Window > 0f ? Mathf.Clamp01(_combo.TimeLeft / _combo.Window) : 0f;
    }

    private void OnStepAdded(int count)
    {
        SetCount(count);

        AudioLibrary.Play(stepSfx, Mathf.Min(maxPitch, 1f + pitchPerStep * (count - 1)));
        CameraManager.Instance?.ShakeCamera(Mathf.Min(maxShake, baseShake + shakePerStep * (count - 1)));
    }

    private void OnFilled()
    {
        // Shown full for a moment before it empties, rather than snapping back
        _shownFill = 1f;
        AudioLibrary.Play(filledSfx);
        CameraManager.Instance?.ShakeCamera(filledShake);
    }

    private void OnReset(int reached)
    {
        _shownFill = 0f;
        SetCount(0);
    }

    private void SetCount(int count)
    {
        if (countText) countText.text = $"x{Mathf.Max(1, count)}";
    }
}
