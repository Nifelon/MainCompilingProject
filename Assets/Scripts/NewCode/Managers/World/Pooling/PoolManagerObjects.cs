using System;
using System.Collections.Generic;
using UnityEngine;
using Game.World.Objects;
using Unity.VisualScripting;

[DefaultExecutionOrder(-210)]
public class PoolManagerObjects : MonoBehaviour
{
    [Serializable] struct Binding { public ObjectType type; public GameObject prefab; }

    [Header("Prefabs")]
    [SerializeField] GameObject defaultSpritePrefab;
    [SerializeField] List<Binding> overrides = new();

    [Header("Sorting")]
    [SerializeField] string sortingLayer = "Objects";
    [SerializeField] int orderOffset = 0;
    [SerializeField] bool ySort = true;
    [SerializeField] int ySortMul = 10;

    // локальные пулы по типам
    private readonly Dictionary<ObjectType, Stack<GameObject>> _pool = new();
    private readonly Dictionary<ObjectType, GameObject> _prefabByType = new();

    void Awake()
    {
        foreach (var b in overrides) if (b.prefab) _prefabByType[b.type] = b.prefab;
    }

    // === ПУЛ ===
    public GameObject Get(ObjectType type)
    {
        if (!_pool.TryGetValue(type, out var s)) { s = new Stack<GameObject>(); _pool[type] = s; }
        if (s.Count > 0) { var go = s.Pop(); go.SetActive(true); return go; }

        var prefab = _prefabByType.TryGetValue(type, out var p) ? p : defaultSpritePrefab;
        var inst = Instantiate(prefab, transform);
        var tag = inst.GetComponent<PooledObjectTag>() ?? inst.AddComponent<PooledObjectTag>();
        tag.Type = type; tag.SR = inst.GetComponentInChildren<SpriteRenderer>();
        return inst;
    }

    public void Release(GameObject go)
    {
        if (!go) return;
        var tag = go.GetComponent<PooledObjectTag>();
        var type = tag ? tag.Type : ObjectType.None;
        if (!_pool.TryGetValue(type, out var s)) { s = new Stack<GameObject>(); _pool[type] = s; }
        go.SetActive(false);
        go.transform.SetParent(transform, false);
        s.Push(go);
    }

    // === СТРИМИНГ ВИЗУАЛА ===
    public void LoadChunk(ObjectManager mgr, ObjectManager.ChunkCoord cc)
    {
        var data = mgr.GetChunkData(cc);
        var list = new List<GameObject>(data.Count);

        foreach (var d in data)
        {
            if (mgr.IsDestroyed(d.id)) continue;

            var go = Get(d.type);
            ApplyVisual(mgr, go, d);
            list.Add(go);
        }

        mgr.SetChunkVisuals(cc, list);
    }

    public void UnloadChunk(ObjectManager mgr, ObjectManager.ChunkCoord cc)
    {
        var gos = mgr.GetChunkVisuals(cc);
        if (gos != null)
        {
            foreach (var go in gos)
                if (go) Release(go);
        }
        mgr.ClearChunkVisuals(cc);
    }

    // настройка визуала (скопировано/адаптировано из старого ObjectManager.ApplyVisual)
    private void ApplyVisual(ObjectManager mgr, GameObject go, ObjectInstanceData inst)
    {
        if (!mgr.TryGetObjectData(inst.type, out var data)) return;

        // позиция
        float cs = mgr.CellSize;
        go.transform.position = new Vector3(inst.worldPos.x, inst.worldPos.y, 0f) + (Vector3)data.pivotOffsetWorld;
        if ((data.tags & ObjectTags.HighSprite) != 0)
            go.transform.position += Vector3.up * (data.visualHeightUnits * cs);

        // теги/кэш
        var wref = go.GetComponent<WorldObjectRef>() ?? go.AddComponent<WorldObjectRef>();
        wref.id = inst.id; wref.type = inst.type;

        var tag = go.GetComponent<PooledObjectTag>() ?? go.AddComponent<PooledObjectTag>();
        if (!tag.SR) tag.SR = go.GetComponentInChildren<SpriteRenderer>();
        if (tag.SR)
        {
            if (data.spriteVariants != null && data.spriteVariants.Length > 0)
                tag.SR.sprite = data.spriteVariants[Mathf.Clamp(inst.variantIndex, 0, data.spriteVariants.Length - 1)];

            tag.SR.sortingLayerID = SortingLayer.NameToID(sortingLayer);
            tag.SR.sortingOrder = orderOffset + (ySort ? -(int)Mathf.Round(go.transform.position.y * ySortMul) : 0);
        }

        // интерактив (ягодные и т.п.)
        bool harvestable = data.harvest.harvestable;
        if (harvestable)
        {
            var cc = go.GetComponent<CircleCollider2D>() ?? go.AddComponent<CircleCollider2D>();
            cc.isTrigger = true;
            cc.radius = 0.45f * cs;
            go.layer = LayerMask.NameToLayer("Interactable");
        }

        // коллизия для «глухих» объектов
        bool blocking = data.movementModifier <= 0f || (data.tags & ObjectTags.StaticObstacle) != 0
                        || inst.type == ObjectType.Rock || inst.type == ObjectType.Oak
                        || inst.type == ObjectType.Spruce || inst.type == ObjectType.Palm;

        var bc = go.GetComponent<BoxCollider2D>();
        if (blocking)
        {
            if (!bc) bc = go.AddComponent<BoxCollider2D>();
            bc.isTrigger = false;
            bc.size = new Vector2(Mathf.Max(0.9f, data.footprint.x) * cs,
                                    Mathf.Max(0.9f, data.footprint.y) * cs);
            bc.offset = new Vector2(bc.size.x * 0.5f, bc.size.y * 0.5f);
        }
        else if (bc) Destroy(bc);
    }
}

public class PooledObjectTag : MonoBehaviour
{
    public ObjectType Type;
    [NonSerialized] public SpriteRenderer SR;
    void Awake() { if (!SR) SR = GetComponentInChildren<SpriteRenderer>(); }
}