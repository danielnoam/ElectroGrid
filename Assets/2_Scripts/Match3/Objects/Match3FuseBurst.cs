using DNExtensions.Systems.ObjectPooling;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class Match3FuseBurst : MonoBehaviour, IPoolable
{
    private static readonly int FillId = Shader.PropertyToID("_Fill");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int SparkColorId = Shader.PropertyToID("_SparkColor");
    private static readonly int ThicknessId = Shader.PropertyToID("_Thickness");

    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField, Min(1f)] private float scale = 3f;
    [SerializeField, Min(0.05f)] private float duration = 0.6f;
    [SerializeField, Min(0f)] private float startThickness = 0.09f;
    [SerializeField, Min(0f)] private float endThickness = 0.02f;

    private MaterialPropertyBlock _block;
    private Vector3 _startScale;
    private Color _color;
    private float _time;
    private bool _playing;

    public static void Play(Match3FuseBurst prefab, SpriteRenderer ring, Color color)
    {
        if (!prefab || !ring) return;

        var burst = ObjectPooler.GetObjectFromPool(prefab, ring.transform.position, Quaternion.identity);
        if (burst) burst.Begin(ring, color);
    }

    private void Begin(SpriteRenderer ring, Color color)
    {
        if (!spriteRenderer) spriteRenderer = GetComponent<SpriteRenderer>();
        _block ??= new MaterialPropertyBlock();

        transform.localScale = ring.transform.lossyScale;
        spriteRenderer.sprite = ring.sprite;
        spriteRenderer.sharedMaterial = ring.sharedMaterial;
        spriteRenderer.sortingLayerID = ring.sortingLayerID;
        spriteRenderer.sortingOrder = ring.sortingOrder + 1;

        _startScale = transform.localScale;
        _color = color;
        _time = 0f;
        _playing = true;
        Apply(0f);
    }

    private void Update()
    {
        if (!_playing) return;

        _time += Mathf.Min(Time.deltaTime, 1f / 30f);
        float t = Mathf.Clamp01(_time / Mathf.Max(0.05f, duration));
        Apply(t);

        if (t < 1f) return;

        _playing = false;
        ObjectPooler.ReturnObjectToPool(gameObject);
    }

    private void Apply(float t)
    {
        float grow = 1f - (1f - t) * (1f - t) * (1f - t);
        transform.localScale = Vector3.LerpUnclamped(_startScale, _startScale * Mathf.Max(1f, scale), grow);

        float strength = 1.5f * (1f - t * t);
        _block.SetFloat(FillId, 1f);
        _block.SetColor(ColorId, _color * strength);
        _block.SetColor(SparkColorId, Color.black);
        _block.SetFloat(ThicknessId, Mathf.Lerp(startThickness, endThickness, t));
        spriteRenderer.SetPropertyBlock(_block);
    }

    public void OnPoolGet()
    {
    }

    public void OnPoolReturn()
    {
        _playing = false;
    }

    public void OnPoolRecycle()
    {
        _playing = false;
    }
}
