// CreatureSpawner.cs — планировщик групп (данные-только) с отложенной инициализацией
// Ждёт несколько кадров перед Initialize, чтобы мир/биомы успели построиться.

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Game.Core;
using Game.World.Map.Biome;
using Game.World.Signals;    // для WorldRegen
using Game.World.Objects;    // для CellSize при очистке (если потребуется)

namespace Game.World.Creatures
{
    [DefaultExecutionOrder(-50)] // позже «систем», но раньше визуальных стримеров
    public class CreatureSpawner : MonoBehaviour, IWorldSystem
    {
        [Header("Refs")]
        [SerializeField] private Transform player;
        [SerializeField] private MonoBehaviour biomeServiceRef;    // IBiomeService
        [SerializeField] private MonoBehaviour reservationRef;     // IReservationService
        [Tooltip("Источник радиуса/размера клетки тайлов — PoolManager.")]
        [SerializeField] private PoolManager tiles;                // ← НОВОЕ: синхронизация единиц

        private IBiomeService _biomes;
        private IReservationService _reserv;

        [Header("Rules & World")]
        [SerializeField] private CreatureSpawnRules rules;
        [SerializeField, Min(8)] private int chunkSize = 64;
        [SerializeField, Min(1)] private int streamRadiusChunks = 3; // запасной лимит (обычно не нужен при круге)
        [SerializeField] private float cellSize = 1f;                // будет переопределяться из tiles
        [SerializeField] private int worldSeed = 12345;

        [Header("Sampling")]
        [SerializeField, Min(4)] private int gridStepCells = 12;   // «растр» внутри чанка

        [Header("Hysteresis")]
        [Tooltip("+N клеток к радиусу для загрузки (0 — на альфу)")]
        [SerializeField] private int loadPaddingCells = 0;
        [Tooltip("+N клеток к радиусу для удержания (0 — на альфу)")]
        [SerializeField] private int keepPaddingCells = 0;

        [Header("Lifecycle")]
        [Tooltip("Сколько кадров подождать до старта (даём миру построиться).")]
        [SerializeField, Min(0)] private int warmupFrames = 2;

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

        public int Order => -50;
        public bool IsReady => _inited; // ← PoolingCreatures будет проверять

        private void Awake()
        {
            WorldSignals.OnWorldRegen += OnWorldRegen;
            StartCoroutine(Co_DeferredInit());
        }

        private void OnDestroy()
        {
            WorldSignals.OnWorldRegen -= OnWorldRegen;
        }

        private IEnumerator Co_DeferredInit()
        {
            for (int i = 0; i < warmupFrames; i++) yield return null;
            Initialize(null);
        }

        public void Initialize(WorldContext ctx)
        {
            _biomes = biomeServiceRef as IBiomeService ?? FindFirstObjectByType<BiomeManager>(FindObjectsInactive.Exclude);
            _reserv = reservationRef as IReservationService;

            if (_biomes == null) { Debug.LogError("[CreatureSpawner] IBiomeService is NULL"); return; }
            if (rules == null) { Debug.LogError("[CreatureSpawner] CreatureSpawnRules is NULL"); return; }

            _activeChunks.Clear();
            _chunkGroups.Clear();

            // Синхронизируем единицы (если есть PoolManager)
            SyncUnitsFromTiles();

            var pos = player ? player.position : Vector3.zero;
            _lastPlayerChunk = WorldToChunk(WorldToCell(pos));
            UpdateStreaming(_lastPlayerChunk);
            _inited = true;

            if (verbose) Debug.Log($"[CreatureSpawner] Init OK. chunk={chunkSize}, gridStep={gridStepCells}, paddings=({loadPaddingCells},{keepPaddingCells})");
        }

        private void Update()
        {
            if (!_inited || !player) return;

            // Подтягиваем единицы «вживую», как в ObjectChunkStreamer
            SyncUnitsFromTiles();

            var pc = WorldToChunk(WorldToCell(player.position));
            if (pc == _lastPlayerChunk) return;
            _lastPlayerChunk = pc;
            UpdateStreaming(pc);
        }

        private void SyncUnitsFromTiles()
        {
            if (tiles == null) return;
            // единицы под тайлы
            cellSize = tiles.CellSizeWorld;
            // streamRadiusChunks используется как лимит, но реальная зона — из кругa тайлов:
            // сам радиус тайлов тянем непосредственно в UpdateStreaming
        }

        private void OnWorldRegen()
        {
            // Полный сброс планов и активных чанков
            _chunkGroups.Clear();
            _activeChunks.Clear();
            _inited = false; // заставим пройти init заново при следующем кадре
            if (verbose) Debug.Log("[CreatureSpawner] WorldRegen: cleared planned groups & active chunks.");
            StartCoroutine(Co_DeferredInit());
        }

