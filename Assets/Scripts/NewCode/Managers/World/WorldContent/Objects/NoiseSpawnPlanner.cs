using System;
using System.Collections.Generic;
using UnityEngine;
using Game.World.Map.Biome;
using Game.World.Objects;
// static ObjectManager;

namespace Game.World.Objects.Spawning
{
    /// Планировщик по биом-профилям: Uniform / Clustered / BlueNoise.
    /// SRP: только считает данные, ничего не спаунит и не трогает пул/вью.
    public sealed class NoiseSpawnPlanner : MonoBehaviour, IObjectSpawnPlanner
    {
        [Header("Services")]
        [SerializeField] private MonoBehaviour biomeServiceBehaviour;     // IBiomeService
        [SerializeField] private MonoBehaviour ruleProviderBehaviour;     // IObjectSpawnRuleProvider
        [SerializeField] private MonoBehaviour reservationBehaviour;      // IReservationService (опц., временно)

        [Header("Config")]
        [SerializeField] private float cellSize = 1f;                     // для worldPos
        [SerializeField] private List<ObjectData> objects = new();        // словарь типов→данные

        private IBiomeService _biomes;
        private IObjectSpawnRuleProvider _rules;
        private IReservationService _reservation; // пока оставим фильтр, вынесем в стример на шаге 3
        private readonly Dictionary<ObjectType, ObjectData> _byType = new();

        void Awake()
        {
            _biomes = biomeServiceBehaviour as IBiomeService
                   ?? FindFirstObjectByType<BiomeManager>(FindObjectsInactive.Exclude);
            _rules = ruleProviderBehaviour as IObjectSpawnRuleProvider
                   ?? FindFirstObjectByType<BiomeSpawnProfileProvider>(FindObjectsInactive.Exclude);
            _reservation = reservationBehaviour as IReservationService
                   ?? FindFirstObjectByType<ReservationService>(FindObjectsInactive.Exclude);

            _byType.Clear();
            foreach (var od in objects) if (od) _byType[od.type] = od;
        }

        public List<ObjectInstanceData> PlanChunk(Vector2Int originCell, int chunkSize, int worldSeed, ulong chunkKey)
        {
            var result = new List<ObjectInstanceData>(128);
            var occupied = new HashSet<Vector2Int>();
            if (_biomes == null || _rules == null || _byType.Count == 0) return result;

            foreach (BiomeType biome in Enum.GetValues(typeof(BiomeType)))
            {
                var profile = _rules.GetProfile(biome);
                if (profile == null || profile.rules == null || profile.rules.Count == 0) continue;

                float densMul = Mathf.Max(0.01f, profile.densityMultiplier);

                foreach (var rule in profile.rules)
                {
                    if (rule == null || rule.objectType == ObjectType.None) continue;

                    // твоё поле: targetPerChunk
                    int target = Mathf.RoundToInt(rule.targetPerChunk * densMul);
                    if (target <= 0) continue;

                    var rng = new System.Random(Hash(worldSeed, originCell.x, originCell.y, (int)rule.objectType));

                    switch (rule.mode)
                    {
                        case SpawnMode.BlueNoise:
                            PlaceBlueNoise(biome, rule, originCell, chunkSize, target, rng, occupied, result, chunkKey);
                            break;
                        case SpawnMode.Clustered:
                            PlaceClustered(biome, rule, originCell, chunkSize, target, rng, occupied, result, chunkKey);
                            break;
                        default: // Uniform
                            PlaceUniform(biome, rule, originCell, chunkSize, target, rng, occupied, result, chunkKey);
                            break;
                    }
                }
            }

            return result;
        }

        // ===== helpers =====

        private static int Hash(int seed, int x, int y, int salt)
        {
            unchecked
            {
                int h = seed;
                h = (h * 397) ^ x;
                h = (h * 397) ^ y;
                h = (h * 397) ^ salt;
                return h;
            }
        }

        private Vector2 CellToWorld(Vector2Int cell) => new(cell.x * cellSize, cell.y * cellSize);

        private bool TryPickCell(BiomeType biome, BiomeObjectRule rule, Vector2Int origin, int chunkSize,
                                 System.Random rng, HashSet<Vector2Int> occ, out Vector2Int cell)
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
            // 0) Резервации (как в ObjectManager)
            if (_reservation != null && _reservation.IsReserved(cell, ReservationMask.All))
                return false;

            // 1) Биом клетки
            if (_biomes == null || _biomes.GetBiomeAtPosition(cell) != neededBiome)
                return false;

            // 2) Perlin-gate
            if (noiseGate && rule.useNoiseGate)
            {
                float n = Mathf.PerlinNoise(cell.x * rule.noiseScale, cell.y * rule.noiseScale);
                if (n < rule.noiseThreshold) return false;
            }

            // 3) Футпринт и занятость
            if (!_byType.TryGetValue(rule.objectType, out var od)) return false;
            for (int x = 0; x < od.footprint.x; x++)
                for (int y = 0; y < od.footprint.y; y++)
                    if (occ.Contains(new Vector2Int(cell.x + x, cell.y + y))) return false;

