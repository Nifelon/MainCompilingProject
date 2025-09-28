// Assets/Game/World/Objects/ObjectManager.cs
using System.Collections.Generic;
using UnityEngine;
using Game.World.Objects.Spawning;        // IObjectSpawnPlanner
//using Game.World.Content.Services;       // IReservationService

namespace Game.World.Objects
{
    /// <summary>
    /// Итоговый фасад объектов мира (SRP):
    /// - хранит кэш планов инстансов по чанкам (данные, без GameObject);
    /// - спавнит/деспавнит визуал через IObjectView (обёртка над пулом);
    /// - ведёт индекс активных объектов по чанкам для корректного despawn;
    /// - отфильтровывает зарезервированные клетки при спавне.
    /// ВНИМАНИЕ: стриминг (когда грузить/выгружать чанк) — СНАРУЖИ (ObjectChunkStreamer).
    /// Правила/планирование — СНАРУЖИ (BiomeSpawnProfileProvider + NoiseSpawnPlanner).
    /// </summary>
    public sealed class ObjectManager : MonoBehaviour
    {
        // ------------ Публичные типы/сигнатуры (совместимость со старым кодом) ------------

        /// <summary> Координаты чанка (в индексах чанков, не в клетках). </summary>
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
        [Tooltip("Размер чанка объектов в КЛЕТКАХ.")]
        [SerializeField] private int chunkSize = 64;

        [Tooltip("Размер клетки в мировых единицах.")]
        [SerializeField] private float cellSize = 1f;

        [Tooltip("Seed мира (детерминирует планирование объектов).")]
        [SerializeField] private int worldSeed = 12345;

        [SerializeField] private bool verbose = false;

        /// <summary>Размер чанка в клетках (для внешних систем).</summary>
        public int ChunkSize => chunkSize;
        /// <summary>Размер клетки в world units.</summary>
        public float CellSize => cellSize;
        /// <summary>Текущий seed мира.</summary>
        public int WorldSeed => worldSeed;

        // ----------------- Services -----------------

        [Header("Services")]
        [Tooltip("Планировщик данных инстансов на чанк.")]
        [SerializeField] private MonoBehaviour plannerBehaviour;   // IObjectSpawnPlanner
        private IObjectSpawnPlanner _planner;

        [Tooltip("Вью-адаптер (обёртка над PoolManagerObjects).")]
        [SerializeField] private MonoBehaviour viewBehaviour;      // IObjectView
        private IObjectView _view;

        [Tooltip("Сервис резерваций клеток (лагеря и т.п.).")]
        [SerializeField] private MonoBehaviour reservationBehaviour; // IReservationService
        private IReservationService _reservation;

        // ----------------- Runtime state -----------------

        /// <summary>Кэш планов инстансов по чанку.</summary>
        private readonly Dictionary<ulong, List<ObjectInstanceData>> _chunkPlans = new();

        /// <summary>Индекс активных (заспавненных) объектов по чанку.</summary>
        private readonly ObjectRuntimeIndex _index = new();

        private static readonly List<ObjectHandle> _tmpHandles = new(64);

        // ----------------- Unity lifecycle -----------------

        private void Awake()
        {
            _planner = plannerBehaviour as IObjectSpawnPlanner
                       ?? FindFirstObjectByType<NoiseSpawnPlanner>(FindObjectsInactive.Exclude);

            _view = viewBehaviour as IObjectView
                    ?? FindFirstObjectByType<ObjectViewPoolAdapter>(FindObjectsInactive.Exclude);

            _reservation = reservationBehaviour as IReservationService
                           ?? FindFirstObjectByType<ReservationService>(FindObjectsInactive.Exclude);

            if (_planner == null) Debug.LogError("[ObjectManager] Planner is not set.");
            if (_view == null) Debug.LogError("[ObjectManager] View adapter is not set.");
            if (_reservation == null) Debug.LogWarning("[ObjectManager] ReservationService is not set (allowed, but camps won't block objects).");
        }

        // ----------------- Public API (дергает стример) -----------------

        /// <summary>Установить seed (например, при регене мира).</summary>
        public void SetSeed(int seed) => worldSeed = seed;

        /// <summary>Обновить единицы измерения (чтобы совпадало с тайлами).</summary>
        public void SetUnits(int chunkSizeCells, float cellSizeWorld)
        {
            chunkSize = chunkSizeCells;
            cellSize = cellSizeWorld;
        }

        /// <summary>
        /// Загрузить визуальную часть чанка: пройтись по плану и заспавнить инстансы через пул,
        /// отфильтровав зарезервированные клетки.
        /// </summary>
        public void LoadChunkVisuals(ChunkCoord cc)
        {
            var key = ChunkKey(cc);
            var plan = GetOrGenerateChunk(cc);
            if (plan == null || plan.Count == 0) return;

            int spawned = 0;

            for (int i = 0; i < plan.Count; i++)
            {
                var inst = plan[i];

                // Фильтр резерваций (лагеря, запретные зоны)
                if (_reservation != null && _reservation.IsReserved(inst.cell, ReservationMask.All))
                    continue;

                var handle = _view.Spawn(inst);
                if (handle.IsValid)
                {
                    _index.Add(key, handle);
                    spawned++;
                }
            }

            if (verbose)
                Debug.Log($"[ObjectManager] LoadChunkVisuals {cc} → spawned={spawned}");
        }

