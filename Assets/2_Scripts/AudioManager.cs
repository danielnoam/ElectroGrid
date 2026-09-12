using DNExtensions;
using UnityEngine;
using UnityEngine.SceneManagement;
using PrimeTween;
using UnityEngine.Audio;

[DisallowMultipleComponent]
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    private const string MusicVolumeParameter = "MusicVolume";
    private const string SfxVolumeParameter = "SFXVolume";
    private const float MinDecibels = -80f;
    
    [Header("Music")]
    [SerializeField, Range(0f, 1f)] private float maxVolume = 0.7f;
    [SerializeField, Min(0f)] private float transitionDuration = 1f;

    [Header("References")]
    [SerializeField] private AudioMixerGroup musicMixerGroup;
    [SerializeField] private AudioMixerGroup sfxMixerGroup;
    [SerializeField] private AudioSource audioSourceA;
    [SerializeField] private AudioSource audioSourceB;
    [SerializeField] private AudioClip mainMenuClip;
    [SerializeField] private ChanceList<AudioClip> gameplayClips;

    
    [Separator]
    [SerializeField, ReadOnly] private float musicVolume = 1f;
    [SerializeField, ReadOnly] private float sfxVolume = 1f;
    
    private AudioSource _currentSource;
    private AudioSource _nextSource;
    private Sequence _musicSequence;

    public float MusicVolume => musicVolume;
    public float SfxVolume => sfxVolume;

    private void Awake()
    {
        if (Instance && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        
        if (!audioSourceA || !audioSourceB)
        {
            Debug.LogError("AudioSources not assigned in MusicManager");
            return;
        }
        
        var settings = SaveManager.Instance ? SaveManager.Instance.Settings : null;
        musicVolume = settings?.musicVolume ?? 1f;
        sfxVolume = settings?.sfxVolume ?? 1f;

        _currentSource = audioSourceA;
        _nextSource = audioSourceB;
    }

    private void Start()
    {
        // Applied here rather than in Awake, an AudioMixer does not reliably accept SetFloat until the first frame
        ApplyMixerVolumes();
    }

    public void SetMusicVolume(float normalized)
    {
        musicVolume = Mathf.Clamp01(normalized);
        if (musicMixerGroup) musicMixerGroup.audioMixer.SetFloat(MusicVolumeParameter, NormalizedToDecibels(musicVolume));
    }

    public void SetSfxVolume(float normalized)
    {
        sfxVolume = Mathf.Clamp01(normalized);
        if (sfxMixerGroup) sfxMixerGroup.audioMixer.SetFloat(SfxVolumeParameter, NormalizedToDecibels(sfxVolume));
    }

    private void ApplyMixerVolumes()
    {
        SetMusicVolume(musicVolume);
        SetSfxVolume(sfxVolume);
    }

    /// <summary>Sliders are linear but loudness is not, so a raw 0-1 value maps onto the mixer's decibel range.</summary>
    private static float NormalizedToDecibels(float normalized)
    {
        return normalized <= 0.0001f ? MinDecibels : Mathf.Log10(normalized) * 20f;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        
        if (Match3GameManager.Instance)
        {
            Match3GameManager.Instance.LevelStarted -= OnLevelStarted;
            Match3GameManager.Instance.LevelStarted += OnLevelStarted;
        }
        
        _musicSequence.Stop();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        
        
        if (GameManager.Instance.MainMenu.SceneName == currentSceneName)
        {
            Play(mainMenuClip);
        }
        else if (GameManager.Instance.Match3Scene.SceneName == currentSceneName)
        {
            Play(gameplayClips.GetRandomItem());

            if (Match3GameManager.Instance)
            {
                Match3GameManager.Instance.LevelStarted -= OnLevelStarted;
                Match3GameManager.Instance.LevelStarted += OnLevelStarted;
            }
        }
    }

    private void OnLevelStarted(Match3LevelData levelData)
    {
        Play(gameplayClips.GetRandomItem());
    }

    private void Play(AudioClip clip)
    {
        if (!clip) return;

        if (_currentSource.clip == clip && _currentSource.isPlaying)
        {
            return;
        }

        _musicSequence.Stop();

        _nextSource.clip = clip;
        _nextSource.volume = 0f;
        _nextSource.Play();

        _musicSequence = Sequence.Create();
        if (!Mathf.Approximately(_currentSource.volume, 0f)) _musicSequence.Group(Tween.AudioVolume(_currentSource, 0f, transitionDuration, Ease.InOutSine));
        if (!Mathf.Approximately(_nextSource.volume, maxVolume)) _musicSequence.Group(Tween.AudioVolume(_nextSource, maxVolume, transitionDuration, Ease.InOutSine));
        _musicSequence.ChainCallback(() =>
            {
                _currentSource.Stop();
                (_currentSource, _nextSource) = (_nextSource, _currentSource);
            });
    }
    
    
    
}