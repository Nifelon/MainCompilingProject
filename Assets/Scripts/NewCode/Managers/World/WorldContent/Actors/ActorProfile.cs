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
        [FormerlySerializedAs("itemId")] public int legacy;   // ������ ��������
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
        public ActorIdKind idKind = ActorIdKind.Creature; // ��� �������� ����
        public NPCEnums npcId;                             // ���� NPC
        public CreatureEnums creatureId;                   // ���� ��������

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

        /// ��������� ������� � GO (Health/��������/AI) � ��������������� � �����������.
        public virtual void ApplyTo(GameObject go, Transform respawn = null)
        {
            if (!go) return;
            var h = go.GetComponent<Health>();
            if (h)
            {
                // ������� ������������� HP (��� ������) + ���������� ���� ��� Kill-�������
                h.ApplyConfig(UnitKind.Player, Mathf.RoundToInt(Mathf.Max(1, health)), 0, respawn, true);
                h.questIdOverride = GetQuestIdString(); // <- ����: ���� ID �� ����� enum���
                h.ResetForPool();
            }
            // ��������/AI � � ����������� (Creature/NPC), ��� �������� ���������� ����������
        }
    }
}