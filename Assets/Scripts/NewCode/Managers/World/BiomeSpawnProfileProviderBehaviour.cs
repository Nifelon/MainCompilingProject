using UnityEngine;
using Game.World.Map.Biome;
using Game.World.Objects.Spawning;

public class BiomeSpawnProfileProviderBehaviour : MonoBehaviour, IObjectSpawnRuleProvider
{
    [SerializeField] private BiomeSpawnProfileProvider asset; // ScriptableObject

    public bool TryGetProfile(BiomeType biome, out BiomeSpawnProfile profile)
    {
        if (asset != null) return asset.TryGetProfile(biome, out profile);
        profile = null; return false;
    }

    // опционально, чтобы было удобно дергать напрямую
    public BiomeSpawnProfile GetProfile(BiomeType biome)
        => asset != null && asset.TryGetProfile(biome, out var p) ? p : null;
}