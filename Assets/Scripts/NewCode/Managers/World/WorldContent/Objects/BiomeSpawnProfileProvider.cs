using System.Collections.Generic;
using UnityEngine;
using Game.World.Map.Biome;
using Game.World.Objects.Spawning;

[CreateAssetMenu(menuName = "World/Biome Spawn Profile Provider")]
public class BiomeSpawnProfileProvider : ScriptableObject, IObjectSpawnRuleProvider
{
    [SerializeField] private List<BiomeSpawnProfile> profiles = new();

    [Header("Fallback")]
    [Tooltip("Профиль по умолчанию для биомов без явного профиля (в т.ч. Biome.None).")]
    [SerializeField] private BiomeSpawnProfile defaultProfile;

    private readonly Dictionary<BiomeType, BiomeSpawnProfile> _map = new();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private readonly HashSet<BiomeType> _warned = new();
#endif

    void OnEnable() => Rebuild();
#if UNITY_EDITOR
    void OnValidate() => Rebuild();
#endif

    private void Rebuild()
    {
        _map.Clear();
        if (profiles != null)
        {
            foreach (var p in profiles)
                if (p != null) _map[p.biome] = p;
        }
        // хотим, чтобы даже None имел профиль
        if (defaultProfile) _map[BiomeType.None] = defaultProfile;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        _warned.Clear();
#endif
    }

    public BiomeSpawnProfile GetProfile(BiomeType biome)
    {
        if (_map.TryGetValue(biome, out var p) && p != null)
            return p;

        if (defaultProfile) return defaultProfile;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (_warned.Add(biome))
            Debug.LogWarning($"[BiomeSpawnProfileProvider] No profile for biome: {biome}");
#endif
        return null;
    }
}
