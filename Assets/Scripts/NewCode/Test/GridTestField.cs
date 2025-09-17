using System.Collections.Generic;
using UnityEngine;

public class GridTestField : MonoBehaviour
{
    [Header("Prefab")]
    public GameObject tilePrefab;          // белая клетка 1×1, по умолчанию неактивна

    [Header("Grid")]
    public int width = 50;
    public int height = 50;
    public float cellSize = 1f;
    public bool centerAtZero = true;       // центрировать сетку вокруг (0,0)

    [Header("Visuals")]
    public bool checkerboard = true;       // шахматная подсветка для ориентации
    public Color colorA = Color.white;
    public Color colorB = new Color(0.9f, 0.9f, 0.9f);

    [Header("Collision (по желанию)")]
    public bool addBoxCollider2D = false;  // для 2D
    public bool addBoxCollider3D = false;  // для 3D ground

    private readonly List<GameObject> _spawned = new();

    [ContextMenu("Build Grid")]
    public void BuildGrid()
    {
        if (!tilePrefab) { Debug.LogError("[GridTestField] Не задан tilePrefab"); return; }
        ClearGrid();

        Vector2 origin = Vector2.zero;
        if (centerAtZero)
        {
            float ox = -(width * 0.5f - 0.5f) * cellSize;
            float oy = -(height * 0.5f - 0.5f) * cellSize;
            origin = new Vector2(ox, oy);
        }    

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                Vector3 pos = new Vector3(origin.x + x * cellSize, origin.y + y * cellSize, 0f);
                var go = Instantiate(tilePrefab, pos, Quaternion.identity, transform);
                go.transform.localPosition = pos;
                go.name = $"Tile_{x}_{y}";
                if (!go.activeSelf) go.SetActive(true);

                // Цвет/спрайт
                var sr = go.GetComponent<SpriteRenderer>();
                if (sr && checkerboard)
                    sr.color = ((x + y) & 1) == 0 ? colorA : colorB;

                // Коллайдеры по опции
                if (addBoxCollider2D && !go.GetComponent<BoxCollider2D>())
                {
                    var c = go.AddComponent<BoxCollider2D>();
                    c.size = Vector2.one * cellSize;
                }
                if (addBoxCollider3D && !go.GetComponent<BoxCollider>())
                {
                    var c = go.AddComponent<BoxCollider>();
                    c.size = new Vector3(cellSize, 0.05f, cellSize);
                }

                _spawned.Add(go);
            }
    }

    [ContextMenu("Clear Grid")]
    public void ClearGrid()
    {
        // чистим то, что создавали
        for (int i = 0; i < _spawned.Count; i++)
            if (_spawned[i]) DestroyImmediate(_spawned[i]);
        _spawned.Clear();

        // на всякий случай — удалим чужих детей
        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(transform.GetChild(i).gameObject);
    }

    // Опционально: авто-построение на старте
    void Start()
    {
        if (transform.childCount == 0) BuildGrid();
    }
    // внутри GridTestField
    public Rect GetWorldBounds()
    {
        // центры крайнего нижнего-левого и верхнего-правого тайлов в ЛОКАЛЕ
        float ox = centerAtZero ? -(width * 0.5f - 0.5f) * cellSize : 0.5f * cellSize;
        float oy = centerAtZero ? -(height * 0.5f - 0.5f) * cellSize : 0.5f * cellSize;

        // мин/макс УГЛЫ поля = центры ± 0.5 клетки
        Vector3 localMin = new Vector3(ox - 0.5f * cellSize, oy - 0.5f * cellSize, 0f);
        Vector3 localMax = new Vector3(ox + (width - 0.5f) * cellSize,
                                       oy + (height - 0.5f) * cellSize, 0f);

        // в мир
        Vector3 wMin = transform.TransformPoint(localMin);
        Vector3 wMax = transform.TransformPoint(localMax);

        return Rect.MinMaxRect(Mathf.Min(wMin.x, wMax.x), Mathf.Min(wMin.y, wMax.y),
                               Mathf.Max(wMin.x, wMax.x), Mathf.Max(wMin.y, wMax.y));
    }
}
