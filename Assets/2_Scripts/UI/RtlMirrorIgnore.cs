using UnityEngine;

/// <summary>
/// Keeps everything under this object out of <see cref="RtlLayoutMirror"/>, for UI whose left and right carry
/// meaning, like the level shape preview, or that orders itself for right-to-left text.
/// </summary>
[DisallowMultipleComponent]
public class RtlMirrorIgnore : MonoBehaviour
{
    [Tooltip("Still move this object to the mirrored side, keeping only its contents as they are. " +
             "The level shape preview moves to the left with the layout, but the shape itself is not flipped")]
    [SerializeField] private bool mirrorSelf;

    public bool MirrorSelf => mirrorSelf;
}
