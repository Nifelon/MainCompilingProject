// Assets/Game/World/Camps/CampManager.cs (patched to NPC data-only)
using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Core;                      // IWorldSystem, WorldContext
using Game.World.Map.Biome;          // IBiomeService, BiomeType
using Game.World.Objects;            // ObjectType (из профилей)
using Game.World.Camps;              // CampProfile
using Game.World.NPC;                // NPCProfile, NPCSpawnList
//using Game.World.Content.Services;   // IReservationService, ReservationMask
using Random = System.Random;

[DefaultExecutionOrder(-230)]
public class CampManager : MonoBehaviour, IWorldSystem
{
    [SerializeField] private int order = -230;          // должен быть после Biome/Reservation
    public int Order => order;

    [Header("Profile & Refs")]
    [SerializeField] private CampProfile profile;
    [SerializeField] private Transform player;                // центр стриминга
    [SerializeField] private MonoBehaviour biomeServiceRef;   // IBiomeService
    [SerializeField] private PoolManagerObjects objectsPool;  // ObjectType->Pool
    [SerializeField] private GameObject groundPatchPrefab;    // визуальный "ковёр"
    [SerializeField] private bool verboseLogs = true;

    [Header("Object DB (для структур)")]
    [SerializeField] private ObjectData[] objectCatalog;
    private Dictionary<ObjectType, ObjectData> _objByType;

    [Header("Reservations")]
    [SerializeField] private ObjectManager objectManager;
    [SerializeField] private int reservePadding = 1;
    [SerializeField] private int reservationPadding = 0;

    [Header("Streaming")]
    [SerializeField] private int chunkSize = 64;
    [SerializeField] private int campChunkRadius = 3;
    [SerializeField] private MonoBehaviour groundSpriteRef;   // IGroundSpriteService
    private IGroundSpriteService GroundSprite;
    [SerializeField] private MonoBehaviour reservationRef;    // IReservationService
    [SerializeField] private PoolManagerMainTile mainTiles;   // стример тайлов
    [SerializeField] private float cellSize = 1f; // world units per cell

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

    // ===== PUBLIC METRICS =====================================================
    public int ActiveCampCount
    {
        get
        {
            int sum = 0;
            foreach (var kv in _chunkCamps) sum += kv.Value.Count;
            return sum;
        }
    }

    // ====== NPC DATA MODEL (NEW) =============================================
    [Serializable]
    public struct PlannedNpc
    {
        public ulong id;             // стабильный id в рамках лагеря
        public Vector2Int cell;      // позиция в клетках
        public NPCProfile profile;   // профиль NPC (для PoolingNPC)
        public int campId;           // идентификатор лагеря (детерминированный)
    }

    // Рантайм-данные лагеря
    private class CampRuntime
    {
        public Vector2Int CenterCell;
        public GameObject GroundPatch;
        public readonly List<GameObject> Structures = new();
        public readonly List<PlannedNpc> PlannedNpcs = new();   // ← вместо List<GameObject> Npcs
    }

    // Перегрузка визуала объектов в чанках, пересекающих круг (когда счищаем природу под лагерь)
    private void ReloadChunksIntersectingCircle(Vector2Int centerCell, int radiusCells)
    {
        if (objectManager == null) return;

        int cs = objectManager.ChunkSize;
        int minCx = Mathf.FloorToInt((centerCell.x - radiusCells) / (float)cs);
        int maxCx = Mathf.FloorToInt((centerCell.x + radiusCells) / (float)cs);
        int minCy = Mathf.FloorToInt((centerCell.y - radiusCells) / (float)cs);
        int maxCy = Mathf.FloorToInt((centerCell.y + radiusCells) / (float)cs);

        for (int cx = minCx; cx <= maxCx; cx++)
            for (int cy = minCy; cy <= maxCy; cy++)
            {
                var cc = new ObjectManager.ChunkCoord(cx, cy);
                objectManager.UnloadChunkVisuals(cc);
                objectManager.LoadChunkVisuals(cc);
            }
    }
    static bool CircleIntersectsRect(Vector2 c, float r, Rect rect)
    {
        // ближайшая точка прямоугольника к центру круга
        float cx = Mathf.Clamp(c.x, rect.xMin, rect.xMax);
        float cy = Mathf.Clamp(c.y, rect.yMin, rect.yMax);

        float dx = cx - c.x;
        float dy = cy - c.y;

        // пересекаются, если расстояние <= радиуса
        return (dx * dx + dy * dy) <= r * r;
    }

