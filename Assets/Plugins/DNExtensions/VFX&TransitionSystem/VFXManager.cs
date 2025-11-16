using System;
using DNExtensions.Button;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;


namespace DNExtensions.VFXManager
{
    [DisallowMultipleComponent]
    public class VFXManager : MonoBehaviour
    {
        public static VFXManager Instance { get; private set; }
    
        [Header("Settings")]
        [Tooltip("If true, the post-processing effects will be automatically set up on Awake.")]
        [SerializeField] private bool autoSetupPostProcessing = true;
        [SerializeField] private bool autoSetCameraIfUsingCameraOverlay;
        
        [Header("References")]
        [SerializeField] private Volume postProcessingVolume;
        [SerializeField] private Camera mainCamera;
        [SerializeField] private Image iconImage;
        [SerializeField] private Image fullScreenImage;
        [SerializeField] private Canvas fullscreenCanvas;
        [SerializeField] private SOVFEffectsSequence[] effectsSequences;
    

        private SOVFEffectsSequence _currentSequence;
        public LensDistortion LensDistortion { get; private set; }
        public ChromaticAberration ChromaticAberration { get; private set; }
        public MotionBlur MotionBlur { get; private set; }
        public Vignette Vignette { get; private set; }
        public PaniniProjection PaniniProjection { get; private set; }
        public DepthOfField DepthOfField { get; private set; }
        
        public Camera MainCamera => mainCamera;
        public Sprite DefaultIconSprite { get; private set; }
        public Sprite DefaultFullScreenSprite { get; private set; }
        public Vector3 DefaultFullScreenPosition { get; private set; }
        public Vector3 DefaultFullScreenScale { get; private set; }
        public Vector3 DefaultFullScreenRotation { get; private set; }
        public Vector3 DefaultIconPosition { get; private set; }
        public Vector3 DefaultIconScale { get; private set; }
        public Vector3 DefaultIconRotation { get; private set; }
        public Color DefaultIconColor { get; private set; }
        public Color DefaultFullScreenColor { get; private set; }
        public float DefaultLensDistortionIntensity { get; private set; }
        public float DefaultLensDistortionXMultiplier { get; private set; }
        public float DefaultLensDistortionYMultiplier { get; private set; }
        public Vector2 DefaultLensDistortionCenter { get; private set; }
        public float DefaultLensDistortionScale { get; private set; }
        public float DefaultChromaticAberrationIntensity { get; private set; }
        public float DefaultMotionBlurIntensity { get; private set; }
        public float DefaultMotionBlurClamp { get; private set; }
        public float DefaultVignetteIntensity { get; private set; }
        public float DefaultVignetteSmoothness { get; private set; }
        public Vector2 DefaultVignetteCenter { get; private set; }
        public bool DefaultVignetteRounded { get; private set; }
        public float DefaultPaniniProjectionDistance { get; private set; }
        public float DefaultPaniniProjectionCropToFit { get; private set; }
        public float DefaultCameraFOV { get; private set; }
        public float DefaultDepthOfFieldFocusDistance { get; private set; }
        public float DefaultDepthOfFieldAperture { get; private set; }
        public float DefaultDepthOfFieldFocalLength { get; private set; }
        
        
        
        public Image FullScreenImage => fullScreenImage;
        public Image IconImage => iconImage;
        
        
        public event Action<SOVFEffectsSequence> OnVFXSequenceStarted;


        private void OnValidate()
        {
            if (effectsSequences == null || effectsSequences.Length == 0) return;
            for (int i = 0; i < effectsSequences.Length; i++)
            {
                for (int j = i + 1; j < effectsSequences.Length; j++)
                {
                    if (effectsSequences[i] == effectsSequences[j])
                    {
                        Debug.LogWarning($"Duplicate SOVFEffectsSequence found: {effectsSequences[i].name}.", this);
                    }
                }
            }
        }


