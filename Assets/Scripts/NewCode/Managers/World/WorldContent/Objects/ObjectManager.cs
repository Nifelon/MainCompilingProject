using System.Collections.Generic;
using UnityEngine;
using Game.World.Objects.Spawning;      // IObjectSpawnPlanner, IObjectView
using Game.World.Services;             // IVisibilityService (опционально, если нужно)
using Game.World.Map.Biome;            // только если где-то логируешь биомы

namespace Game.World.Objects
{
    /// <summary>
    /// Итоговый фасад: без правил, без планирования, без логики стриминга.
    /// - План за чанк -> IObjectSpawnPlanner
    /// - Визуал/пул -> IObjectView (адаптер над PoolManagerObjects)
    /// - Стриминг (circle–rect, гистерезис) живёт в стримере и дергает этот фасад.
    /// </summary>
    public sealed class ObjectManager : MonoBehaviour
    {
        // ----------------- Public API (сохранён для совместимости) -----------------

        /// <summary> Координаты чанка в клетках (индексы по сетке чанков). </summary>
        [System.Serializable]
        public struct ChunkCoord
        {
            public int x;
            public int y;
            public ChunkCoord(int x, int y) { this.x = x; this.y = y; }
            public override string ToString() => $"({x},{y})";
        }

        // ----------------- Config / Units -----------------

        [Header("Config / Units")]
        [Tooltip("Размер чанка в клетках.")]
        [SerializeField] private int chunkSize = 32;

        [Tooltip("Размер клетки в мировых единицах.")]
        [SerializeField] private float cellSize = 1f;

        [Tooltip("Текущий seed мира (для детерминированного планирования).")]
        [SerializeField] private int worldSeed = 12345;

        [SerializeField] private bool verbose = false;

        // ----------------- Services -----------------

        [Header("Services")]
        [Tooltip("Планировщик данных инстансов на чанк (SRP: считает только данные).")]
        [SerializeField] private MonoBehaviour plannerBehaviour;     // IObjectSpawnPlanner
        private IObjectSpawnPlanner _planner;

        [Tooltip("Адаптер вью/пула (SRP: создаёт/возвращает в пул).")]
        [SerializeField] private MonoBehaviour viewBehaviour;        // IObjectView
        private IObjectView _view;

        [Tooltip("Опционально: сервис видимости, если нужно логировать/проверять дистанции.")]
        [SerializeField] private MonoBehaviour visibilityBehaviour;  // IVisibilityService
        private IVisibilityService _visibility;

        // ----------------- Runtime State -----------------

        /// <summary> Кэш: план инстансов по ключу чанка. </summary>
        private readonly Dictionary<ulong, List<ObjectInstanceData>> _chunkPlans = new();

        /// <summary> Активный визуал по ключу чанка. </summary>
        private readonly Dictionary<ulong, List<ObjectHandle>> _spawnedByChunk = new();

        private static readonly List<ObjectHandle> _tmpList = new(64);

        // ----------------- Unity lifecycle -----------------

        private void Awake()
        {
            _planner = plannerBehaviour as IObjectSpawnPlanner
                       ?? FindFirstObjectByType<NoiseSpawnPlanner>(FindObjectsInactive.Exclude);

            _view = viewBehaviour as IObjectView
                    ?? FindFirstObjectByType<ObjectViewPoolAdapter>(FindObjectsInactive.Exclude);

            _visibility = visibilityBehaviour as IVisibilityService
                          ?? FindFirstObjectByType<VisibilityService>(FindObjectsInactive.Exclude);

            if (_planner == null) Debug.LogError("[ObjectManager] Planner is not set.");
            if (_view == null) Debug.LogError("[ObjectManager] View adapter is not set.");

            // Подпишемся на реген мира — полная очистка
            WorldSignals.OnWorldRegen += HandleWorldRegen;
        }

        private void OnDestroy()
        {
            WorldSignals.OnWorldRegen -= HandleWorldRegen;
        }

        // ----------------- Public methods (дергаются стримером/пулом) -----------------

        /// <summary> Задать seed (например, из WorldGenLab при смене сида).</summary>
        public void SetSeed(int seed) => worldSeed = seed;

