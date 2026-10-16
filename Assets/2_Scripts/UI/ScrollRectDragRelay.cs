using PrimeTween;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Passes drags and scroll wheel input on to the parent ScrollRect. Buttons with an EventTrigger otherwise swallow
/// every drag that starts on them, so a list made of buttons could not be scrolled by dragging a button.
/// With a controller, selecting a button scrolls the list so the button is in view.
/// </summary>
public class ScrollRectDragRelay : MonoBehaviour, IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler, ISelectHandler
{
    private const float SelectMargin = 90f;

    private ScrollRect _scrollRect;

    private ScrollRect ScrollRect => _scrollRect ? _scrollRect : _scrollRect = GetComponentInParent<ScrollRect>();

    public void OnInitializePotentialDrag(PointerEventData eventData)
    {
        if (ScrollRect) ScrollRect.OnInitializePotentialDrag(eventData);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (ScrollRect) ScrollRect.OnBeginDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (ScrollRect) ScrollRect.OnDrag(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (ScrollRect) ScrollRect.OnEndDrag(eventData);
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (ScrollRect) ScrollRect.OnScroll(eventData);
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (!ControllerFocus.ControllerActive || !ScrollRect || !ScrollRect.content) return;

        var viewport = ScrollRect.viewport ? ScrollRect.viewport : (RectTransform)ScrollRect.transform;
        var item = (RectTransform)transform;
        float center = viewport.InverseTransformPoint(item.position).y;
        float half = item.rect.height * 0.5f;

        float shift = 0f;
        if (center + half + SelectMargin > viewport.rect.yMax) shift = viewport.rect.yMax - (center + half + SelectMargin);
        else if (center - half - SelectMargin < viewport.rect.yMin) shift = viewport.rect.yMin - (center - half - SelectMargin);
        if (Mathf.Approximately(shift, 0f)) return;

        ScrollRect.StopMovement();
        var content = ScrollRect.content;
        Tween.UIAnchoredPosition(content, content.anchoredPosition + new Vector2(0f, shift), 0.2f, Ease.OutQuad);
    }
}
