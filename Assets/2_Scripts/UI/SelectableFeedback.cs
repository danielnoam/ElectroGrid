using DNExtensions.Systems.AudioLibrary;
using DNExtensions.Utilities;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Select and submit sounds for a Selectable, plus optional pointer-hover selection.
/// The DNExtensions SelectableAnimator used to own both; it is animation only since the package rework.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Selectable))]
public class SelectableFeedback : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool mouseSelectsSelectable;
    [SerializeField, AudioLibraryID] private string selectSfx;
    [SerializeField, AudioLibraryID] private string submitSfx;

    [Header("References")]
    [SerializeField, ReadOnly] private Selectable selectable;


    private void OnValidate()
    {
        if (!selectable) selectable = GetComponent<Selectable>();
    }

    private void Awake()
    {
        if (!selectable) selectable = GetComponent<Selectable>();

        selectable.OnSelect(OnSelect);
        selectable.OnSubmit(OnSubmit);
        selectable.OnPointerClick(OnSubmit);

        if (mouseSelectsSelectable)
        {
            selectable.OnPointerEnter(OnPointerEnter);
            selectable.OnPointerExit(OnPointerExit);
        }
    }

    private void OnSelect(BaseEventData eventData)
    {
        if (!eventData.selectedObject.activeSelf || !selectable.interactable) return;

        AudioLibrary.Play(selectSfx);
    }

    private void OnSubmit(BaseEventData eventData)
    {
        // EventTrigger delivers clicks even when the Selectable is not interactable
        if (!selectable.interactable) return;

        AudioLibrary.Play(submitSfx);
    }

    private void OnPointerEnter(BaseEventData eventData)
    {
        if (!selectable.interactable) return;

        if (eventData is PointerEventData pointerEventData)
        {
            pointerEventData.selectedObject = pointerEventData.pointerEnter;
        }
    }

    private void OnPointerExit(BaseEventData eventData)
    {
        if (!selectable.interactable) return;

        if (eventData is PointerEventData pointerEventData)
        {
            pointerEventData.selectedObject = null;
        }
    }
}