    // ===== IWorldSystem ========================================================
    public void Initialize(WorldContext ctx)
    {
        GroundSprite = groundSpriteRef as IGroundSpriteService;

        _biomes = biomeServiceRef as IBiomeService;
        Reservation = reservationRef as IReservationService;
        if (_biomes == null)
        {
            Debug.LogError("[CampManager] IBiomeService is null");
            return;
        }

        // seed
        if (worldSeed == 0)
        {
            if (ctx != null && ctx.Seed != 0)
                worldSeed = ctx.Seed;
            if (worldSeed == 0)
                worldSeed = Environment.TickCount;
        }

        // карта ObjectType -> ObjectData для структур лагеря
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
        if (_objByType == null) { d = default; return false; }
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
        // радиус в клетках — такой же, как у PoolManager.radius
        int tileRadius = 60;
        float cellSize = 1f; // подставь своё
        float loadR = (tileRadius + 8) * cellSize; // гистерезис
        float keepR = (tileRadius + 12) * cellSize;

        Vector2 c = CellToWorld(WorldToCell(player.position));
        int R = Mathf.CeilToInt((tileRadius + 12) / (float)chunkSize) + 1;

        var shouldLoad = new HashSet<Vector2Int>();
        var shouldKeep = new HashSet<Vector2Int>();

        for (int dy = -R; dy <= R; dy++)
            for (int dx = -R; dx <= R; dx++)
            {
                var ch = new Vector2Int(centerChunk.x + dx, centerChunk.y + dy);
                Rect rect = new Rect(
                    ch.x * chunkSize, ch.y * chunkSize,
                    chunkSize, chunkSize
                ); // если cellSize!=1 — умножь

                if (CircleIntersectsRect(c, loadR, rect)) shouldLoad.Add(ch);
                if (CircleIntersectsRect(c, keepR, rect)) shouldKeep.Add(ch);
            }

        foreach (var ch in shouldLoad)
            if (_activeChunks.Add(ch)) LoadChunkCamps(ch);

        var toRemove = new List<Vector2Int>();
        foreach (var ch in _activeChunks)
            if (!shouldKeep.Contains(ch)) { UnloadChunkCamps(ch); toRemove.Add(ch); }
        foreach (var ch in toRemove) _activeChunks.Remove(ch);
    }

    private void LoadChunkCamps(Vector2Int chunk)
    {
        var list = GetDeterministicCampsForChunk(chunk);

        // 1) Пред-резерв зоны лагерей + зачистка природы
        if (list != null)
        {
            // резервируем через ReservationService (в клетках)
            if (Reservation != null)
            {
                foreach (var camp in list)
                    Reservation.ReserveCircle(camp.CenterCell, profile.campRadius + reservePadding, ReservationMask.All);
            }

            // чистим уже заспавнённые объекты (переводим клетку → world units)
            if (objectManager != null)
            {
                foreach (var camp in list)
                {
                    var centerW = new Vector2(camp.CenterCell.x * cellSize, camp.CenterCell.y * cellSize);
                    var rW = (profile.campRadius + reservePadding) * cellSize;
                    objectManager.RemoveObjectsInCircle(centerW, rW);
                }
            }

            // 2) Мгновенно перегружаем затронутые чанки (визуал)
            foreach (var camp in list)
                ReloadChunksIntersectingCircle(camp.CenterCell, profile.campRadius + reservePadding);
        }

        // 3) Спавн визуала лагеря + ПЛАНИРОВАНИЕ NPC (без Instantiate)
        _chunkCamps[chunk] = list;
        foreach (var camp in list) SpawnCampRuntime(camp);
    }

