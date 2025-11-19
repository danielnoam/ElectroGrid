using UnityEngine;

[CreateAssetMenu(fileName = "New Match3 Tutorial", menuName = "Scriptable Objects/Match3 Tutorial")]
public class SOMatch3Tutorial : ScriptableObject
{
    [Header("Tutorial Settings")]
    [SerializeField] private string tutorialTitle = "Tutorial";
    [SerializeField, Multiline(5)] private string tutorialText = "Tutorial";
    [SerializeField] private Sprite tutorialSprite;
    
    public string TutorialTitle => tutorialTitle;
    public string TutorialText => tutorialText;
    public Sprite TutorialSprite => tutorialSprite;
    
    
}