            // 4) Избегать других объектов — твои поля
            if (rule.avoidOtherObjects)
            {
                int r = Mathf.CeilToInt(Mathf.Max(rule.minDistanceAny, rule.minDistanceSameType));
                for (int dx = -r; dx <= r; dx++)
                    for (int dy = -r; dy <= r; dy++)
                        if (occ.Contains(new Vector2Int(cell.x + dx, cell.y + dy))) return false;
            }

            // 5) Частное правило (пример с пальмами)
            if (rule.objectType == ObjectType.Palm && neededBiome == BiomeType.Desert)
                if (!IsNearBiome(cell, BiomeType.Savanna, 3)) return false;

            return true;
        }

        private bool IsNearBiome(Vector2Int cell, BiomeType target, int radius)
        {
            if (_biomes == null) return false;
            for (int dx = -radius; dx <= radius; dx++)
                for (int dy = -radius; dy <= radius; dy++)
                    if (_biomes.GetBiomeAtPosition(new Vector2Int(cell.x + dx, cell.y + dy)) == target)
                        return true;
            return false;
        }

        private void Occupy(Vector2Int footprint, Vector2Int cell, HashSet<Vector2Int> occ)
        {
            for (int x = 0; x < footprint.x; x++)
                for (int y = 0; y < footprint.y; y++)
                    occ.Add(new Vector2Int(cell.x + x, cell.y + y));
        }

        private ObjectInstanceData MakeInstance(BiomeObjectRule rule, Vector2Int cell, System.Random rng, ulong chunkKey, int idx)
        {
            var data = _byType[rule.objectType];
            int maxV = (data.spriteVariants != null && data.spriteVariants.Length > 0) ? data.spriteVariants.Length : 1;
            int vIdx = (maxV > 1) ? rng.Next(0, maxV) : 0;

            return new ObjectInstanceData
            {
                id = ((ulong)(uint)idx) | (chunkKey << 32),
                type = rule.objectType,
                cell = cell,
                variantIndex = vIdx,
                worldPos = new Vector2(cell.x * cellSize, cell.y * cellSize),
                footprint = data.footprint
            };
        }

        private void PlaceUniform(BiomeType biome, BiomeObjectRule rule, Vector2Int origin, int chunkSize, int target,
                                  System.Random rng, HashSet<Vector2Int> occ, List<ObjectInstanceData> outList, ulong chunkKey)
        {
            int placed = 0;
            while (placed < target && outList.Count < target * 2) // страховка от бесц. цикла
            {
                if (!TryPickCell(biome, rule, origin, chunkSize, rng, occ, out var cell)) break;
                var inst = MakeInstance(rule, cell, rng, chunkKey, outList.Count);
                Occupy(_byType[rule.objectType].footprint, cell, occ);
                outList.Add(inst); placed++;
            }
        }

        private void PlaceClustered(BiomeType biome, BiomeObjectRule rule, Vector2Int origin, int chunkSize, int target,
                                    System.Random rng, HashSet<Vector2Int> occ, List<ObjectInstanceData> outList, ulong chunkKey)
        {
            int avgSat = Mathf.Max(1, (rule.clusterCountRange.x + rule.clusterCountRange.y) / 2);
            int clusters = Mathf.Max(1, target / Mathf.Max(1, avgSat));

            for (int c = 0; c < clusters && outList.Count < target; c++)
            {
                if (!TryPickCell(biome, rule, origin, chunkSize, rng, occ, out var seed)) continue;

                var inst = MakeInstance(rule, seed, rng, chunkKey, outList.Count);
                Occupy(_byType[rule.objectType].footprint, seed, occ);
                outList.Add(inst);

                int satellites = rng.Next(rule.clusterCountRange.x, rule.clusterCountRange.y + 1);
                for (int s = 0; s < satellites && outList.Count < target; s++)
                {
                    var near = seed + new Vector2Int(rng.Next(-3, 4), rng.Next(-3, 4));
                    if (!IsAllowed(biome, rule, near, occ, noiseGate: false)) continue;

                    var sat = MakeInstance(rule, near, rng, chunkKey, outList.Count);
                    Occupy(_byType[rule.objectType].footprint, near, occ);
                    outList.Add(sat);
                }
            }
        }

        private void PlaceBlueNoise(BiomeType biome, BiomeObjectRule rule, Vector2Int origin, int chunkSize, int target,
                                    System.Random rng, HashSet<Vector2Int> occ, List<ObjectInstanceData> outList, ulong chunkKey)
        {
            int step = Mathf.Max(1, Mathf.RoundToInt(rule.minDistanceSameType));
            for (int x = 0; x < chunkSize; x += step)
                for (int y = 0; y < chunkSize; y += step)
                {
                    var cell = origin + new Vector2Int(x, y);
                    if (!IsAllowed(biome, rule, cell, occ)) continue;

                    var inst = MakeInstance(rule, cell, rng, chunkKey, outList.Count);
                    Occupy(_byType[rule.objectType].footprint, cell, occ);
                    outList.Add(inst);
                    if (outList.Count >= target) return;
                }
        }
    }
}
