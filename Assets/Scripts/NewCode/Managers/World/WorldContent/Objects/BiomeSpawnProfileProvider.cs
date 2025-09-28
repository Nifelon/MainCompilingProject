using System.Collections.Generic;
using UnityEngine;
using Game.World.Map.Biome;

namespace Game.World.Objects.Spawning
{
    /// Хранит индекс профилей и быстро отдаёт нужный по биому.
    public sealed class BiomeSpawnProfileProvider : MonoBehaviour, IObjectSpawnRuleProvider
    {
        [Header("Profiles (SO)")]
        [SerializeField] private List<BiomeSpawnProfile> profiles = new();

        private Dictionary<BiomeType, BiomeSpawnProfile> _byBiome;

        void Awake() => BuildIndex();

        public void BuildIndex()
        {
            _byBiome = new Dictionary<BiomeType, BiomeSpawnProfile>(profiles?.Count ?? 0);
            if (profiles == null) return;

            foreach (var p in profiles)
                if (p != null)
                    _byBiome[p.biome] = p; // последний с таким биомом «перекрывает» предыдущие
        }

        public BiomeSpawnProfile GetProfile(BiomeType biome)
        {
            if (_byBiome != null && _byBiome.TryGetValue(biome, out var p))
                return p;

            Debug.LogWarning($"[BiomeSpawnProfileProvider] No profile for biome: {biome}");
            return null;
        }

        public bool TryGetProfile(BiomeType biome, out BiomeSpawnProfile profile)
        {
            profile = GetProfile(biome);
            return profile != null;
        }
    }
}