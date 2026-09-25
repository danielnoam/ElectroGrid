using DNExtensions.Utilities;
using DNExtensions.Utilities.Button;
using PrimeTween;
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-50)]
public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance { get; private set; }
    
    [Header("Match3")]
    [SerializeField] private float gridPadding = 0.5f; 

    [Header("Tilt")]
    [Tooltip("Largest camera offset, as a fraction of the orthographic size, so it scales with the board")]
    [SerializeField, Range(0f, 0.2f)] private float tiltMaxOffset = 0.04f;
    [Tooltip("How far the phone must tilt from its resting angle, in gravity units, to reach the full offset")]
    [SerializeField, Range(0.05f, 1f)] private float tiltForFullOffset = 0.3f;
    [Tooltip("How quickly the offset follows the input")]
    [SerializeField, Min(0.1f)] private float tiltSmoothing = 6f;
    [Tooltip("How quickly the resting angle adapts to the way the phone is being held. Lower keeps the tilt longer")]
    [SerializeField, Min(0f)] private float tiltRestAdaptSpeed = 0.5f;
    
    [Header("References")]
    [SerializeField] private Camera cam;
    
    
    [Separator]
    [SerializeField, ReadOnly] private Vector2Int gridSize = new Vector2Int(10,10);
    private Match3GameManager _match3GameManager;
    private Vector3 _basePosition;
    private Quaternion _baseRotation;
    private Vector3 _shakePosition;
    private float _shakeRoll;
    private Sequence _shakeSequence;
    private Vector2 _tilt;
    private Vector3 _restGravity;
    private bool _hasRestGravity;
    private InputDevice _tiltSensor;

    /// <summary>The current tilt offset in world units. Parallax layers follow a fraction of it.</summary>
    public Vector3 TiltOffset { get; private set; }

    private static bool IsTiltEnabled => !SaveManager.Instance || SaveManager.Instance.Settings.tiltEnabled;



    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }

        if (!cam) return;

        _basePosition = cam.transform.localPosition;
        _baseRotation = cam.transform.localRotation;
    }

    private void Start()
    {
        SubscribeToMatch3Manager();
    }

    private void OnEnable()
    {
        SubscribeToMatch3Manager();
    }

    private void SubscribeToMatch3Manager()
    {
        if (!_match3GameManager) _match3GameManager = Match3GameManager.Instance;
        if (_match3GameManager)
        {
            _match3GameManager.LevelStarted -= OnLevelStarted;
            _match3GameManager.LevelStarted += OnLevelStarted;
        }
    }

    private void OnDisable()
    {
        ReleaseTiltSensor();
        _shakeSequence.Stop();

        if (_match3GameManager)
        {
            _match3GameManager.LevelStarted -= OnLevelStarted;
        }

    }




    private void OnLevelStarted(Match3LevelData level)
    {
        if (level == null) return;
        var gridShape = level.Level.GridShape;
        var size = new Vector2Int(gridShape.Grid.Width, gridShape.Grid.Height);
        UpdateGridSize(size);
    }

    private void UpdateGridSize(Vector2Int size)
    {
        gridSize = size;
        FitCameraToGrid();
    }

    [Button(ButtonPlayMode.OnlyWhenPlaying)]
    private void FitCameraToGrid()
    {
        if (!cam) return;
        
        float screenAspect = (float)Screen.width / Screen.height;
        float gridAspect = gridSize.x / gridSize.y;

        if (screenAspect >= gridAspect)
        {
            cam.orthographicSize = (gridSize.y / 2f) + gridPadding;
        }
        else
        {
            float targetHeight =  gridSize.x / screenAspect;
            cam.orthographicSize = (targetHeight / 2f) + gridPadding;
        }
    }
    
    /// <summary>
    /// Shakes offsets rather than the transform, the same strengths as PrimeTween's orthographic camera shake,
    /// so the shake and the tilt can both be added onto the camera's resting position in LateUpdate.
    /// </summary>
    [Button(ButtonPlayMode.OnlyWhenPlaying)]
    public void ShakeCamera(float strength = 1f,float duration = 0.5f,float frequency = 10f)
    {
        if (!cam) return;
        if (SaveManager.Instance && !SaveManager.Instance.ScreenShakeEnabled) return;
        if (FirebaseManager.Instance) strength *= FirebaseManager.Instance.ScreenShakeIntensityMultiplier;

        float positionStrength = strength * cam.orthographicSize * 0.03f;

        _shakeSequence.Stop();
        _shakeSequence = Sequence.Create()
            .Group(Tween.ShakeCustom(this, Vector3.zero,
                new ShakeSettings(new Vector3(positionStrength, positionStrength), duration, frequency),
                (manager, value) => manager._shakePosition = value))
            .Group(Tween.ShakeCustom(this, Vector3.zero,
                new ShakeSettings(new Vector3(0f, 0f, strength * 0.6f), duration, frequency),
                (manager, value) => manager._shakeRoll = value.z))
            .ChainCallback(this, manager =>
            {
                manager._shakePosition = Vector3.zero;
                manager._shakeRoll = 0f;
            });
    }

    private void LateUpdate()
    {
        if (!cam) return;

        UpdateTilt();

        cam.transform.localPosition = _basePosition + TiltOffset + _shakePosition;
        cam.transform.localRotation = _baseRotation * Quaternion.Euler(0f, 0f, _shakeRoll);
    }

    private void UpdateTilt()
    {
        bool active = IsTiltEnabled;

        if (active && Application.isMobilePlatform) AcquireTiltSensor();
        else ReleaseTiltSensor();

        Vector2 target = active ? ReadTiltInput() : Vector2.zero;

        // Unscaled, windows pause the game at a time scale of 0 and the view should still respond
        _tilt = Vector2.Lerp(_tilt, target, 1f - Mathf.Exp(-tiltSmoothing * Time.unscaledDeltaTime));

        float reach = cam.orthographic ? cam.orthographicSize * tiltMaxOffset : tiltMaxOffset;
        TiltOffset = new Vector3(_tilt.x, _tilt.y, 0f) * reach;
    }

    private Vector2 ReadTiltInput()
    {
        return Application.isMobilePlatform ? ReadDeviceTilt() : ReadMouseTilt();
    }

    private static Vector2 ReadMouseTilt()
    {
        var mouse = Mouse.current;
        if (mouse == null || Screen.width <= 0 || Screen.height <= 0) return Vector2.zero;

        Vector2 position = mouse.position.ReadValue();
        var normalized = new Vector2(position.x / Screen.width * 2f - 1f, position.y / Screen.height * 2f - 1f);
        return Vector2.ClampMagnitude(normalized, 1f);
    }

    /// <summary>
    /// Tilt relative to a resting angle that drifts toward however the phone is held, so the view reacts to a
    /// change of angle and then settles, rather than being permanently offset by a phone held at 50 degrees.
    /// </summary>
    private Vector2 ReadDeviceTilt()
    {
        if (!TryReadGravity(out Vector3 gravity)) return Vector2.zero;

        if (!_hasRestGravity)
        {
            _restGravity = gravity;
            _hasRestGravity = true;
        }

        _restGravity = Vector3.Lerp(_restGravity, gravity, 1f - Mathf.Exp(-tiltRestAdaptSpeed * Time.unscaledDeltaTime));

        Vector3 delta = gravity - _restGravity;
        return Vector2.ClampMagnitude(new Vector2(delta.x, delta.y) / tiltForFullOffset, 1f);
    }

    private bool TryReadGravity(out Vector3 gravity)
    {
        gravity = Vector3.zero;

        switch (_tiltSensor)
        {
            case GravitySensor gravitySensor:
                gravity = gravitySensor.gravity.ReadValue();
                break;
            case Accelerometer accelerometer:
                // Includes hand movement as well as gravity, the tilt smoothing filters most of it out
                gravity = accelerometer.acceleration.ReadValue();
                break;
            default:
                return false;
        }

        if (gravity.sqrMagnitude < 0.0001f) return false;

        gravity.Normalize();
        return true;
    }

    /// <summary>
    /// Sensors are off by default in the Input System and cost battery while on, so one is only enabled while the
    /// tilt is. Gravity is preferred; plenty of budget phones only have an accelerometer.
    /// </summary>
    private void AcquireTiltSensor()
    {
        if (_tiltSensor != null) return;

        InputDevice candidate = GravitySensor.current;
        if (candidate == null) candidate = Accelerometer.current;
        if (candidate == null) return;

        InputSystem.EnableDevice(candidate);
        _tiltSensor = candidate;
        _hasRestGravity = false;
    }

    private void ReleaseTiltSensor()
    {
        if (_tiltSensor == null) return;

        if (_tiltSensor.added) InputSystem.DisableDevice(_tiltSensor);
        _tiltSensor = null;
        _hasRestGravity = false;
    }
}