        private void Awake()
        {
            if (Instance && Instance != this) 
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            if (iconImage)
            {
                DefaultIconSprite = iconImage.sprite;
                DefaultIconColor = iconImage.color;
                DefaultIconPosition = iconImage.rectTransform.localPosition;
                DefaultIconScale = iconImage.rectTransform.localScale;
                DefaultIconRotation = iconImage.rectTransform.localEulerAngles;
                
                iconImage.color = Color.clear;
            }

            if (fullScreenImage)
            {
                DefaultFullScreenSprite = fullScreenImage.sprite;
                DefaultFullScreenColor = fullScreenImage.color;
                DefaultFullScreenPosition = fullScreenImage.rectTransform.localPosition;
                DefaultFullScreenScale = fullScreenImage.rectTransform.localScale;
                DefaultFullScreenRotation = fullScreenImage.rectTransform.localEulerAngles;
                
                fullScreenImage.color = Color.clear;
            }
            
            if (!mainCamera) mainCamera = Camera.main;
            if (mainCamera) DefaultCameraFOV = mainCamera.fieldOfView;
            
            if (fullscreenCanvas && autoSetCameraIfUsingCameraOverlay && mainCamera)
            {
                fullscreenCanvas.worldCamera = mainCamera;
            }
            
            SetupPostProcessingVolume();
        }

        private void OnEnable()
        {
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
        }

        private void OnDisable()
        {
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        }

        private void OnActiveSceneChanged(Scene currentScene, Scene nextScene)
        {
            if (!mainCamera) mainCamera = Camera.main;
            if (mainCamera) DefaultCameraFOV = mainCamera.fieldOfView;
            
            if (fullscreenCanvas && autoSetCameraIfUsingCameraOverlay && mainCamera)
            {
                fullscreenCanvas.worldCamera = mainCamera;
            }
            
            SetupPostProcessingVolume();
        }
        

        private void SetupPostProcessingVolume()
        {
            if (!postProcessingVolume) postProcessingVolume = FindAnyObjectByType<Volume>();
            if (!postProcessingVolume)
            {
                Debug.Log("No Post Processing Volume found in the scene!");
                return;
            }

            if (postProcessingVolume.profile.TryGet(out Vignette vignette))
            {
                Vignette = vignette;
                DefaultVignetteIntensity = Vignette.intensity.value;
                DefaultVignetteSmoothness = Vignette.smoothness.value;
                DefaultVignetteCenter = Vignette.center.value;
                DefaultVignetteRounded = Vignette.rounded.value;
                if (!autoSetupPostProcessing) return;
                Vignette.active = true;
                Vignette.color.overrideState = true;
                Vignette.center.overrideState = true;
                Vignette.intensity.overrideState = true;
                Vignette.smoothness.overrideState = true;
                Vignette.rounded.overrideState = true;
            }

            if (postProcessingVolume.profile.TryGet(out LensDistortion lensDistortion))
            {
                LensDistortion = lensDistortion;
                DefaultLensDistortionIntensity = LensDistortion.intensity.value;
                DefaultLensDistortionXMultiplier = LensDistortion.xMultiplier.value;
                DefaultLensDistortionYMultiplier = LensDistortion.yMultiplier.value;
                DefaultLensDistortionCenter = LensDistortion.center.value;
                DefaultLensDistortionScale = LensDistortion.scale.value;
                if (!autoSetupPostProcessing) return;
                LensDistortion.active = true;
                LensDistortion.intensity.overrideState = true;
                LensDistortion.center.overrideState = true;
                LensDistortion.scale.overrideState = true;
                LensDistortion.xMultiplier.overrideState = true;
                LensDistortion.yMultiplier.overrideState = true;
            }

            if (postProcessingVolume.profile.TryGet(out ChromaticAberration chromaticAberration))
            {
                ChromaticAberration = chromaticAberration;
                DefaultChromaticAberrationIntensity = ChromaticAberration.intensity.value;
                if (!autoSetupPostProcessing) return;
                ChromaticAberration.active = true;
            }

            if (postProcessingVolume.profile.TryGet(out MotionBlur motionBlur))
            {
                MotionBlur = motionBlur;
                DefaultMotionBlurIntensity = MotionBlur.intensity.value;
                DefaultMotionBlurClamp = MotionBlur.clamp.value;
                if (!autoSetupPostProcessing) return;
                MotionBlur.active = true;
                MotionBlur.intensity.overrideState = true;
                MotionBlur.quality.overrideState = true;
                MotionBlur.clamp.overrideState = true;
            }
            
            if (postProcessingVolume.profile.TryGet(out PaniniProjection paniniProjection))
            {
                PaniniProjection = paniniProjection;
                DefaultPaniniProjectionDistance = PaniniProjection.distance.value;
                DefaultPaniniProjectionCropToFit = PaniniProjection.cropToFit.value;
                if (!autoSetupPostProcessing) return;
                PaniniProjection.active = true;
                PaniniProjection.distance.overrideState = true;
                PaniniProjection.cropToFit.overrideState = true;
            }
            
            if (postProcessingVolume.profile.TryGet(out DepthOfField depthOfField))
            {
                DepthOfField = depthOfField;
                DefaultDepthOfFieldFocusDistance = DepthOfField.focusDistance.value;
                DefaultDepthOfFieldAperture = DepthOfField.aperture.value;
                DefaultDepthOfFieldFocalLength = DepthOfField.focalLength.value;
                if (!autoSetupPostProcessing) return;
                DepthOfField.active = true;
                DepthOfField.focusDistance.overrideState = true;
                DepthOfField.aperture.overrideState = true;
                DepthOfField.focalLength.overrideState = true;
            }
        }

        
        /// <summary>
        /// Plays a specific visual effects sequence.
        /// </summary>
        /// <param name="vfxSequence">The visual effects sequence to play.</param>
        /// <returns>The duration of the visual effects sequence.</returns>
        public float PlayVFX(SOVFEffectsSequence vfxSequence)
        {
            if (!vfxSequence) return 0;
            if (!vfxSequence.SequenceIsAdditive) ResetActiveEffects();

            _currentSequence = vfxSequence;
            var vfxDuration = _currentSequence.PlaySequence();
            OnVFXSequenceStarted?.Invoke(vfxSequence);

            return vfxDuration;

        }
        

