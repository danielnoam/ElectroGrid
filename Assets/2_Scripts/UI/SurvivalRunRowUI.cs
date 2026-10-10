using System.Globalization;
using TMPro;
using UnityEngine;

/// <summary>One line of the Survival best runs list.</summary>
public class SurvivalRunRowUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI rankText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private TextMeshProUGUI dateText;

    public void Set(int rank, SurvivalRunRecord run, CultureInfo culture)
    {
        culture ??= CultureInfo.CurrentCulture;
        int seconds = Mathf.FloorToInt(run.timePlayed);

        if (rankText) rankText.text = rank.ToString(culture);
        if (scoreText) scoreText.text = run.score.ToString("N0", culture);
        if (timeText) timeText.text = $"{seconds / 60:00}:{seconds % 60:00}";
        if (dateText) dateText.text = run.PlayedAt.ToString("d", culture);
    }
}
