using UnityEngine;

/// <summary>
/// Follows part of the <see cref="CameraManager"/> tilt offset. An orthographic camera has no depth of its own, so a layer
/// that moves with the camera appears to slide less on screen, which reads as further back.
/// </summary>
[DisallowMultipleComponent]
public class ParallaxLayer : MonoBehaviour
{
    [Tooltip("0 moves with the board, 1 moves with the camera and appears infinitely far away")]
    [SerializeField, Range(0f, 1f)] private float depth = 0.6f;

    private Vector3 _basePosition;

    public float Depth
    {
        get => depth;
        set => depth = Mathf.Clamp01(value);
    }

    private void OnEnable()
    {
        _basePosition = transform.localPosition;
    }

    private void OnDisable()
    {
        transform.localPosition = _basePosition;
    }

    private void LateUpdate()
    {
        Vector3 offset = CameraManager.Instance ? CameraManager.Instance.TiltOffset : Vector3.zero;
        transform.localPosition = _basePosition + offset * depth;
    }
}
