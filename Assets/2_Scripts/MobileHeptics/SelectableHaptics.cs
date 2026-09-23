using DNExtensions.Utilities;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SelectableHaptics : MonoBehaviour
{

    [Header("Settings")]
    [Tooltip("Duration of the haptic feedback in milliseconds")]
    [SerializeField, Min(1)] private long duration = 50;
    [SerializeField] private bool playOnSubmit = true;
    [SerializeField] private bool playOnSelect;
    [SerializeField] private bool playOnDeselect;

    [Header("References")]
    [SerializeField] private Selectable selectable;


    private void Awake()
    {
        if (!selectable) return;

        if (playOnSubmit)
        {
            selectable.OnSubmit(PlayHaptics);
            selectable.OnPointerClick(PlayHaptics);
        }
        if (playOnSelect) selectable.OnSelect(PlayHaptics);
        if (playOnDeselect) selectable.OnDeselect(PlayHaptics);
    }

    private void PlayHaptics(BaseEventData eventData)
    {
        if (!isActiveAndEnabled || !selectable.interactable) return;

        MobileHaptics.Vibrate(duration);
    }
}
