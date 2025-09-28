using System.Collections.Generic;
using UnityEngine;
using Game.World.Objects;

namespace Game.World.Objects.Streaming
{
    /// Стримит чанки объектов вокруг игрока кругом видимости (как тайлы).
    /// Ничего не генерирует и не трогает пул напрямую — только вызывает ObjectManager.
    [DefaultExecutionOrder(-185)]
    public sealed class ObjectChunkStreamer : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private Transform player;
        [SerializeField] private ObjectManager objectManager;

        [Header("Units (как у тайлов)")]
        [Tooltip("Размер клетки в world units")]
        [SerializeField] private float cellSize = 1f;

        [Tooltip("Радиус окна в КЛЕТКАХ (как у тайлов). Желательно прокинуть тот же, что в PoolManagerMainTile).")]
        [SerializeField] private int tileRadius = 64;

        [Header("Objects (чанки)")]
        [Tooltip("Размер чанка объектов в КЛЕТКАХ (должен совпадать с ObjectManager.ChunkSize).")]
        [SerializeField] private int objectsChunkSize = 64;

        [Header("Hysteresis (зона удержания побольше, чтобы не щёлкало)")]
        [SerializeField] private int loadPaddingCells = 8;   // +N клеток к радиусу при загрузке
        [SerializeField] private int keepPaddingCells = 12;  // +N клеток к радиусу при удержании

        // runtime
        private readonly HashSet<ulong> _active = new();
        private Vector2Int _lastCenterChunk = new(int.MinValue, int.MinValue);

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
            if (objectManager && objectsChunkSize != objectManager.ChunkSize)
                objectsChunkSize = objectManager.ChunkSize;

            WorldSignals.OnWorldRegen += OnWorldRegen;
        }

        void OnDestroy()
        {
            WorldSignals.OnWorldRegen -= OnWorldRegen;
        }

        void Update()
        {
            if (!player || !objectManager) return;

            // центр круга — позиция игрока в мире
            Vector2 c = player.position;

            // центральный чанк в клетках
            var centerCell = WorldToCell(c);
            var centerChunk = new Vector2Int(
                DivFloor(centerCell.x, objectsChunkSize),
                DivFloor(centerCell.y, objectsChunkSize)
            );

            if (centerChunk != _lastCenterChunk)
            {
                Refresh(centerChunk, c);
                _lastCenterChunk = centerChunk;
            }
        }

        // === core ===
        private void Refresh(Vector2Int centerChunk, Vector2 centerWorld)
        {
            float loadR = (tileRadius + loadPaddingCells) * cellSize;
            float keepR = (tileRadius + keepPaddingCells) * cellSize;

            // сколько чанков осматривать вокруг (по таксу на удержание)
            int R = Mathf.CeilToInt((tileRadius + keepPaddingCells) / (float)objectsChunkSize) + 1;

            var shouldLoad = new HashSet<ulong>();
            var shouldKeep = new HashSet<ulong>();

            for (int dy = -R; dy <= R; dy++)
                for (int dx = -R; dx <= R; dx++)
                {
                    var ch = new Vector2Int(centerChunk.x + dx, centerChunk.y + dy);

                    Rect rect = ChunkRectWorld(ch);
                    ulong key = ChunkKey(ch);

                    if (CircleIntersectsRect(centerWorld, loadR, rect)) shouldLoad.Add(key);
                    if (CircleIntersectsRect(centerWorld, keepR, rect)) shouldKeep.Add(key);
                }

            // SPAWN: всё, что нужно держать и ещё не активно
            foreach (var key in shouldLoad)
            {
                if (_active.Contains(key)) continue;
                objectManager.LoadChunkVisuals(ToCoord(key));
                _active.Add(key);
            }

            // DESPAWN: всё, что активно, но больше не нужно удерживать
            var toKill = new List<ulong>();
            foreach (var key in _active)
            {
                if (!shouldKeep.Contains(key))
                {
                    objectManager.UnloadChunkVisuals(ToCoord(key));
                    toKill.Add(key);
                }
            }
            for (int i = 0; i < toKill.Count; i++) _active.Remove(toKill[i]);
        }

        private void OnWorldRegen()
        {
            // Полный сброс
            foreach (var key in _active)
                objectManager.UnloadChunkVisuals(ToCoord(key));
            _active.Clear();
            _lastCenterChunk = new Vector2Int(int.MinValue, int.MinValue);
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

        // Публичные настройки из кода (если надо подтянуть из тайлового менеджера)
        public void ConfigureFromTiles(int tileRadiusCells, float cellSizeWorld)
        {
            tileRadius = tileRadiusCells;
            cellSize = cellSizeWorld;
        }
    }
}
