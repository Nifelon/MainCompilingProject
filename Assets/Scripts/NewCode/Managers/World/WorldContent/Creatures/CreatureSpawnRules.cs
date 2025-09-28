using System.Collections.Generic;
using UnityEngine;
using Game.World.Map.Biome;


namespace Game.World.Creatures
{
    [CreateAssetMenu(menuName = "World/Creatures/Creature Spawn Rules", fileName = "creature_spawn_rules")]
    public class CreatureSpawnRules : ScriptableObject
    {
        [System.Serializable]
        public struct WeightedGroup
        {
            public CreatureGroupProfile group;
            public int weight; // чем больше, тем чаще
        }


        [System.Serializable]
        public struct BiomeRule
        {
            public BiomeType biome;
            public List<WeightedGroup> groups; // набор групп, которые встречаются в этом биоме
            [Min(0f)] public float spawnDensity; // 0..1 — шанс на попытку группы в ячейке сетки
            [Min(1)] public int minGroupDistance; // мин. расстояние между центрами групп (клетки)
            [Min(0)] public int maxAlive; // общий лимит активных существ этого биома (0 = без лимита)
            [Min(0f)] public float respawnCooldown;// сек до повторной попытки, если группа была выбита
        }


        public List<BiomeRule> biomeRules = new();


        public bool TryGetRule(BiomeType biome, out BiomeRule rule)
        {
            for (int i = 0; i < biomeRules.Count; i++)
            {
                if (biomeRules[i].biome == biome) { rule = biomeRules[i]; return true; }
            }
            rule = default; return false;
        }
    }
}