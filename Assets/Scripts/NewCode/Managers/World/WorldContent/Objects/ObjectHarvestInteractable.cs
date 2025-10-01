// World/WorldContent/Objects/ObjectHarvestInteractable.cs
using UnityEngine;
using Game.World.Objects;

[RequireComponent(typeof(Collider2D))]
public class ObjectHarvestInteractable : MonoBehaviour, IInteractable
{
    [Header("Data")]
    public ObjectData data;        // присваивается из вью-адаптера
    public Vector2Int cell;        // координата клетки (если нужна логике)

    [Header("Items")]
    [SerializeField] private ItemDatabase itemDatabase; // назначь в инспекторе

    public string Hint =>
        !string.IsNullOrEmpty(data?.harvest.interactPrompt)
            ? data.harvest.interactPrompt
            : "E — Взять";

    private bool _taken;
    private ObjectType _originalType;

    private void Awake()
    {
        // Запомним исходный тип для корректного восстановления при респавне
        var wref = GetComponent<WorldObjectRef>();
        _originalType = wref ? wref.type : ObjectType.None;
    }

    public void Interact(GameObject actor)
    {
        if (_taken || data?.harvest == null || !data.harvest.harvestable)
            return;

        _taken = true;

        // 1) Выдать дропы
        var drops = data.harvest.harvestDrops;
        if (drops != null && drops.Count > 0)
        {
            for (int i = 0; i < drops.Count; i++)
            {
                var d = drops[i];
                if (string.IsNullOrEmpty(d.itemId)) continue;

                // безопасный диапазон
                int min = Mathf.Min(d.minCount, d.maxCount);
                int max = Mathf.Max(d.minCount, d.maxCount);
                int n = Random.Range(min, max + 1);
                if (n <= 0) continue;

                // 1) пробуем как enum: "Berry", "Skin"...
                if (System.Enum.TryParse<ItemId>(d.itemId, true, out var eid))
                {
                    InventoryService.Add(eid, n);
                }
                // 2) или через базу по имени ассета (если имя в SO ≠ имени enum)
                else if (itemDatabase != null)
                {
                    InventoryService.TryAddByName(d.itemId, n, itemDatabase);
                }
                else
                {
                    Debug.LogWarning($"[Harvest] Unknown item '{d.itemId}' and no ItemDatabase assigned.");
                }
            }
        }

        // 2) Поведение после сбора
        var toType = data.harvest.transformToType;
        if (data.harvest.destroyOnHarvest || toType == ObjectType.None)
        {
            // На альфу — просто скрываем/депуллим
            gameObject.SetActive(false);
        }
        else
        {
            // Трансформация, напр.: BerryBush -> Bush
            var wref = GetComponent<WorldObjectRef>();
            if (wref != null) wref.type = toType;

            // Деактивируем — при повторной активации твой вью-адаптер подтянет спрайт/коллайдер по новому типу
            gameObject.SetActive(false);
        }

        // 3) Респавн (на альфу обычно 0 — выкл)
        float t = Mathf.Max(0f, data.harvest.respawnSeconds);
        if (t > 0f) StartCoroutine(CoRespawn(t));
    }

    private System.Collections.IEnumerator CoRespawn(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Вернуть исходный тип, если до этого трансформировали
        var wref = GetComponent<WorldObjectRef>();
        if (wref != null) wref.type = _originalType;

        _taken = false;
        gameObject.SetActive(true); // адаптер на OnEnable/активации подтянет визуал и коллайдер
    }
}
