using UnityEngine;

[CreateAssetMenu(fileName = "New Item Data", menuName = "Scriptable Objects/Item Data")]
public class SOItemData : ScriptableObject
{

    [Header("Item Data")]
    [SerializeField] private string label = "New Item";
    [SerializeField] private Sprite sprite;
    [SerializeField, Min(0)] private int worth;

    public string Label => label;
    public Sprite Sprite => sprite;
    public int Worth => worth;

}