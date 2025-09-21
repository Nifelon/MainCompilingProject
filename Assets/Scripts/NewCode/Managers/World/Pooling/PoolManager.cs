using System.Collections.Generic;
using UnityEngine;
using Game.World.Map.Biome; // IBiomeService / BiomeManager

/// Управляет окном активных клеток вокруг игрока.
/// Прокидывает в пул правильную функцию окраски: cell -> biome -> color.
[DefaultExecutionOrder(-200)]
public class PoolManager : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] Transform player;
    [SerializeField] PoolManagerMainTile mainTilePool;

    [Header("Параметры окна")]
    [SerializeField, Min(1)] int radius = 40; // в клетках
    [SerializeField] float cellSize = 1f;     // 1 клетка = 1 юнит

    // Внутреннее
    Vector2Int _lastCell;
    IBiomeService _biomes;

    void Reset()
    {
#if UNITY_2023_1_OR_NEWER
        player = FindFirstObjectByType<PlayerMeleeController>()?.transform;
        mainTilePool = FindFirstObjectByType<PoolManagerMainTile>();
#else
        player = FindObjectOfType<PlayerMeleeController>()?.transform;
        mainTilePool = FindObjectOfType<PoolManagerMainTile>();
#endif
    }

    void Awake()
    {
        if (!player) Debug.LogWarning("[PoolManager] Player не назначен");
        if (!mainTilePool) Debug.LogError("[PoolManager] MainTilePool не назначен");

        // Unity 6: безопасный поиск и ожидание готовности биомов
#if UNITY_2023_1_OR_NEWER
        _biomes = FindFirstObjectByType<BiomeManager>(FindObjectsInactive.Exclude);
#else
        _biomes = FindObjectOfType<BiomeManager>();
#endif
        if (_biomes != null && _biomes.IsBiomesReady) BindColorAndRefresh();
        else StartCoroutine(CoBindWhenReady());
    }

    System.Collections.IEnumerator CoBindWhenReady()
    {
        // ждём, пока BiomeManager появится и отметится готовым
        while (_biomes == null || !_biomes.IsBiomesReady)
        {
#if UNITY_2023_1_OR_NEWER
            if (_biomes == null) _biomes = FindFirstObjectByType<BiomeManager>(FindObjectsInactive.Exclude);
#else
            if (_biomes == null) _biomes = FindObjectOfType<BiomeManager>();
#endif
            yield return null;
        }
        BindColorAndRefresh();
    }

    void BindColorAndRefresh()
    {
        // ВАЖНО: BiomeManager ожидает КЛЕТКУ МИРА (центрированную), без сдвигов/flip.
        mainTilePool.SpriteFn = (Vector2Int cell) =>
        {
            var bt = _biomes.GetBiomeAtPosition(cell);
            return _biomes.GetBiomeSprite(bt);
        };

        // Принудительный первый прогон, когда всё готово
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
            RefreshAround(cell);
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
        mainTilePool.ClearAll();
        _lastCell = WorldToCell(player.position);
        RefreshAround(_lastCell);
    }

    // ==== внутренняя логика ====

    Vector2Int WorldToCell(Vector3 world)
    {
        // если есть смещение корня мира, вычти его здесь:
        // world -= worldRoot.position;
        int x = Mathf.FloorToInt(world.x / cellSize);
        int y = Mathf.FloorToInt(world.y / cellSize);
        return new Vector2Int(x, y);
    }

    Vector3 CellToWorld(Vector2Int cell)
        => new Vector3(cell.x * cellSize, cell.y * cellSize, 0f);

    void RefreshAround(Vector2Int center)
    {
        // 1) Добавить недостающие клетки
        for (int y = -radius; y <= radius; y++)
            for (int x = -radius; x <= radius; x++)
            {
                var cell = new Vector2Int(center.x + x, center.y + y);

                if (!mainTilePool.activeSquares.ContainsKey(cell))
                {
                    var worldPos = CellToWorld(cell);
                    var go = mainTilePool.GetSquare(worldPos, cell);
                    if (go != null) mainTilePool.activeSquares[cell] = go;
                }
            }

        // 2) Удалить далёкие (радиус Чебышёва — без sqrt)
        int killR = Mathf.RoundToInt(radius * 1.5f);

        var remove = new List<Vector2Int>();
        foreach (var kv in mainTilePool.activeSquares)
        {
            int dx = Mathf.Abs(kv.Key.x - center.x);
            int dy = Mathf.Abs(kv.Key.y - center.y);
            if (Mathf.Max(dx, dy) > killR)
            {
                mainTilePool.ReturnSquare(kv.Value);
                remove.Add(kv.Key);
            }
        }
        foreach (var key in remove) mainTilePool.activeSquares.Remove(key);
    }
}