        /// <summary> Загрузить визуал чанка (создать инстансы из планов и заспаунить через пул).</summary>
        public void LoadChunkVisuals(ChunkCoord cc)
        {
            var key = ChunkKey(cc);
            var plan = GetOrGenerateChunk(cc);

            if (plan == null || plan.Count == 0) return;
            if (!_spawnedByChunk.TryGetValue(key, out var list))
            {
                list = new List<ObjectHandle>(plan.Count);
                _spawnedByChunk[key] = list;
            }

            for (int i = 0; i < plan.Count; i++)
            {
                var handle = _view.Spawn(plan[i]);
                if (handle.IsValid)
                    list.Add(handle);
            }

            if (verbose)
                Debug.Log($"[ObjectManager] LoadChunkVisuals {cc} → spawned={list.Count}");
        }

        /// <summary> Выгрузить визуал чанка (вернуть в пул/удалить).</summary>
        public void UnloadChunkVisuals(ChunkCoord cc)
        {
            var key = ChunkKey(cc);
            if (!_spawnedByChunk.TryGetValue(key, out var list)) return;

            for (int i = 0; i < list.Count; i++)
                _view.Despawn(list[i]);

            list.Clear();
            _spawnedByChunk.Remove(key);

            if (verbose)
                Debug.Log($"[ObjectManager] UnloadChunkVisuals {cc}");
        }

        /// <summary> Снять все объекты в круге (например, перед спавном лагеря). Радиус — в мировых ед.</summary>
        public int RemoveObjectsInCircle(Vector2 worldCenter, float worldRadius)
        {
            float r2 = worldRadius * worldRadius;
            int removed = 0;

            foreach (var kv in _spawnedByChunk)
            {
                _tmpList.Clear();
                var list = kv.Value;

                for (int i = 0; i < list.Count; i++)
                {
                    // у IObjectView должен быть метод получения Bounds/позиции хэндла
                    Bounds b = _view.GetWorldBounds(list[i]);
                    Vector2 pos = b.center;
                    if ((pos - worldCenter).sqrMagnitude <= r2)
                        _tmpList.Add(list[i]);
                }

                for (int i = 0; i < _tmpList.Count; i++)
                {
                    _view.Despawn(_tmpList[i]);
                    list.Remove(_tmpList[i]);
                    removed++;
                }
            }

            if (verbose && removed > 0)
                Debug.Log($"[ObjectManager] RemoveObjectsInCircle center={worldCenter} r={worldRadius} → {removed}");

            return removed;
        }

        /// <summary> Полная очистка мира (на реген, смену сида, выгрузку сцены).</summary>
        public void DespawnAll()
        {
            foreach (var kv in _spawnedByChunk)
            {
                var list = kv.Value;
                for (int i = 0; i < list.Count; i++)
                    _view.Despawn(list[i]);
            }
            _spawnedByChunk.Clear();
            _chunkPlans.Clear();

            if (verbose)
                Debug.Log("[ObjectManager] DespawnAll + Clear plans");
        }

        // ----------------- Planning cache -----------------

        /// <summary>
        /// Вернуть кэшированный план инстансов по чанку (или сгенерировать через планировщик).
        /// Данные — только данные (без GameObject).
        /// </summary>
        public List<ObjectInstanceData> GetOrGenerateChunk(ChunkCoord cc)
        {
            var key = ChunkKey(cc);
            if (_chunkPlans.TryGetValue(key, out var list)) return list;

            if (_planner == null) return EmptyPlan();

            var origin = ChunkOrigin(cc);
            list = _planner.PlanChunk(origin, chunkSize, worldSeed, key);
            _chunkPlans[key] = list ?? EmptyPlan();

            return _chunkPlans[key];
        }

        // ----------------- Helpers -----------------

        private static List<ObjectInstanceData> EmptyPlan() => new();

        /// <summary> Левый-нижний угол чанка в клетках. </summary>
        public Vector2Int ChunkOrigin(ChunkCoord cc) => new(cc.x * chunkSize, cc.y * chunkSize);

        /// <summary> Ключ чанка: (uint)x | ((ulong)(uint)y << 32). </summary>
        public static ulong ChunkKey(ChunkCoord cc)
        {
            unchecked { return (uint)cc.x | ((ulong)(uint)cc.y << 32); }
        }

        /// <summary> Прямоугольник чанка в мировых координатах (полезно для отладки/кликов).</summary>
        public Rect GetChunkWorldRect(ChunkCoord cc)
        {
            var o = ChunkOrigin(cc);
            return new Rect(o.x * cellSize, o.y * cellSize, chunkSize * cellSize, chunkSize * cellSize);
        }

        private void HandleWorldRegen()
        {
            DespawnAll();
            // worldSeed должен быть обновлён раньше через SetSeed(seed)
        }
    }
}
