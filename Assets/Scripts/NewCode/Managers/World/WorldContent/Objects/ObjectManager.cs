using System;
using System.Collections.Generic;
using UnityEngine;
using Game.World.Map.Biome;   // BiomeType, IBiomeService / BiomeManager
using Game.World.Objects;     // ObjectType, ObjectData, BiomeSpawnProfile, BiomeObjectRule, ObjectTags

/// <summary>
/// Генерирует объекты по чанкам на основе профилей биомов, выводит визуал через PoolManagerObjects,
/// отдаёт API для стриминга из PoolManager (Load/UnloadChunkVisuals).
/// </summary>
[DefaultExecutionOrder(-225)]
public class ObjectManager : MonoBehaviour
{
    // ========== CONFIG ==========
    [Header("Generation")]
    [Tooltip("Глобальный сид мира для детерминированной генерации.")]
    [SerializeField] private int worldSeed = 12345;
    // клетки, «занятые» лагерями/структурами – сюда обычный спавн не имеет права
    private readonly HashSet<Vector2Int> _reservedCells = new();

    [Tooltip("Размер чанка в клетках. ДОЛЖЕН совпадать с PoolManager.objectsChunkSize.")]
    [SerializeField] private int chunkSize = 64;

    [Tooltip("Профили спавна по биомам (по одному на биом).")]
    [SerializeField] private List<BiomeSpawnProfile> biomeProfiles = new();

    [Tooltip("База объектов (ScriptableObject'ы-«паспорта»).")]
    [SerializeField] private List<ObjectData> objectDatabase = new();

    [Header("Rendering / Pool")]
    [Tooltip("Пул объектов (префабы). Если не назначить — найдётся автоматически в сцене.")]
    [SerializeField] private PoolManagerObjects objectPool;

    [Tooltip("Размер клетки в мировых юнитах. Совпадает с тайловой сеткой.")]
    [SerializeField] private float cellSize = 1f;

    [Tooltip("Включить сортировку по Y для SpriteRenderer (2D спрайтовая сортировка).")]
    [SerializeField] private bool ySort = true;

    [Tooltip("Множитель для order при Y-сортировке (чем больше — тем стабильнее порядок).")]
    [SerializeField] private int ySortMul = 10;
    // === persistence ===
    private readonly Dictionary<ulong, ObjectType[]> _origTypes = new(); // snapshot типов при генерации
    [SerializeField] private string saveFolder = "chunks/objects";        // внутри persistentDataPath

    // === render/sorting (если ещё нет)
    [SerializeField] private string objectsSortingLayer = "Objects";
    [SerializeField] private int objectsOrderOffset = 0;
    private int _objLayerID;

    // ========== DEPENDENCIES ==========
    private IBiomeService _biomes;

    // ========== RUNTIME STATE ==========
    // Быстрый доступ
    private readonly Dictionary<BiomeType, BiomeSpawnProfile> _profiles = new();
    private readonly Dictionary<ObjectType, ObjectData> _objByType = new();

    // Данные и визуал по чанкам
    private readonly Dictionary<ulong, List<ObjectInstanceData>> _chunkData = new();
    private readonly Dictionary<ulong, List<GameObject>> _chunkVisual = new();

    // Дельты состояния (сбор/разрушение и т.п.)
    private readonly Dictionary<ulong, ObjectState> _stateDiffs = new();

    // ==================== PUBLIC (для PoolManager) ====================

    public int ChunkSize => chunkSize;

    public readonly struct ChunkCoord
    {
        public readonly int x, y;
        public ChunkCoord(int x, int y) { this.x = x; this.y = y; }
    }
    /// Забронировать прямоугольную область (в КЛЕТКАХ).
    public void ReserveArea(RectInt rect)
    {
        for (int x = rect.xMin; x < rect.xMax; x++)
            for (int y = rect.yMin; y < rect.yMax; y++)
                _reservedCells.Add(new Vector2Int(x, y));
    }

