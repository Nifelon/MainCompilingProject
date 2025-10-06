// Managers/World/WorldContent/Objects/ObjectHarvestInteractable.cs
using UnityEngine;
using Game.World.Objects;
using Game.Items; // ItemMap, ItemId

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class ObjectHarvestInteractable : MonoBehaviour, IInteractable
{
    public ObjectData Data { get; private set; }
    public Vector2Int Cell { get; private set; }

    [SerializeField] private bool deactivateOnHarvest = false;

    SpriteRenderer _sr;
    Collider2D _col;
    WorldObjectRef _wref;

    bool _taken;
    ObjectType _originalType;

    public void Setup(ObjectData data, Vector2Int cell)
    {
        Data = data;
        Cell = cell;

        if (_sr == null) _sr = GetComponent<SpriteRenderer>();
        if (_col == null) _col = GetComponent<Collider2D>();
        if (_wref == null) _wref = GetComponent<WorldObjectRef>();

        _originalType = _wref ? _wref.type : ObjectType.None;
        _taken = false;

        if (_col) _col.enabled = true;
        if (_sr) _sr.enabled = true;

        enabled = Data != null && Data.harvest.harvestable;
    }

    public string Hint =>
        (Data != null && !string.IsNullOrEmpty(Data.harvest.interactPrompt))
            ? Data.harvest.interactPrompt
            : "E — Взять";

    public void Interact(GameObject actor)
    {
        if (!enabled || _taken || Data == null || !Data.harvest.harvestable) return;
        _taken = true;

        var drops = Data.harvest.harvestDrops;
        if (drops != null)
        {
            for (int i = 0; i < drops.Count; i++)
            {
                var d = drops[i];

                // chance: 0 трактуем как 100% на альфе
                float p = (d.chance <= 0f) ? 1f : Mathf.Clamp01(d.chance);
                if (Random.value > p) continue;

                // безопасный диапазон количества
                int min = d.minCount < d.maxCount ? d.minCount : d.maxCount;
                int max = d.maxCount > d.minCount ? d.maxCount : d.minCount;
                int n = Random.Range(min, max + 1);
                if (n <= 0) continue;

                // 1) Предпочтительно: ItemSO → GUID → ItemId
                if (d.item != null && !string.IsNullOrWhiteSpace(d.item.Guid))
                {
                    if (ItemMap.TryEnumByGuid(d.item.Guid, out var enumId) && ItemMap.IsValid(enumId))
                    {
                        InventoryService.Add(enumId, n);
                    }
                    else
                    {
                        Debug.LogWarning($"[Harvest] No ItemId mapping for {d.item.name} ({d.item.Guid}) on {name} ({Data.type}).");
                    }
                }
                // 2) Фолбэк: легаси enum, только если валиден в ItemMap
                else if (ItemMap.IsValid(d.itemId))
                {
                    InventoryService.Add(d.itemId, n);
                }
                else
                {
                    Debug.LogWarning($"[Harvest] Drop has no valid item reference on {name} ({Data?.type}).");
                }
            }
        }

        // Поведение после сбора
        var toType = Data.harvest.transformToType;
        if (Data.harvest.destroyOnHarvest || toType == ObjectType.None)
        {
            if (deactivateOnHarvest)
            {
                if (_col) _col.enabled = false;
                if (_sr) _sr.enabled = false;
                enabled = false;
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
        else
        {
            if (_wref != null) _wref.type = toType;
            ApplyViewFor(_wref.type);
            if (_col) _col.enabled = false;
            enabled = false;
        }

        // Респавн
        float t = Mathf.Max(0f, Data.harvest.respawnSeconds);
        if (t > 0f) HarvestRespawnScheduler.Schedule(this, Time.time + t);
    }

    void ApplyViewFor(ObjectType type)
    {
        var newData = Data;
        if (_wref != null && Data != null && _wref.type != Data.type)
        {
            newData = ObjectDataDB.Get(_wref.type) ?? Data;
        }

        Sprite spr = null;
        if (newData != null && newData.spriteVariants != null && newData.spriteVariants.Length > 0)
            spr = newData.spriteVariants[0];

        if (_sr != null && spr != null) _sr.sprite = spr;

        if (_col is BoxCollider2D bc && _sr != null && _sr.sprite != null)
        {
            var b = _sr.sprite.bounds;
            bc.size = b.size; bc.offset = b.center;
        }
    }

    internal void RespawnNow()
    {
        _taken = false;
        if (_wref != null) _wref.type = _originalType;
        ApplyViewFor(_wref ? _wref.type : (Data ? Data.type : ObjectType.None));

        if (deactivateOnHarvest)
        {
            if (_sr) _sr.enabled = true;
            if (_col) _col.enabled = true;
            enabled = Data != null && Data.harvest.harvestable;
        }
        else
        {
            if (!gameObject.activeSelf) gameObject.SetActive(true);
            enabled = Data != null && Data.harvest.harvestable;
        }
    }
}
