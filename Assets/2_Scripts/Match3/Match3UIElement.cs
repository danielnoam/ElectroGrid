using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class Match3UIElement : MonoBehaviour
{

    [SerializeField] private Image image;
    [SerializeField] private TextMeshProUGUI text;
    
    
    
    public void Setup(Sprite sprite, string progressText)
    {
        if (image)
        {
            if (!sprite)
            {
                image.gameObject.SetActive(false);
            }
            else
            {
                image.sprite = sprite;
            }
        }

        if (text)
        {
            text.text = progressText;
        }
    }

    public void UpdateProgress(string progressText)
    {
        if (text)
        {
            text.text = progressText;
        }
    }
}