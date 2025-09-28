using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Actors
{
    public enum Faction { Neutral, Player, Bandits, Merchants, Animals }
    [Flags] public enum ActorTags { None = 0, Humanoid = 1 << 0, Beast = 1 << 1, Melee = 1 << 2, Ranged = 1 << 3, Boss = 1 << 4 }

    [Serializable] public struct LootEntry { public int itemId; public Vector2Int countRange; [Range(0, 1)] public float chance; }

    public abstract class ActorProfile : ScriptableObject
    {
        [Header("Identity")]
        public string id;
        public string displayName;
        public Sprite portrait;

        [Header("Faction/Tags")]
        public Faction faction = Faction.Neutral;
        public ActorTags tags;

        [Header("Perception")]
        public float viewDistance = 12;
        [Range(0, 360)] public float viewAngle = 120;
        public float hearingRadius = 8;

        [Header("Stats/Combat")]
        public float health = 100;
        public float moveSpeed = 3.5f;
        public float armor = 0;
        public float attackDamage = 10;
        public float attackRate = 1f;
        public float attackRange = 1.5f;
        public float projectileSpeed = 0; // 0 => melee

        [Header("Brain")]
        public string brainId; // ключ на BT/FSM

        [Header("Loot")]
        public List<LootEntry> loot = new();
    }
}