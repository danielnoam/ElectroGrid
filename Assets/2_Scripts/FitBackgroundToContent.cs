using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class FitBackgroundToContent : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private RectTransform contentRect;
    
    [Header("Settings")]
    [SerializeField] private float horizontalPadding = 20f;
    [SerializeField] private float verticalPadding = 20f;
    [SerializeField] private bool updateInEditor = true;
    [SerializeField] private bool autoUpdateAtRuntime = true;

    private RectTransform _rectTransform;
    private bool _needsUpdate;
    private bool _isUpdating;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
    }

    private void Start()
    {
        UpdateSize();
    }

    private void OnEnable()
    {
        _needsUpdate = true;
    }

    private void OnValidate()
    {
#if UNITY_EDITOR
        if (updateInEditor && !_isUpdating)
        {
            _rectTransform = GetComponent<RectTransform>();
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null)
                {
                    UpdateSize();
                }
            };
        }
#endif
    }

    private void LateUpdate()
    {
        if (!Application.isPlaying && updateInEditor)
        {
            UpdateSize();
        }
        else if (Application.isPlaying && autoUpdateAtRuntime && _needsUpdate)
        {
            UpdateSize();
        }
    }

    public void ForceUpdate()
    {
        _needsUpdate = true;
        if (contentRect)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        }
        UpdateSize();
    }

    private void UpdateSize()
    {
        if (!contentRect || !_rectTransform || _isUpdating) return;

        _isUpdating = true;

        if (Application.isPlaying)
        {
            Canvas.ForceUpdateCanvases();
        }

        _rectTransform.anchorMin = contentRect.anchorMin;
        _rectTransform.anchorMax = contentRect.anchorMax;
        _rectTransform.anchoredPosition = contentRect.anchoredPosition;

        _rectTransform.offsetMin = contentRect.offsetMin - new Vector2(horizontalPadding, verticalPadding);
        _rectTransform.offsetMax = contentRect.offsetMax + new Vector2(horizontalPadding, verticalPadding);

        _needsUpdate = false;
        _isUpdating = false;
    }
}