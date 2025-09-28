using System;
using System.Collections.Generic;
using UnityEngine;
using Game.World.Map.Biome;   // BiomeType, IBiomeService / BiomeManager
using Game.World.Objects;     // ObjectType, ObjectData, BiomeSpawnProfile, BiomeObjectRule, ObjectTags

/// <summary>
///        ,    PoolManagerObjects,
///  API    PoolManager (Load/UnloadChunkVisuals).
/// </summary>
[DefaultExecutionOrder(-225)]
public class ObjectManager : MonoBehaviour
{
    // ========== CONFIG ==========
    [Header("Generation")]
    [Tooltip("     .")]
    [SerializeField] private int worldSeed = 12345;

    [Tooltip("   .    PoolManager.objectsChunkSize.")]
    [SerializeField] private int chunkSize = 64;

    [Tooltip("    (   ).")]
    [SerializeField] private List<BiomeSpawnProfile> biomeProfiles = new();

    [Tooltip("  (ScriptableObject'-).")]
    [SerializeField] private List<ObjectData> objectDatabase = new();

    [Header("Rendering / Pool")]
    [Tooltip("  ().        .")]
    [SerializeField] private PoolManagerObjects objectPool;

    [Tooltip("    .    .")]
    [SerializeField] private float cellSize = 1f;

    [Tooltip("   Y  SpriteRenderer (2D  ).")]
    [SerializeField] private bool ySort = true;

    [Tooltip("  order  Y- (     ).")]
    [SerializeField] private int ySortMul = 10;

    // === persistence ===
    private readonly Dictionary<ulong, ObjectType[]> _origTypes = new(); // snapshot   

    // === biome / rules / db ===
    private readonly Dictionary<BiomeType, BiomeSpawnProfile> _profiles = new();
    private readonly Dictionary<ObjectType, ObjectData> _objByType = new();

    //     /runtime
    private readonly Dictionary<ulong, List<ObjectInstanceData>> _chunkData = new();
    private readonly Dictionary<ulong, List<GameObject>> _chunkVisual = new();

    // notify pool about data-driven destruction if needed
    public event Action<ulong, GameObject> OnVisualObjectDestroyed;

    //   (/  ..)
    private readonly Dictionary<ulong, ObjectState> _stateDiffs = new();

    // =============== SERVICES ===============
    private IBiomeService _biomes;
    [SerializeField] private MonoBehaviour reservationBehaviour;
    private IReservationService _reservation => reservationBehaviour as IReservationService;

    // =============== RUNTIME HELPERS ===============
    private int _objLayerID;
    private Transform _objectsRoot;
    private Transform _adhocRoot;

    // ==================== TYPES ====================

    public readonly struct ChunkCoord
    {
        public readonly int x, y;
        public ChunkCoord(int x, int y) { this.x = x; this.y = y; }
        public override string ToString() => $"({x},{y})";
    }

    ///    ( ).
    public void ReserveArea(RectInt rect)
    {
        if (_reservation != null)
        {
            //  ReserveRect       
            for (int x = rect.xMin; x < rect.xMax; x++)
                for (int y = rect.yMin; y < rect.yMax; y++)
                    _reservation.ReserveCircle(new Vector2Int(x, y), 0, ReservationMask.Nature | ReservationMask.Camps);
        }
        else
        {
            for (int x = rect.xMin; x < rect.xMax; x++)
                for (int y = rect.yMin; y < rect.yMax; y++)
                    _reservedCells.Add(new Vector2Int(x, y));
        }
    }

    private readonly HashSet<Vector2Int> _reservedCells = new();

    public bool IsReserved(Vector2Int cell) =>
        _reservation != null
            ? _reservation.IsReserved(cell, ReservationMask.All)
            : _reservedCells.Contains(cell);

    public Vector2Int ChunkOrigin(ChunkCoord c) => new(c.x * chunkSize, c.y * chunkSize);
    public ulong ChunkKey(ChunkCoord c) => ((ulong)(uint)c.x << 32) | (uint)c.y;

    /// <summary>/     .</summary>
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

    /// <summary>     .</summary>
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

    // === Pool-facing API (optional): allow external pool to register visuals ===
    public IReadOnlyList<ObjectInstanceData> GetChunkData(ChunkCoord cc)
    {
        return GetOrGenerateChunk(cc);
    }

    public void SetChunkVisuals(ChunkCoord cc, List<GameObject> list)
    {
        _chunkVisual[ChunkKey(cc)] = list;
    }

    public List<GameObject> GetChunkVisuals(ChunkCoord cc)
    {
        _chunkVisual.TryGetValue(ChunkKey(cc), out var list);
        return list;
    }

    public void ClearChunkVisuals(ChunkCoord cc)
    {
        var key = ChunkKey(cc);
        _chunkVisual.Remove(key);
        SaveChunkDiffs(key);
    }