    /// Быстрая проверка «клетка забронирована?»
    public bool IsReserved(Vector2Int cell) => _reservedCells.Contains(cell);
    public Vector2Int ChunkOrigin(ChunkCoord c) => new(c.x * chunkSize, c.y * chunkSize);
    public ulong ChunkKey(ChunkCoord c) => ((ulong)(uint)c.x << 32) | (uint)c.y;

    /// <summary>Ленивая генерация: вернёт готовые инстансы или сгенерирует заново.</summary>

    /// <summary>Создать/взять из пула визуал объектов чанка.</summary>
    public void LoadChunkVisuals(ChunkCoord cc)
    {
        var key = ChunkKey(cc);
        if (_chunkVisual.ContainsKey(key)) return;

        var data = GetOrGenerateChunk(cc);
        var list = new List<GameObject>(data.Count);
        foreach (var d in data)
        {
            if (IsDestroyed(d.id)) continue;

            var go = objectPool ? objectPool.Get(d.type) : CreateAdhocGO(d.type);
            ApplyVisual(go, d);
            list.Add(go);
        }
        _chunkVisual[key] = list;
    }

    /// <summary>Вернуть визуал объектов чанка в пул.</summary>
    public void UnloadChunkVisuals(ChunkCoord cc)
    {
        var key = ChunkKey(cc);
        if (!_chunkVisual.TryGetValue(key, out var list)) return;

        foreach (var go in list)
        {
            if (!go) continue;
            if (objectPool) objectPool.Release(go);
            else Destroy(go);
        }
        list.Clear();
        _chunkVisual.Remove(key);
        SaveChunkDiffs(key);
    }

    // ==================== UNITY LIFECYCLE ====================

    void Awake()
    {
        // Biomes
        _biomes = FindFirstObjectByType<BiomeManager>(FindObjectsInactive.Exclude);
        if (_biomes == null)
            Debug.LogError("[ObjectManager] BiomeManager не найден в сцене.");

        // Pool
        if (!objectPool)
            objectPool = FindFirstObjectByType<PoolManagerObjects>(FindObjectsInactive.Exclude);
        if (!objectPool)
            Debug.LogWarning("[ObjectManager] PoolManagerObjects не найден. Будет использоваться Instantiate/Destroy (на альфе ок).");

        // Build maps
        _profiles.Clear();
        foreach (var p in biomeProfiles)
            if (p) _profiles[p.biome] = p;

        _objByType.Clear();
        foreach (var od in objectDatabase)
            if (od) _objByType[od.type] = od;
        _objLayerID = SortingLayer.NameToID(objectsSortingLayer);
        var dir = System.IO.Path.Combine(Application.persistentDataPath, saveFolder);
        if (!System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);
    }

    // ==================== GENERATION ====================

    private List<ObjectInstanceData> GenerateChunk(ChunkCoord cc)
    {
        var result = new List<ObjectInstanceData>(128);
        var origin = ChunkOrigin(cc);
        var key = ChunkKey(cc);

        // Набор занятых клеток (любыми объектами) в рамках чанка — чтобы не пересекать футпринты
        var occupied = new HashSet<Vector2Int>();

        // Проходим все профили биомов и накладываем их правила на чанк
        foreach (var kv in _profiles)
        {
            var biome = kv.Key;
            var prof = kv.Value;
            float densMul = Mathf.Max(0.01f, prof.densityMultiplier);

            foreach (var rule in prof.rules)
            {
                int target = Mathf.RoundToInt(rule.targetPerChunk * densMul);
                if (target <= 0) continue;

                var rng = new System.Random(Hash(worldSeed, cc.x, cc.y, (int)rule.objectType));

                switch (rule.mode)
                {
                    case SpawnMode.BlueNoise:
                        PlaceBlueNoise(biome, rule, origin, target, rng, occupied, result, key);
                        break;

                    case SpawnMode.Clustered:
                        PlaceClustered(biome, rule, origin, target, rng, occupied, result, key);
                        break;

                    default: // Uniform
                        PlaceUniform(biome, rule, origin, target, rng, occupied, result, key);
                        break;
                }
            }
        }
        _origTypes[ChunkKey(cc)] = result.ConvertAll(r => r.type).ToArray();
        ApplySavedDiffs(ChunkKey(cc), result);
        return result;
    }

