using System.Collections.Generic;
using UnityEngine;
using Game.World.Map.Biome; // IBiomeService / BiomeManager
using Game.World.Objects;   // ObjectManager (визуал объектов грузим/выгружаем через него)

/// Управляет окном активных клеток вокруг игрока (тайлы + объекты).
/// 1) Тайлы: выдаёт пулу функцию "клетка -> спрайт биома" и стримит окно тайлов.
/// 2) Объекты: стримит чанки объектов вокруг игрока (через ObjectManager).
[DefaultExecutionOrder(-200)]
public class PoolManager : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] Transform player;
    [SerializeField] PoolManagerMainTile mainTilePool;

    [Tooltip("Менеджер объектов: генерация и визуал объектов по чанкам")]
    [SerializeField] ObjectManager objectManager;

    [Header("Параметры окна (тайлы)")]
    [SerializeField, Min(1)] int radius = 40; // в клетках
    [SerializeField] float cellSize = 1f;   // 1 клетка = 1 юнит

    [Header("Параметры стриминга объектов")]
    [Tooltip("Размер чанка объектов, должен совпадать с ObjectManager.chunkSize")]
    [SerializeField] int objectsChunkSize = 64;

    [Tooltip("Радиус стриминга в чанках объектов")]
    [SerializeField, Min(1)] int objectsChunkRadius = 2;
    int _chunkLoadRadius, _chunkUnloadRadius;
    // Внутреннее
    Vector2Int _lastCell;
    IBiomeService _biomes;
    readonly HashSet<ulong> _activeObjectChunks = new(); // какие чанки объектов сейчас загружены

    void Reset()
    {
        player = FindFirstObjectByType<PlayerMeleeController>()?.transform;
        mainTilePool = FindFirstObjectByType<PoolManagerMainTile>();
        objectManager = FindFirstObjectByType<ObjectManager>();
    }

    void Awake()
    {
        if (!player) Debug.LogWarning("[PoolManager] Player не назначен");
        if (!mainTilePool) Debug.LogError("[PoolManager] MainTilePool не назначен");
        if (!objectManager) objectManager = FindObjectOfType<ObjectManager>();
        if (objectManager && objectsChunkSize != objectManager.ChunkSize)
            objectsChunkSize = objectManager.ChunkSize;  // подхватываем из ObjectManager

        // Находим BiomeManager и ждём его готовности
        _biomes = FindFirstObjectByType<BiomeManager>(FindObjectsInactive.Exclude);
        if (_biomes != null && _biomes.IsBiomesReady) BindBiomeSpriteAndRefresh();
        else StartCoroutine(CoBindWhenReady());
        if (objectManager && objectsChunkSize != objectManager.ChunkSize)
            objectsChunkSize = objectManager.ChunkSize;
        RecomputeChunkRadii();
    }

    System.Collections.IEnumerator CoBindWhenReady()
    {
        // ждём, пока BiomeManager появится и отметится готовым
        while (_biomes == null || !_biomes.IsBiomesReady)
        {
            if (_biomes == null) _biomes = FindFirstObjectByType<BiomeManager>(FindObjectsInactive.Exclude);
            yield return null;
        }
        BindBiomeSpriteAndRefresh();
    }

    void BindBiomeSpriteAndRefresh()
    {
        mainTilePool.SpriteFn = (Vector2Int cell) =>
        {
            var bt = _biomes.GetBiomeAtPosition(cell);
            return _biomes.GetBiomeSprite(bt);
        };

        // очистим закэшированные до готовности биомов пустые чанки
        if (objectManager) objectManager.DespawnAll();  // <-- добавь

        ForceRefresh(); // он уже вызывает RefreshObjectChunks
    }

    void Start()
    {
        // На случай, если биомы уже были готовы — сделаем первичный прогон
        if (_biomes != null && _biomes.IsBiomesReady)
        {
            _lastCell = new Vector2Int(int.MinValue, int.MinValue);
            ForceRefresh();
        }
    }

    void Update()
    {
        if (!player || !mainTilePool) return;

        var cell = WorldToCell(player.position);
        if (cell != _lastCell)
        {
            _lastCell = cell;
            RefreshAround(cell);          // тайлы
            RefreshObjectChunks(cell);    // объекты
        }

        // Быстрый отладчик: показать биом под игроком
        // if (Input.GetKeyDown(KeyCode.F9) && _biomes != null)
        // {
        //     var b = _biomes.GetBiomeAtPosition(cell);
        //     Debug.Log($"[BiomeDBG] cell={cell} biome={b}");
        // }
    }

    public void ForceRefresh()
    {
        if (!player || !mainTilePool) return;

        // Сначала сброс тайлов
        mainTilePool.ClearAll();

        // Вычисляем центр
        _lastCell = WorldToCell(player.position);

        // Обновляем окно тайлов
        RefreshAround(_lastCell);

        // Полный ресет стриминга объектов
        UnloadAllObjectChunks();
        RefreshObjectChunks(_lastCell);
    }


    // ==== внутренняя логика (тайлы) ====

    Vector2Int WorldToCell(Vector3 world)
    {
        int x = Mathf.FloorToInt(world.x / cellSize);
        int y = Mathf.FloorToInt(world.y / cellSize);
        return new Vector2Int(x, y);
    }

    Vector3 CellToWorld(Vector2Int cell)
        => new Vector3(cell.x * cellSize, cell.y * cellSize, 0f);

    void RefreshAround(Vector2Int center)
    {
        // 1) ДОБАВИТЬ недостающие клетки (квадратное окно)
        for (int y = -radius; y <= radius; y++)
            for (int x = -radius; x <= radius; x++)
            {
                var cell = new Vector2Int(center.x + x, center.y + y);
                if (!mainTilePool.activeSquares.ContainsKey(cell))
                {
                    var go = mainTilePool.GetSquare(CellToWorld(cell), cell); // GetSquare сам регистрирует cell в activeSquares
                                                                              // ничего руками в словарь не пишем, GetSquare уже это делает
                }
            }

        // 2) УДАЛИТЬ далёкие (Чебышёв > killR) — только после foreach по СНИМКУ
        int killR = Mathf.RoundToInt(radius * 1.5f); // killR >= radius
        var snapshot = new List<KeyValuePair<Vector2Int, GameObject>>(mainTilePool.activeSquares);
        for (int i = 0; i < snapshot.Count; i++)
        {
            var kv = snapshot[i];
            int dx = Mathf.Abs(kv.Key.x - center.x);
            int dy = Mathf.Abs(kv.Key.y - center.y);
            if (Mathf.Max(dx, dy) > killR)
                mainTilePool.ReturnSquare(kv.Value); // ReturnSquare сам удаляет запись из activeSquares
        }
    }

    // ==== внутренняя логика (объекты) ====

    void RefreshObjectChunks(Vector2Int centerCell)
    {
        if (!objectManager) return;

        // центр круга в world units
        Vector2 c = CellToWorld(centerCell);
        float loadR = (_chunkLoadRadius * objectsChunkSize) * cellSize;   // клетки -> мир
        float keepR = (_chunkUnloadRadius * objectsChunkSize) * cellSize;

        // Просматриваем только ограниченную окрестность по чанкам:
        int R = _chunkUnloadRadius + 1;
        var shouldLoad = new HashSet<ulong>();
        var shouldKeep = new HashSet<ulong>();

        for (int dy = -R; dy <= R; dy++)
            for (int dx = -R; dx <= R; dx++)
            {
                int cx = Mathf.FloorToInt(centerCell.x / (float)objectsChunkSize) + dx;
                int cy = Mathf.FloorToInt(centerCell.y / (float)objectsChunkSize) + dy;

                Rect rect = new Rect(
                    cx * objectsChunkSize * cellSize,
                    cy * objectsChunkSize * cellSize,
                    objectsChunkSize * cellSize,
                    objectsChunkSize * cellSize
                );

                ulong key = ChunkKey(cx, cy);
                if (CircleIntersectsRect(c, loadR, rect)) shouldLoad.Add(key);
                if (CircleIntersectsRect(c, keepR, rect)) shouldKeep.Add(key);
            }

        // ДЕСПАВН: всё, что не попадает в keep
        foreach (var key in _activeObjectChunks)
            if (!shouldKeep.Contains(key))
                objectManager.UnloadChunkVisuals(new ObjectManager.ChunkCoord((int)(key >> 32), (int)(key & 0xffffffff)));

        // СПАВН: всё, что попадает в load и ещё не активно
        foreach (var key in shouldLoad)
            if (!_activeObjectChunks.Contains(key))
                objectManager.LoadChunkVisuals(new ObjectManager.ChunkCoord((int)(key >> 32), (int)(key & 0xffffffff)));

        _activeObjectChunks.Clear();
        foreach (var k in shouldKeep) _activeObjectChunks.Add(k);
    }

    static bool CircleIntersectsRect(Vector2 c, float r, Rect rect)
    {
        var closest = new Vector2(
            Mathf.Clamp(c.x, rect.xMin, rect.xMax),
            Mathf.Clamp(c.y, rect.yMin, rect.yMax)
        );
        return (closest - c).sqrMagnitude <= r * r;
    }

    void UnloadAllObjectChunks()
    {
        if (!objectManager) { _activeObjectChunks.Clear(); return; }
        foreach (var key in _activeObjectChunks)
        {
            var cc = new ObjectManager.ChunkCoord((int)(key >> 32), (int)(key & 0xffffffff));
            objectManager.UnloadChunkVisuals(cc);
        }
        _activeObjectChunks.Clear();
    }
    void RecomputeChunkRadii()
    {
        _chunkLoadRadius = Mathf.CeilToInt(radius / (float)objectsChunkSize);
        _chunkUnloadRadius = Mathf.CeilToInt(Mathf.RoundToInt(radius * 1.5f) / (float)objectsChunkSize);
    }

    static ulong ChunkKey(int x, int y) => ((ulong)(uint)x << 32) | (uint)y;
    public int RadiusCells => radius;
    public float CellSizeWorld => cellSize;
}
