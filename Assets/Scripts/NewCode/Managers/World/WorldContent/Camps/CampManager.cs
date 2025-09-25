// Assets/Game/World/Camps/CampManager.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Core;                      // IWorldSystem, WorldContext
using Game.World.Map.Biome;          // IBiomeService, BiomeType
using Game.World.Objects;            // ObjectType (из профилей)
using Game.World.Camps;              // CampProfile
using Random = System.Random;

[DefaultExecutionOrder(-225)]
public class CampManager : MonoBehaviour, IWorldSystem
{
    [SerializeField] private int order = 200;          // должен быть после Biome/Reservation
    public int Order => order;

    [Header("Profile & Refs")]
    [SerializeField] private CampProfile profile;
    [SerializeField] private Transform player;                // центр стриминга
    [SerializeField] private MonoBehaviour biomeServiceRef;   // IBiomeService
    [SerializeField] private PoolManagerObjects objectsPool;  // ObjectType->Pool
    [SerializeField] private GameObject groundPatchPrefab;    // визуальный "ковёр"
    [SerializeField] private bool verboseLogs = true;
    [SerializeField] private ObjectData[] objectCatalog;
    private Dictionary<ObjectType, ObjectData> _objByType;
    [Header("Reservations")]
    [SerializeField] private int reservationPadding = 0;
    [Header("Streaming")]
    [SerializeField] private int chunkSize = 64;
    [SerializeField] private int campChunkRadius = 3;
    [SerializeField] private MonoBehaviour groundSpriteRef; // IGroundSpriteService
    private IGroundSpriteService GroundSprite;
    [SerializeField] private MonoBehaviour reservationRef;    // IReservationService
    [SerializeField] private PoolManagerMainTile mainTiles;   // стример тайлов

    private IReservationService Reservation;
    [Header("Seeding")]
    [Tooltip("Глобальное семя мира (если 0 — возьмём из контекста; если и там нет, из Environment.TickCount).")]
    [SerializeField] private int worldSeed = 0;

    private IBiomeService _biomes;

    // активные чанки с лагерями и их рантайм
    private readonly HashSet<Vector2Int> _activeChunks = new();
    private readonly Dictionary<Vector2Int, List<CampRuntime>> _chunkCamps = new();

    // кэш позиции игрока в чанках
    private Vector2Int _lastPlayerChunk;
    private bool _inited;

    // ===== ПУБЛИЧНЫЕ МЕТРИКИ ===================================================
    public int ActiveCampCount
    {
        get
        {
            int sum = 0;
            foreach (var kv in _chunkCamps) sum += kv.Value.Count;
            return sum;
        }
    }

    // ===== IWorldSystem ========================================================
    public void Initialize(WorldContext ctx)
    {
        GroundSprite = groundSpriteRef as IGroundSpriteService;
        // deps
        _biomes = biomeServiceRef as IBiomeService;
        if (_biomes == null)
        {
            Debug.LogError("[CampManager] IBiomeService is null");
            return;
        }

        // seed
        if (worldSeed == 0)
        {
            if (ctx != null && ctx.Seed != 0)
                worldSeed = ctx.Seed;                 // берём сид из контекста (int)

            if (worldSeed == 0)
                worldSeed = Environment.TickCount;    // запасной вариант
        }
        _objByType = new Dictionary<ObjectType, ObjectData>(objectCatalog?.Length ?? 0);
        if (objectCatalog != null)
            foreach (var d in objectCatalog) if (d) _objByType[d.type] = d;
        // первичная инициализация стриминга
        _activeChunks.Clear();
        _chunkCamps.Clear();

        var playerPos = player ? player.position : Vector3.zero;
        _lastPlayerChunk = WorldToChunk(WorldToCell(playerPos));
        UpdateStreaming(_lastPlayerChunk);
        _inited = true;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (verboseLogs)
            Debug.Log($"[CampManager] Init complete. seed={worldSeed}, chunks={_activeChunks.Count}, campsActive={ActiveCampCount}");
#endif
    }
    private bool TryGetData(ObjectType t, out ObjectData d)
    {
        if (_objByType == null)
        {
            d = default;   // или = null;
            return false;
        }
        return _objByType.TryGetValue(t, out d);
    }
    // ===== UPDATE стриминга ====================================================
    private void Update()
    {
        if (!_inited || player == null || _biomes == null) return;

        var pc = WorldToChunk(WorldToCell(player.position));
        if (pc == _lastPlayerChunk) return;
        _lastPlayerChunk = pc;
        UpdateStreaming(pc);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (verboseLogs)
            Debug.Log($"[CampManager] Streaming at {pc}. activeChunks={_activeChunks.Count}, campsActive={ActiveCampCount}");
#endif
    }