        // === Public API for PoolingCreatures ===
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
                    for (int j = 0; j < g.members.Count; j++)
                    {
                        var u = g.members[j];
                        var d = u.cell - centerCell;
                        if (d.sqrMagnitude <= r2) buffer.Add(u);
                    }
                }
            }
            return buffer.Count;
        }

        // === Streaming ===
        private void UpdateStreaming(Vector2Int centerChunk)
        {
            // Радиус тайлов в клетках берём из PoolManager (если есть),
            // иначе пойдём от грубого лимита по чанкам.
            int tileRadiusCells = tiles ? tiles.RadiusCells : (streamRadiusChunks * chunkSize);
            float cs = cellSize;

            float loadR = (tileRadiusCells + loadPaddingCells) * cs; // мир
            float keepR = (tileRadiusCells + keepPaddingCells) * cs; // мир

            // Центр круга в мире
            Vector2 c = (Vector2)player.position;

            // Сколько чанков проверять вокруг — считаем по полу-диагонали чанка
            float chunkWorldSize = chunkSize * cs;
            float halfDiag = 0.5f * chunkWorldSize * 1.41421356f; // sqrt(2)
            int R = Mathf.CeilToInt((keepR + halfDiag) / chunkWorldSize);

            var shouldLoad = new HashSet<Vector2Int>();
            var shouldKeep = new HashSet<Vector2Int>();

            for (int dy = -R; dy <= R; dy++)
                for (int dx = -R; dx <= R; dx++)
                {
                    var ch = new Vector2Int(centerChunk.x + dx, centerChunk.y + dy);

                    // прямоугольник чанка в world units
                    Rect rect = new Rect(
                        ch.x * chunkWorldSize,
                        ch.y * chunkWorldSize,
                        chunkWorldSize,
                        chunkWorldSize
                    );

                    if (CircleIntersectsRect(c, loadR, rect)) shouldLoad.Add(ch);
                    if (CircleIntersectsRect(c, keepR, rect)) shouldKeep.Add(ch);
                }

            // SPAWN
            foreach (var ch in shouldLoad)
                if (_activeChunks.Add(ch))
                    LoadChunk(ch);

            // DESPAWN
            var toRemove = new List<Vector2Int>();
            foreach (var ch in _activeChunks)
                if (!shouldKeep.Contains(ch))
                {
                    UnloadChunk(ch);
                    toRemove.Add(ch);
                }
            for (int i = 0; i < toRemove.Count; i++) _activeChunks.Remove(toRemove[i]);

            if (verbose)
                Debug.Log($"[CreatureSpawner] stream: load={shouldLoad.Count}, keep={shouldKeep.Count}, active={_activeChunks.Count}, R={R}, tileR={tileRadiusCells}");
        }

        // Геометрия
        static bool CircleIntersectsRect(Vector2 c, float r, Rect rect)
        {
            float cx = Mathf.Clamp(c.x, rect.xMin, rect.xMax);
            float cy = Mathf.Clamp(c.y, rect.yMin, rect.yMax);
            float dx = cx - c.x;
            float dy = cy - c.y;
            return (dx * dx + dy * dy) <= r * r;
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

                    if (!rules.TryGetRuleOrFallback(biome, out var rule))
                    {
                        if (logReasons) Debug.Log($"[CreatureSpawner] skip(no rule+no fallback) biome={biome} at {cell}");
                        continue;
                    }

                    if (rng.NextDouble() > Mathf.Clamp01(rule.spawnDensity))
                    {
                        if (logReasons) Debug.Log($"[CreatureSpawner] skip(density) at {cell}");
                        continue;
                    }

                    if (_reserv != null && _reserv.IsReserved(cell, ReservationMask.Creatures))
                    {
                        if (logReasons) Debug.Log($"[CreatureSpawner] skip(reserved center) at {cell}");
                        continue;
                    }

                    if (!IsFarFromOtherGroups(cell, rule.minGroupDistance))
                    {
                        if (logReasons) Debug.Log($"[CreatureSpawner] skip(too close) at {cell}");
                        continue;
                    }

                    var grp = PickGroup(rule.groups, rng);
                    if (!grp) { if (logReasons) Debug.Log($"[CreatureSpawner] skip(no group pick) at {cell}"); continue; }

                    var pg = BuildPlannedGroup(cell, grp, rng);
                    if (pg.members.Count == 0) { if (logReasons) Debug.Log($"[CreatureSpawner] skip(empty group) at {cell}"); continue; }

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
            var pg = new PlannedGroup { groupId = Hash3(worldSeed, center.x, center.y), center = center, group = grp };

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

        // Helpers
        private Vector2Int WorldToCell(Vector3 w)
        {
            int x = Mathf.FloorToInt(w.x / cellSize);
            int y = Mathf.FloorToInt(w.y / cellSize);
            return new Vector2Int(x, y);
        }

        private Vector2Int WorldToChunk(Vector2Int cell)
        {
            int cx = cell.x >= 0 ? cell.x / chunkSize : (cell.x - (chunkSize - 1)) / chunkSize;
            int cy = cell.y >= 0 ? cell.y / chunkSize : (cell.y - (chunkSize - 1)) / chunkSize;
            return new Vector2Int(cx, cy);
        }

        private Vector2Int ChunkToWorldCell(Vector2Int chunk) => new(chunk.x * chunkSize, chunk.y * chunkSize);

        private static int NextRange(System.Random rng, int minInclusive, int maxInclusive)
        { if (minInclusive > maxInclusive) (minInclusive, maxInclusive) = (maxInclusive, minInclusive); return minInclusive + rng.Next(maxInclusive - minInclusive + 1); }

        private static int Hash3(int a, int b, int c) => (a * 73856093) ^ (b * 19349663) ^ (c * 83492791);

        private static ulong MakeId(Vector2Int center, CreatureProfile p, int idx)
        { unchecked { uint h = (uint)Hash3(center.x, center.y, p ? p.name.GetHashCode() : 0); return ((ulong)h << 32) | (uint)idx; } }

        private static Vector2 RandInCircle(float r, System.Random rng)
        { double ang = rng.NextDouble() * Math.PI * 2.0; double rad = rng.NextDouble() * r; return new Vector2((float)(rad * Math.Cos(ang)), (float)(rad * Math.Sin(ang))); }
    }
}
