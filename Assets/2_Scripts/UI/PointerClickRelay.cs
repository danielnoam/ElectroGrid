using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Raises <see cref="Clicked"/> when this graphic is tapped, for closing a window by tapping its backdrop.</summary>
public class PointerClickRelay : MonoBehaviour, IPointerClickHandler
{
    public event Action<PointerEventData> Clicked;

    public static PointerClickRelay On(Component target)
    {
        var relay = target.GetComponent<PointerClickRelay>();
        return relay ? relay : target.gameObject.AddComponent<PointerClickRelay>();
    }

    /// <summary>
    /// True when the tap landed outside every given rect. A backdrop also receives taps that fall through parts
    /// of the window that are not raycast targets, so the window's own area has to be excluded explicitly.
    /// </summary>
    public static bool IsOutside(PointerEventData eventData, params RectTransform[] rects)
    {
        foreach (var rect in rects)
        {
            if (rect && RectTransformUtility.RectangleContainsScreenPoint(rect, eventData.position, eventData.pressEventCamera))
                return false;
        }

        return true;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Clicked?.Invoke(eventData);
    }
}
