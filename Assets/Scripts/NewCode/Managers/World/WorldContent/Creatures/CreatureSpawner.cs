
// CreatureSpawner.cs — планировщик групп существ (данные-только)
// Работает без WorldContext: умеет авто-инициализироваться, если включён флаг
// Требует: IBiomeService, (опц.) IReservationService, CreatureSpawnRules

using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Core;
using Game.World.Map.Biome;

namespace Game.World.Creatures
{
    [DefaultExecutionOrder(-220)]
    public class CreatureSpawner : MonoBehaviour, IWorldSystem
    {
        [Header("Refs")]
        [SerializeField] private Transform player;
        [SerializeField] private MonoBehaviour biomeServiceRef;    // IBiomeService
        [SerializeField] private MonoBehaviour reservationRef;     // IReservationService

        private IBiomeService _biomes;
        private IReservationService _reserv;

        [Header("Rules & World")]
        [SerializeField] private CreatureSpawnRules rules;
        [SerializeField] private int chunkSize = 64;
        [SerializeField] private int streamRadiusChunks = 3;
        [SerializeField] private float cellSize = 1f;
        [SerializeField] private int worldSeed = 12345;

        [Header("Sampling")]
        [Tooltip("Шаг сетки попыток в чанке (клетки). Чем меньше — тем больше шансов появления групп.")]
        [SerializeField, Min(4)] private int gridStepCells = 16;

        [Header("Lifecycle")]
        [Tooltip("Если мир не вызывает Initialize(WorldContext), включи это, чтобы компонент сам инициализировался.")]
        [SerializeField] private bool autoInitialize = true;

        [Header("Debug")]
        [SerializeField] private bool verbose = false;
        [SerializeField] private bool logReasons = false;

        // ===== Planned data =====
        [Serializable]
        public struct PlannedUnit
        {
            public ulong id;
            public Vector2Int cell;
            public CreatureProfile profile;
            public int groupId;
        }

        private sealed class PlannedGroup
        {
            public int groupId;
            public Vector2Int center;
            public CreatureGroupProfile group;
            public readonly List<PlannedUnit> members = new();
        }

        private readonly HashSet<Vector2Int> _activeChunks = new();
        private readonly Dictionary<Vector2Int, List<PlannedGroup>> _chunkGroups = new();
        private Vector2Int _lastPlayerChunk;
        private bool _inited;

        public int Order => -220;

        private void Awake()
        {
            if (autoInitialize && !_inited)
            {
                // Попробуем авто-инициализироваться без WorldContext
                Initialize(null);
            }
        }

        public void Initialize(WorldContext ctx)
        {
            _biomes = biomeServiceRef as IBiomeService;
            _reserv = reservationRef as IReservationService;

            if (_biomes == null)
            {
                Debug.LogError("[CreatureSpawner] IBiomeService is NULL — укажи BiomeManager в инспекторе biomeServiceRef");
                return;
            }
            if (rules == null)
            {
                Debug.LogError("[CreatureSpawner] CreatureSpawnRules is NULL — укажи ассет правил в инспекторе");
                return;
            }

            _activeChunks.Clear();
            _chunkGroups.Clear();

            var pos = player ? player.position : Vector3.zero;
            _lastPlayerChunk = WorldToChunk(WorldToCell(pos));
            UpdateStreaming(_lastPlayerChunk);
            _inited = true;

            if (verbose)
                Debug.Log($"[CreatureSpawner] Init OK. chunkSize={chunkSize}, gridStep={gridStepCells}, streamRadius={streamRadiusChunks}");
        }

        private void Update()
        {
            if (!_inited) return;
            if (player == null) return;

            var pc = WorldToChunk(WorldToCell(player.position));
            if (pc == _lastPlayerChunk) return;
            _lastPlayerChunk = pc;
            UpdateStreaming(pc);
        }

