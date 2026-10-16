using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Links a list of selectables top to bottom for controller navigation, skipping any that are hidden or not
/// interactable. Left and right are left empty, so sliders keep using them to change their value.
/// </summary>
public class NavigationChain : MonoBehaviour
{
    [SerializeField] private List<Selectable> selectables = new List<Selectable>();
    [Tooltip("Down from the last item goes to the first, and up from the first to the last")]
    [SerializeField] private bool wrap;

    private readonly List<Selectable> _usable = new List<Selectable>();

    private void LateUpdate()
    {
        _usable.Clear();
        foreach (var selectable in selectables)
        {
            if (selectable && selectable.isActiveAndEnabled && selectable.IsInteractable()) _usable.Add(selectable);
        }

        for (int i = 0; i < _usable.Count; i++)
        {
            var navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnUp = i > 0 ? _usable[i - 1] : wrap ? _usable[^1] : null,
                selectOnDown = i < _usable.Count - 1 ? _usable[i + 1] : wrap ? _usable[0] : null
            };
            if (!NavigationEquals(_usable[i].navigation, navigation)) _usable[i].navigation = navigation;
        }
    }

    private static bool NavigationEquals(Navigation a, Navigation b)
    {
        return a.mode == b.mode && a.selectOnUp == b.selectOnUp && a.selectOnDown == b.selectOnDown && a.selectOnLeft == b.selectOnLeft && a.selectOnRight == b.selectOnRight;
    }
}
