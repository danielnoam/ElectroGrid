using UnityEngine;

public enum Match3TutorialTrigger
{
    AnyLevelStart,
    LevelHasObstacles,
    LevelHasBottomObjects,
    HelperSpawned,
    LineBreakMade
}

[CreateAssetMenu(fileName = "New Match3 Tutorial", menuName = "Scriptable Objects/Match3 Tutorial")]
public class SOMatch3Tutorial : ScriptableObject
{
    [Header("Tutorial Settings")]
    [SerializeField] private string tutorialTitle = "Tutorial";
    [SerializeField, Multiline(5)] private string tutorialText = "Tutorial";
    [SerializeField] private Sprite tutorialSprite;

    [Header("Contextual Display")]
    [Tooltip("Show this card once, in game, the first time its trigger fires")]
    [SerializeField] private bool showContextually = true;
    [SerializeField] private Match3TutorialTrigger trigger = Match3TutorialTrigger.AnyLevelStart;

    public string TutorialTitle => tutorialTitle;
    public string TutorialText => tutorialText;
    public Sprite TutorialSprite => tutorialSprite;
    public bool ShowContextually => showContextually;
    public Match3TutorialTrigger Trigger => trigger;
}
