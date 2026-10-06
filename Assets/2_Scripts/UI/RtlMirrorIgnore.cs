using UnityEngine;

/// <summary>
/// Keeps this object and everything under it out of <see cref="RtlLayoutMirror"/>, for UI whose left and right
/// carry meaning, like the level shape preview, or that orders itself for right-to-left text.
/// </summary>
[DisallowMultipleComponent]
public class RtlMirrorIgnore : MonoBehaviour
{
}
