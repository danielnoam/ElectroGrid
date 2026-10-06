using System;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class Match3UIElement : MonoBehaviour
{

    [SerializeField] private Image image;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private TextMeshProUGUI progressCount;
    [SerializeField] private TextMeshProUGUI amountCount;
    
    private Sequence _sequence;
    private Vector3 _startScale;
    private int _currentProgress;
    private int _totalAmount;

    private void Awake()
    {
        _startScale = progressCount.transform.localScale;
    }

    private void OnEnable()
    {
        L10n.LanguageChanged += OrderForLanguage;
        OrderForLanguage();
    }

    private void OnDisable()
    {
        L10n.LanguageChanged -= OrderForLanguage;
    }

    // The prefab carries an RtlMirrorIgnore: mirroring the whole row would also swap "12" and "/30". Instead the label
    // moves to the far side of the counter, and the counter keeps reading left to right
    private void OrderForLanguage()
    {
        if (progressText) progressText.transform.SetSiblingIndex(L10n.IsRightToLeft ? transform.childCount - 1 : 0);
    }

    private void Update()
    {
        progressCount.text = $"{_currentProgress}";
        amountCount.text = $"/{_totalAmount}";
    }

    public void SetLabel(string text)
    {
        if (progressText) progressText.text = text;
    }

    public void Setup(Sprite sprite, string text)
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
        
        progressCount.gameObject.SetActive(false);
        amountCount.gameObject.SetActive(false);
        
        
        if (progressText)
        {
            progressText.text = $"{text}";
        }
    }
    
    public void Setup(Sprite sprite, string text, int progress)
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
        amountCount.gameObject.SetActive(false);

        _currentProgress = progress;
        
        if (progressText)
        {
            progressText.text = $"{text}";
        }
    }

    public void Setup(Sprite sprite, string text, (int, int) progress)
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

        _currentProgress = progress.Item1;
        _totalAmount = progress.Item2;
        
        
        if (progressText)
        {
            progressText.text = $"{text}";
        }
    }
    


    public void UpdateProgress((int, int) progress)
    {
        if (!progressCount) return;

        if (_sequence.isAlive)
        {
            _sequence.Stop();
        }
        var changeAmount = progress.Item1 - _currentProgress;
        
        _sequence = Sequence.Create();
        _sequence.Group(Tween.Custom(_currentProgress,
            endValue: progress.Item1,
            duration: 0.1f * changeAmount,
            onValueChange: value => _currentProgress = Mathf.RoundToInt(value)));
        _sequence.Group(Tween.Scale(progressCount.transform, _startScale * 1.3f, 0.1f, Ease.InSine));
        _sequence.Chain(Tween.Scale(progressCount.transform, _startScale, 0.1f, Ease.OutSine));
    }
    
    public void UpdateProgress(int progress)
    {
        if (!progressCount) return;

        if (_sequence.isAlive)
        {
            _sequence.Stop();
        }
        
        _sequence = Sequence.Create();
        _sequence.Group(Tween.Custom(_currentProgress,
            endValue: progress,
            duration: 0.1f,
            onValueChange: value => _currentProgress = Mathf.RoundToInt(value)));
        _sequence.Group(Tween.Scale(progressCount.transform, _startScale * 1.3f, 0.1f, Ease.InSine));
        _sequence.Chain(Tween.Scale(progressCount.transform, _startScale, 0.1f, Ease.OutSine));
    }
}