    private void UpdateStreaming(Vector2Int centerChunk)
    {
        var wanted = WantedChunks(centerChunk, campChunkRadius);

        // load новые
        foreach (var ch in wanted)
        {
            if (_activeChunks.Add(ch))
                LoadChunkCamps(ch);
        }

        // unload ушедшие
        var toRemove = new List<Vector2Int>();
        foreach (var ch in _activeChunks)
        {
            if (!wanted.Contains(ch))
            {
                UnloadChunkCamps(ch);
                toRemove.Add(ch);
            }
        }
        foreach (var ch in toRemove) _activeChunks.Remove(ch);
    }

    private void LoadChunkCamps(Vector2Int chunk)
    {
        var list = GetDeterministicCampsForChunk(chunk);
        _chunkCamps[chunk] = list;
        foreach (var camp in list) SpawnCampRuntime(camp);
    }

    private void UnloadChunkCamps(Vector2Int chunk)
    {
        if (!_chunkCamps.TryGetValue(chunk, out var list)) return;
        foreach (var camp in list) DespawnCampRuntime(camp);
        _chunkCamps.Remove(chunk);
    }

    // ===== ДЕТЕРМИНИРОВАННАЯ ГЕНЕРАЦИЯ ========================================

    // Возвращает 0..1 лагерей для чанка, детерминированно
    private List<CampRuntime> GetDeterministicCampsForChunk(Vector2Int chunk)
    {
        var result = new List<CampRuntime>(1);

        // 1) посчитаем кандидатный центр и RNG
        if (!TryComputeChunkCandidate(chunk, out var centerCell, out var rng)) return result;

        // 2) биом / шум
        if (!IsAllowedBiome(_biomes.GetBiomeAtPosition(centerCell))) return result;
        if (profile.useNoiseGate && !PassesNoise(centerCell)) return result;

        // 3) мягкая вероятность (50%) — детерминированно
        if (rng.NextDouble() < 0.5) return result;

        // 4) minDistance — сравним с соседями по детерминированному правилу.
        // Если есть соседний валидный кандидат ближе minDist и его "хеш" меньше — мы уступаем.
        if (!WinsMinDistanceTiebreak(chunk, centerCell, profile.minDistanceBetweenCamps)) return result;

        // победили → создаём рантайм
        result.Add(new CampRuntime { CenterCell = centerCell });
        return result;
    }

    // Детерминированный кандидат центра в чанке (+ rng для этого чанка)
    private bool TryComputeChunkCandidate(Vector2Int chunk, out Vector2Int centerCell, out Random rng)
    {
        int seed = Hash3(worldSeed, profile.seedSalt, chunk.x * 73856093 ^ chunk.y * 19349663);
        rng = new Random(seed);

        // центр чанка + джиттер
        var baseCell = ChunkToWorldCell(chunk);
        int jx = NextRange(rng, -chunkSize / 3, chunkSize / 3);
        int jy = NextRange(rng, -chunkSize / 3, chunkSize / 3);
        centerCell = baseCell + new Vector2Int(jx, jy);
        return true;
    }

    // Побеждаем ли мы соседей при учёте minDistance (детерминированный тай-брейк)
    private bool WinsMinDistanceTiebreak(Vector2Int chunk, Vector2Int myCenter, int minDist)
    {
        if (minDist <= 0) return true;

        int rChunks = Mathf.CeilToInt((minDist + chunkSize - 1) / (float)chunkSize);
        for (int dx = -rChunks; dx <= rChunks; dx++)
        {
            for (int dy = -rChunks; dy <= rChunks; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                var nb = new Vector2Int(chunk.x + dx, chunk.y + dy);

                // посчитаем соседского кандидата
                if (!TryComputeChunkCandidate(nb, out var nCenter, out var nbRng)) continue;

                // те же гейты: биом/шум/вероятность
                if (!IsAllowedBiome(_biomes.GetBiomeAtPosition(nCenter))) continue;
                if (profile.useNoiseGate && !PassesNoise(nCenter)) continue;
                if (nbRng.NextDouble() < 0.5) continue;

                // близко?
                if ((nCenter - myCenter).sqrMagnitude < minDist * minDist)
                {
                    // tie-break: выигрывает меньший "приоритет"
                    long myKey = TieBreakKey(chunk);
                    long nbKey = TieBreakKey(nb);
                    if (nbKey < myKey) return false; // сосед выигрывает → мы отказываемся
                }
            }
        }
        return true;
    }

