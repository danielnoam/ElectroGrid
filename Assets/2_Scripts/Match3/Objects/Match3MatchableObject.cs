using DNExtensions.Systems.ObjectPooling;
using UnityEngine;

[SelectionBase]
public class Match3MatchableObject : Match3SwappableObject
{

    public override bool IsMatchable => true;

    public override void Initialize(SOItemData data, Match3GridHandler gridHandler)
    {
        base.Initialize(data, gridHandler);

        if (data) UpdateEmissionMask(data.EmissionMask);
    }

    protected override void OnMatched()
    {
        Match3EffectManager.Instance?.CreateMatchBackgroundParticle(transform.position, _itemData);
    }

    protected override void SpawnDestroyParticle()
    {
        if (!destroyParticle) return;

        var particleGo = ObjectPooler.GetObjectFromPool(destroyParticle.gameObject, transform.position, Quaternion.identity);
        var poolableParticle = particleGo.GetComponent<PoolableParticleSystem>();
        var mainModule = poolableParticle.particle.main;
        var textureSheetModule = poolableParticle.particle.textureSheetAnimation;
        mainModule.startColor = itemRenderer.color;
        textureSheetModule.SetSprite(0, itemRenderer.sprite);
        poolableParticle.Play(transform.position);
    }

    private Material _baseMaterial;

    private void UpdateEmissionMask(Texture2D emissionMask)
    {
        if (!_baseMaterial) _baseMaterial = itemRenderer.sharedMaterial;
        itemRenderer.sharedMaterial = EmissionMaterials.Get(_baseMaterial, emissionMask);
    }
}