    private void PlaceUniform(BiomeType biome, BiomeObjectRule rule, Vector2Int origin, int target,
                              System.Random rng, HashSet<Vector2Int> occ, List<ObjectInstanceData> outList, ulong chunkKey)
    {
        int triesMax = target * 8;
        for (int i = 0, placed = 0; i < triesMax && placed < target; i++)
        {
            var local = new Vector2Int(rng.Next(0, chunkSize), rng.Next(0, chunkSize));
            var cell = origin + local;
            if (!IsAllowed(biome, rule, cell, occ)) continue;

            var inst = MakeInstance(rule, cell, rng, chunkKey, outList.Count);
            Occupy(_objByType[rule.objectType].footprint, cell, occ);
            outList.Add(inst); placed++;
        }
    }

    private void PlaceClustered(BiomeType biome, BiomeObjectRule rule, Vector2Int origin, int target,
                                System.Random rng, HashSet<Vector2Int> occ, List<ObjectInstanceData> outList, ulong chunkKey)
    {
        int avgSat = Mathf.Max(1, (rule.clusterCountRange.x + rule.clusterCountRange.y) / 2);
        int clusters = Mathf.Max(1, target / Mathf.Max(1, avgSat));

        for (int c = 0; c < clusters && outList.Count < target; c++)
        {
            if (!TryPickCell(biome, rule, origin, rng, occ, out var seed)) continue;

            var inst = MakeInstance(rule, seed, rng, chunkKey, outList.Count);
            Occupy(_objByType[rule.objectType].footprint, seed, occ);
            outList.Add(inst);

            int satellites = rng.Next(rule.clusterCountRange.x, rule.clusterCountRange.y + 1);
            for (int s = 0; s < satellites && outList.Count < target; s++)
            {
                var near = seed + new Vector2Int(rng.Next(-3, 4), rng.Next(-3, 4));
                if (!IsAllowed(biome, rule, near, occ, noiseGate: false)) continue;

                var sat = MakeInstance(rule, near, rng, chunkKey, outList.Count);
                Occupy(_objByType[rule.objectType].footprint, near, occ);
                outList.Add(sat);
            }
        }
    }

    private void PlaceBlueNoise(BiomeType biome, BiomeObjectRule rule, Vector2Int origin, int target,
                                System.Random rng, HashSet<Vector2Int> occ, List<ObjectInstanceData> outList, ulong chunkKey)
    {
        int step = Mathf.Max(1, Mathf.RoundToInt(rule.minDistanceSameType));
        for (int x = 0; x < chunkSize; x += step)
            for (int y = 0; y < chunkSize; y += step)
            {
                if (outList.Count >= target) return;

                var jitter = new Vector2Int(rng.Next(0, step), rng.Next(0, step));
                var cell = origin + new Vector2Int(Mathf.Min(x + jitter.x, chunkSize - 1),
                                                     Mathf.Min(y + jitter.y, chunkSize - 1));

                if (!IsAllowed(biome, rule, cell, occ)) continue;

                var inst = MakeInstance(rule, cell, rng, chunkKey, outList.Count);
                Occupy(_objByType[rule.objectType].footprint, cell, occ);
                outList.Add(inst);
            }
    }

    private bool TryPickCell(BiomeType biome, BiomeObjectRule rule, Vector2Int origin, System.Random rng,
                             HashSet<Vector2Int> occ, out Vector2Int cell)
    {
        for (int i = 0; i < 32; i++)
        {
            var local = new Vector2Int(rng.Next(0, chunkSize), rng.Next(0, chunkSize));
            var c = origin + local;
            if (IsAllowed(biome, rule, c, occ)) { cell = c; return true; }
        }
        cell = default; return false;
    }

