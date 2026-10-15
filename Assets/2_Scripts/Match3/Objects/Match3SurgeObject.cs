using UnityEngine;

/// <summary>
/// Left behind by a match of five or more in a line. It holds the piece it was made from, and swapping it with any
/// neighbour zaps every piece of that kind on the board.
/// </summary>
[SelectionBase]
public class Match3SurgeObject : Match3SwappableObject
{
    [Header("Surge")]
    [SerializeField] private Sprite frameSprite;
    [SerializeField, Range(0.1f, 1f)] private float coreScale = 0.5f;
    [SerializeField] private float shakeDistance = 0.025f;
    [SerializeField] private float shakeAngle = 6f;
    [SerializeField] private float shakeSpeed = 18f;

    private SpriteRenderer _core;
    private SOItemData _target;
    private float _shakeSeed;

    public override bool IsMatchable => false;
    public SOItemData Target => _target;

    public override void Initialize(SOItemData data, Match3GridHandler gridHandler)
    {
        base.Initialize(null, gridHandler);

        _target = data;
        _shakeSeed = Random.value * 100f;
        if (itemRenderer)
        {
            itemRenderer.sprite = frameSprite ? frameSprite : Resources.Load<Sprite>("Sprites/ShapeSurge");
            itemRenderer.color = data ? data.Color : Color.white;
            itemRenderer.transform.localRotation = Quaternion.identity;
        }

        EnsureCore();
        _core.sprite = data ? data.Sprite : null;
        _core.color = data ? data.Color : Color.white;
        gameObject.name = data ? $"Match3SurgeObject ({data.Label})" : "Match3SurgeObject";
    }

    private void Update()
    {
        if (!_core) return;

        float t = Time.time * shakeSpeed + _shakeSeed;
        float x = Mathf.PerlinNoise(t, 0f) * 2f - 1f;
        float y = Mathf.PerlinNoise(0f, t) * 2f - 1f;
        float z = Mathf.PerlinNoise(t, t) * 2f - 1f;
        _core.transform.localPosition = new Vector3(x, y, 0f) * shakeDistance;
        _core.transform.localRotation = Quaternion.Euler(0f, 0f, z * shakeAngle);
    }

    protected override void OnMatched()
    {
    }

    private void EnsureCore()
    {
        if (_core) return;

        var coreObject = new GameObject("Core");
        coreObject.transform.SetParent(itemRenderer ? itemRenderer.transform : transform, false);
        coreObject.transform.localScale = Vector3.one * coreScale;
        _core = coreObject.AddComponent<SpriteRenderer>();
        if (!itemRenderer) return;

        _core.sharedMaterial = itemRenderer.sharedMaterial;
        _core.sortingLayerID = itemRenderer.sortingLayerID;
        _core.sortingOrder = itemRenderer.sortingOrder + 1;
    }
}
