using System;
using System.Collections.Generic;
using UnityEngine;
using Game.World.Objects;

[DefaultExecutionOrder(-210)]
public class PoolManagerObjects : MonoBehaviour
{
    [Serializable] struct Binding { public ObjectType type; public GameObject prefab; }

    [Header("Prefabs")]
    [Tooltip("Базовый префаб с SpriteRenderer (если у типа нет своего override).")]
    [SerializeField] GameObject defaultSpritePrefab;

    [Tooltip("Особые префабы для конкретных типов (если нужны).")]
    [SerializeField] List<Binding> overrides = new();

    [Header("Root")]
    [SerializeField] Transform objectsRoot;

    readonly Dictionary<ObjectType, Queue<GameObject>> _pools = new();
    readonly Dictionary<ObjectType, GameObject> _prefabByType = new();

    void Awake()
    {
        if (!objectsRoot)
        {
            var t = new GameObject("ObjectsRoot").transform;
            t.SetParent(transform, false);
            objectsRoot = t;
        }

        foreach (ObjectType t in Enum.GetValues(typeof(ObjectType)))
            _pools[t] = new Queue<GameObject>();

        foreach (var b in overrides)
            if (b.prefab) _prefabByType[b.type] = b.prefab;

        if (!defaultSpritePrefab)
            Debug.LogError("[PoolManagerObjects] Default Sprite Prefab не задан.");
    }

    public void Prewarm(ObjectType type, int count)
    {
        for (int i = 0; i < count; i++)
        {
            var go = CreateNew(type);
            go.SetActive(false);
            _pools[type].Enqueue(go);
        }
    }

    public GameObject Get(ObjectType type)
    {
        var q = _pools[type];
        var go = q.Count > 0 ? q.Dequeue() : CreateNew(type);
        go.transform.SetParent(objectsRoot, false);
        go.SetActive(true);

        // кэшируем компоненты
        var tag = go.GetComponent<PooledObjectTag>();
        if (!tag) tag = go.AddComponent<PooledObjectTag>();
        tag.Type = type;
        if (!tag.SR) tag.SR = go.GetComponentInChildren<SpriteRenderer>();

        return go;
    }

    public void Release(GameObject go)
    {
        if (!go) return;
        var tag = go.GetComponent<PooledObjectTag>();
        var type = tag ? tag.Type : ObjectType.Bush;

        go.SetActive(false);
        go.transform.SetParent(objectsRoot, false);
        _pools[type].Enqueue(go);
    }

    GameObject CreateNew(ObjectType type)
    {
        GameObject prefab = null;
        _prefabByType.TryGetValue(type, out prefab);
        if (!prefab) prefab = defaultSpritePrefab;

        var go = Instantiate(prefab, objectsRoot);
        go.name = $"pooled_{type}";

        // кэш рендера
        var tag = go.GetComponent<PooledObjectTag>() ?? go.AddComponent<PooledObjectTag>();
        tag.Type = type;
        tag.SR = go.GetComponentInChildren<SpriteRenderer>();

        return go;
    }
}

public class PooledObjectTag : MonoBehaviour
{
    public ObjectType Type;
    [NonSerialized] public SpriteRenderer SR;
    void Awake() { if (!SR) SR = GetComponentInChildren<SpriteRenderer>(); }
}