        /// <summary>
        /// Выгрузить визуальную часть чанка: взять все активные объекты из индекса и вернуть в пул.
        /// </summary>
        public void UnloadChunkVisuals(ChunkCoord cc)
        {
            var key = ChunkKey(cc);
            int removed = 0;

            foreach (var h in _index.RemoveAllInChunk(key))
            {
                _view.Despawn(h);
                removed++;
            }

            if (verbose)
                Debug.Log($"[ObjectManager] UnloadChunkVisuals {cc} → despawned={removed}");
        }

        /// <summary>
        /// Удалить (despawn) все объекты в круге. Радиус в world units.
        /// Полезно, например, перед спавном лагеря.
        /// </summary>
        public int RemoveObjectsInCircle(Vector2 worldCenter, float worldRadius)
        {
            float r2 = worldRadius * worldRadius;
            int removed = 0;

            // Проходим по всем чанкам, для которых у нас есть планы (используем как список ключей).
            foreach (var kv in _chunkPlans)
            {
                var key = kv.Key;
                var dict = _index.GetChunk(key);
                if (dict == null || dict.Count == 0) continue;

                _tmpHandles.Clear();

                foreach (var pair in dict)
                {
                    var h = pair.Value;
                    var b = _view.GetWorldBounds(h);
                    var pos = (Vector2)b.center;
                    if ((pos - worldCenter).sqrMagnitude <= r2)
                        _tmpHandles.Add(h);
                }

                for (int i = 0; i < _tmpHandles.Count; i++)
                {
                    var h = _tmpHandles[i];
                    _view.Despawn(h);
                    _index.Remove(key, h.Id, out _);
                    removed++;
                }
            }

            if (verbose && removed > 0)
                Debug.Log($"[ObjectManager] RemoveObjectsInCircle center={worldCenter} r={worldRadius} → {removed}");

            return removed;
        }

        /// <summary>
        /// Полная очистка: despawn всех активных, очистка индекса и кэша планов.
        /// Вызывай при регене мира/смене сида/выгрузке сцены.
        /// </summary>
        public void DespawnAll()
        {
            // Выгрузка всех активных
            // (Если нужно быстрее — можно добавить перечислитель всех хэндлов в ObjectRuntimeIndex)
            foreach (var kv in _chunkPlans)
            {
                var key = kv.Key;
                var dict = _index.GetChunk(key);
                if (dict == null) continue;

                foreach (var h in dict.Values)
                    _view.Despawn(h);
            }

            _index.Clear();
            _chunkPlans.Clear();

            if (verbose)
                Debug.Log("[ObjectManager] DespawnAll → cleared index & plans");
        }

        // ----------------- Planning cache -----------------

        /// <summary>
        /// Вернуть план инстансов по чанку (из кэша или сгенерировать через планировщик).
        /// Возвращает данные (без GameObject).
        /// </summary>
        public List<ObjectInstanceData> GetOrGenerateChunk(ChunkCoord cc)
        {
            var key = ChunkKey(cc);
            if (_chunkPlans.TryGetValue(key, out var list)) return list;

            if (_planner == null)
            {
                if (verbose) Debug.LogWarning("[ObjectManager] Planner is null, returning empty plan");
                list = new List<ObjectInstanceData>(0);
            }
            else
            {
                var origin = ChunkOrigin(cc);
                list = _planner.PlanChunk(origin, chunkSize, worldSeed, key) ?? new List<ObjectInstanceData>(0);
            }

            _chunkPlans[key] = list;
            return list;
        }

        // ----------------- Helpers -----------------

        /// <summary> Левый-нижний угол чанка в клетках. </summary>
        public Vector2Int ChunkOrigin(ChunkCoord cc) => new(cc.x * chunkSize, cc.y * chunkSize);

        /// <summary> Прямоугольник чанка в world units (удобно для отладки/клика). </summary>
        public Rect GetChunkWorldRect(ChunkCoord cc)
        {
            var o = ChunkOrigin(cc);
            return new Rect(o.x * cellSize, o.y * cellSize, chunkSize * cellSize, chunkSize * cellSize);
        }

        /// <summary>
        /// Ключ чанка: X в старших 32 битах, Y в младших (совместим с ObjectChunkStreamer).
        /// </summary>
        public static ulong ChunkKey(ChunkCoord cc)
            => ((ulong)(uint)cc.x << 32) | (uint)cc.y;

        /// <summary> Обратно: ключ в координаты (если потребуется). </summary>
        public static ChunkCoord KeyToChunk(ulong key)
            => new((int)(key >> 32), (int)(key & 0xffffffff));
    }
}
