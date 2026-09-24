using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Sways the camera with how the phone is held, or with the mouse on desktop, so the flat board reads as
/// floating over the scene. <see cref="ParallaxLayer"/> objects follow part of the sway to sit further back.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
[DefaultExecutionOrder(-50)]
public class CameraTilt : MonoBehaviour
{
    [Header("Response")]
    [Tooltip("Largest camera offset, as a fraction of the orthographic size, so it scales with the board")]
    [SerializeField, Range(0f, 0.2f)] private float maxOffset = 0.04f;
    [Tooltip("How far the phone must tilt from its resting angle, in gravity units, to reach the full offset")]
    [SerializeField, Range(0.05f, 1f)] private float tiltForFullOffset = 0.3f;
    [Tooltip("How quickly the offset follows the input")]
    [SerializeField, Min(0.1f)] private float smoothing = 6f;
    [Tooltip("How quickly the resting angle adapts to the way the phone is being held. Lower keeps the tilt longer")]
    [SerializeField, Min(0f)] private float restAdaptSpeed = 0.5f;

    private Transform _rig;
    private Camera _camera;
    private Vector2 _tilt;
    private Vector3 _restGravity;
    private bool _hasRest;
    private InputDevice _sensor;

    /// <summary>The current camera offset in world units. Parallax layers follow a fraction of it.</summary>
    public static Vector3 Offset { get; private set; }

    private static bool IsEnabledInSettings => !SaveManager.Instance || SaveManager.Instance.Settings.tiltEnabled;

    private void Awake()
    {
        _camera = GetComponent<Camera>();

        // The shake tweens the camera's local position and rotation, so the tilt moves a parent and the two add up
        _rig = new GameObject("CameraTiltRig").transform;
        _rig.SetParent(transform.parent, false);
        transform.SetParent(_rig, true);
    }

    private void OnDisable()
    {
        ReleaseSensor();
        Offset = Vector3.zero;
        if (_rig) _rig.localPosition = Vector3.zero;
    }

    private void OnDestroy()
    {
        Offset = Vector3.zero;
    }

    private void LateUpdate()
    {
        bool active = IsEnabledInSettings;

        if (active && Application.isMobilePlatform) AcquireSensor();
        else ReleaseSensor();

        Vector2 target = active ? ReadInput() : Vector2.zero;

        // Unscaled, windows pause the game at a time scale of 0 and the view should still respond
        float blend = 1f - Mathf.Exp(-smoothing * Time.unscaledDeltaTime);
        _tilt = Vector2.Lerp(_tilt, target, blend);

        float reach = _camera.orthographic ? _camera.orthographicSize * maxOffset : maxOffset;
        Offset = new Vector3(_tilt.x, _tilt.y, 0f) * reach;
        _rig.localPosition = Offset;
    }

    private Vector2 ReadInput()
    {
        return Application.isMobilePlatform ? ReadDeviceTilt() : ReadMouse();
    }

    private static Vector2 ReadMouse()
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

        if (!_hasRest)
        {
            _restGravity = gravity;
            _hasRest = true;
        }

        _restGravity = Vector3.Lerp(_restGravity, gravity, 1f - Mathf.Exp(-restAdaptSpeed * Time.unscaledDeltaTime));

        Vector3 delta = gravity - _restGravity;
        return Vector2.ClampMagnitude(new Vector2(delta.x, delta.y) / tiltForFullOffset, 1f);
    }

    private bool TryReadGravity(out Vector3 gravity)
    {
        gravity = Vector3.zero;

        switch (_sensor)
        {
            case GravitySensor gravitySensor:
                gravity = gravitySensor.gravity.ReadValue();
                break;
            case Accelerometer accelerometer:
                // Includes hand movement as well as gravity, the smoothing in LateUpdate filters most of it out
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
    private void AcquireSensor()
    {
        if (_sensor != null) return;

        InputDevice candidate = GravitySensor.current;
        if (candidate == null) candidate = Accelerometer.current;
        if (candidate == null) return;

        InputSystem.EnableDevice(candidate);
        _sensor = candidate;
        _hasRest = false;
    }

    private void ReleaseSensor()
    {
        if (_sensor == null) return;

        if (_sensor.added) InputSystem.DisableDevice(_sensor);
        _sensor = null;
        _hasRest = false;
    }
}
