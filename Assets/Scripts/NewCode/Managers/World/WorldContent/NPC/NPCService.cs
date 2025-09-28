using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Actors;

namespace Game.World.NPC
{
    /// <summary>√лобальный реестр NPC. ƒержите один экземпл€р в сцене.</summary>
    [DefaultExecutionOrder(-150)]
    public class NPCService : MonoBehaviour, INPCService
    {
        private readonly Dictionary<ulong, NPCInfo> _byId = new();
        private readonly Dictionary<int, HashSet<ulong>> _campIndex = new();        // campId -> ids
        private readonly Dictionary<Faction, HashSet<ulong>> _factionIndex = new(); // faction -> ids
        private readonly Dictionary<NpcRole, HashSet<ulong>> _roleIndex = new();    // role -> ids
        private readonly List<NPCInfo> _tmp = new(capacity: 32);

        private ulong _nextId = 1;

        public int Count => _byId.Count;

        public event Action<NPCInfo> OnSpawned;
        public event Action<NPCInfo> OnDespawned;

        public ulong Register(NPCAgent agent, NPCProfile profile, int campId)
        {
            if (!agent || !profile)
            {
                Debug.LogWarning("[NPCService] Register: пустой agent или profile");
                return 0;
            }
            if (agent.RegisteredId != 0 && _byId.ContainsKey(agent.RegisteredId))
                return agent.RegisteredId;

            var id = _nextId++;
            var info = new NPCInfo(id, agent.transform, profile, campId);

            _byId[id] = info;
            agent.RegisteredId = id;
            IndexAdd(in info);

            OnSpawned?.Invoke(info);
            return id;
        }

        public void Unregister(ulong id)
        {
            if (id == 0) return;
            if (!_byId.TryGetValue(id, out var info)) return;

            _byId.Remove(id);
            IndexRemove(in info);

            OnDespawned?.Invoke(info);
        }

        public bool TryGet(ulong id, out NPCInfo info) => _byId.TryGetValue(id, out info);

        public void GetAll(List<NPCInfo> buffer, NpcFilter filter)
        {
            buffer.Clear();
            if (filter.any)
            {
                // берЄм сразу Values словар€
                foreach (var v in _byId.Values)
                    buffer.Add(v);
                return;
            }

            HashSet<ulong> seed = null;
            if (filter.campId.HasValue && _campIndex.TryGetValue(filter.campId.Value, out var byCamp)) seed = byCamp;
            if (filter.faction.HasValue && _factionIndex.TryGetValue(filter.faction.Value, out var byFaction)) seed = MergeSeed(seed, byFaction);
            if (filter.role.HasValue && _roleIndex.TryGetValue(filter.role.Value, out var byRole)) seed = MergeSeed(seed, byRole);

            if (seed == null)
            {
                // важно: нельз€ PassFilter(in kv.Value) Ч это свойство.
                // берЄм значение, кладЄм в локальную переменную и еЄ уже передаЄм как 'in'
                foreach (var v in _byId.Values)
                {
                    var info = v; // локальна€ переменна€ Ч теперь можно 'in'
                    if (PassFilter(in info, in filter))
                        buffer.Add(info);
                }
            }
            else
            {
                foreach (var id in seed)
                {
                    if (_byId.TryGetValue(id, out var info) && PassFilter(in info, in filter))
                        buffer.Add(info);
                }
            }
        }

        public void GetInRadius(Vector2 center, float radius, List<NPCInfo> buffer, NpcFilter filter)
        {
            GetAll(buffer, filter);
            float r2 = radius * radius;
            int w = 0;
            for (int i = 0; i < buffer.Count; i++)
            {
                var info = buffer[i];
                var d2 = (info.Position - center).sqrMagnitude; // <-- живые координаты
                if (d2 <= r2) buffer[w++] = info;
            }
            if (w < buffer.Count) buffer.RemoveRange(w, buffer.Count - w);
        }

        public bool TryGetClosest(Vector2 center, float radius, NpcFilter filter, out NPCInfo result)
        {
            var tmp = _tmp; tmp.Clear();
            GetInRadius(center, radius, tmp, filter);

            float best = float.MaxValue;
            result = default;
            foreach (var info in tmp)
            {
                float d2 = (info.Position - center).sqrMagnitude; // <-- живые координаты
                if (d2 < best) { best = d2; result = info; }
            }
            return best < float.MaxValue;
        }

        // -------- internals --------

        private static bool PassFilter(in NPCInfo info, in NpcFilter f)
        {
            if (f.any) return true;
            if (f.campId.HasValue && info.campId != f.campId.Value) return false;
            if (f.faction.HasValue && info.faction != f.faction.Value) return false;
            if (f.role.HasValue && info.role != f.role.Value) return false;
            return true;
        }

        private static HashSet<ulong> MergeSeed(HashSet<ulong> a, HashSet<ulong> b)
        {
            if (a == null) return b;
            if (b == null) return a;
            return (a.Count <= b.Count) ? a : b; // минимизируем перебор, итог всЄ равно прогон€етс€ через PassFilter
        }

        private void IndexAdd(in NPCInfo info)
        {
            if (!_campIndex.TryGetValue(info.campId, out var byCamp)) _campIndex[info.campId] = byCamp = new();
            byCamp.Add(info.id);

            if (!_factionIndex.TryGetValue(info.faction, out var byFaction)) _factionIndex[info.faction] = byFaction = new();
            byFaction.Add(info.id);

            if (!_roleIndex.TryGetValue(info.role, out var byRole)) _roleIndex[info.role] = byRole = new();
            byRole.Add(info.id);
        }

        private void IndexRemove(in NPCInfo info)
        {
            if (_campIndex.TryGetValue(info.campId, out var byCamp)) { byCamp.Remove(info.id); if (byCamp.Count == 0) _campIndex.Remove(info.campId); }
            if (_factionIndex.TryGetValue(info.faction, out var byFaction)) { byFaction.Remove(info.id); if (byFaction.Count == 0) _factionIndex.Remove(info.faction); }
            if (_roleIndex.TryGetValue(info.role, out var byRole)) { byRole.Remove(info.id); if (byRole.Count == 0) _roleIndex.Remove(info.role); }
        }
    }
}