    private bool IsAllowed(BiomeType neededBiome, BiomeObjectRule rule, Vector2Int cell, HashSet<Vector2Int> occ, bool noiseGate = true)
    {
        if (_reservedCells.Contains(cell)) return false;
        // 1) биом клетки должен совпадать с профилем
        if (_biomes == null || _biomes.GetBiomeAtPosition(cell) != neededBiome) return false;

        // 2) шумовая маска (Perlin-gate)
        if (noiseGate && rule.useNoiseGate)
        {
            float n = Mathf.PerlinNoise(cell.x * rule.noiseScale, cell.y * rule.noiseScale);
            if (n < rule.noiseThreshold) return false;
        }

        // 3) футпринт влезает и клетки не заняты другими объектами
        if (!_objByType.TryGetValue(rule.objectType, out var od)) return false;

        for (int x = 0; x < od.footprint.x; x++)
            for (int y = 0; y < od.footprint.y; y++)
            {
                var c = new Vector2Int(cell.x + x, cell.y + y);
                if (occ.Contains(c)) return false;
            }

        // 4) «аура» дистанций от уже занятых клеток
        if (rule.avoidOtherObjects)
        {
            int r = Mathf.CeilToInt(Mathf.Max(rule.minDistanceAny, rule.minDistanceSameType));
            for (int dx = -r; dx <= r; dx++)
                for (int dy = -r; dy <= r; dy++)
                    if (occ.Contains(new Vector2Int(cell.x + dx, cell.y + dy))) return false;
        }

        // 5) Частный кейс: Пальмы в Desert — только рядом с Oasis (эмуляция воды)
        if (rule.objectType == ObjectType.Palm && neededBiome == BiomeType.Desert)
            if (!IsNearBiome(cell, BiomeType.Savanna, 3)) return false;

        return true;
    }
    /// Создать объект заданного типа в точной клетке.
    /// Возвращает его id (или 0, если отложено/не удалось).
    public ulong PlaceObject(ObjectType type, Vector2Int cell, int variantIndex = 0)
    {
        // 0) проверим наличие паспорта
        if (!_objByType.TryGetValue(type, out var data)) return 0;

        // 1) вычисляем координаты чанка для клетки
        int cx = cell.x >= 0 ? cell.x / chunkSize : (cell.x - (chunkSize - 1)) / chunkSize;
        int cy = cell.y >= 0 ? cell.y / chunkSize : (cell.y - (chunkSize - 1)) / chunkSize;
        var cc = new ChunkCoord(cx, cy);
        var key = ChunkKey(cc);

        // 2) убеждаемся, что можем работать с данными чанка
        if (!_chunkData.TryGetValue(key, out var list))
        {
            // биомы должны быть готовы, иначе переносим постановку (вернём 0)
            if (!(_biomes is BiomeManager bm) || !bm.IsBiomesReady) return 0;
            list = new List<ObjectInstanceData>();
            _chunkData[key] = list;
        }

        // 3) создаём запись
        int safeVariant = 0;
        if (data.spriteVariants != null && data.spriteVariants.Length > 0)
            safeVariant = Mathf.Clamp(variantIndex, 0, data.spriteVariants.Length - 1);

        var inst = new ObjectInstanceData
        {
            id = ((ulong)(uint)list.Count) | (key << 32),
            type = type,
            cell = cell,
            variantIndex = safeVariant,
            worldPos = CellToWorld(cell),
            footprint = data.footprint
        };

        list.Add(inst);

        // 4) чтобы в эту область не попали другие объекты, сразу отмечаем футпринт как «резерв»
        for (int x = 0; x < data.footprint.x; x++)
            for (int y = 0; y < data.footprint.y; y++)
                _reservedCells.Add(new Vector2Int(cell.x + x, cell.y + y));

        // 5) если визуал чанка уже загружен — нарисуем прямо сейчас
        if (_chunkVisual.TryGetValue(key, out var gos))
        {
            var go = objectPool ? objectPool.Get(type) : CreateAdhocGO(type);
            ApplyVisual(go, inst);
            gos.Add(go);
        }

        return inst.id;
    }
    /// Зарезервировать круг (радиус в клетках).
    public void ReserveCircle(Vector2Int center, int radius)
    {
        int r2 = radius * radius;
        for (int y = center.y - radius; y <= center.y + radius; y++)
            for (int x = center.x - radius; x <= center.x + radius; x++)
            {
                var c = new Vector2Int(x, y);
                if ((c - center).sqrMagnitude <= r2) _reservedCells.Add(c);
            }
    }