        // ========== Public API (for PoolingCreatures) ==========
        public int GatherPlannedCreaturesWithinRadius(Vector2Int centerCell, int radiusCells, List<PlannedUnit> buffer)
        {
            buffer.Clear();
            int r2 = radiusCells * radiusCells;
            foreach (var kv in _chunkGroups)
            {
                var list = kv.Value;
                for (int i = 0; i < list.Count; i++)
                {
                    var g = list[i];
                    var mem = g.members;
                    for (int j = 0; j < mem.Count; j++)
                    {
                        var u = mem[j];
                        var d = u.cell - centerCell;
                        if (d.sqrMagnitude <= r2) buffer.Add(u);
                    }
                }
            }
            return buffer.Count;
        }

        // ========== Streaming ==========
        private void UpdateStreaming(Vector2Int centerChunk)
        {
            var wanted = WantedChunks(centerChunk, streamRadiusChunks);

            // LOAD
            foreach (var ch in wanted)
                if (_activeChunks.Add(ch)) LoadChunk(ch);

            // UNLOAD
            _toRemove.Clear();
            foreach (var ch in _activeChunks) if (!wanted.Contains(ch)) _toRemove.Add(ch);
            for (int i = 0; i < _toRemove.Count; i++) UnloadChunk(_toRemove[i]);
            for (int i = 0; i < _toRemove.Count; i++) _activeChunks.Remove(_toRemove[i]);
        }

        private void LoadChunk(Vector2Int chunk)
        {
            var baseCell = ChunkToWorldCell(chunk);
            int step = Mathf.Clamp(gridStepCells, 4, chunkSize);
            var rng = new System.Random(Hash3(worldSeed, chunk.x, chunk.y));

            var list = new List<PlannedGroup>(4);
            int attempts = 0, placed = 0;

            for (int ox = 0; ox < chunkSize; ox += step)
                for (int oy = 0; oy < chunkSize; oy += step)
                {
                    attempts++;
                    var cell = new Vector2Int(baseCell.x + ox, baseCell.y + oy);
                    var biome = _biomes.GetBiomeAtPosition(cell);

                    if (!rules.TryGetRule(biome, out var rule))
                    {
                        if (logReasons) Debug.Log($"[CreatureSpawner] skip(no rule) biome={biome} at {cell}");
                        continue;
                    }

                    if (rng.NextDouble() > Mathf.Clamp01(rule.spawnDensity))
                    {
                        if (logReasons) Debug.Log($"[CreatureSpawner] skip(density) biome={biome} at {cell}");
                        continue;
                    }

                    if (_reserv != null && _reserv.IsReserved(cell, ReservationMask.Creatures))
                    {
                        if (logReasons) Debug.Log($"[CreatureSpawner] skip(reserved center) at {cell}");
                        continue;
                    }

                    if (!IsFarFromOtherGroups(cell, rule.minGroupDistance))
                    {
                        if (logReasons) Debug.Log($"[CreatureSpawner] skip(too close to other group) at {cell}");
                        continue;
                    }

                    var grp = PickGroup(rule.groups, rng);
                    if (!grp)
                    {
                        if (logReasons) Debug.Log($"[CreatureSpawner] skip(no group pick) at {cell}");
                        continue;
                    }

                    var pg = BuildPlannedGroup(cell, grp, rng);
                    if (pg.members.Count == 0)
                    {
                        if (logReasons) Debug.Log($"[CreatureSpawner] skip(empty group after placement) at {cell}");
                        continue;
                    }

                    list.Add(pg);
                    placed++;
                }

            if (list.Count > 0) _chunkGroups[chunk] = list;
            if (verbose) Debug.Log($"[CreatureSpawner] Loaded {chunk}, attempts={attempts}, groups={placed}, step={step}");
        }

        private void UnloadChunk(Vector2Int chunk)
        {
            _chunkGroups.Remove(chunk);
            if (verbose) Debug.Log($"[CreatureSpawner] Unloaded {chunk}");
        }

        private CreatureGroupProfile PickGroup(List<CreatureSpawnRules.WeightedGroup> list, System.Random rng)
        {
            if (list == null || list.Count == 0) return null;
            int sum = 0; for (int i = 0; i < list.Count; i++) sum += Mathf.Max(0, list[i].weight);
            if (sum <= 0) return list[0].group;

            int r = rng.Next(sum), acc = 0;
            for (int i = 0; i < list.Count; i++)
            {
                acc += Mathf.Max(0, list[i].weight);
                if (r < acc) return list[i].group;
            }
            return list[^1].group;
        }