    private long TieBreakKey(Vector2Int chunk)
    {
        // стабильный приоритет: хеш по сид/соль/координаты; при равенстве — лексикографический
        long h = (long)Hash3(worldSeed, profile.seedSalt, chunk.x * 911 + chunk.y * 3571) & 0x7FFFFFFF;
        return (h << 32) ^ ((long)(chunk.x & 0xFFFF) << 16) ^ (uint)(chunk.y & 0xFFFF);
    }

    private bool IsAllowedBiome(BiomeType b)
    {
        return profile.allowedBiomes == null || profile.allowedBiomes.Length == 0
             ? true
             : Array.IndexOf(profile.allowedBiomes, b) >= 0;
    }

    private bool PassesNoise(Vector2Int cell)
    {
        float nx = (cell.x + worldSeed + profile.seedSalt) * profile.noiseScale;
        float ny = (cell.y + worldSeed - profile.seedSalt) * profile.noiseScale;
        float v = Mathf.PerlinNoise(nx, ny);
        return v >= profile.noiseThreshold;
    }

    // ===== Spawn / Despawn =====================================================

    private void SpawnCampRuntime(CampRuntime camp)
    {
        // 0) Резервация и оверрайд спрайтов тайлов
        if (Reservation != null)
        {
            Reservation.ReserveCircle(
                camp.CenterCell,
                profile.campRadius + reservationPadding,
                ReservationMask.Nature | ReservationMask.Camps
            );
        }

        if (GroundSprite != null && profile.campGroundSprite != null)
        {
            GroundSprite.SetSpriteCircle(camp.CenterCell, profile.campRadius, profile.campGroundSprite);
        }

        // Перерисовать видимые клетки (без ?. для Unity)
        if (mainTiles != null)
            mainTiles.RefreshCellsInCircle(camp.CenterCell, profile.campRadius + reservationPadding);

        // ===== дальше твой спавн структур и NPC — без изменений =====
        var rngStruct = new System.Random(Hash3(
            worldSeed, profile.seedSalt,
            camp.CenterCell.x * 48611 ^ camp.CenterCell.y * 1223
        ));

        if (profile.structures != null)
        {
            foreach (var s in profile.structures)
            {
                int count = Mathf.Clamp(NextRange(rngStruct, s.countRange.x, s.countRange.y), 0, 999);
                var positions = PickStructurePositionsDet(camp.CenterCell, count, s, rngStruct);

                foreach (var cellPos in positions)
                {
                    if (!TryGetData(s.type, out var data))
                    {
                        Debug.LogWarning($"[CampManager] No ObjectData for {s.type}");
                        continue;
                    }

                    GameObject go = data.prefabOverride != null
                        ? Instantiate(data.prefabOverride, transform)
                        : (objectsPool != null ? objectsPool.Get(s.type) : null);

                    if (go == null)
                    {
                        go = new GameObject($"Camp_{s.type}");
                        go.transform.SetParent(transform, false);
                        go.AddComponent<SpriteRenderer>();
                    }

                    var wpos = CellToWorld(cellPos);
                    wpos.x += data.pivotOffsetWorld.x;
                    wpos.y += data.pivotOffsetWorld.y + data.visualHeightUnits;
                    go.transform.position = wpos;
                    SetZByY(go.transform);

                    var sr = go.GetComponentInChildren<SpriteRenderer>(true) ?? go.AddComponent<SpriteRenderer>();
                    if (data.spriteVariants != null && data.spriteVariants.Length > 0)
                    {
                        int key = Hash3(worldSeed, (int)s.type, cellPos.x * 48611 ^ cellPos.y * 1223);
                        int idx = Mathf.Abs(key) % data.spriteVariants.Length;
                        var sprite = data.spriteVariants[idx];
                        if (sprite != null) sr.sprite = sprite;
                    }
                    sr.sortingLayerID = SortingLayer.NameToID("Objects");

                    if (sr.sprite == null)
                        Debug.LogWarning($"[CampManager] Sprite is NULL for {s.type} at {cellPos}. Fill ObjectData.spriteVariants or prefab.");

                    camp.Structures.Add(go);
                }
            }
        }

        var GOLDEN32 = unchecked((int)0x9E3779B9u);
        var rngNpc = new System.Random(Hash3(
            worldSeed, profile.seedSalt ^ GOLDEN32,
            camp.CenterCell.x * 7349 ^ camp.CenterCell.y * 3163
        ));

        if (profile.npcRoles != null)
        {
            foreach (var role in profile.npcRoles)
            {
                int target = Mathf.Clamp(NextRange(rngNpc, role.countRange.x, role.countRange.y), 0, 999);
                var positions = PickNpcPositionsDet(camp.CenterCell, role, camp.Structures, rngNpc, target);

                int spawned = 0;
                foreach (var cellPos in positions)
                {
                    if (spawned >= target) break;
                    if (role.prefab == null) continue;

                    var npc = Instantiate(role.prefab, transform);
                    npc.transform.position = CellToWorld(cellPos);
                    SetZByY(npc.transform);
                    camp.Npcs.Add(npc);
                    spawned++;
                }
            }
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (verboseLogs)
            Debug.Log($"[CampManager] Spawn camp at {camp.CenterCell}. totalActive={ActiveCampCount}");
#endif
    }

    private void DespawnCampRuntime(CampRuntime camp)
    {
        // 0) Снять оверрайд спрайтов и резервацию
        if (GroundSprite != null)
            GroundSprite.ClearSpriteCircle(camp.CenterCell, profile.campRadius);

        if (Reservation != null)
        {
            Reservation.ReleaseCircle(
                camp.CenterCell,
                profile.campRadius + reservationPadding,
                ReservationMask.Nature | ReservationMask.Camps
            );
        }

        // Перерисовать видимые клетки
        if (mainTiles != null)
            mainTiles.RefreshCellsInCircle(camp.CenterCell, profile.campRadius + reservationPadding);

        // 1) Ковёр (если использовался)
        if (camp.GroundPatch) Destroy(camp.GroundPatch);
        camp.GroundPatch = null;

        // 2) Структуры
        foreach (var go in camp.Structures)
        {
            if (!go) continue;
            if (objectsPool != null) objectsPool.Release(go);
            else Destroy(go);
        }
        camp.Structures.Clear();

        // 3) NPC
        foreach (var npc in camp.Npcs)
            if (npc) Destroy(npc);
        camp.Npcs.Clear();
    }

    // ===== Layout helpers (детерминированные) ==================================

    private List<Vector2Int> PickStructurePositionsDet(Vector2Int centerCell, int count, CampProfile.CampStructure s, Random rng)
    {
        var list = new List<Vector2Int>(count);
        int R = Mathf.Max(2, profile.campRadius - s.ringOffset);
        int pad = Mathf.Max(0, profile.layoutPadding);

        switch (s.dist)
        {
            case CampProfile.CampStructure.Distribution.Center:
                for (int i = 0; i < count; i++) list.Add(centerCell);
                break;

            case CampProfile.CampStructure.Distribution.Ring:
            case CampProfile.CampStructure.Distribution.InnerRing:
                {
                    int radius = s.dist == CampProfile.CampStructure.Distribution.InnerRing ? Mathf.Max(1, R / 2) : R;
                    for (int i = 0; i < count; i++)
                    {
                        double jitter = (rng.NextDouble() - 0.5) * 0.4; // ~±0.2 рад
                        double ang = (i / (double)count) * Math.PI * 2.0 + jitter;
                        var pos = centerCell + new Vector2Int(
                            Mathf.RoundToInt((float)(radius * Math.Cos(ang))),
                            Mathf.RoundToInt((float)(radius * Math.Sin(ang))));
                        list.Add(pos);
                    }
                    break;
                }

            case CampProfile.CampStructure.Distribution.Grid:
                {
                    int side = Mathf.CeilToInt(Mathf.Sqrt(count));
                    int step = Mathf.Max(1, pad + 1);
                    int half = side / 2;
                    for (int gx = -half; gx <= half && list.Count < count; gx++)
                        for (int gy = -half; gy <= half && list.Count < count; gy++)
                        {
                            var p = centerCell + new Vector2Int(gx * step, gy * step);
                            if ((p - centerCell).sqrMagnitude <= R * R) list.Add(p);
                        }
                    break;
                }

            default: // RandomScatter
                {
                    int tries = count * 10;
                    while (list.Count < count && tries-- > 0)
                    {
                        var p = centerCell + RandInCircleDet(profile.campRadius, rng);
                        bool ok = true;
                        for (int i = 0; i < list.Count; i++)
                            if ((list[i] - p).sqrMagnitude < s.minDistanceSameType * s.minDistanceSameType) { ok = false; break; }
                        if (ok) list.Add(p);
                    }
                    break;
                }
        }
        return list;
    }

    private List<Vector2Int> PickNpcPositionsDet(Vector2Int centerCell, CampProfile.CampNpcRole role, List<GameObject> structures, Random rng, int target)
    {
        var res = new List<Vector2Int>(target);
        switch (role.dist)
        {
            case CampProfile.CampNpcRole.NpcDistribution.Center:
                res.Add(centerCell);
                break;

            case CampProfile.CampNpcRole.NpcDistribution.Ring:
                {
                    int count = Math.Max(1, target);
                    for (int i = 0; i < count; i++)
                    {
                        double ang = (i / (double)count) * Math.PI * 2.0;
                        var p = centerCell + new Vector2Int(
                            Mathf.RoundToInt(role.radius * Mathf.Cos((float)ang)),
                            Mathf.RoundToInt(role.radius * Mathf.Sin((float)ang)));
                        res.Add(p);
                    }
                    break;
                }

            case CampProfile.CampNpcRole.NpcDistribution.GuardPerTent:
                {
                    foreach (var go in structures)
                    {
                        if (!go) continue;
                        if (!go.name.Contains("Tent", StringComparison.OrdinalIgnoreCase)) continue;
                        var cell = WorldToCell(go.transform.position) + role.guardOffset;
                        res.Add(cell);
                    }
                    break;
                }

            default: // Random
                {
                    for (int i = 0; i < target; i++)
                        res.Add(centerCell + RandInCircleDet(Mathf.Max(1, profile.campRadius - 1), rng));
                    break;
                }
        }
        return res;
    }

    // ===== Math / Utils ========================================================

    private Vector2Int WorldToCell(Vector3 pos) => new(Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.y));
    private Vector3 CellToWorld(Vector2Int cell) => new(cell.x, cell.y, 0f);

