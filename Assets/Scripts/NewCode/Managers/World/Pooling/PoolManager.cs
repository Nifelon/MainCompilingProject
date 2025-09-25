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
        if (objectManager) objectManager.ClearChunkCache();  // <-- добавь

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

        var bm = FindObjectOfType<BiomeManager>();
        if (bm == null || !bm.IsBiomesReady) return;

        // текущие координаты чанка по центру окна
        int cx = centerCell.x >= 0 ? centerCell.x / objectsChunkSize
                                   : (centerCell.x - (objectsChunkSize - 1)) / objectsChunkSize;
        int cy = centerCell.y >= 0 ? centerCell.y / objectsChunkSize
                                   : (centerCell.y - (objectsChunkSize - 1)) / objectsChunkSize;

        // набор чанков, которые должны быть активны
        var should = new HashSet<ulong>();
        for (int dy = -objectsChunkRadius; dy <= objectsChunkRadius; dy++)
            for (int dx = -objectsChunkRadius; dx <= objectsChunkRadius; dx++)
            {
                int x = cx + dx, y = cy + dy;
                ulong key = ChunkKey(x, y);
                should.Add(key);
            }

        // выгружаем лишние
        foreach (var key in _activeObjectChunks)
        {
            if (should.Contains(key)) continue;
            var cc = new ObjectManager.ChunkCoord((int)(key >> 32), (int)(key & 0xffffffff));
            objectManager.UnloadChunkVisuals(cc);
        }

        // загружаем недостающие
        foreach (var key in should)
        {
            if (_activeObjectChunks.Contains(key)) continue;
            var cc = new ObjectManager.ChunkCoord((int)(key >> 32), (int)(key & 0xffffffff));
            objectManager.LoadChunkVisuals(cc);
        }

        // фиксация активного набора
        _activeObjectChunks.Clear();
        foreach (var k in should) _activeObjectChunks.Add(k);
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
}
