using System.Collections.Generic;
using UnityEngine;
using Game.World.Map.Biome;   // важно: нужен BiomeType

namespace Game.World.Creatures
{
    [CreateAssetMenu(menuName = "World/Creatures/Creature Spawn Rules", fileName = "CreatureSpawnRules")]
    public class CreatureSpawnRules : ScriptableObject
    {
        [System.Serializable]
        public struct WeightedGroup
        {
            public CreatureGroupProfile group;
            public int weight;
        }

        [System.Serializable]
        public struct BiomeRule
        {
            public BiomeType biome;
            public List<WeightedGroup> groups;
            [Min(0f)] public float spawnDensity;
            [Min(1)] public int minGroupDistance;
            [Min(0)] public int maxAlive;
            [Min(0f)] public float respawnCooldown;
        }

        public List<BiomeRule> biomeRules = new();

        [Header("Fallback (used when no direct rule for biome)")]
        public bool useFallbackForUnknownBiomes = true;
        public List<WeightedGroup> fallbackGroups = new();   // сюда DeerHerd/WolfPack
        [Min(0f)] public float fallbackSpawnDensity = 0.35f;
        [Min(1)] public int fallbackMinGroupDistance = 18;
        [Min(0)] public int fallbackMaxAlive = 0;
        [Min(0f)] public float fallbackRespawnCooldown = 60f;

        public bool TryGetRule(BiomeType biome, out BiomeRule rule)
        {
            for (int i = 0; i < biomeRules.Count; i++)
                if (biomeRules[i].biome == biome) { rule = biomeRules[i]; return true; }
            rule = default; return false;
        }

        // ← ЭТОГО метода у тебя не было
        public bool TryGetRuleOrFallback(BiomeType biome, out BiomeRule rule)
        {
            if (TryGetRule(biome, out rule)) return true;

            if (useFallbackForUnknownBiomes && fallbackGroups != null && fallbackGroups.Count > 0)
            {
                rule = new BiomeRule
                {
                    biome = biome,
                    groups = fallbackGroups,
                    spawnDensity = fallbackSpawnDensity,
                    minGroupDistance = fallbackMinGroupDistance,
                    maxAlive = fallbackMaxAlive,
                    respawnCooldown = fallbackRespawnCooldown
                };
                return true;
            }

            rule = default;
            return false;
        }
    }
}