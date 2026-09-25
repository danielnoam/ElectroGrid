using UnityEngine;

/// <summary>
/// Keeps edge-anchored UI clear of notches, camera cutouts and the home indicator. Unlike insetting a whole
/// container by the safe area, it only moves the element when the unsafe area actually reaches it: the distance
/// from the edge becomes the larger of the designed margin and the inset plus a gap, so phones without a cutout,
/// and UI that already sits far enough in, are left exactly as designed.
/// </summary>
/// <remarks>
/// Assumes the parent's edge is the screen edge, which holds for full-screen menu screens and canvas children,
/// and that the pivot sits on the anchored edge. Do not use on a rect whose anchored position is animated; call
/// <see cref="ResolveDistance"/> from the animating script instead.
/// </remarks>
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class SafeAreaMargin : MonoBehaviour
{
    public enum Edge
    {
        Top,
        Bottom
    }

    [SerializeField] private Edge edge = Edge.Bottom;
    [Tooltip("How far inside this rect its visible content starts, for a container whose children carry their own margin")]
    [SerializeField, Min(0f)] private float contentClearance;
    [Tooltip("Room kept between the content and the unsafe area, in canvas units")]
    [SerializeField, Min(0f)] private float gap = DefaultGap;

    public const float DefaultGap = 16f;

    private RectTransform _rectTransform;
    private float _designedDistance;
    private Rect _appliedSafeArea;
    private Vector2 _appliedCanvasSize;

    private void Awake()
    {
        _rectTransform = (RectTransform)transform;
        _designedDistance = Mathf.Abs(_rectTransform.anchoredPosition.y);
    }

    // Checked every frame rather than once: the canvas scaler settles after Awake, and the safe area changes
    // with rotation, resolution and the Device Simulator
    private void LateUpdate()
    {
        var canvasRect = GetCanvasRect(_rectTransform);
        if (!canvasRect) return;

        Rect safeArea = Screen.safeArea;
        Vector2 canvasSize = canvasRect.rect.size;
        if (safeArea == _appliedSafeArea && canvasSize == _appliedCanvasSize) return;

        _appliedSafeArea = safeArea;
        _appliedCanvasSize = canvasSize;

        float distance = ResolveDistance(_rectTransform, edge, _designedDistance, contentClearance, gap);
        float y = edge == Edge.Top ? -distance : distance;
        _rectTransform.anchoredPosition = new Vector2(_rectTransform.anchoredPosition.x, y);
    }

    /// <summary>
    /// The distance from the edge an element should sit at: its designed distance, or further in if the device's
    /// unsafe area on that edge would otherwise reach its content.
    /// </summary>
    public static float ResolveDistance(RectTransform rect, Edge edge, float designedDistance, float contentClearance = 0f, float gap = DefaultGap)
    {
        float inset = GetInset(rect, edge);
        if (inset <= 0f) return designedDistance;

        return Mathf.Max(designedDistance, inset + gap - contentClearance);
    }

    /// <summary>The unsafe area on one edge of the screen, in the canvas units the rect is laid out in.</summary>
    public static float GetInset(RectTransform rect, Edge edge)
    {
        var canvasRect = GetCanvasRect(rect);
        if (!canvasRect || Screen.height <= 0) return 0f;

        Rect safeArea = Screen.safeArea;
        float insetPixels = edge == Edge.Top ? Screen.height - safeArea.yMax : safeArea.yMin;
        float unitsPerPixel = canvasRect.rect.height / Screen.height;

        return Mathf.Max(0f, insetPixels * unitsPerPixel);
    }

    private static RectTransform GetCanvasRect(RectTransform rect)
    {
        var canvas = rect ? rect.GetComponentInParent<Canvas>() : null;
        return canvas ? (RectTransform)canvas.rootCanvas.transform : null;
    }
}
