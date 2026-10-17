using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Match3TutorialUIElement : MonoBehaviour
{
    [SerializeField] private Image image;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Color accentColor = new Color(1f, 0.031f, 0.38f, 1f);

    public void Setup(Sprite sprite, string title, string description, IEnumerable<string> highlightTerms = null)
    {
        if (image)
        {
            image.gameObject.SetActive(sprite);
            if (sprite) image.sprite = sprite;
        }

        if (titleText)
        {
            titleText.text = title;
            titleText.color = accentColor;
        }

        if (descriptionText)
        {
            descriptionText.text = Highlight(description, highlightTerms?.Where(term => !string.Equals(term, title, System.StringComparison.OrdinalIgnoreCase)));
        }
    }

    private string Highlight(string text, IEnumerable<string> terms)
    {
        if (string.IsNullOrEmpty(text)) return text;

        var patterns = (terms ?? Enumerable.Empty<string>())
            .Where(term => !string.IsNullOrWhiteSpace(term))
            .Distinct()
            .OrderByDescending(term => term.Length)
            .Select(term => $@"(?<!\w){Regex.Escape(term)}(?!\w)")
            .Append(@"\d+");

        string hex = ColorUtility.ToHtmlStringRGB(accentColor);
        return Regex.Replace(text, string.Join("|", patterns), match => $"<color=#{hex}>{match.Value}</color>", RegexOptions.IgnoreCase);
    }
}
