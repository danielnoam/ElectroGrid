
using UnityEngine;
using UnityEditor;
using DNExtensions.ObjectPooling;
using UnityEngine.Audio;
using Random = UnityEngine.Random;




namespace DNExtensions
{
    
    [CreateAssetMenu(fileName = "New AudioEvent", menuName = "Scriptable Objects/New Audio Event")]
    public class SOAudioEvent : ScriptableObject
    {

        [Header("Settings")] 
        public AudioClip[] clips;
        public AudioMixerGroup mixerGroup;
        [MinMaxRange(0f, 1f)] public RangedFloat volume = 1f;
        [MinMaxRange(-3f, 3f)] public RangedFloat pitch = 1f;

        [Range(-1f, 1f), Tooltip("Left,Right")]
        public float stereoPan = 0f;

        [Range(0f, 1f), Tooltip("2D,3D")] public float spatialBlend = 0f;
        [Range(0f, 1.1f)] public float reverbZoneMix = 1f;
        public bool bypassEffects;
        public bool bypassListenerEffects;
        public bool bypassReverbZones;
        public bool loop;

        [Header("3D Sound")] public bool set3DSettings = false;
        [EnableIf("set3DSettings")] [MinMaxRange(0f, 5f)] public float dopplerLevel = 1f;
        [EnableIf("set3DSettings")] [MinMaxRange(0f, 360f)] public float spread = 0f;
        [EnableIf("set3DSettings")] public AudioRolloffMode rolloffMode = AudioRolloffMode.Logarithmic;
        [EnableIf("set3DSettings")] [Min(0)] public float minDistance = 1f;
        [EnableIf("set3DSettings")] [Min(0)] public float maxDistance = 500f;



        [Header("OneShot Object Pooler")]
        public bool useObjectPooler = true;
        public GameObject oneShotPrefab;


        
        
        #region Play AE ----------------------------------------------------------------------------

        public void Play(AudioSource source)
        {
            if (!source || !source.enabled) return;

            if (clips.Length == 0)
            {
                Debug.Log("No clips found");
                return;
            }


            SetAudioSourceSettings(source);
            source.Play();
        }

        public void Play(AudioSource source, float delay)
        {
            if (!source || !source.enabled) return;

            if (clips.Length == 0)
            {
                Debug.Log("No clips found");
                return;
            }

            SetAudioSourceSettings(source);
            source.PlayDelayed(delay);
        }


        public void PlayAtPoint(Vector3 position = new())
        {
            if (clips.Length == 0)
            {
                Debug.Log("No clips found");
                return;
            }


            if (useObjectPooler && oneShotPrefab)
            {
                GameObject oneShotObject = ObjectPooler.GetObjectFromPool(oneShotPrefab, position, Quaternion.identity);
                if (oneShotObject.TryGetComponent(out AudioSource source))
                {
                    source.transform.position = position;
                    SetAudioSourceSettings(source);
                    source.Play();
                    if (source.TryGetComponent(out AutoReturnToPool returnToPool))
                    {
                        returnToPool.Initialize(source.clip.length);
                    }
                }
            }
            else
            {
                AudioSource source = new GameObject("OneShotAudioEvent").AddComponent<AudioSource>();

                source.transform.position = position;
                SetAudioSourceSettings(source);
                source.Play();
                Destroy(source.gameObject, source.clip.length);
            }
        }

        public void PlayAtPoint(float delay, Vector3 position = new())
        {
            if (clips.Length == 0)
            {
                Debug.Log("No clips found");
                return;
            }



            if (!useObjectPooler)
            {
                AudioSource source = new GameObject("OneShotAudioEvent").AddComponent<AudioSource>();

                source.transform.position = position;
                SetAudioSourceSettings(source);
                source.PlayDelayed(delay);
                Destroy(source.gameObject, source.clip.length + delay);
            }
            else
            {
                GameObject oneShotObject = ObjectPooler.GetObjectFromPool(oneShotPrefab, position, Quaternion.identity);
                if (oneShotObject.TryGetComponent(out AudioSource source))
                {
                    source.transform.position = position;
                    SetAudioSourceSettings(source);
                    source.Play();

                    if (source.TryGetComponent(out AutoReturnToPool returnToPool))
                    {
                        returnToPool.Initialize(source.clip.length);
                    }
                }
            }
        }




        #endregion Play AE ----------------------------------------------------------------------------



        #region Autdio source control ------------------------------------------------------------------------------------------------

        private void SetAudioSourceSettings(AudioSource source)
        {
            if (!source) return;

            source.clip = clips[Random.Range(0, clips.Length)];
            source.outputAudioMixerGroup = mixerGroup;
            source.volume = Random.Range(volume.minValue, volume.maxValue);
            source.pitch = Random.Range(pitch.minValue, pitch.maxValue);
            source.panStereo = stereoPan;
            source.spatialBlend = spatialBlend;
            source.reverbZoneMix = reverbZoneMix;
            source.bypassEffects = bypassEffects;
            source.bypassListenerEffects = bypassListenerEffects;
            source.bypassReverbZones = bypassReverbZones;
            source.loop = loop;
            if (set3DSettings)
            {
                source.dopplerLevel = dopplerLevel;
                source.spread = spread;
                source.minDistance = minDistance;
                source.maxDistance = maxDistance;
                source.rolloffMode = rolloffMode;
            }
        }

        public void Stop(AudioSource source)
        {
            if (!source) return;

            foreach (var clip in clips)
            {
                if (source.clip == clip)
                {
                    source.Stop();
                }
            }
        }

        public void Pause(AudioSource source)
        {
            if (!source) return;

            foreach (var clip in clips)
            {
                if (source.clip == clip)
                {
                    source.Pause();
                }
            }
        }

        public void Continue(AudioSource source)
        {
            if (!source) return;

            foreach (var clip in clips)
            {
                if (source.clip == clip)
                {
                    source.UnPause();
                }
            }
        }

        public bool IsPlaying(AudioSource source)
        {
            if (!source) return false;

            foreach (var clip in clips)
            {
                if (source.clip != clip) continue;

                if (source.isPlaying)
                {
                    return true;
                }
            }

            return false;
        }

        #endregion Autdio source control ------------------------------------------------------------------------------------------------
        
    }


    #region Editor ------------------------------------------------------------------------------------------------

#if UNITY_EDITOR


// Preview button
    [CustomEditor(typeof(SOAudioEvent), true)]
    public class AudioEventEditor : UnityEditor.Editor
    {

        [SerializeField] private AudioSource previewer;

        public void OnEnable()
        {
            previewer = EditorUtility
                .CreateGameObjectWithHideFlags("Audio preview", HideFlags.HideAndDontSave, typeof(AudioSource))
                .GetComponent<AudioSource>();
        }

        public void OnDisable()
        {
            DestroyImmediate(previewer.gameObject);
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUI.BeginDisabledGroup(serializedObject.isEditingMultipleObjects);
            if (GUILayout.Button("Preview Sound"))
            {
                ((SOAudioEvent)target).Play(previewer);

                if (previewer.clip)
                {
                    Debug.Log("Playing " + previewer.clip.name);
                }

            }

            if (GUILayout.Button("Stop Sound"))
            {
                ((SOAudioEvent)target).Stop(previewer);
            }

            EditorGUI.EndDisabledGroup();
        }
    }
#endif

    #endregion Editor ------------------------------------------------------------------------------------------------





}