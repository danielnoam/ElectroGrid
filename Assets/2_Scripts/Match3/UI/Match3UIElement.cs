using System;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class Match3UIElement : MonoBehaviour
{

    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private TextMeshProUGUI progressCount;
    [SerializeField] private TextMeshProUGUI amountCount;
    
    private Sequence _sequence;
    private Vector3 _startScale;
    private int _currentProgress;
    private int _totalAmount;
    private bool _stacked;

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
        if (!progressText) return;
        progressText.transform.SetSiblingIndex(L10n.IsRightToLeft && !_stacked ? transform.childCount - 1 : 0);
    }

    private void Update()
    {
        progressCount.text = $"{_currentProgress}";
        amountCount.text = $"/{_totalAmount}";
    }

    /// <summary>Label on top, a big number under it. For the moves or time card in the top bar.</summary>
    public void UseStackedLayout(float labelMaxSize = 34f, float numberMaxSize = 110f)
    {
        _stacked = true;

        var row = GetComponent<HorizontalLayoutGroup>();
        if (row) DestroyImmediate(row);

        var column = GetComponent<VerticalLayoutGroup>() ?? gameObject.AddComponent<VerticalLayoutGroup>();
        column.childAlignment = TextAnchor.MiddleCenter;
        column.childControlWidth = true;
        column.childControlHeight = true;
        column.childForceExpandWidth = true;
        column.childForceExpandHeight = false;
        column.spacing = 0f;

        if (progressText)
        {
            progressText.alignment = TextAlignmentOptions.Center;
            progressText.fontSizeMax = labelMaxSize;
            var labelElement = progressText.GetComponent<LayoutElement>() ?? progressText.gameObject.AddComponent<LayoutElement>();
            labelElement.preferredHeight = labelMaxSize * 1.2f;
            labelElement.flexibleHeight = 0f;
        }

        if (progressCount)
        {
            progressCount.alignment = TextAlignmentOptions.Center;
            progressCount.fontSizeMax = numberMaxSize;
            var numberElement = progressCount.GetComponent<LayoutElement>() ?? progressCount.gameObject.AddComponent<LayoutElement>();
            numberElement.flexibleHeight = 1f;
            numberElement.flexibleWidth = 1f;
        }

        OrderForLanguage();
    }

    /// <summary>Gives the counter room next to its label, for a goal sharing a narrow card with other goals.</summary>
    public void UseCompactRow(float counterMinWidth = 120f, float textMaxSize = 46f)
    {
        var row = GetComponent<HorizontalLayoutGroup>();
        if (row) row.padding = new RectOffset(12, 12, 4, 4);

        foreach (var text in new[] { progressText, progressCount, amountCount })
        {
            if (text) text.fontSizeMax = textMaxSize;
        }

        foreach (var counter in new[] { progressCount, amountCount })
        {
            if (!counter) continue;
            var element = counter.GetComponent<LayoutElement>() ?? counter.gameObject.AddComponent<LayoutElement>();
            element.minWidth = counterMinWidth * 0.5f;
        }
    }

    public void SetLabel(string text)
    {
        if (progressText) progressText.text = text;
    }

    public void Setup(string text)
    {
        progressCount.gameObject.SetActive(false);
        amountCount.gameObject.SetActive(false);
        
        
        if (progressText)
        {
            progressText.text = $"{text}";
        }
    }
    
    public void Setup(string text, int progress)
    {
        amountCount.gameObject.SetActive(false);

        _currentProgress = progress;
        
        if (progressText)
        {
            progressText.text = $"{text}";
        }
    }

    public void Setup(string text, (int, int) progress)
    {
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
            progressCount.transform.localScale = _startScale;
        }
        var changeAmount = progress.Item1 - _currentProgress;
        
        _sequence = Sequence.Create();
        _sequence.Group(Tween.Custom(_currentProgress,
            endValue: progress.Item1,
            duration: Mathf.Clamp(0.1f * changeAmount, 0.1f, 0.6f),
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
            progressCount.transform.localScale = _startScale;
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