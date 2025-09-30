using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Game.World.Map.Biome;
using Game.World.Objects;

namespace Game.World.Objects.Spawning
{
    /// Планировщик по биом-профилям: Uniform / Clustered / BlueNoise.
    /// SRP: только считает данные, ничего не спаунит и не трогает пул/вью.
    public sealed class NoiseSpawnPlanner : MonoBehaviour, IObjectSpawnPlanner
    {
        [Header("Services")]
        [SerializeField] private MonoBehaviour biomeServiceBehaviour;     // IBiomeService
        [SerializeField] private MonoBehaviour ruleProviderBehaviour;     // IObjectSpawnRuleProvider
        [SerializeField] private MonoBehaviour reservationBehaviour;      // IReservationService (опционально)

        [Header("Config")]
        [SerializeField] private float cellSize = 1f;                     // для worldPos
        [SerializeField] private List<ObjectData> objects = new();        // словарь типов→данные

        private IBiomeService _biomes;
        private IObjectSpawnRuleProvider _rules;
        private IReservationService _reservation;
        private readonly Dictionary<ObjectType, ObjectData> _byType = new();

        void Awake()
        {
            _biomes = biomeServiceBehaviour as IBiomeService
                   ?? FindFirstObjectByType<BiomeManager>(FindObjectsInactive.Exclude);
            _rules = ruleProviderBehaviour as IObjectSpawnRuleProvider
                   ?? FindFirstObjectByType<BiomeSpawnProfileProviderBehaviour>(FindObjectsInactive.Exclude) as IObjectSpawnRuleProvider;
            _reservation = reservationBehaviour as IReservationService
                   ?? FindFirstObjectByType<ReservationService>(FindObjectsInactive.Exclude);

            _byType.Clear();
            foreach (var od in objects) if (od) _byType[od.type] = od;
        }

