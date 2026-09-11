using DNExtensions.ObjectPooling;
using UnityEngine;

[SelectionBase]
public class Match3MatchableObject : Match3SwappableObject
{
    private static readonly int EmissionMask = Shader.PropertyToID("_Emission_Mask");

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
        var oneShotParticle = particleGo.GetComponent<OneShotParticle>();
        var mainModule = oneShotParticle.particle.main;
        var textureSheetModule = oneShotParticle.particle.textureSheetAnimation;
        mainModule.startColor = itemRenderer.color;
        textureSheetModule.SetSprite(0, itemRenderer.sprite);
        oneShotParticle.Play(transform.position);
    }

    private void UpdateEmissionMask(Texture2D emissionMask)
    {
        itemRenderer.material.SetTexture(EmissionMask, emissionMask);
    }
}
