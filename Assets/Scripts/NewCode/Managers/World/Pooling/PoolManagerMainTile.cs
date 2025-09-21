using System.Collections.Generic;
using UnityEngine;

/// Пул квадратов сетки. Не знает о биомах — цвет задаётся через ColorFn.
public class PoolManagerMainTile : MonoBehaviour
{
    [Header("Prefab и контейнер")]
    [SerializeField] GameObject tilePrefab;
    [SerializeField] Transform tilesRoot;

    [Header("Размер пула")]
    [SerializeField] int poolCount = 8000;
    //[SerializeField] int hardCap = 20000;

    // Делегат окраски тайла: клетка мира -> цвет
    public System.Func<Vector2Int, Sprite> SpriteFn;

    // Активные тайлы и очередь свободных
    public readonly Dictionary<Vector2Int, GameObject> activeSquares = new();
    readonly Queue<GameObject> tilePool = new();

    int _created;

    void Awake()
    {
        GenerateSquarePool();
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
        return go;
    }

    public void ReturnSquare(GameObject square)
    {
        if (!square) return;
        square.name = "sleepTile";
        square.SetActive(false);
        square.transform.SetParent(tilesRoot, false);
        tilePool.Enqueue(square);
    }

    public void ClearAll()
    {
        foreach (var kv in activeSquares)
            ReturnSquare(kv.Value);
        activeSquares.Clear();
    }
}