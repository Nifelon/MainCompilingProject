using System;
using UnityEngine;
using Game.Actors;
using Game.World.Map.Biome;

namespace Game.World.Creatures
{
    [System.Flags] public enum BiomeMask { None = 0, Tundra = 1 << 0, Taiga = 1 << 1, Forest = 1 << 2, Savanna = 1 << 3, Desert = 1 << 4, Jungle = 1 << 5 }

    [CreateAssetMenu(menuName = "World/Creatures/CreatureProfile", fileName = "creature_")]
    public class CreatureProfile : ActorProfile
    {
        [Header("Habitat/Spawn")]
        public BiomeMask biomeMask;
        public Vector2Int packSize = new(1, 3);
        public float spawnDensity = 0.2f;      // дл€ мирового спавнера
        public float respawnCooldown = 60f;
        public int maxAlive = 10;

        [Header("Behavior")]
        public float agroRadius = 8f;
        public float fleeThresholdHp = 0.2f;   // 20% HP Ч бежит
        public float territorialRadius = 12f;
        public bool dayActive = true;
        public bool nightActive = false;
        public float cohesionRadius = 4f;      // Ђста€ї
    }
}