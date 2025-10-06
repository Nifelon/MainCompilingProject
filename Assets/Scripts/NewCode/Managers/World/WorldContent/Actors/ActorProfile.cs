using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Game.Items;

namespace Game.Actors
{
    public static class ActorIds { public const string Player = "Player"; }
    public enum Faction { Neutral, Player, Bandits, Merchants, Animals }
    [Flags] public enum ActorTags { None = 0, Humanoid = 1 << 0, Beast = 1 << 1, Melee = 1 << 2, Ranged = 1 << 3, Boss = 1 << 4 }

    [Serializable]
    public struct LootEntry
    {
        public ItemId item;
        [FormerlySerializedAs("itemId")] public int legacy;   // м€гка€ миграци€
        public Vector2Int countRange;
        [Range(0, 1)] public float chance;
        public bool TryResolve(out ItemId id)
        {
            if (item != ItemId.None) { id = item; return true; }
            if (legacy != 0 && Enum.IsDefined(typeof(ItemId), legacy)) { id = (ItemId)legacy; return true; }
            id = ItemId.None; return false;
        }
    }

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
        public float projectileSpeed = 0;

        [Header("Brain")]
        public string brainId;

        [Header("Quest/Identity (enums)")]
        public ActorIdKind idKind = ActorIdKind.Creature; // чем €вл€етс€ актЄр
        public NPCEnums npcId;                             // если NPC
        public CreatureEnums creatureId;                   // если существо

        public string GetQuestIdString()
        {
            switch (idKind)
            {
                case ActorIdKind.NPC: return npcId.ToString();
                case ActorIdKind.Creature: return creatureId.ToString();
                default: return "Player";
            }
        }

        [Header("Loot")]
        public List<LootEntry> loot = new();

        /// ѕрименить профиль к GO (Health/движение/AI) Ч переопредел€йте в наследниках.
        public virtual void ApplyTo(GameObject go, Transform respawn = null)
        {
            if (!go) return;
            var h = go.GetComponent<Health>();
            if (h)
            {
                // базова€ инициализаци€ HP (без регена) + корректна€ цель дл€ Kill-квестов
                h.ApplyConfig(UnitKind.Player, Mathf.RoundToInt(Mathf.Max(1, health)), 0, respawn, true);
                h.questIdOverride = GetQuestIdString(); // <- ключ: берЄм ID из ваших enumТов
                h.ResetForPool();
            }
            // движение/AI Ч в наследниках (Creature/NPC), где известны конкретные компоненты
        }
    }
}