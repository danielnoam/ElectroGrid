using System;
using DNExtensions;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Match3 Level", menuName = "Scriptable Objects/Match3 Level")]
public class SOMatch3Level : ScriptableObject
{
    [Header("Level Settings")]
    [SerializeField] private SOGridShape gridShape;
    [SerializeField] private ChanceList<SOItemData> matchObjects = new ChanceList<SOItemData>();
    [SerializeReference] private List<Match3Objective> objectives = new List<Match3Objective>();
    [SerializeReference] private List<Match3LoseCondition> loseConditions = new List<Match3LoseCondition>();

    public SOGridShape GridShape => gridShape;
    public ChanceList<SOItemData> MatchObjects => matchObjects;
    public List<Match3Objective> Objectives => objectives;
    public List<Match3LoseCondition> LoseConditions => loseConditions;
}