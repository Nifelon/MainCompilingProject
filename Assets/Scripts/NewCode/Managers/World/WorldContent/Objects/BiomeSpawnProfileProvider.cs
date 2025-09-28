using System.Collections.Generic;
using UnityEngine;
using Game.World.Map.Biome;

[CreateAssetMenu(menuName = "World/Biome Spawn Profile Provider")]
public class BiomeSpawnProfileProvider : ScriptableObject
{
    [SerializeField] private List<BiomeSpawnProfile> profiles = new();
    [Header("Fallback")]
    [Tooltip("Профиль по умолчанию, если для биома нет явного профиля. Рекомендуется указать.")]
    [SerializeField] private BiomeSpawnProfile defaultProfile;

    private readonly Dictionary<BiomeType, BiomeSpawnProfile> _map = new();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private HashSet<BiomeType> _warned = new();
#endif

    private void OnEnable() { Rebuild(); }
#if UNITY_EDITOR
    private void OnValidate() { Rebuild(); }
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
        _warned?.Clear();
#endif
    }

    public BiomeSpawnProfile GetProfile(BiomeType biome)
    {
        if (_map.TryGetValue(biome, out var p) && p != null)
            return p;

        if (defaultProfile) return defaultProfile;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (_warned != null && !_warned.Contains(biome))
        {
            _warned.Add(biome);
            Debug.LogWarning($"[BiomeSpawnProfileProvider] No profile for biome: {biome}");
        }
#endif
        return null;
    }
}
