using UnityEngine;
using UnityEngine.Localization;

public enum Match3TutorialTrigger
{
    AnyLevelStart,
    LevelHasObstacles,
    LevelHasBottomObjects,
    HelperSpawned,
    LineBreakMade,
    SurvivalStart
}

[CreateAssetMenu(fileName = "New Match3 Tutorial", menuName = "Scriptable Objects/Match3 Tutorial")]
public class SOMatch3Tutorial : ScriptableObject
{
    [Header("Tutorial Settings")]
    [Tooltip("Entries live in the Content string table")]
    [SerializeField] private LocalizedString tutorialTitle;
    [SerializeField] private LocalizedString tutorialText;
    [SerializeField] private Sprite tutorialSprite;

    [Header("Contextual Display")]
    [Tooltip("Show this card once, in game, the first time its trigger fires")]
    [SerializeField] private bool showContextually = true;
    [SerializeField] private Match3TutorialTrigger trigger = Match3TutorialTrigger.AnyLevelStart;

    public string TutorialTitle => L10n.Get(tutorialTitle);
    public string TutorialText => L10n.Get(tutorialText);
    public Sprite TutorialSprite => tutorialSprite;
    public bool ShowContextually => showContextually;
    public Match3TutorialTrigger Trigger => trigger;
}
