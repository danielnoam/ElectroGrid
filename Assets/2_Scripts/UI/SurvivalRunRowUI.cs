using System.Globalization;
using PrimeTween;
using TMPro;
using UnityEngine;

/// <summary>One line of the Survival high score table: rank, score, time played. Empty slots show dashes.</summary>
public class SurvivalRunRowUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI rankText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private TextMeshProUGUI dateText;

    private Tween _blink;

    private void OnDisable()
    {
        _blink.Stop();
    }

    public void Set(int rank, SurvivalRunRecord run, CultureInfo culture, Color color, bool blink)
    {
        culture ??= CultureInfo.CurrentCulture;
        int seconds = Mathf.FloorToInt(run.timePlayed);

        SetTexts(rank.ToString(culture), run.score.ToString("N0", culture), $"{seconds / 60:00}:{seconds % 60:00}", color);
        if (dateText) dateText.text = run.PlayedAt.ToString("d", culture);

        _blink.Stop();
        SetAlpha(1f);
        if (blink) _blink = Tween.Custom(1f, 0.25f, 0.45f, SetAlpha, Ease.InOutSine, cycles: -1, cycleMode: CycleMode.Yoyo, useUnscaledTime: true);
    }

    public void SetEmpty(int rank, CultureInfo culture, Color color)
    {
        culture ??= CultureInfo.CurrentCulture;
        _blink.Stop();
        SetTexts(rank.ToString(culture), "-----", "--:--", color);
        if (dateText) dateText.text = string.Empty;
    }

    private void SetTexts(string rank, string score, string time, Color color)
    {
        if (rankText) { rankText.text = rank; rankText.color = color; }
        if (scoreText) { scoreText.text = score; scoreText.color = color; }
        if (timeText) { timeText.text = time; timeText.color = color; }
        if (dateText) dateText.color = color;
    }

    private void SetAlpha(float alpha)
    {
        if (rankText) rankText.alpha = alpha;
        if (scoreText) scoreText.alpha = alpha;
        if (timeText) timeText.alpha = alpha;
        if (dateText) dateText.alpha = alpha;
    }
}
