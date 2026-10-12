using UnityEngine;

public static class WindowButtons
{
    public static CanvasGroup FadeGroup(Component buttonInRow)
    {
        if (!buttonInRow || !buttonInRow.transform.parent) return null;

        var row = buttonInRow.transform.parent;
        var group = row.GetComponent<CanvasGroup>();
        if (!group) group = row.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        return group;
    }
}
