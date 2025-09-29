using System.Collections.Generic;
using UnityEngine;
using Game.World.Objects;
using Game.World.Signals;

namespace Game.World.Objects.Streaming
{
    /// <summary>
    /// Стримит чанки объектов вокруг игрока кругом видимости (как тайлы).
    /// Никакой генерации и прямой работы с пулом — только вызовы ObjectManager.
    /// </summary>
    [DefaultExecutionOrder(-185)]
    public sealed class ObjectChunkStreamer : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private Transform player;
        [SerializeField] private ObjectManager objectManager;
        [Tooltip("Источник радиуса/размера клетки тайлов — PoolManager (не PoolManagerMainTile).")]
        [SerializeField] private PoolManager tiles;

        [Header("Units (как у тайлов)")]
        [Tooltip("Размер клетки в world units (будет переопределён значением из PoolManager, если он задан).")]
        [SerializeField] private float cellSize = 1f;
        [Tooltip("Радиус окна в КЛЕТКАХ (будет переопределён значением из PoolManager, если он задан).")]
        [SerializeField] private int tileRadius = 64;

        [Header("Objects (чанки)")]
        [Tooltip("Размер чанка объектов в КЛЕТКАХ (должен совпадать с ObjectManager.ChunkSize).")]
        [SerializeField] private int objectsChunkSize = 64;

        [Header("Hysteresis")]
        [Tooltip("+N клеток к радиусу для загрузки (0 — точное совпадение с тайлами)")]
        [SerializeField] private int loadPaddingCells = 0;
        [Tooltip("+N клеток к радиусу для удержания (0 — точное совпадение с тайлами)")]
        [SerializeField] private int keepPaddingCells = 0;

        [Header("Behavior")]
        [Tooltip("Периодическая подстраховка обновления (сек). 0 — отключить таймер.")]
        [SerializeField] private float refreshInterval = 0.15f;
        [SerializeField] private bool verbose = true;

        // runtime
        private readonly HashSet<ulong> _active = new();
        private Vector2 _lastWorldCenter;
        private float _refreshTimer;
        private bool _firstTick = true;

        void Reset()
        {
            if (!player) player = FindFirstObjectByType<PlayerMeleeController>()?.transform;
            if (!objectManager) objectManager = FindFirstObjectByType<ObjectManager>();
            if (objectManager && objectsChunkSize != objectManager.ChunkSize)
                objectsChunkSize = objectManager.ChunkSize;
        }

        void Awake()
        {
            if (!objectManager) objectManager = FindFirstObjectByType<ObjectManager>();
            if (objectManager && (objectsChunkSize <= 0 || objectsChunkSize != objectManager.ChunkSize))
                objectsChunkSize = objectManager.ChunkSize;

            WorldSignals.OnWorldRegen += OnWorldRegen;
        }

        void Start()
        {
            // Подхватим единицы до первого Refresh, чтобы стартовый чанк подгрузился без движения
            if (tiles != null)
            {
                tileRadius = tiles.RadiusCells;
                cellSize = tiles.CellSizeWorld;
            }
            _firstTick = true; // гарантированно сделаем первый Refresh
        }

        void OnDestroy()
        {
            WorldSignals.OnWorldRegen -= OnWorldRegen;
        }

        void Update()
        {
            if (!player || !objectManager) return;

            // 1) живо синхронизируемся с тайловым менеджером (если он есть)
            if (tiles != null)
            {
                tileRadius = tiles.RadiusCells;
                cellSize = tiles.CellSizeWorld;
            }

            // 2) центр круга в мире
            Vector2 c = player.position;

            // 3) обновлять: первый кадр, смещение ≥ 0.5 клетки, либо по таймеру
            float move2 = (c - _lastWorldCenter).sqrMagnitude;
            float thr2 = (cellSize * 0.5f) * (cellSize * 0.5f);
            _refreshTimer += Time.deltaTime;

            if (_firstTick || move2 >= thr2 || (refreshInterval > 0f && _refreshTimer >= refreshInterval))
            {
                var centerCell = WorldToCell(c);
                var centerChunk = new Vector2Int(
                    DivFloor(centerCell.x, objectsChunkSize),
                    DivFloor(centerCell.y, objectsChunkSize)
                );

                int loaded = Refresh(centerChunk, c);

                // Если ничего не загрузили и всё пусто — форсим центральный чанк
                if (loaded == 0 && _active.Count == 0)
                {
                    var key = ChunkKey(centerChunk);
                    _active.Add(key);
                    objectManager.LoadChunkVisuals(centerChunk.x, centerChunk.y);
                    if (verbose)
                        Debug.LogWarning($"[ObjectChunkStreamer] Forced-load center chunk {centerChunk} (tileRadius={tileRadius}, cellSize={cellSize}, chunkSize={objectsChunkSize}).");
                }

                _lastWorldCenter = c;
                _refreshTimer = 0f;
                _firstTick = false;
            }
        }

