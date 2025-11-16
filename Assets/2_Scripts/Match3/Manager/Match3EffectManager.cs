using System;
using DNExtensions.VFXManager;
using UnityEngine;

public class Match3EffectManager : MonoBehaviour
{
    
    [Header("Settings")]
    [SerializeField] private SOVFEffectsSequence startLevelSequence;
    [SerializeField] private SOVFEffectsSequence endLevelSequence;

    [Header("References")]
    [SerializeField] private Match3GameManager gameManager;
    [SerializeField] private Match3GridHandler gridHandler;
    [SerializeField] private Match3PlayHandler playHandler;

    public SOVFEffectsSequence StartLevelSequence => startLevelSequence;
    public SOVFEffectsSequence EndLevelSequence => endLevelSequence;

    private void OnEnable()
    {
        if (gameManager)
        {
            gameManager.LevelStarted += OnLevelStarted;
            gameManager.LevelFailed += OnLevelEnded;
            gameManager.LevelComplete += OnLevelEnded;
        }
    }

    private void OnDisable()
    {
        if (gameManager)
        {
            gameManager.LevelStarted -= OnLevelStarted;
            gameManager.LevelFailed -= OnLevelEnded;
            gameManager.LevelComplete -= OnLevelEnded;
        }
    }

    
    private void OnLevelStarted(Match3LevelData levelData)
    {
        VFXManager.Instance?.PlayVFX(startLevelSequence);

    }
    private void OnLevelEnded(Match3LevelData levelData)
    {
        VFXManager.Instance?.PlayVFX(endLevelSequence);
    }


}