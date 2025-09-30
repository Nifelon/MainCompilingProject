using System.Collections.Generic;
using UnityEngine;
using Game.World.Map.Biome; // IBiomeService / BiomeManager
using Game.World.Objects;   // ObjectManager (визуал объектов грузим/выгружаем через него)
using Game.World.Signals;

[DefaultExecutionOrder(-200)]
public class PoolManager : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] Transform player;
    [SerializeField] PoolManagerMainTile mainTilePool;

    [Header("Параметры окна (тайлы)")]
    [SerializeField, Min(1)] int radius = 40;   // в клетках
    [SerializeField] float cellSize = 1f;       // 1 клетка = 1 юнит

    // Внутреннее
    Vector2Int _lastCell;
    IBiomeService _biomes;

    void OnValidate()
    {
        if (radius < 1) radius = 1;
        if (cellSize <= 0f) cellSize = 1f;
    }

    void Reset()
    {
        player = FindFirstObjectByType<PlayerMeleeController>()?.transform;
        mainTilePool = FindFirstObjectByType<PoolManagerMainTile>();
    }

    void Awake()
    {
        if (!player) Debug.LogWarning("[PoolManager] Player не назначен");
        if (!mainTilePool) Debug.LogError("[PoolManager] MainTilePool не назначен");

        // Находим BiomeManager и ждём его готовности
        _biomes = FindFirstObjectByType<BiomeManager>(FindObjectsInactive.Exclude);
        if (_biomes != null && _biomes.IsBiomesReady) BindBiomeSpriteAndRefresh();
        else StartCoroutine(CoBindWhenReady());
    }
    void OnEnable() { WorldSignals.OnWorldRegen += HandleWorldRegen; }
    void OnDisable() { WorldSignals.OnWorldRegen -= HandleWorldRegen; }

    void HandleWorldRegen()
    {
        // Сбросим маркер «последней клетки» и принудительно перерисуем окно
        _lastCell = new Vector2Int(int.MinValue, int.MinValue);
        ForceRefresh();
    }

    System.Collections.IEnumerator CoBindWhenReady()
    {
        // ждём, пока BiomeManager появится и отметится готовым
        while (_biomes == null || !_biomes.IsBiomesReady)
        {
            if (_biomes == null)
                _biomes = FindFirstObjectByType<BiomeManager>(FindObjectsInactive.Exclude);
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

        ForceRefresh();
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
            RefreshAround(cell); // тайлы
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

        // Сброс тайлов
        mainTilePool.ClearAll();

        // Вычисляем центр окна
        _lastCell = WorldToCell(player.position);

        // Обновляем окно тайлов
        RefreshAround(_lastCell);
    }

    // ==== внутренняя логика (ТАЙЛЫ) ====

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
                    var go = mainTilePool.GetSquare(CellToWorld(cell), cell); // GetSquare сам регистрирует cell
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
                mainTilePool.ReturnSquare(kv.Value); // ReturnSquare удаляет из activeSquares
        }
    }

    // Публичные параметры для ObjectChunkStreamer
    public int RadiusCells => radius;
    public float CellSizeWorld => cellSize;
}