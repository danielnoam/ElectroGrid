using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Match3TutorialUIElement : MonoBehaviour
{
    [SerializeField] private Image image;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    
    public void Setup(Sprite sprite, string title, string description)
    {
        if (image && sprite)
        {
            image.sprite = sprite;
        }

        if (titleText)
        {
            titleText.text = title;
        }

        if (descriptionText)
        {
            descriptionText.text = description;
        }
    }
}