    // =============== UNITY LIFECYCLE ===============

    void Awake()
    {
        // Biomes
        _biomes = FindFirstObjectByType<BiomeManager>(FindObjectsInactive.Exclude);
        if (_biomes == null)
            Debug.LogError("[ObjectManager] BiomeManager    .");

        // Root for objects
        var root = new GameObject("Objects_Root");
        root.transform.SetParent(transform, false);
        _objectsRoot = root.transform;

        if (!objectPool)
            Debug.LogWarning("[ObjectManager] PoolManagerObjects  .   Instantiate/Destroy (  ).");

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

        //    ( )        
        var occupied = new HashSet<Vector2Int>();

        //          
        foreach (var kv in _profiles)
        {
            var biome = kv.Key;
            var profile = kv.Value;
            if (profile == null || profile.rules == null) continue;

            foreach (var rule in profile.rules)
            {
                if (!_objByType.TryGetValue(rule.objectType, out var od)) continue;

                int target = Mathf.Max(0, rule.targetPerChunk);
                if (target == 0) continue;

                var rng = new System.Random(unchecked((int)(worldSeed ^ (cc.x * 73856093) ^ (cc.y * 19349663) ^ (int)rule.objectType)));

                if (rule.spawnMode == SpawnMode.Clustered)
                    PlaceClustered(biome, rule, origin, target, rng, occupied, result, key);
                else
                    PlaceUniform(biome, rule, origin, target, rng, occupied, result, key);
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

            var inst0 = MakeInstance(rule, seed, rng, chunkKey, outList.Count);
            Occupy(_objByType[rule.objectType].footprint, seed, occ);
            outList.Add(inst0);

            int satellites = rng.Next(rule.clusterCountRange.x, rule.clusterCountRange.y + 1);
            for (int s = 0; s < satellites && outList.Count < target; s++)
            {
                var cell = seed + new Vector2Int(
                    rng.Next(-rule.clusterRadiusCells, rule.clusterRadiusCells + 1),
                    rng.Next(-rule.clusterRadiusCells, rule.clusterRadiusCells + 1)
                );

                if (!CellInChunk(cell, origin)) continue;
                if (!IsAllowed(biome, rule, cell, occ)) continue;

                var inst = MakeInstance(rule, cell, rng, chunkKey, outList.Count);
                Occupy(_objByType[rule.objectType].footprint, cell, occ);
                outList.Add(inst);
            }
        }
    }

    private bool TryPickCell(BiomeType biome, BiomeObjectRule rule, Vector2Int origin,
                             System.Random rng, HashSet<Vector2Int> occ, out Vector2Int cell)
    {
        for (int tries = 0; tries < 32; tries++)
        {
            var local = new Vector2Int(rng.Next(0, chunkSize), rng.Next(0, chunkSize));
            var p = origin + local;

            if (!IsAllowed(biome, rule, p, occ)) continue;
            cell = p;
            return true;
        }
        cell = default;
        return false;
    }

    private bool IsAllowed(BiomeType neededBiome, BiomeObjectRule rule, Vector2Int cell, HashSet<Vector2Int> occ, bool noiseGate = true)
    {
        // резервации (лагеря и т.п.)
        if (_reservation != null)
        {
            if (_reservation.IsReserved(cell, ReservationMask.Nature | ReservationMask.Camps)) return false;
        }
        else
        {
            if (_reservedCells.Contains(cell)) return false;                        // 
        }

        // 1)      
        if (_biomes == null || _biomes.GetBiomeAtPosition(cell) != neededBiome) return false;

        // 2)   (Perlin-gate)
        if (noiseGate && rule.useNoiseGate)
        {
            float n = Mathf.PerlinNoise(cell.x * rule.noiseScale, cell.y * rule.noiseScale);
            if (n < rule.noiseThreshold) return false;
        }

        // 3)        
        if (!_objByType.TryGetValue(rule.objectType, out var od)) return false;

        for (int x = 0; x < od.footprint.x; x++)
            for (int y = 0; y < od.footprint.y; y++)
            {
                var c = new Vector2Int(cell.x + x, cell.y + y);
                if (occ.Contains(c)) return false;
            }

        // 4)      
        if (rule.avoidOtherObjects)
        {
            int r = Mathf.CeilToInt(Mathf.Max(rule.minDistanceAny, rule.minDistanceSameType));
            for (int dx = -r; dx <= r; dx++)
                for (int dy = -r; dy <= r; dy++)
                    if (occ.Contains(new Vector2Int(cell.x + dx, cell.y + dy))) return false;
        }

        return true;
    }

    private bool CellInChunk(Vector2Int cell, Vector2Int origin)
        => cell.x >= origin.x && cell.x < origin.x + chunkSize &&
           cell.y >= origin.y && cell.y < origin.y + chunkSize;

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

        // 
        go.transform.position = new Vector3(inst.worldPos.x, inst.worldPos.y, 0f) + (Vector3)data.pivotOffsetWorld;
        if ((data.tags & ObjectTags.HighSprite) != 0)
            go.transform.position += Vector3.up * (data.visualHeightUnits * cellSize);

        // Renderer
        var tag = go.GetComponent<PooledObjectTag>() ?? go.AddComponent<PooledObjectTag>();
        tag.Type = inst.type;
        var sr = tag.SR ??= go.GetComponentInChildren<SpriteRenderer>();
        if (data.spriteVariants != null && data.spriteVariants.Length > 0)
            sr.sprite = data.spriteVariants[Mathf.Clamp(inst.variantIndex, 0, data.spriteVariants.Length - 1)];

        // sort layer / order
        sr.sortingLayerID = _objLayerID;
        sr.sortingOrder = ySort ? -(int)Mathf.Round(go.transform.position.y * ySortMul) : 0;

        go.layer = LayerMask.NameToLayer("WorldObject");
        go.SetActive(true);

        // коллизии/интерактив — (упрощённо, как было)
        var blocking = (data.tags & ObjectTags.StaticObstacle) != 0 || data.movementModifier <= 0f;
        var bc = go.GetComponent<BoxCollider2D>();
        if (blocking)
        {
            if (!bc) bc = go.AddComponent<BoxCollider2D>();
            bc.isTrigger = false;
            bc.size = new Vector2(Mathf.Max(0.9f, data.footprint.x) * cellSize, Mathf.Max(0.9f, data.footprint.y) * cellSize);
            bc.offset = new Vector2(bc.size.x * 0.5f, bc.size.y * 0.5f);
        }
        else
        {
            if (bc) Destroy(bc);
        }
    }

