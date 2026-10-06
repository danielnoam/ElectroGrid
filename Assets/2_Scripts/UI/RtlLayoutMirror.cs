using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Mirrors the UI under this object left to right while the language reads right to left, and back again when it
/// does not. Sits on each canvas. Free-standing rects flip their anchors, pivot and position, layout groups reverse
/// and swap their padding, sliders and horizontal fills run the other way. Rects placed by a layout group are left
/// to the group. Text alignment is handled by RTLTextMeshPro, which flips it per string.
/// </summary>
/// <remarks>
/// Every flip is its own inverse, so switching back applies the same flip again. UI created after the switch, such
/// as top bar counters or level buttons, is picked up on the next frame. Anything under an <see cref="RtlMirrorIgnore"/>
/// keeps its layout, for content whose left and right mean something, like the level shape preview.
/// </remarks>
[DisallowMultipleComponent]
public class RtlLayoutMirror : MonoBehaviour
{
    private readonly HashSet<Object> _mirrored = new HashSet<Object>();
    private readonly List<RectTransform> _rects = new List<RectTransform>();
    private bool _rtl;

    private void OnEnable()
    {
        L10n.LanguageChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        L10n.LanguageChanged -= Refresh;
    }

    private void LateUpdate()
    {
        if (_rtl) Sweep();
    }

    private void Refresh()
    {
        _rtl = L10n.IsRightToLeft;
        if (_rtl) Sweep();
        else Restore();
    }

    private void Sweep()
    {
        GetComponentsInChildren(true, _rects);

        // Destroyed UI, like the previous level's counters, would otherwise pile up here
        if (_mirrored.Count > _rects.Count * 2) _mirrored.RemoveWhere(target => !target);

        foreach (var rect in _rects)
        {
            if (rect == transform || _mirrored.Contains(rect)) continue;
            if (!Owns(rect)) continue;

            Mirror(rect);
            _mirrored.Add(rect);
        }
    }

    private void Restore()
    {
        foreach (var target in _mirrored)
            if (target is RectTransform rect && rect) Mirror(rect);
        _mirrored.Clear();
    }

    // A nested mirror or an ignore marker takes over everything below it
    private bool Owns(Transform target)
    {
        for (var t = target; t && t != transform; t = t.parent)
        {
            if (t.GetComponent<RtlMirrorIgnore>()) return false;
            if (t != target && t.GetComponent<RtlLayoutMirror>()) return false;
        }
        return true;
    }

    private static void Mirror(RectTransform rect)
    {
        if (!IsPlacedByLayoutGroup(rect))
        {
            var min = rect.anchorMin;
            var max = rect.anchorMax;
            rect.anchorMin = new Vector2(1f - max.x, min.y);
            rect.anchorMax = new Vector2(1f - min.x, max.y);
            rect.pivot = new Vector2(1f - rect.pivot.x, rect.pivot.y);
            rect.anchoredPosition = new Vector2(-rect.anchoredPosition.x, rect.anchoredPosition.y);
        }

        if (rect.TryGetComponent(out LayoutGroup group)) MirrorGroup(group);

        if (rect.TryGetComponent(out Slider slider))
        {
            if (slider.direction == Slider.Direction.LeftToRight) slider.SetDirection(Slider.Direction.RightToLeft, false);
            else if (slider.direction == Slider.Direction.RightToLeft) slider.SetDirection(Slider.Direction.LeftToRight, false);
        }

        if (rect.TryGetComponent(out Image image) && image.type == Image.Type.Filled && image.fillMethod == Image.FillMethod.Horizontal)
            image.fillOrigin = image.fillOrigin == (int)Image.OriginHorizontal.Left ? (int)Image.OriginHorizontal.Right : (int)Image.OriginHorizontal.Left;
    }

    private static bool IsPlacedByLayoutGroup(RectTransform rect)
    {
        var parent = rect.parent;
        if (!parent || !parent.TryGetComponent(out LayoutGroup group) || !group.enabled) return false;
        return !rect.TryGetComponent(out LayoutElement element) || !element.ignoreLayout;
    }

    private static void MirrorGroup(LayoutGroup group)
    {
        var padding = group.padding;
        group.padding = new RectOffset(padding.right, padding.left, padding.top, padding.bottom);
        group.childAlignment = MirrorAnchor(group.childAlignment);

        switch (group)
        {
            case HorizontalLayoutGroup horizontal:
                horizontal.reverseArrangement = !horizontal.reverseArrangement;
                break;
            case GridLayoutGroup grid:
                grid.startCorner = grid.startCorner switch
                {
                    GridLayoutGroup.Corner.UpperLeft => GridLayoutGroup.Corner.UpperRight,
                    GridLayoutGroup.Corner.UpperRight => GridLayoutGroup.Corner.UpperLeft,
                    GridLayoutGroup.Corner.LowerLeft => GridLayoutGroup.Corner.LowerRight,
                    _ => GridLayoutGroup.Corner.LowerLeft
                };
                break;
        }
    }

    private static TextAnchor MirrorAnchor(TextAnchor anchor)
    {
        return anchor switch
        {
            TextAnchor.UpperLeft => TextAnchor.UpperRight,
            TextAnchor.UpperRight => TextAnchor.UpperLeft,
            TextAnchor.MiddleLeft => TextAnchor.MiddleRight,
            TextAnchor.MiddleRight => TextAnchor.MiddleLeft,
            TextAnchor.LowerLeft => TextAnchor.LowerRight,
            TextAnchor.LowerRight => TextAnchor.LowerLeft,
            _ => anchor
        };
    }
}