    private bool IsNearBiome(Vector2Int cell, BiomeType target, int radius)
    {
        for (int dx = -radius; dx <= radius; dx++)
            for (int dy = -radius; dy <= radius; dy++)
            {
                var c = new Vector2Int(cell.x + dx, cell.y + dy);
                if (_biomes.GetBiomeAtPosition(c) == target) return true;
            }
        return false;
    }

    private ObjectInstanceData MakeInstance(BiomeObjectRule rule, Vector2Int cell, System.Random rng, ulong chunkKey, int idx)
    {
        var data = _objByType[rule.objectType];
        int maxV = (data.spriteVariants != null && data.spriteVariants.Length > 0) ? data.spriteVariants.Length : 1;
        int vIdx = Mathf.Clamp(rule.variants > 1 ? rng.Next(0, rule.variants) : 0, 0, Mathf.Max(0, maxV - 1));

        return new ObjectInstanceData
        {
            id = ((ulong)(uint)idx) | (chunkKey << 32),
            type = rule.objectType,
            cell = cell,
            variantIndex = vIdx,
            worldPos = CellToWorld(cell),
            footprint = data.footprint
        };
    }

    // ==================== VISUAL ====================

    private void ApplyVisual(GameObject go, ObjectInstanceData inst)
    {
        var data = _objByType[inst.type];

        // позиция
        go.transform.position = new Vector3(inst.worldPos.x, inst.worldPos.y, 0f) + (Vector3)data.pivotOffsetWorld;
        if ((data.tags & ObjectTags.HighSprite) != 0)
            go.transform.position += Vector3.up * (data.visualHeightUnits * cellSize);

        // ссылки
        var wref = go.GetComponent<WorldObjectRef>() ?? go.AddComponent<WorldObjectRef>();
        wref.id = inst.id; wref.type = inst.type;

        var tag = go.GetComponent<PooledObjectTag>();
        var sr = (tag && tag.SR) ? tag.SR : go.GetComponentInChildren<SpriteRenderer>();
        if (sr)
        {
            if (data.spriteVariants != null && data.spriteVariants.Length > 0)
                sr.sprite = data.spriteVariants[Mathf.Clamp(inst.variantIndex, 0, data.spriteVariants.Length - 1)];
            if (sr.sortingLayerID != _objLayerID) sr.sortingLayerID = _objLayerID;
            sr.sortingOrder = objectsOrderOffset + (ySort ? -(int)Mathf.Round(go.transform.position.y * ySortMul) : 0);
            sr.sortingLayerID = SortingLayer.NameToID("Objects");
        }

        // интерактив для сборных (ягодных)
        bool harvestable = data.harvest.harvestable;
        if (harvestable)
        {
            var cc = go.GetComponent<CircleCollider2D>() ?? go.AddComponent<CircleCollider2D>();
            cc.isTrigger = true;
            cc.radius = 0.45f * cellSize;
            go.layer = LayerMask.NameToLayer("Interactable"); // создай слой в проекте
        }

        // коллизии для непроходимых
        bool blocking = data.movementModifier <= 0f || (data.tags & ObjectTags.StaticObstacle) != 0
                        || inst.type == ObjectType.Rock || inst.type == ObjectType.Oak
                        || inst.type == ObjectType.Spruce || inst.type == ObjectType.Palm;
        var bc = go.GetComponent<BoxCollider2D>();
        if (blocking)
        {
            if (!bc) bc = go.AddComponent<BoxCollider2D>();
            bc.isTrigger = false;
            bc.size = new Vector2(Mathf.Max(0.9f, data.footprint.x) * cellSize,
                                  Mathf.Max(0.9f, data.footprint.y) * cellSize);
            bc.offset = new Vector2(bc.size.x * 0.5f, bc.size.y * 0.5f);
        }
        else
        {
            if (bc) Destroy(bc);
        }
    }

