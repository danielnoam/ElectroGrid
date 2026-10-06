using UnityEngine;
using UnityEngine.Localization;

[CreateAssetMenu(fileName = "New Item Data", menuName = "Scriptable Objects/Item Data")]
public class SOItemData : ScriptableObject
{

    [Header("Item Data")]
    [Tooltip("Internal name, used for object names in the hierarchy")]
    [SerializeField] private string label = "New Item";
    [Tooltip("The name players see, from the Content string table. Falls back to the label when empty")]
    [SerializeField] private LocalizedString displayName;
    [SerializeField] private Color color = Color.white;
    [SerializeField] private Sprite sprite;
    [SerializeField] private Texture2D emissionMask;

    public string Label => label;
    public string DisplayName => displayName.IsEmpty ? label : L10n.Get(displayName);
    public Sprite Sprite => sprite;
    public Texture2D EmissionMask => emissionMask;
    public Color Color => color;

}