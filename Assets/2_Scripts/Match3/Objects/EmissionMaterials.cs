using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One shared material per piece material and emission mask, so pieces of the same kind share a material instead of
/// each making its own copy through renderer.material. Fewer materials, and the SRP Batcher keeps them batched.
/// </summary>
public static class EmissionMaterials
{
    private static readonly int EmissionMaskId = Shader.PropertyToID("_Emission_Mask");
    private static readonly Dictionary<(Material, Texture), Material> Cache = new Dictionary<(Material, Texture), Material>();

    public static Material Get(Material source, Texture mask)
    {
        if (!source) return null;

        var key = (source, mask);
        if (Cache.TryGetValue(key, out var material) && material) return material;

        material = new Material(source) { name = $"{source.name} ({(mask ? mask.name : "no mask")})", hideFlags = HideFlags.DontSave };
        material.SetTexture(EmissionMaskId, mask);
        Cache[key] = material;
        return material;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Cache.Clear();
    }
}