    // ==================== GAMEPLAY (сбор/разрушение) ====================

    private bool IsDestroyed(ulong id)
        => _stateDiffs.TryGetValue(id, out var st) && st.isDestroyed;

    public void MarkDestroyed(ulong id)
    {
        if (!_stateDiffs.TryGetValue(id, out var st)) st = new ObjectState();
        st.isDestroyed = true;
        _stateDiffs[id] = st;
    }

    /// <summary>Сбор урожая (BerryBush -> Bush). Вернёт true, если сбор успешен.</summary>
    public bool TryHarvest(ulong id, System.Random rng = null)
    {
        ulong chunkKey = id >> 32;
        int index = (int)(id & 0xffffffff);

        if (!_chunkData.TryGetValue(chunkKey, out var list)) return false;
        if (index < 0 || index >= list.Count) return false;

        var inst = list[index];
        if (!_objByType.TryGetValue(inst.type, out var data) || !data.harvest.harvestable) return false;

        // Выдача дропа (если настроен)
        rng ??= new System.Random(unchecked((int)id));
        if (data.harvest.harvestDrops != null)
        {
            foreach (var drop in data.harvest.harvestDrops)
            {
                if (UnityEngine.Random.value <= drop.chance)
                {
                    int n = UnityEngine.Random.Range(drop.minCount, drop.maxCount + 1);
                    // Подключи свою систему инвентаря:
                    // Inventory.Give(drop.itemId, n);
                }
            }
        }

        // Изменение состояния и визуала
        if (data.harvest.destroyOnHarvest)
        {
            MarkDestroyed(id);
            if (_chunkVisual.TryGetValue(chunkKey, out var gos) && index < gos.Count && gos[index] != null)
                if (objectPool) objectPool.Release(gos[index]); else Destroy(gos[index]);
        }
        else
        {
            inst.type = data.harvest.transformToType; // BerryBush -> Bush
            list[index] = inst;

            if (_chunkVisual.TryGetValue(chunkKey, out var gos) && index < gos.Count && gos[index] != null)
                ApplyVisual(gos[index], inst);
        }

        return true;
    }

    // ==================== HELPERS ====================

    private Vector2 CellToWorld(Vector2Int cell) => new(cell.x * cellSize, cell.y * cellSize);

    private void Occupy(Vector2Int size, Vector2Int anchor, HashSet<Vector2Int> occ)
    {
        for (int x = 0; x < size.x; x++)
            for (int y = 0; y < size.y; y++)
                occ.Add(new Vector2Int(anchor.x + x, anchor.y + y));
    }

    private static int Hash(int seed, int x, int y, int salt)
    {
        unchecked
        {
            int h = seed;
            h ^= x * 73856093;
            h ^= y * 19349663;
            h ^= salt * 83492791;
            h = (h << 13) ^ h;
            return h;
        }
    }
    // Фолбэк, если objectPool не назначен (debug/альфа)
    private Transform _adhocRoot;

    private GameObject CreateAdhocGO(ObjectType type)
    {
        if (_adhocRoot == null)
        {
            _adhocRoot = new GameObject("Objects_Adhoc").transform;
            _adhocRoot.SetParent(transform, false);
        }

        var go = new GameObject($"adhoc_{type}");
        go.transform.SetParent(_adhocRoot, false);

        // базовый визуал
        var sr = go.AddComponent<SpriteRenderer>();

        // кэш, чтобы ApplyVisual не искал компоненты
        var tag = go.AddComponent<PooledObjectTag>();
        tag.Type = type;
        tag.SR = sr;

        return go;
    }
    // ObjectManager.cs (в классе)
    private bool BiomesReady =>
        _biomes != null && (_biomes as BiomeManager)?.IsBiomesReady == true;

