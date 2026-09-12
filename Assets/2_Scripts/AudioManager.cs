using DNExtensions;
using UnityEngine;
using UnityEngine.SceneManagement;
using PrimeTween;
using UnityEngine.Audio;

[DisallowMultipleComponent]
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }
    
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
    [SerializeField, ReadOnly] private bool isMuted;
    
    private AudioSource _currentSource;
    private AudioSource _nextSource;
    private Sequence _musicSequence;
    private Sequence _audioSequence;

    public bool IsMuted => isMuted;

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
        
        isMuted = SaveManager.Instance && SaveManager.Instance.IsMuted;
        _currentSource = audioSourceA;
        _nextSource = audioSourceB;
    }

    private void Start()
    {
        // Applied here rather than in Awake, an AudioMixer does not reliably accept SetFloat until the first frame
        ApplyMuteVolume(isMuted ? -80f : 0f);
    }

    private void ApplyMuteVolume(float volume)
    {
        if (!musicMixerGroup || !sfxMixerGroup) return;

        musicMixerGroup.audioMixer.SetFloat("MusicVolume", volume);
        sfxMixerGroup.audioMixer.SetFloat("SFXVolume", volume);
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
        _audioSequence.Stop();
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
    
    
    public void ToggleAudio()
    {
        if (!musicMixerGroup || !sfxMixerGroup) return;

        isMuted = !isMuted;
        SaveManager.Instance?.SetMuted(isMuted);

        _audioSequence.Stop();
        _audioSequence = Sequence.Create();

        _audioSequence.Group(Tween.Custom(
            startValue: isMuted ? 0f : -80f,
            endValue: isMuted ? -80f : 0,
            duration: transitionDuration / 2,
            onValueChange: ApplyMuteVolume,
            Ease.InOutSine));


    }
    
}