        private PlannedGroup BuildPlannedGroup(Vector2Int center, CreatureGroupProfile grp, System.Random rng)
        {
            var pg = new PlannedGroup
            {
                groupId = Hash3(worldSeed, center.x, center.y),
                center = center,
                group = grp
            };

            if (grp.members != null)
            {
                foreach (var m in grp.members)
                {
                    if (!m.profile) continue;
                    int cnt = Mathf.Clamp(NextRange(rng, m.count.x, m.count.y), 0, 999);

                    for (int i = 0; i < cnt; i++)
                    {
                        var off = RandInCircle(grp.cohesionRadius, rng) + RandInCircle(grp.formationJitter, rng);
                        var cell = new Vector2Int(center.x + Mathf.RoundToInt(off.x), center.y + Mathf.RoundToInt(off.y));

                        if (_reserv != null && _reserv.IsReserved(cell, ReservationMask.Creatures))
                        {
                            if (logReasons) Debug.Log($"[CreatureSpawner] member skip(reserved) at {cell}");
                            continue;
                        }

                        ulong id = MakeId(center, m.profile, i);
                        pg.members.Add(new PlannedUnit { id = id, cell = cell, profile = m.profile, groupId = pg.groupId });
                    }
                }
            }
            return pg;
        }

        private bool IsFarFromOtherGroups(Vector2Int cell, int minDist)
        {
            if (minDist <= 0) return true;
            int r2 = minDist * minDist;
            foreach (var kv in _chunkGroups)
            {
                var list = kv.Value; if (list == null) continue;
                for (int i = 0; i < list.Count; i++)
                {
                    var d = list[i].center - cell;
                    if (d.sqrMagnitude < r2) return false;
                }
            }
            return true;
        }

        // ===== Helpers =====
        private Vector2Int WorldToCell(Vector3 w) => new(Mathf.RoundToInt(w.x / cellSize), Mathf.RoundToInt(w.y / cellSize));
        private Vector2Int WorldToChunk(Vector2Int cell)
        {
            int cx = cell.x >= 0 ? cell.x / chunkSize : (cell.x - (chunkSize - 1)) / chunkSize;
            int cy = cell.y >= 0 ? cell.y / chunkSize : (cell.y - (chunkSize - 1)) / chunkSize;
            return new Vector2Int(cx, cy);
        }
        private Vector2Int ChunkToWorldCell(Vector2Int chunk) => new(chunk.x * chunkSize, chunk.y * chunkSize);

        private HashSet<Vector2Int> WantedChunks(Vector2Int center, int r)
        {
            var set = new HashSet<Vector2Int>();
            for (int dx = -r; dx <= r; dx++)
                for (int dy = -r; dy <= r; dy++)
                    set.Add(new Vector2Int(center.x + dx, center.y + dy));
            return set;
        }

        private static int NextRange(System.Random rng, int minInclusive, int maxInclusive)
        { if (minInclusive > maxInclusive) (minInclusive, maxInclusive) = (maxInclusive, minInclusive); return minInclusive + rng.Next(maxInclusive - minInclusive + 1); }
        private static int Hash3(int a, int b, int c) => (a * 73856093) ^ (b * 19349663) ^ (c * 83492791);
        private static ulong MakeId(Vector2Int center, CreatureProfile p, int idx)
        { unchecked { uint h = (uint)Hash3(center.x, center.y, p ? p.name.GetHashCode() : 0); return ((ulong)h << 32) | (uint)idx; } }
        private static Vector2 RandInCircle(float r, System.Random rng)
        {
            double ang = rng.NextDouble() * Math.PI * 2.0; double rad = rng.NextDouble() * r;
            return new Vector2((float)(rad * Math.Cos(ang)), (float)(rad * Math.Sin(ang)));
        }

        private static readonly List<Vector2Int> _toRemove = new(16);
    }
}
