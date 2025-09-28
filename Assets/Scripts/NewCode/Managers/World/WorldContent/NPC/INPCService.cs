using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Actors;       // Faction

namespace Game.World.NPC
{
    /// <summary>Фильтр для выборок: по фракции/роли/лагерю.</summary>
    public struct NpcFilter
    {
        public bool any;                 // игнорировать остальные поля (взять всех)
        public Faction? faction;
        public NpcRole? role;
        public int? campId;

        public static NpcFilter Any => new NpcFilter { any = true };
    }

    /// <summary>Плоская проекция данных NPC в реестре (позиция вычисляется на лету).</summary>
    public readonly struct NPCInfo
    {
        public readonly ulong id;
        public readonly Transform transform;
        public readonly GameObject go;
        public readonly NPCProfile profile;
        public readonly Faction faction;
        public readonly NpcRole role;
        public readonly int campId;

        /// <summary>Текущая позиция в мире (НЕ кэшируется).</summary>
        public Vector2 Position => transform ? (Vector2)transform.position : default;

        public NPCInfo(ulong id, Transform t, NPCProfile p, int campId)
        {
            this.id = id;
            transform = t;
            go = t ? t.gameObject : null;
            profile = p;
            faction = p ? p.faction : Faction.Neutral;
            role = p is not null ? p.role : NpcRole.Worker;
            this.campId = campId;
        }
    }

    /// <summary>Публичный интерфейс глобального реестра NPC.</summary>
    public interface INPCService
    {
        ulong Register(NPCAgent agent, NPCProfile profile, int campId);
        void Unregister(ulong id);

        bool TryGet(ulong id, out NPCInfo info);
        int Count { get; }

        void GetAll(List<NPCInfo> buffer, NpcFilter filter);
        void GetInRadius(Vector2 center, float radius, List<NPCInfo> buffer, NpcFilter filter);
        bool TryGetClosest(Vector2 center, float radius, NpcFilter filter, out NPCInfo result);

        event Action<NPCInfo> OnSpawned;
        event Action<NPCInfo> OnDespawned;
    }
}