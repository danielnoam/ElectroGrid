using DNExtensions;
using UnityEngine;

[CreateAssetMenu(fileName = "New Item Data", menuName = "Scriptable Objects/Item Data")]
public class SOItemData : ScriptableObject
{

    [Header("Item Data")]
    [SerializeField] private string label = "New Item";
    [SerializeField, Min(0)] private int worth;
    [SerializeField] private ChanceList<Sprite> sprites;

    public string Label => label;
    public Sprite Sprite => sprites.GetRandomItem();
    public int Worth => worth;

}