    private void UnloadChunkCamps(Vector2Int chunk)
    {
        if (!_chunkCamps.TryGetValue(chunk, out var list)) return;
        foreach (var camp in list) DespawnCampRuntime(camp);
        _chunkCamps.Remove(chunk);
    }

    // ===== ДЕТЕРМИНИСТИЧЕСКАЯ ГЕНЕРАЦИЯ ========================================

    // Возвращает 0..1 лагерей для чанка
    private List<CampRuntime> GetDeterministicCampsForChunk(Vector2Int chunk)
    {
        var result = new List<CampRuntime>(1);

        // 1) посчитаем кандидатный центр и RNG
        if (!TryComputeChunkCandidate(chunk, out var centerCell, out var rng)) return result;

        // 2) биом / шум
        if (!IsAllowedBiome(_biomes.GetBiomeAtPosition(centerCell))) return result;
        if (profile.useNoiseGate && !PassesNoise(centerCell)) return result;

        // 3) мягкая вероятность (50%)
        if (rng.NextDouble() < 0.5) return result;

        // 4) minDistance — сравнение с соседями
        if (!WinsMinDistanceTiebreak(chunk, centerCell, profile.minDistanceBetweenCamps)) return result;

        result.Add(new CampRuntime { CenterCell = centerCell });
        Reservation.ReserveCircle(centerCell, profile.creaturesNoSpawnRadius, ReservationMask.Creatures);
        return result;
    }

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

    private bool WinsMinDistanceTiebreak(Vector2Int chunk, Vector2Int myCenter, int rChunks)
    {
        if (rChunks <= 0) return true;

        var myKey = TieBreakKey(chunk);
        for (int dx = -rChunks; dx <= rChunks; dx++)
        {
            for (int dy = -rChunks; dy <= rChunks; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                var otherChunk = new Vector2Int(chunk.x + dx, chunk.y + dy);

                if (!TryComputeChunkCandidate(otherChunk, out var otherCenter, out _)) continue;
                if (!IsAllowedBiome(_biomes.GetBiomeAtPosition(otherCenter))) continue;
                if (profile.useNoiseGate && !PassesNoise(otherCenter)) continue;
                if (new Vector2Int(otherCenter.x - myCenter.x, otherCenter.y - myCenter.y).sqrMagnitude >
                    profile.minDistanceBetweenCamps * profile.minDistanceBetweenCamps) continue;

                var otherKey = TieBreakKey(otherChunk);
                if (otherKey < myKey) return false;   // сосед выигрывает → мы уступаем
            }
        }
        return true;
    }

