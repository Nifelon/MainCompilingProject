using System;
using System.Collections.Generic;
using UnityEngine;
using Game.World.Objects;

[DefaultExecutionOrder(-210)]
public class PoolManagerObjects : MonoBehaviour
{
    [Serializable]
    struct Binding { public ObjectType type; public GameObject prefab; }

    [Header("Prefabs")]
    [SerializeField] GameObject defaultSpritePrefab;
    [SerializeField] List<Binding> overrides = new();

    // (Осталось для совместимости; сортировку теперь делает ObjectViewPoolAdapter)
    [Header("Sorting (handled by ObjectViewPoolAdapter)")]
    [SerializeField] string sortingLayer = "Objects";
    [SerializeField] int orderOffset = 0;
    [SerializeField] bool ySort = true;
    [SerializeField] int ySortMul = 10;

    // локальные пулы по типам
    private readonly Dictionary<ObjectType, Stack<GameObject>> _pool = new();
    private readonly Dictionary<ObjectType, GameObject> _prefabByType = new();

    void Awake()
    {
        foreach (var b in overrides)
            if (b.prefab) _prefabByType[b.type] = b.prefab;
    }

    // === ПУЛ ===
    public GameObject Get(ObjectType type)
    {
        if (!_pool.TryGetValue(type, out var stack))
        {
            stack = new Stack<GameObject>();
            _pool[type] = stack;
        }

        if (stack.Count > 0)
        {
            var go = stack.Pop();
            if (go) go.SetActive(true);
            return go;
        }

        var prefab = (_prefabByType.TryGetValue(type, out var p) && p) ? p : defaultSpritePrefab;
        var inst = Instantiate(prefab, transform);

        var tag = inst.GetComponent<PooledObjectTag>() ?? inst.AddComponent<PooledObjectTag>();
        tag.Type = type;
        tag.SR = inst.GetComponentInChildren<SpriteRenderer>();

        return inst;
    }

    public void Release(GameObject go)
    {
        if (!go) return;

        var tag = go.GetComponent<PooledObjectTag>();
        var type = tag ? tag.Type : ObjectType.None;

        if (!_pool.TryGetValue(type, out var stack))
        {
            stack = new Stack<GameObject>();
            _pool[type] = stack;
        }

        // небольшая гигиена, чтобы не «тянулись» старые спрайты
        if (tag?.SR) tag.SR.sprite = null;

        go.SetActive(false);
        go.transform.SetParent(transform, false);
        stack.Push(go);
    }

    // Опционально: жёсткая очистка пула (если нужно на выгрузке сцены)
    public void ClearAll()
    {
        foreach (var kv in _pool)
        {
            var stack = kv.Value;
            while (stack.Count > 0)
            {
                var go = stack.Pop();
                if (go) Destroy(go);
            }
        }
        _pool.Clear();
    }
}

public class PooledObjectTag : MonoBehaviour
{
    public ObjectType Type;
    [NonSerialized] public SpriteRenderer SR;

    void Awake()
    {
        if (!SR) SR = GetComponentInChildren<SpriteRenderer>();
    }
}
