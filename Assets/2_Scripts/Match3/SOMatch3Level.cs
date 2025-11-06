using DNExtensions;
using UnityEngine;

[CreateAssetMenu(fileName = "New Match3 Level", menuName = "Scriptable Objects/Match3 Level")]
public class SOMatch3Level : ScriptableObject
{
    [Header("Level Settings")]
    [SerializeField, Min(0)] private int allowedMoves = 15;
    [SerializeField] private ChanceList<SOItemData> matchObjects = new ChanceList<SOItemData>();
    
    [Header("Grid")]
    [SerializeField] private SOGridShape gridShape;


    
    

    

    public SOGridShape GridShape => gridShape;
    public ChanceList<SOItemData> MatchObjects => matchObjects;
    public int AllowedMoves => allowedMoves;
}