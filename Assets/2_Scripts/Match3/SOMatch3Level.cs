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
    [SerializeField] private PopulationDirection populationDirection = PopulationDirection.TopToBottom;
    [SerializeField, Min(1)] private float populationDuration = 1f;
    
    

    

    public SOGridShape GridShape => gridShape;
    public PopulationDirection PopulationDirection => populationDirection;
    public float PopulationDuration => populationDuration;
    public ChanceList<SOItemData> MatchObjects => matchObjects;
    public int AllowedMoves => allowedMoves;
}