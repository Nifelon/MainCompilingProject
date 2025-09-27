using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Actors;

namespace Game.World.NPC
{
    public enum NpcRole { Guard, Patrol, Trader, Healer, Worker, Leader }
    [Flags] public enum NpcTags { None = 0, Trader = 1 << 0, Healer = 1 << 1, Leader = 1 << 2 }
    public enum SpawnAnchor { Any, Center, Fire, Gate, Tents, Perimeter }

    [Serializable] public struct LoadoutItem { public int itemId; public int count; [Range(0, 1)] public float chance; }

    [CreateAssetMenu(menuName = "World/NPC/NpcProfile", fileName = "npc_")]
    public class NPCProfile : ActorProfile
    {
        [Header("NPC Role & Behavior")]
        public NpcRole role = NpcRole.Guard;
        public NpcTags npcTags;
        public SpawnAnchor preferredAnchor = SpawnAnchor.Center;
        public float leashRadius = 15f;
        public float wanderRadius = 5f;
        public float patrolSpeedMul = 1f;
        public float chaseSpeedMul = 1.15f;

        [Header("Loadout / Interactions")]
        public List<LoadoutItem> loadout = new();
        public UnityEngine.Object dialogueRef;
        public bool isTrader;
        public int traderInventoryId;
        public bool isQuestGiver;
        public AudioClip[] voiceSet;
    }
}