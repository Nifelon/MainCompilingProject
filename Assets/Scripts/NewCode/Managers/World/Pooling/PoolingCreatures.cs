// PoolingCreatures.cs — визуализация существ через общий NpcPool (с дефолтным префабом)

using System.Collections.Generic;
using UnityEngine;

namespace Game.World.Creatures
{
    [DefaultExecutionOrder(-205)]
    public class PoolingCreatures : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private Transform player;
        [SerializeField] private CreatureSpawner spawner;
        [SerializeField] private NpcPool pool;                 // общий пул
        [SerializeField] private GameObject defaultCreaturePrefab;

        [Header("Streaming")]
        [SerializeField] private int loadRadius = 48;
        [SerializeField] private int unloadRadius = 56;
        [SerializeField] private float cellSize = 1f;

        [Header("Debug")]
        [SerializeField] private bool verbose = false;

        private readonly Dictionary<ulong, GameObject> _active = new();

        private static readonly List<CreatureSpawner.PlannedUnit> s_units = new(256);
        private static readonly HashSet<ulong> s_keep = new();
        private static readonly List<ulong> s_tmp = new(64);

        private void Awake()
        {
            if (!pool) pool = FindFirstObjectByType<NpcPool>(FindObjectsInactive.Exclude);
            if (!spawner) spawner = FindFirstObjectByType<CreatureSpawner>(FindObjectsInactive.Exclude);
            if (unloadRadius < loadRadius) unloadRadius = loadRadius + 8;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!defaultCreaturePrefab)
                Debug.LogWarning("[PoolingCreatures] DefaultCreaturePrefab is NULL — если в профиле нет префаба, спавна не будет.");
#endif
        }

        private void OnDisable()
        {
            s_tmp.Clear(); foreach (var id in _active.Keys) s_tmp.Add(id);
            for (int i = 0; i < s_tmp.Count; i++) Despawn(s_tmp[i]);
            _active.Clear();
        }

        private void Update()
        {
            if (!player || !spawner || !pool) return;
            var center = WorldToCell(player.position);

            spawner.GatherPlannedCreaturesWithinRadius(center, unloadRadius, s_units);

            s_keep.Clear();
            int unload2 = unloadRadius * unloadRadius, load2 = loadRadius * loadRadius;

            for (int i = 0; i < s_units.Count; i++)
            {
                var u = s_units[i];
                var d2 = (u.cell - center).sqrMagnitude;
                if (d2 <= unload2) s_keep.Add(u.id);
            }

            s_tmp.Clear();
            foreach (var kv in _active) if (!s_keep.Contains(kv.Key)) s_tmp.Add(kv.Key);
            for (int i = 0; i < s_tmp.Count; i++) Despawn(s_tmp[i]);

            for (int i = 0; i < s_units.Count; i++)
            {
                var u = s_units[i];
                var d2 = (u.cell - center).sqrMagnitude;
                if (d2 > load2) continue;
                if (_active.ContainsKey(u.id)) continue;
                Spawn(u);
            }

            if (verbose)
                Debug.Log($"[PoolingCreatures] active={_active.Count}, planned={s_units.Count}");
        }

        private void Spawn(CreatureSpawner.PlannedUnit u)
        {
            var prefab = (u.profile && u.profile.prefab) ? u.profile.prefab : defaultCreaturePrefab;
            if (!prefab)
            {
                if (verbose)
                    Debug.LogWarning($"[PoolingCreatures] No prefab/default for {u.id} (profile={(u.profile ? u.profile.name : "null")})");
                return;
            }

            var go = pool.Get(prefab);
            go.transform.SetParent(transform, false);
            go.transform.position = CellToWorld(u.cell);
            SetZByY(go.transform);

            var sr = go.GetComponentInChildren<SpriteRenderer>() ?? go.AddComponent<SpriteRenderer>();

            if (ReferenceEquals(prefab, defaultCreaturePrefab))
            {
                // для дефолтного префаба ВСЕГДА выставляем спрайт по профилю
                if (u.profile && u.profile.icon) sr.sprite = u.profile.icon;
            }
            else
            {
                // у профильного префаба обычно уже есть спрайт; если нет — подстрахуемся и поставим иконку
                if (!sr.sprite && u.profile && u.profile.icon) sr.sprite = u.profile.icon;
            }

            _active[u.id] = go;

            if (verbose)
                Debug.Log($"[PoolingCreatures] Spawn id={u.id} at {u.cell} prefab={(prefab == defaultCreaturePrefab ? "DEFAULT" : prefab.name)}");
        }

        private void Despawn(ulong id)
        {
            if (!_active.TryGetValue(id, out var go) || !go) { _active.Remove(id); return; }

            var key = NpcPool.GetPrefabKey(go) ?? defaultCreaturePrefab;

            // Чистим визуал дефолтного префаба, чтобы не тянуть спрайт между разными профилями
            if (ReferenceEquals(key, defaultCreaturePrefab))
            {
                var sr = go.GetComponentInChildren<SpriteRenderer>();
                if (sr) sr.sprite = null;
            }

            pool.Release(key, go);
            _active.Remove(id);
        }

        private Vector2Int WorldToCell(Vector3 w) => new(Mathf.RoundToInt(w.x / cellSize), Mathf.RoundToInt(w.y / cellSize));
        private Vector3 CellToWorld(Vector2Int c) => new(c.x * cellSize, c.y * cellSize, 0);
        private static void SetZByY(Transform t, float zScale = 0.001f, float baseZ = 0f)
        { var p = t.position; p.z = baseZ - p.y * zScale; t.position = p; }
    }
}