    public List<ObjectInstanceData> GetOrGenerateChunk(ChunkCoord cc)
    {
        var key = ChunkKey(cc);
        if (_chunkData.TryGetValue(key, out var list)) return list;

        // важно: не кэшируем чанк, пока биомы не готовы
        if (!BiomesReady) return new List<ObjectInstanceData>();

        list = GenerateChunk(cc);
        _chunkData[key] = list;
        return list;
    }
    public void ClearChunkCache()
    {
        // выгрузка визуала
        foreach (var kv in _chunkVisual)
            foreach (var go in kv.Value)
                if (go) { if (objectPool) objectPool.Release(go); else Destroy(go); }

        _chunkVisual.Clear();
        _chunkData.Clear();
        _stateDiffs.Clear();

        // ВАЖНО: чистим резерв под лагеря (их пересоздаст CampManager)
        _reservedCells.Clear();
        _origTypes.Clear(); // если используешь снапшоты для дельт
    }
    [Serializable] class ChunkDelta { public int i; public bool d; public int t = -1; }
    [Serializable] class ChunkSave { public int v = 1; public List<ChunkDelta> deltas = new(); }

    private string PathFor(ulong key)
    {
        int x = (int)(key >> 32); int y = (int)(key & 0xffffffff);
        return System.IO.Path.Combine(Application.persistentDataPath, saveFolder, $"{x}_{y}.json");
    }

    private void ApplySavedDiffs(ulong key, List<ObjectInstanceData> list)
    {
        var path = PathFor(key);
        if (!System.IO.File.Exists(path)) return;
        var json = System.IO.File.ReadAllText(path);
        var save = JsonUtility.FromJson<ChunkSave>(json);
        if (save?.deltas == null) return;

        foreach (var d in save.deltas)
        {
            if (d.i < 0 || d.i >= list.Count) continue;
            var id = ((ulong)(uint)d.i) | (key << 32);
            if (d.d) { MarkDestroyed(id); continue; }
            if (d.t >= 0)
            {
                list[d.i] = new ObjectInstanceData
                {
                    id = list[d.i].id,
                    cell = list[d.i].cell,
                    worldPos = list[d.i].worldPos,
                    footprint = list[d.i].footprint,
                    variantIndex = list[d.i].variantIndex,
                    type = (ObjectType)d.t
                };
            }
        }
    }

    public void SaveChunkDiffs(ulong key)
    {
        if (!_chunkData.TryGetValue(key, out var list)) return;
        var deltas = new List<ChunkDelta>();
        var orig = _origTypes.TryGetValue(key, out var arr) ? arr : null;

        for (int i = 0; i < list.Count; i++)
        {
            var id = ((ulong)(uint)i) | (key << 32);
            bool destroyed = IsDestroyed(id);
            bool changedType = orig != null && i < orig.Length && list[i].type != orig[i];
            if (!destroyed && !changedType) continue;
            deltas.Add(new ChunkDelta
            {
                i = i,
                d = destroyed,
                t = changedType ? (int)list[i].type : -1
            });
        }
        var save = new ChunkSave { deltas = deltas };
        var json = JsonUtility.ToJson(save, true);
        System.IO.File.WriteAllText(PathFor(key), json);
    }

    private void SaveAllLoadedChunks()
    {
        foreach (var key in _chunkData.Keys) SaveChunkDiffs(key);
    }

    void OnDisable() => SaveAllLoadedChunks();
}

// ==================== DATA STRUCTS ====================

public struct ObjectInstanceData
{
    public ulong id;
    public ObjectType type;
    public Vector2Int cell;
    public int variantIndex;
    public Vector2 worldPos;
    public Vector2Int footprint;
}

public struct ObjectState
{
    public bool isDestroyed;
}
public class WorldObjectRef : MonoBehaviour
{
    public ulong id;
    public ObjectType type;
}
