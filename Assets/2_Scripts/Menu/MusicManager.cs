using DNExtensions;
using UnityEngine;
using UnityEngine.SceneManagement;
using PrimeTween;

[DisallowMultipleComponent]
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }
    
    [Header("Settings")]
    [SerializeField, Range(0f, 1f)] private float maxVolume = 0.7f;
    [SerializeField, Min(0f)] private float transitionDuration = 1f;

    [Header("References")]
    [SerializeField] private AudioSource audioSourceA;
    [SerializeField] private AudioSource audioSourceB;
    [SerializeField] private AudioClip mainMenuClip;
    [SerializeField] private ChanceList<AudioClip> gameplayClips;

    private AudioSource _currentSource;
    private AudioSource _nextSource;
    private Sequence _transitionSequence;

    private void Awake()
    {
        if (Instance && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        SetupAudioSources();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    private void SetupAudioSources()
    {
        if (!audioSourceA || !audioSourceB)
        {
            Debug.LogError("AudioSources not assigned in MusicManager");
            return;
        }

        audioSourceA.loop = true;
        audioSourceB.loop = true;
        audioSourceA.volume = 0f;
        audioSourceB.volume = 0f;
        audioSourceA.playOnAwake = false;
        audioSourceB.playOnAwake = false;

        _currentSource = audioSourceA;
        _nextSource = audioSourceB;
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
        }
    }

    private void Play(AudioClip clip)
    {
        if (!clip) return;

        if (_currentSource.clip == clip && _currentSource.isPlaying)
        {
            return;
        }

        _transitionSequence.Stop();

        _nextSource.clip = clip;
        _nextSource.volume = 0f;
        _nextSource.Play();

        _transitionSequence = Sequence.Create()
            .Group(Tween.AudioVolume(_currentSource, 0f, transitionDuration, Ease.InOutSine))
            .Group(Tween.AudioVolume(_nextSource, maxVolume, transitionDuration, Ease.InOutSine))
            .ChainCallback(() =>
            {
                _currentSource.Stop();
                (_currentSource, _nextSource) = (_nextSource, _currentSource);
            });
    }
    
}