    // ==================== GAMEPLAY (/) ====================

    public bool IsDestroyed(ulong id)
        => _stateDiffs.TryGetValue(id, out var st) && st.isDestroyed;

    public void MarkDestroyed(ulong id)
    {
        if (!_stateDiffs.TryGetValue(id, out var st)) st = new ObjectState();
        st.isDestroyed = true;
        _stateDiffs[id] = st;
    }

    /// <summary>  (BerryBush -> Bush).  true,   .</summary>
    public bool TryHarvest(ulong id, System.Random rng = null)
    {
        ulong chunkKey = id >> 32;
        int index = (int)(id & 0xffffffff);

        if (!_chunkData.TryGetValue(chunkKey, out var list)) return false;
        if (index < 0 || index >= list.Count) return false;

        var inst = list[index];
        if (!_objByType.TryGetValue(inst.type, out var data) || !data.harvest.harvestable) return false;

        //   ( )
        rng ??= new System.Random(unchecked((int)id));
        if (data.harvest.harvestDrops != null)
        {
            foreach (var drop in data.harvest.harvestDrops)
            {
                if (UnityEngine.Random.value <= drop.chance)
                {
                    int n = UnityEngine.Random.Range(drop.minCount, drop.maxCount + 1);
                    // Inventory.Give(drop.itemId, n);
                }
            }
        }

        //    
        if (data.harvest.destroyOnHarvest)
        {
            MarkDestroyed(id);
            if (_chunkVisual.TryGetValue(chunkKey, out var gos) && index < gos.Count && gos[index] != null)
            {
                if (objectPool) objectPool.Release(gos[index]); else Destroy(gos[index]);
                OnVisualObjectDestroyed?.Invoke(id, gos[index]);
            }
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

    private List<ObjectInstanceData> GetOrGenerateChunk(ChunkCoord cc)
    {
        var key = ChunkKey(cc);
        if (_chunkData.TryGetValue(key, out var list)) return list;

        // :   ,    
        if (!BiomesReady) return new List<ObjectInstanceData>();

        list = GenerateChunk(cc);
        _chunkData[key] = list;
        return list;
    }

    /// <summary>
    ///  (  )      .
    ///  _chunkData    ,   .
    /// </summary>
    public void RemoveObjectsInCircle(Vector2 worldCenter, float worldRadius)
    {
        float r2 = worldRadius * worldRadius;

        var min = new Vector2(worldCenter.x - worldRadius, worldCenter.y - worldRadius);
        var max = new Vector2(worldCenter.x + worldRadius, worldCenter.y + worldRadius);
        var minCell = new Vector2Int(Mathf.FloorToInt(min.x / cellSize), Mathf.FloorToInt(min.y / cellSize));
        var maxCell = new Vector2Int(Mathf.FloorToInt(max.x / cellSize), Mathf.FloorToInt(max.y / cellSize));

        var minChunk = new ChunkCoord(Mathf.FloorToInt(minCell.x / (float)chunkSize), Mathf.FloorToInt(minCell.y / (float)chunkSize));
        var maxChunk = new ChunkCoord(Mathf.FloorToInt(maxCell.x / (float)chunkSize), Mathf.FloorToInt(maxCell.y / (float)chunkSize));

        for (int cy = minChunk.y; cy <= maxChunk.y; cy++)
            for (int cx = minChunk.x; cx <= maxChunk.x; cx++)
            {
                var cc = new ChunkCoord(cx, cy);
                var key = ChunkKey(cc);
                if (!_chunkData.TryGetValue(key, out var list)) continue;

                for (int i = 0; i < list.Count; i++)
                {
                    var inst = list[i];
                    if (IsDestroyed(inst.id)) continue;

                    var d2 = (inst.worldPos - worldCenter).sqrMagnitude;
                    if (d2 <= r2)
                    {
                        MarkDestroyed(inst.id);
                        // снять визуал, если он загружен
                        if (_chunkVisual.TryGetValue(key, out var gos) && i < gos.Count && gos[i] != null)
                        {
                            if (objectPool) objectPool.Release(gos[i]); else Destroy(gos[i]);
                            OnVisualObjectDestroyed?.Invoke(inst.id, gos[i]);
                        }
                    }
                }

                SaveChunkDiffs(key);
            }
    }

    private void ApplySavedDiffs(ulong key, List<ObjectInstanceData> list)
    {
        var path = System.IO.Path.Combine(Application.persistentDataPath, saveFolder, $"{(int)(key >> 32)}_{(int)(key & 0xffffffff)}.json");
        if (!System.IO.File.Exists(path)) return;

        var json = System.IO.File.ReadAllText(path);
        var save = JsonUtility.FromJson<ChunkSave>(json);
        if (save?.deltas == null) return;

        foreach (var d in save.deltas)
        {
            if (d.i < 0 || d.i >= list.Count) continue;
            var id = ((ulong)(uint)d.i) | (key << 32);

            if (d.d) MarkDestroyed(id);
            if (d.t >= 0 && d.t < (int)ObjectType.__COUNT)
            {
                // при необходимости — заменить тип
                // var inst = list[d.i]; inst.type = (ObjectType)d.t; list[d.i] = inst;
            }
        }
    }

    private void SaveChunkDiffs(ulong key)
    {
        if (!_chunkData.TryGetValue(key, out var list)) return;
        if (!_origTypes.TryGetValue(key, out var orig)) return;

        var deltas = new List<ChunkDelta>(list.Count);
        for (int i = 0; i < list.Count; i++)
        {
            var id = ((ulong)(uint)i) | (key << 32);

            bool changed = false;
            var delta = new ChunkDelta { i = i };

            if (_stateDiffs.TryGetValue(id, out var st) && st.isDestroyed)
            {
                delta.d = true; changed = true;
            }

            // при необходимости — delta.t

            if (changed) deltas.Add(delta);
        }

        var save = new ChunkSave { deltas = deltas };
        var json = JsonUtility.ToJson(save, true);
        var path = System.IO.Path.Combine(Application.persistentDataPath, saveFolder, $"{(int)(key >> 32)}_{(int)(key & 0xffffffff)}.json");
        System.IO.File.WriteAllText(path, json);
    }

    private void SaveAllLoadedChunks()
    {
        foreach (var key in _chunkData.Keys) SaveChunkDiffs(key);
    }

    // ObjectManager.cs ( )
    private bool BiomesReady =>
        _biomes != null && (_biomes as BiomeManager)?.IsBiomesReady == true;

    public List<ObjectInstanceData> GetOrGenerateChunk(ChunkCoord cc) => GetOrGenerateChunk(cc); // kept for compatibility (if referenced)

    // ==================== PERSIST DATA FORMATS ====================

    [Serializable] private class ChunkDelta { public int i; public bool d; public int t = -1; }
    [Serializable] private class ChunkSave { public int v = 1; public List<ChunkDelta> deltas = new(); }

    // ==================== ADHOC (no pool) ====================

    GameObject CreateAdhocGO(ObjectType type)
    {
        if (_adhocRoot == null)
        {
            _adhocRoot = new GameObject("Objects_Adhoc").transform;
            _adhocRoot.SetParent(transform, false);
        }

        var go = new GameObject($"adhoc_{type}");
        go.transform.SetParent(_adhocRoot, false);

        //  
        var sr = go.AddComponent<SpriteRenderer>();

        // ,  ApplyVisual   
        var tag = go.AddComponent<PooledObjectTag>();
        tag.Type = type;
        tag.SR = sr;

        return go;
    }

    // ObjectManager.cs ( )
    private bool BiomesReadyFlag =>
        _biomes != null && (_biomes as BiomeManager)?.IsBiomesReady == true;

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
}
