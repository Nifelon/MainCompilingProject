using Game.World.Map.Biome;
using System.Collections.Generic;
using UnityEngine;

/// Пул квадратов сетки. Не знает о биомах — цвет задаётся через ColorFn.
public class PoolManagerMainTile : MonoBehaviour
{
    [Header("Prefab и контейнер")]
    [SerializeField] GameObject tilePrefab;
    [SerializeField] Transform tilesRoot;
    private readonly Dictionary<GameObject, Vector2Int> _goToCell = new();
    [Header("Размер пула")]
    [SerializeField] int poolCount = 8000;
    //[SerializeField] int hardCap = 20000;

    // Делегат окраски тайла: клетка мира -> цвет
    public System.Func<Vector2Int, Sprite> SpriteFn;

    // Активные тайлы и очередь свободных
    public readonly Dictionary<Vector2Int, GameObject> activeSquares = new();
    readonly Queue<GameObject> tilePool = new();
    [SerializeField] private MonoBehaviour groundPaintRef; // IGroundPaintService
    private IGroundSpriteService _groundSprite;

    int _created;

    void Awake()
    {
        GenerateSquarePool();
        _groundSprite = groundPaintRef as IGroundSpriteService;
    }

    public void GenerateSquarePool()
    {
        if (!tilePrefab) { Debug.LogError("[MainTile] tilePrefab is null"); return; }
        if (!tilesRoot) { Debug.LogError("[MainTile] tilesRoot is null"); return; }

        for (int i = 0; i < poolCount; i++)
        {
            var go = Instantiate(tilePrefab, tilesRoot);
            if (!go.TryGetComponent<PooledTile>(out _)) go.AddComponent<PooledTile>();
            go.name = "sleepTile";
            go.SetActive(false);
            tilePool.Enqueue(go);
            _created++;
        }
    }
    public void UpdateTileVisual(Vector2Int cell, GameObject tileGo)
    {
        var view = tileGo.GetComponent<GroundTileView>();
        if (!view) view = tileGo.AddComponent<GroundTileView>();

        // 1) базовый спрайт для этой клетки (какой у тебя там SpriteFn/биом)
        if (SpriteFn != null)
        {
            var baseSprite = SpriteFn(cell);
            view.SetDefaultSprite(baseSprite);
        }

        // 2) оверрайд лагеря — ТОЛЬКО если сервис его вернул
        if (_groundSprite != null && _groundSprite.TryGetSprite(cell, out var s))
            view.ApplySpriteOverride(s, true);
        else
            view.ApplySpriteOverride(null, false);
    }

    /// Выдать тайл, поставить позицию и цвет.
    public GameObject GetSquare(Vector3 position, Vector2Int cell)
    {
        if (!tilePrefab || !tilesRoot) return null;

        GameObject go = tilePool.Count > 0 ? tilePool.Dequeue()
                                           : Instantiate(tilePrefab, tilesRoot);

        if (!go.TryGetComponent<PooledTile>(out var pt)) pt = go.AddComponent<PooledTile>();
        pt.EnsureSpriteRenderer();

        go.transform.SetParent(tilesRoot, false);
        go.transform.localPosition = position;

        if (SpriteFn != null)
        {
            var col = SpriteFn(cell);
            if (pt.sr) pt.sr.sprite = col;
            else
            {
                var sr = go.GetComponentInChildren<SpriteRenderer>(true);
                if (sr) sr.sprite = col;
            }
        }

        go.name = $"tile_{cell.x}_{cell.y}";
        go.SetActive(true);

        // регистрируем активную клетку
        activeSquares[cell] = go;
        _goToCell[go] = cell;

        // применяем базовый спрайт + оверрайд лагеря
        UpdateTileVisual(cell, go);
        return go;
    }

    public void ReturnSquare(GameObject square)
    {
        if (!square) return;

        // снять из индексов
        if (_goToCell.TryGetValue(square, out var cell))
        {
            activeSquares.Remove(cell);
            _goToCell.Remove(square);
        }

        square.name = "sleepTile";
        square.SetActive(false);
        square.transform.SetParent(tilesRoot, false);
        tilePool.Enqueue(square);
    }

    public void ClearAll()
    {
        var values = new List<GameObject>(activeSquares.Values);
        activeSquares.Clear();
        _goToCell.Clear();

        foreach (var go in values)
            ReturnSquare(go);
    }
    public void RefreshCellsInCircle(Vector2Int center, int radius)
    {
        int r2 = radius * radius;

        // делаем снапшот ключей, чтобы перечисление было безопасным
        // (можно хранить _tmpKeys как поле, но проще сначала так)
        var keys = new List<Vector2Int>(activeSquares.Keys);

        for (int i = 0; i < keys.Count; i++)
        {
            var cell = keys[i];
            if ((cell - center).sqrMagnitude <= r2 &&
                activeSquares.TryGetValue(cell, out var go))
            {
                UpdateTileVisual(cell, go);
            }
        }
    }
}