    private Vector2Int WorldToChunk(Vector2Int cell)
    {
        int cx = cell.x >= 0 ? cell.x / chunkSize : (cell.x - (chunkSize - 1)) / chunkSize;
        int cy = cell.y >= 0 ? cell.y / chunkSize : (cell.y - (chunkSize - 1)) / chunkSize;
        return new Vector2Int(cx, cy);
    }
    private Vector2Int ChunkToWorldCell(Vector2Int chunk) => new(chunk.x * chunkSize + chunkSize / 2, chunk.y * chunkSize + chunkSize / 2);

    private HashSet<Vector2Int> WantedChunks(Vector2Int center, int r)
    {
        var set = new HashSet<Vector2Int>();
        for (int dx = -r; dx <= r; dx++)
            for (int dy = -r; dy <= r; dy++)
                set.Add(new Vector2Int(center.x + dx, center.y + dy));
        return set;
    }

    private static int NextRange(Random rng, int minInclusive, int maxInclusive)
    {
        if (minInclusive > maxInclusive) (minInclusive, maxInclusive) = (maxInclusive, minInclusive);
        // Random.Next(a, bExclusive)
        return rng.Next(minInclusive, maxInclusive + 1);
    }

    private static Vector2Int RandInCircleDet(int radius, Random rng)
    {
        // равномерно по кругу для наших целочисленных координат
        double ang = rng.NextDouble() * Math.PI * 2.0;
        double rad = rng.NextDouble() * radius;
        return new Vector2Int(
            Mathf.RoundToInt((float)(rad * Math.Cos(ang))),
            Mathf.RoundToInt((float)(rad * Math.Sin(ang))));
    }

    private static int Hash3(int a, int b, int c) => (a * 73856093) ^ (b * 19349663) ^ (c * 83492791);

    private static void SetZByY(Transform t, float zScale = 0.001f, float baseZ = 0f)
    {
        var p = t.position;
        p.z = baseZ - p.y * zScale;
        t.position = p;
    }

    // Рантайм-данные лагеря
    private class CampRuntime
    {
        public Vector2Int CenterCell;
        public GameObject GroundPatch;
        public readonly List<GameObject> Structures = new();
        public readonly List<GameObject> Npcs = new();
    }
}
