using System.Collections.Generic;
using UnityEngine;
using Game.World.Objects;
using UnityEngine.Tilemaps;

namespace Game.World.Objects
{
    /// Обёртка над PoolManagerObjects: спаун/деспаун + установка спрайта/сортировки/коллайдера + harvest-инициализация.
    [DefaultExecutionOrder(-205)]
    public sealed class ObjectViewPoolAdapter : MonoBehaviour, IObjectView
    {
        [Header("Pool")]
        [SerializeField] private PoolManagerObjects pool;

        [Header("Database")]
        [Tooltip("Список данных объектов (ObjectData) для маппинга type -> спрайты/футпринт/harvest.")]
        [SerializeField] private List<ObjectData> objectDatabase = new();

        [Header("Rendering")]
        [SerializeField] private string sortingLayerName = "Objects";
        [SerializeField] private bool ySort = true;
        [Tooltip("Во сколько раз умножать -Y при расчёте sortingOrder")]
        [SerializeField] private int ySortMul = 10;

        [Header("Collider")]
        [SerializeField] private bool autoFitCollider = true;
        [Tooltip("Capsule по вертикали для «высоких» спрайтов, иначе BoxCollider2D")]
        [SerializeField] private bool preferCapsuleForTallSprites = true;
        [Tooltip("Считать спрайт «высоким», если высота/ширина ≥ этого порога")]
        [SerializeField] private float tallAspectThreshold = 1.4f;

        [Header("Grid")]
        [Tooltip("Размер клетки в мировых координатах (для передачи cell в harvest).")]
        [SerializeField] private float cellSize = 1f;

        // cache
        private readonly Dictionary<ObjectType, ObjectData> _byType = new();
        private int _sortingLayerId;

        void Awake()
        {
            _byType.Clear();
            foreach (var od in objectDatabase) if (od) _byType[od.type] = od;
            _sortingLayerId = SortingLayer.NameToID(sortingLayerName);

            if (!pool)
                pool = FindFirstObjectByType<PoolManagerObjects>(FindObjectsInactive.Exclude);

            if (!pool)
                Debug.LogWarning("[ObjectViewPoolAdapter] PoolManagerObjects is not set.");
        }

        public ObjectHandle Spawn(ObjectInstanceData inst)
        {
            // 1) Получить GO из пула по типу
            GameObject go = pool ? pool.Get(inst.type) : new GameObject($"Object_{inst.type}");
            if (!go) return default;

            // 2) Позиция и «Z по Y» (если нужно)
            var t = go.transform;
            t.position = new Vector3(inst.worldPos.x, inst.worldPos.y, t.position.z);
            if (ySort) SetZByY(t);

            // 3) Рендерер и спрайт-вариант
            var sr = go.GetComponentInChildren<SpriteRenderer>();
            if (!sr) sr = go.AddComponent<SpriteRenderer>();

            if (_byType.TryGetValue(inst.type, out var data) && data.spriteVariants != null && data.spriteVariants.Length > 0)
            {
                int vi = Mathf.Clamp(inst.variantIndex, 0, data.spriteVariants.Length - 1);
                sr.sprite = data.spriteVariants[vi];
            }

            // 4) Сортировка
            sr.sortingLayerID = _sortingLayerId;
            sr.sortingOrder = ySort ? Mathf.RoundToInt(-t.position.y * ySortMul) : 0;

            // 5) Коллайдер (подгон под спрайт)
            if (autoFitCollider)
                FitCollider(go, sr);

            // 6) Тег-ссылка (удобно для поиска по id/type на объекте)
            var wref = go.GetComponent<WorldObjectRef>() ?? go.AddComponent<WorldObjectRef>();
            wref.id = inst.id;
            wref.type = inst.type;

            // 7) Harvest: включить/настроить по данным
            var hi = go.GetComponent<ObjectHarvestInteractable>() ?? go.AddComponent<ObjectHarvestInteractable>();

            if (_byType.TryGetValue(inst.type, out var od) && od != null && od.harvest.harvestable)
            {
                // вычисляем клетку (если нужна логике harvest)
                Vector2Int cell = WorldToCell(inst.worldPos, cellSize);
                hi.Setup(od, cell);
                hi.enabled = true;

                // интерактив — делаем коллайдер триггером
                var col = go.GetComponent<Collider2D>();
                if (col) col.isTrigger = true;
            }
            else
            {
                hi.enabled = false;
                // если не интерактив — можно вернуть isTrigger в false (по желанию):
                var col = go.GetComponent<Collider2D>();
                if (col && !(col is TilemapCollider2D)) col.isTrigger = false;
            }

            return new ObjectHandle(inst.id, go);
        }

        public void Despawn(ObjectHandle handle)
        {
            if (!handle.IsValid) return;
            if (pool) pool.Release(handle.Go);
            else Destroy(handle.Go);
        }

        public Bounds GetWorldBounds(ObjectHandle handle)
        {
            if (!handle.IsValid) return new Bounds(Vector3.zero, Vector3.zero);
            var sr = handle.Go.GetComponentInChildren<SpriteRenderer>();
            if (sr && sr.sprite) return sr.bounds;
            return new Bounds(handle.Go.transform.position, Vector3.one * 0.25f);
        }

        // ------- helpers --------

        private static Vector2Int WorldToCell(Vector2 worldPos, float size)
        {
            if (size <= 0f) size = 1f;
            return new Vector2Int(Mathf.FloorToInt(worldPos.x / size), Mathf.FloorToInt(worldPos.y / size));
        }

        private static void SetZByY(Transform t)
        {
            var p = t.position;
            // простой вариант: чем ниже по Y, тем "выше" в Z (чтобы не мерцало)
            t.position = new Vector3(p.x, p.y, p.y * 0.001f);
        }

        private void FitCollider(GameObject go, SpriteRenderer sr)
        {
            if (!sr || !sr.sprite) return;

            var b = sr.sprite.bounds; // локальные координаты рендера
            var scale = go.transform.lossyScale;
            var aspect = b.size.y / Mathf.Max(0.0001f, b.size.x);

            if (preferCapsuleForTallSprites && aspect >= tallAspectThreshold)
            {
                var cap = go.GetComponent<CapsuleCollider2D>() ?? go.AddComponent<CapsuleCollider2D>();
                cap.direction = CapsuleDirection2D.Vertical;
                cap.size = new Vector2(b.size.x * scale.x, b.size.y * scale.y);
                cap.offset = new Vector2(b.center.x * scale.x, b.center.y * scale.y);

                var bc = go.GetComponent<BoxCollider2D>();
                if (bc) bc.enabled = false;
            }
            else
            {
                var bc = go.GetComponent<BoxCollider2D>() ?? go.AddComponent<BoxCollider2D>();
                bc.size = new Vector2(b.size.x * scale.x, b.size.y * scale.y);
                bc.offset = new Vector2(b.center.x * scale.x, b.center.y * scale.y);

                var cc = go.GetComponent<CapsuleCollider2D>();
                if (cc) cc.enabled = false;
            }
        }
    }
}