    private long TieBreakKey(Vector2Int chunk)
    {
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

    // ===== Spawn / Despawn (visual for camp, NPC planning only) ===============

    private void SpawnCampRuntime(CampRuntime camp)
    {
        // 0) Резервация и оверрайд спрайтов тайлов
        if (Reservation != null)
            Reservation.ReserveCircle(camp.CenterCell, profile.campRadius + reservePadding, ReservationMask.All);

        if (GroundSprite != null && profile.campGroundSprite != null)
            GroundSprite.SetSpriteCircle(camp.CenterCell, profile.campRadius, profile.campGroundSprite);

        if (mainTiles != null)
            mainTiles.RefreshCellsInCircle(camp.CenterCell, profile.campRadius + reservationPadding);

        // 1) Спавн структур
        var rngStruct = new Random(Hash3(
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
                        : (objectsPool != null ? objectsPool.Get(data.type) : new GameObject($"{s.type}"));

                    go.transform.position = CellToWorld(cellPos)
                                          + (Vector3)data.pivotOffsetWorld
                                          + ((data.tags & ObjectTags.HighSprite) != 0
                                                ? Vector3.up * data.visualHeightUnits
                                                : Vector3.zero);

                    SetZByY(go.transform);

                    var sr = go.GetComponentInChildren<SpriteRenderer>() ?? go.AddComponent<SpriteRenderer>();
                    if (data.spriteVariants != null && data.spriteVariants.Length > 0)
                    {
                        int key = Hash3(worldSeed, (int)s.type, cellPos.x * 48611 ^ cellPos.y * 1223);
                        int idx = Mathf.Abs(key) % data.spriteVariants.Length;
                        var sprite = data.spriteVariants[idx];
                        if (sprite) sr.sprite = sprite;
                    }
                    sr.sortingLayerID = SortingLayer.NameToID("Objects");

                    if (sr.sprite == null)
                        Debug.LogWarning($"[CampManager] Sprite is NULL for {s.type} at {cellPos}. Fill ObjectData.spriteVariants or prefab.");

                    camp.Structures.Add(go);
                }
            }
        }

        // 2) NPC — ТОЛЬКО ПЛАНИРОВАНИЕ (без Instantiate)
        var GOLDEN32 = unchecked((int)0x9E3779B9u);
        var rngNpc = new Random(Hash3(
            worldSeed, profile.seedSalt ^ GOLDEN32,
            camp.CenterCell.x * 7349 ^ camp.CenterCell.y * 3163
        ));

        camp.PlannedNpcs.Clear();

        if (profile != null && profile.npcPack != null &&
            profile.npcPack.entries != null && profile.npcPack.entries.Count > 0)
        {
            PlanNpcPack(camp, rngNpc);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (verboseLogs)
                Debug.Log($"[CampManager] Planned NPCs from npcPack: {camp.PlannedNpcs.Count}");
#endif
        }
        else if (profile != null && profile.npcRoles != null)
        {
            PlanNpcLegacy(camp, rngNpc);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (verboseLogs)
                Debug.Log($"[CampManager] Planned NPCs (legacy roles): {camp.PlannedNpcs.Count}");
#endif
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (verboseLogs)
            Debug.Log($"[CampManager] Spawn camp (visual only) at {camp.CenterCell}. totalActive={ActiveCampCount}");
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

        if (mainTiles != null)
            mainTiles.RefreshCellsInCircle(camp.CenterCell, profile.campRadius + reservationPadding);

        // 1) Ковёр
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

        // 3) NPC (только данные)
        camp.PlannedNpcs.Clear();
    }

    // ===== NPC PLANNING (no instantiation) ====================================

    private void PlanNpcPack(CampRuntime camp, Random rng)
    {
        var pack = profile.npcPack;
        if (pack == null || pack.entries == null || pack.entries.Count == 0) return;

        int campId = Hash3(worldSeed, profile.seedSalt, camp.CenterCell.x * 101 + camp.CenterCell.y * 997);
        var occupied = new List<Vector2Int>(16);

        foreach (var entry in pack.entries)
        {
            var npcProfile = entry.profile; // Game.World.NPC.NPCProfile
            if (npcProfile == null) continue; // для планирования обязателен профиль

            int target = Mathf.Clamp(NextRange(rng, entry.count.x, entry.count.y), 0, 999);
            if (target <= 0) continue;

            var points = PickNpcPositionsFromEntry(camp.CenterCell, profile.campRadius, entry, camp.Structures, rng, target, occupied);

            for (int i = 0; i < points.Count && i < target; i++)
            {
                var cell = points[i];
                var id = MakePlannedNpcId(campId, cell);
                camp.PlannedNpcs.Add(new PlannedNpc { id = id, cell = cell, profile = npcProfile, campId = campId });
                occupied.Add(cell);
            }
        }
    }

    private void PlanNpcLegacy(CampRuntime camp, Random rng)
    {
        if (profile.npcRoles == null) return;

        int campId = Hash3(worldSeed, profile.seedSalt, camp.CenterCell.x * 101 + camp.CenterCell.y * 997);

        foreach (var role in profile.npcRoles)
        {
            int target = Mathf.Clamp(NextRange(rng, role.countRange.x, role.countRange.y), 0, 999);
            if (target <= 0) continue;

            var positions = PickNpcPositionsDet(camp.CenterCell, role, camp.Structures, rng, target);
            for (int i = 0; i < positions.Count && i < target; i++)
            {
                var cell = positions[i];
                // В legacy CampNpcRole хранит prefab GameObject, а не NPCProfile — поэтому профиля может не быть.
                // PoolingNPC должен пропускать записи без profile.
                var id = MakePlannedNpcId(campId, cell);
                camp.PlannedNpcs.Add(new PlannedNpc { id = id, cell = cell, profile = null, campId = campId });
            }
        }
    }

    // Публичный API для PoolingNPC: получить запланированных NPC в радиусе
    public int GatherPlannedNpcsWithinRadius(Vector2Int centerCell, int radiusCells, List<PlannedNpc> buffer)
    {
        buffer.Clear();
        int r2 = radiusCells * radiusCells;
        foreach (var kv in _chunkCamps)
        {
            var camps = kv.Value;
            for (int i = 0; i < camps.Count; i++)
            {
                var list = camps[i].PlannedNpcs;
                for (int j = 0; j < list.Count; j++)
                {
                    var p = list[j];
                    var d = p.cell - centerCell;
                    if (d.sqrMagnitude <= r2) buffer.Add(p);
                }
            }
        }
        return buffer.Count;
    }

    // Подбор позиций под одну запись пакета (оставлено без изменений)
    private List<Vector2Int> PickNpcPositionsFromEntry(
        Vector2Int centerCell, int campRadius,
        NPCSpawnList.Entry entry,
        List<GameObject> structures,
        Random rng, int target,
        List<Vector2Int> occupied
    )
    {
        var res = new List<Vector2Int>(target);
        float spacing = Mathf.Max(0f, entry.spacing);
        float spacing2 = spacing * spacing;
        int ringR = Mathf.Clamp(entry.radiusFromCenter > 0 ? Mathf.RoundToInt(entry.radiusFromCenter)
                                                          : Mathf.Max(1, campRadius - 1),
                                1, Mathf.Max(1, campRadius));

        bool FarEnough(Vector2Int p)
        {
            for (int i = 0; i < res.Count; i++) if ((res[i] - p).sqrMagnitude < spacing2) return false;
            for (int i = 0; i < occupied.Count; i++) if ((occupied[i] - p).sqrMagnitude < spacing2) return false;
            return true;
        }

        switch (entry.anchor)
        {
            case NPCSpawnList.SpawnAnchor.Center:
                {
                    int tries = target * 8;
                    while (res.Count < target && tries-- > 0)
                    {
                        var jitter = new Vector2Int(
                            Mathf.RoundToInt((float)((rng.NextDouble() - 0.5) * 2.0)),
                            Mathf.RoundToInt((float)((rng.NextDouble() - 0.5) * 2.0))
                        );
                        var p = centerCell + jitter;
                        if (FarEnough(p)) res.Add(p);
                    }
                    break;
                }
            case NPCSpawnList.SpawnAnchor.Perimeter:
                {
                    int count = Math.Max(1, target);
                    for (int i = 0; i < count && res.Count < target; i++)
                    {
                        double ang = (i / (double)count) * Math.PI * 2.0 + (rng.NextDouble() - 0.5) * 0.3;
                        var p = centerCell + new Vector2Int(
                            Mathf.RoundToInt(ringR * Mathf.Cos((float)ang)),
                            Mathf.RoundToInt(ringR * Mathf.Sin((float)ang))
                        );
                        if (FarEnough(p)) res.Add(p);
                    }
                    int tries = target * 8;
                    while (res.Count < target && tries-- > 0)
                    {
                        double ang = rng.NextDouble() * Math.PI * 2.0;
                        var p = centerCell + new Vector2Int(
                            Mathf.RoundToInt(ringR * Mathf.Cos((float)ang)),
                            Mathf.RoundToInt(ringR * Mathf.Sin((float)ang))
                        );
                        if (FarEnough(p)) res.Add(p);
                    }
                    break;
                }
            case NPCSpawnList.SpawnAnchor.Tents:
                {
                    foreach (var sgo in structures)
                    {
                        if (!sgo) continue;
                        if (sgo.name.IndexOf("Tent", StringComparison.OrdinalIgnoreCase) < 0) continue;

                        var c = WorldToCell(sgo.transform.position);
                        var dir = c - centerCell; if (dir == Vector2Int.zero) dir = new Vector2Int(1, 0);
                        var off = new Vector2Int(Mathf.Clamp(dir.x, -1, 1), Mathf.Clamp(dir.y, -1, 1));
                        var p = c + off;
                        if (FarEnough(p)) res.Add(p);
                        if (res.Count >= target) break;
                    }
                    int tries = target * 8;
                    while (res.Count < target && tries-- > 0)
                    {
                        var p = centerCell + RandInCircleDet(Mathf.Max(1, campRadius - 1), rng);
                        if (FarEnough(p)) res.Add(p);
                    }
                    break;
                }
            case NPCSpawnList.SpawnAnchor.Fire:
                {
                    Vector2Int anchor = centerCell;
                    foreach (var sgo in structures)
                    {
                        if (!sgo) continue;
                        var name = sgo.name;
                        if (name.IndexOf("Fire", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            name.IndexOf("Campfire", StringComparison.OrdinalIgnoreCase) >= 0)
                        { anchor = WorldToCell(sgo.transform.position); break; }
                    }
                    int tries = target * 8;
                    while (res.Count < target && tries-- > 0)
                    {
                        var p = anchor + RandInCircleDet(2, rng);
                        if (FarEnough(p)) res.Add(p);
                    }
                    break;
                }
            case NPCSpawnList.SpawnAnchor.Gate:
                {
                    Vector2Int anchor = centerCell;
                    foreach (var sgo in structures)
                    {
                        if (!sgo) continue;
                        if (sgo.name.IndexOf("Gate", StringComparison.OrdinalIgnoreCase) >= 0)
                        { anchor = WorldToCell(sgo.transform.position); break; }
                    }
                    int tries = target * 8;
                    while (res.Count < target && tries-- > 0)
                    {
                        var p = anchor + RandInCircleDet(2, rng);
                        if (FarEnough(p)) res.Add(p);
                    }
                    break;
                }
            default: // Any
                {
                    int tries = target * 16;
                    while (res.Count < target && tries-- > 0)
                    {
                        var p = centerCell + RandInCircleDet(Mathf.Max(1, campRadius - 1), rng);
                        if (FarEnough(p)) res.Add(p);
                    }
                    break;
                }
        }
        return res;
    }

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

    private static int NextRange(Random rng, int minInclusive, int maxInclusive)
    {
        if (minInclusive > maxInclusive) (minInclusive, maxInclusive) = (maxInclusive, minInclusive);
        return rng.Next(minInclusive, maxInclusive + 1);
    }

    private static Vector2Int RandInCircleDet(int radius, Random rng)
    {
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

    private static ulong MakePlannedNpcId(int campId, Vector2Int cell)
    {
        unchecked
        {
            uint h = (uint)campId;
            h ^= (uint)(cell.x * 73856093) ^ (uint)(cell.y * 19349663);
            h ^= 0x9E3779B9u;
            return ((ulong)h << 32) | (uint)((cell.x & 0xFFFF) << 16) | (uint)(cell.y & 0xFFFF);
        }
    }
}

// Оставляем утилиту для визуала — пригодится при спавне в PoolingNPC
public class NpcVisualApplier : MonoBehaviour
{
    public void Apply(NPCProfile profile)
    {
        if (profile && profile.skin && profile.skin.baseSprite)
        {
            var sr = GetComponentInChildren<SpriteRenderer>() ?? gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = profile.skin.baseSprite;
        }
    }
}