        // === core ===
        private int Refresh(Vector2Int centerChunk, Vector2 centerWorld)
        {
            float loadR = (tileRadius + loadPaddingCells) * cellSize; // мир
            float keepR = (tileRadius + keepPaddingCells) * cellSize; // мир

            // Сколько чанков проверять вокруг по УДЕРЖАНИЮ (в мирах + полу-диагональ чанка)
            float chunkWorldSize = objectsChunkSize * cellSize;
            float halfDiag = 0.5f * chunkWorldSize * 1.41421356f; // sqrt(2)
            int R = Mathf.CeilToInt((keepR + halfDiag) / chunkWorldSize);

            var shouldLoad = new HashSet<ulong>();
            var shouldKeep = new HashSet<ulong>();

            for (int dy = -R; dy <= R; dy++)
                for (int dx = -R; dx <= R; dx++)
                {
                    var ch = new Vector2Int(centerChunk.x + dx, centerChunk.y + dy);
                    Rect rect = ChunkRectWorld(ch);     // world units
                    ulong key = ChunkKey(ch);

                    if (CircleIntersectsRect(centerWorld, loadR, rect)) shouldLoad.Add(key);
                    if (CircleIntersectsRect(centerWorld, keepR, rect)) shouldKeep.Add(key);
                }

            int loaded = 0;

            // SPAWN
            foreach (var key in shouldLoad)
            {
                if (_active.Contains(key)) continue;
                _active.Add(key);
                var cc = ToCoord(key);
                objectManager.LoadChunkVisuals(cc.x, cc.y);
                loaded++;
            }

            // DESPAWN
            var toKill = new List<ulong>();
            foreach (var key in _active)
            {
                if (!shouldKeep.Contains(key)) toKill.Add(key);
            }
            for (int i = 0; i < toKill.Count; i++)
            {
                var key = toKill[i];
                var cc = ToCoord(key);
                objectManager.UnloadChunkVisuals(cc.x, cc.y);
                _active.Remove(key);
            }

            if (verbose && (loaded > 0 || toKill.Count > 0))
                Debug.Log($"[ObjectChunkStreamer] loaded={loaded}, despawned={toKill.Count}, active={_active.Count}");

            return loaded;
        }

        private void OnWorldRegen()
        {
            _active.Clear();
            _firstTick = true;
            if (verbose) Debug.Log("[ObjectChunkStreamer] WorldRegen: cleared all active object chunks.");
        }

        // === helpers ===
        private Vector2Int WorldToCell(Vector2 world)
        {
            int x = Mathf.FloorToInt(world.x / cellSize);
            int y = Mathf.FloorToInt(world.y / cellSize);
            return new Vector2Int(x, y);
        }

        private Rect ChunkRectWorld(Vector2Int ch)
        {
            float x = ch.x * objectsChunkSize * cellSize;
            float y = ch.y * objectsChunkSize * cellSize;
            float s = objectsChunkSize * cellSize;
            return new Rect(x, y, s, s);
        }

        private static int DivFloor(int x, int s) => x >= 0 ? x / s : (x - (s - 1)) / s;

        private static ulong ChunkKey(Vector2Int ch) => ((ulong)(uint)ch.x << 32) | (uint)ch.y;

        private static ObjectManager.ChunkCoord ToCoord(ulong key)
            => new ObjectManager.ChunkCoord((int)(key >> 32), (int)(key & 0xffffffff));

        private static bool CircleIntersectsRect(Vector2 c, float r, Rect rect)
        {
            float cx = Mathf.Clamp(c.x, rect.xMin, rect.xMax);
            float cy = Mathf.Clamp(c.y, rect.yMin, rect.yMax);
            float dx = cx - c.x;
            float dy = cy - c.y;
            return (dx * dx + dy * dy) <= r * r;
        }

        // Публичные настройки из кода (если надо подтянуть из тайлового менеджера вручную)
        public void ConfigureFromTiles(int tileRadiusCells, float cellSizeWorld)
        {
            tileRadius = tileRadiusCells;
            cellSize = cellSizeWorld;
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            if (!objectManager) return;
            Gizmos.color = new Color(0f, 1f, 0f, 0.12f);
            foreach (var key in _active)
            {
                var cc = ToCoord(key);
                var r = objectManager.GetChunkWorldRect(new ObjectManager.ChunkCoord(cc.x, cc.y));
                Gizmos.DrawCube(r.center, r.size);
            }
        }
#endif
    }
}