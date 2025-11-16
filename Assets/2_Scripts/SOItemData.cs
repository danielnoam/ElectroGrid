using DNExtensions;
using UnityEngine;

[CreateAssetMenu(fileName = "New Item Data", menuName = "Scriptable Objects/Item Data")]
public class SOItemData : ScriptableObject
{

    [Header("Item Data")]
    [SerializeField] private string label = "New Item";
    [SerializeField] private Color color = Color.white;
    [SerializeField] private Sprite sprite;
    [SerializeField] private Texture2D emissionMask;

    public string Label => label;
    public Sprite Sprite => sprite;
    public Texture2D EmissionMask => emissionMask;
    public Color Color => color;

}