        public List<ObjectInstanceData> PlanChunk(Vector2Int originCell, int chunkSize, int worldSeed, ulong chunkKey)
        {
            var result = new List<ObjectInstanceData>(128);
            var occupied = new HashSet<Vector2Int>();

            if (_biomes == null || _rules == null || _byType.Count == 0)
                return result;

            // 0) Собираем клетки чанка по реальным биомам
            int totalCells = chunkSize * chunkSize;
            var cellsByBiome = new Dictionary<BiomeType, List<Vector2Int>>(4);

            for (int dy = 0; dy < chunkSize; dy++)
                for (int dx = 0; dx < chunkSize; dx++)
                {
                    var cell = new Vector2Int(originCell.x + dx, originCell.y + dy);
                    var biome = _biomes.GetBiomeAtPosition(cell);
                    if (!cellsByBiome.TryGetValue(biome, out var list))
                        cellsByBiome[biome] = list = new List<Vector2Int>(totalCells / 2);
                    list.Add(cell);
                }

            // 1) Для КАЖДОГО биома в чанке — отдельное планирование по его клеткам
            foreach (var kv in cellsByBiome)
            {
                var biome = kv.Key;
                var biomeCells = kv.Value;
                if (biomeCells == null || biomeCells.Count == 0) continue;

                // Профиль биома с fallback через провайдер
                if (!_rules.TryGetProfile(biome, out var profile) || profile == null || profile.rules == null || profile.rules.Count == 0)
                    continue;

                float densityMul = Mathf.Max(0.1f, profile.densityMultiplier);
                float areaMul = (chunkSize * chunkSize) / (64f * 64f); // масштаб от «базового 64×64»
                float areaFrac = biomeCells.Count / (float)totalCells;  // доля площади этого биома в чанке

                // Индивидуальный RNG на биом (чтобы разные биомы в одном чанке не «конфликтовали»)
                // Соль: seed + origin + biome
                int biomeSalt = (int)biome * 97; // простая соль
                var baseRng = new System.Random(Hash(worldSeed, originCell.x, originCell.y, biomeSalt));

                foreach (var rule in profile.rules)
                {
                    if (rule == null || rule.objectType == ObjectType.None) continue;
                    if (!_byType.TryGetValue(rule.objectType, out var od)) continue;

                    // Целевое количество с учётом плотности профиля, масштаба чанка и доли биома.
                    int target = Mathf.RoundToInt(Mathf.Max(0, rule.targetPerChunk) * densityMul * areaMul * areaFrac);
                    // Если в профиле включена гарантия минимума — не даём округлиться в 0, когда биома мало
                   // if (rule.ensureMin && areaFrac > 0f) target = Mathf.Max(target, 1);
                    if (target <= 0) continue;

                    // Отдельный RNG под правило, чтобы варианты были устойчивыми к порядку
                    var rng = new System.Random(Hash(baseRng.Next(), (int)rule.objectType, target, biomeSalt));

                    // Планируем ТОЛЬКО по клеткам этого биома
                    switch (rule.mode)
                    {
                        case SpawnMode.BlueNoise:
                            PlaceBlueNoiseForCells(biome, rule, biomeCells, target, rng, occupied, result, chunkKey);
                            break;
                        case SpawnMode.Clustered:
                            PlaceClusteredForCells(biome, rule, biomeCells, target, rng, occupied, result, chunkKey);
                            break;
                        default:
                            PlaceUniformForCells(biome, rule, biomeCells, target, rng, occupied, result, chunkKey);
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

        private bool TryPickFromList(BiomeType biome, BiomeObjectRule rule, List<Vector2Int> cells,
                                     System.Random rng, HashSet<Vector2Int> occ, out Vector2Int cell)
        {
            // Несколько попыток выбрать допустимую клетку из набора bioma
            for (int i = 0; i < 48; i++)
            {
                var c = cells[rng.Next(0, cells.Count)];
                if (IsAllowed(biome, rule, c, occ)) { cell = c; return true; }
            }
            cell = default; return false;
        }

        private bool IsAllowed(BiomeType neededBiome, BiomeObjectRule rule, Vector2Int cell, HashSet<Vector2Int> occ, bool noiseGate = true)
        {
            // 0) Резервации
            if (_reservation != null && _reservation.IsReserved(cell, ReservationMask.Nature))
                return false;

            // 1) Биом клетки (на всякий случай оставим, хотя уже отобрано по списку)
            if (_biomes == null || _biomes.GetBiomeAtPosition(cell) != neededBiome)
                return false;

            // 2) Noise gate
            if (noiseGate && rule.useNoiseGate)
            {
                float n = Mathf.PerlinNoise(cell.x * rule.noiseScale, cell.y * rule.noiseScale);
                if (n < rule.noiseThreshold) return false;
            }

            // 3) Футпринт и занятость
            if (!_byType.TryGetValue(rule.objectType, out var od)) return false;
            var fp = od.footprint; // Vector2Int: ширина/высота в клетках (или полуоси — используй по своему соглашению)
            for (int x = 0; x < fp.x; x++)
                for (int y = 0; y < fp.y; y++)
                    if (occ.Contains(new Vector2Int(cell.x + x, cell.y + y))) return false;

            // 4) Мин. дистанции до любых/своих — простая «квадратная» проверка по окрестности
            if (rule.avoidOtherObjects)
            {
                int r = Mathf.CeilToInt(Mathf.Max(rule.minDistanceAny, rule.minDistanceSameType));
                for (int dx = -r; dx <= r; dx++)
                    for (int dy = -r; dy <= r; dy++)
                        if (occ.Contains(new Vector2Int(cell.x + dx, cell.y + dy))) return false;
            }

            // 5) Пример частного правила (если нужно)
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
                worldPos = new Vector2((cell.x + 0.5f) * cellSize, (cell.y + 0.5f) * cellSize),
                footprint = data.footprint
            };
        }

        // ===== Режимы размещения для набора клеток одного биома =====

        private void PlaceUniformForCells(BiomeType biome, BiomeObjectRule rule, List<Vector2Int> cells, int target,
                                          System.Random rng, HashSet<Vector2Int> occ, List<ObjectInstanceData> outList, ulong chunkKey)
        {
            int placed = 0;
            int guard = Mathf.Max(64, target * 6); // защита от бесконечного цикла
            while (placed < target && guard-- > 0)
            {
                if (!TryPickFromList(biome, rule, cells, rng, occ, out var cell)) break;
                var inst = MakeInstance(rule, cell, rng, chunkKey, outList.Count);
                Occupy(_byType[rule.objectType].footprint, cell, occ);
                outList.Add(inst);
                placed++;
            }
        }

        private void PlaceClusteredForCells(BiomeType biome, BiomeObjectRule rule, List<Vector2Int> cells, int target,
                                            System.Random rng, HashSet<Vector2Int> occ, List<ObjectInstanceData> outList, ulong chunkKey)
        {
            int avgSat = Mathf.Max(1, (rule.clusterCountRange.x + rule.clusterCountRange.y) / 2);
            int clusters = Mathf.Max(1, target / Mathf.Max(1, avgSat));

            for (int c = 0; c < clusters && outList.Count < target; c++)
            {
                if (!TryPickFromList(biome, rule, cells, rng, occ, out var seed)) continue;

                var inst = MakeInstance(rule, seed, rng, chunkKey, outList.Count);
                Occupy(_byType[rule.objectType].footprint, seed, occ);
                outList.Add(inst);

                int satellites = rng.Next(rule.clusterCountRange.x, rule.clusterCountRange.y + 1);
                for (int s = 0; s < satellites && outList.Count < target; s++)
                {
                    // маленькое случайное смещение вокруг seed
                    var near = seed + new Vector2Int(rng.Next(-3, 4), rng.Next(-3, 4));
                    if (!cells.Contains(near)) continue;                         // строго в пределах биома
                    if (!IsAllowed(biome, rule, near, occ, noiseGate: false)) continue;

                    var sat = MakeInstance(rule, near, rng, chunkKey, outList.Count);
                    Occupy(_byType[rule.objectType].footprint, near, occ);
                    outList.Add(sat);
                }
            }
        }

        private void PlaceBlueNoiseForCells(BiomeType biome, BiomeObjectRule rule, List<Vector2Int> cells, int target,
                                            System.Random rng, HashSet<Vector2Int> occ, List<ObjectInstanceData> outList, ulong chunkKey)
        {
            // Простая дискретная «сетка» по minDistanceSameType, но только по клеткам данного биома
            int step = Mathf.Max(1, Mathf.RoundToInt(rule.minDistanceSameType));
            // Перемешаем порядок клеток, чтобы не было регулярной решётки
            var indices = Enumerable.Range(0, cells.Count).OrderBy(_ => rng.Next()).ToArray();

            for (int ii = 0; ii < indices.Length && outList.Count < target; ii++)
            {
                var cell = cells[indices[ii]];
                // не слишком часто, но «разрежаем» по шагу
                if (((cell.x - cells[0].x) % step) != 0 || ((cell.y - cells[0].y) % step) != 0)
                    continue;

                if (!IsAllowed(biome, rule, cell, occ)) continue;

                var inst = MakeInstance(rule, cell, rng, chunkKey, outList.Count);
                Occupy(_byType[rule.objectType].footprint, cell, occ);
                outList.Add(inst);
            }
        }
    }
}
