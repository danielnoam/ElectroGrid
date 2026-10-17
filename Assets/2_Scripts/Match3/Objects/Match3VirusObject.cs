using DNExtensions.Systems.ObjectPooling;
using PrimeTween;
using UnityEngine;

[SelectionBase]
public class Match3VirusObject : Match3ObstacleObject
{

    [Header("Virus")]
    [SerializeField] private Texture2D emissionMask;
    [SerializeField, Min(0f)] private float wobbleSpeed = 1.4f;
    [SerializeField, Min(0f)] private float wobbleAngle = 12f;
    [SerializeField, Min(0.01f)] private float spreadDuration = 0.35f;

    private float _wobbleOffset;
    private bool _sharedMaterialSet;

    public override void Initialize(SOItemData data, Match3GridHandler gridHandler)
    {
        base.Initialize(data, gridHandler);

        _wobbleOffset = Random.Range(0f, 10f);
        if (emissionMask && itemRenderer && !_sharedMaterialSet)
        {
            itemRenderer.sharedMaterial = EmissionMaterials.Get(itemRenderer.sharedMaterial, emissionMask);
            _sharedMaterialSet = true;
        }
    }

    private void LateUpdate()
    {
        if (!itemRenderer) return;

        float angle = Mathf.Sin((Time.time + _wobbleOffset) * wobbleSpeed) * wobbleAngle + (Time.time + _wobbleOffset) * wobbleAngle;
        itemRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    /// <summary>Grows out of the virus at <paramref name="from"/> into its own cell, instead of popping in.</summary>
    public void SpreadFrom(Vector3 from)
    {
        if (!_currentTile) return;

        _movementSequence.Stop();
        var target = _currentTile.transform.position;
        transform.position = new Vector3(from.x, from.y, transform.position.z);
        transform.localScale = _baseScale * 0.35f;

        _movementSequence = Sequence.Create()
            .Group(Tween.Position(transform, new Vector3(target.x, target.y, transform.position.z), spreadDuration, Ease.OutBack))
            .Group(Tween.Scale(transform, _baseScale, spreadDuration, Ease.OutBack));

        SpawnSpreadParticle(target);
    }

    private void SpawnSpreadParticle(Vector3 position)
    {
        if (!destroyParticle) return;

        var particleGo = ObjectPooler.GetObjectFromPool(destroyParticle.gameObject, position, Quaternion.identity);
        var particle = particleGo.GetComponent<PoolableParticleSystem>();
        particle.Play(position);
    }
}