        /// <summary>
        /// Gets a random sequence from the sequence list.
        /// </summary>
        /// <returns>A random SOVFEffectsSequence.</returns>
        public SOVFEffectsSequence GetRandomEffect()
        {
            var randomVFXIndex = Random.Range(0, effectsSequences.Length);
            var vfxSequence = effectsSequences[randomVFXIndex];

            return vfxSequence;
        }
        
        /// <summary>
        /// Resets all visual effects to their default state.
        /// </summary>
        [Button(ButtonPlayMode.OnlyWhenPlaying)]
        public void ResetActiveEffects() 
        {
            if (_currentSequence)
            {
                _currentSequence.ResetSequenceEffects();
                _currentSequence = null;
            }
            
            if (iconImage)
            {
                iconImage.sprite = DefaultIconSprite;
                iconImage.color = DefaultIconColor;
                iconImage.rectTransform.localPosition = DefaultIconPosition;
                iconImage.rectTransform.localScale = DefaultIconScale;
                iconImage.rectTransform.localEulerAngles = DefaultIconRotation;
            }
            
            if (fullScreenImage)
            {
                fullScreenImage.sprite = DefaultFullScreenSprite;
                fullScreenImage.color = DefaultFullScreenColor;
                fullScreenImage.rectTransform.localPosition = DefaultFullScreenPosition;
                fullScreenImage.rectTransform.localScale = DefaultFullScreenScale;
                fullScreenImage.rectTransform.localEulerAngles = DefaultFullScreenRotation;
            }
            
            if (Vignette)
            {
                Vignette.intensity.value = DefaultVignetteIntensity;
                Vignette.smoothness.value = DefaultVignetteSmoothness;
                Vignette.center.value = DefaultVignetteCenter;
                Vignette.rounded.value = DefaultVignetteRounded;
            }
            
            if (LensDistortion)
            {
                LensDistortion.intensity.value = DefaultLensDistortionIntensity;
                LensDistortion.xMultiplier.value = DefaultLensDistortionXMultiplier;
                LensDistortion.yMultiplier.value = DefaultLensDistortionYMultiplier;
                LensDistortion.center.value = DefaultLensDistortionCenter;
                LensDistortion.scale.value = DefaultLensDistortionScale;
            }
            
            if (ChromaticAberration)
            {
                ChromaticAberration.intensity.value = DefaultChromaticAberrationIntensity;
            }
            
            if (MotionBlur)
            {
                MotionBlur.intensity.value = DefaultMotionBlurIntensity;
                MotionBlur.clamp.value = DefaultMotionBlurClamp;
            }
            
            if (PaniniProjection)
            {
                PaniniProjection.distance.value = DefaultPaniniProjectionDistance;
                PaniniProjection.cropToFit.value = DefaultPaniniProjectionCropToFit;
            }

            // Reset DepthOfField
            if (DepthOfField)
            {
                DepthOfField.focusDistance.value = DefaultDepthOfFieldFocusDistance;
                DepthOfField.aperture.value = DefaultDepthOfFieldAperture;
                DepthOfField.focalLength.value = DefaultDepthOfFieldFocalLength;
            }
            
            // Reset Camera FOV
            if (mainCamera)
            {
                mainCamera.fieldOfView = DefaultCameraFOV;
            }
        }
    }
}