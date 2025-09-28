// PoolingCreatures.cs Ч визуализаци€ существ через общий NpcPool
// ∆дЄм, пока CreatureSpawner.IsReady == true, и только после этого начинаем спавнить.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.World.Creatures
{
    [DefaultExecutionOrder(0)] // после систем и после спавнера
    public class PoolingCreatures : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private Transform player;
        [SerializeField] private CreatureSpawner spawner;
        [SerializeField] private NpcPool pool;
        [SerializeField] private GameObject defaultCreaturePrefab;

        [Header("Streaming")]
        [SerializeField] private int loadRadius = 48;
        [SerializeField] private int unloadRadius = 56;
        [SerializeField] private float cellSize = 1f;

        [Header("Lifecycle")]
        [SerializeField, Min(0)] private int warmupFrames = 2;  // даЄм миру ещЄ пару кадров на отрисовку клеток

        [Header("Debug")]
        [SerializeField] private bool verbose = false;

        private readonly Dictionary<ulong, GameObject> _active = new();
        private static readonly List<CreatureSpawner.PlannedUnit> s_units = new(256);
        private static readonly HashSet<ulong> s_keep = new();
        private static readonly List<ulong> s_tmp = new(64);

        private bool _running;

        private void Awake()
        {
            if (!pool) pool = FindFirstObjectByType<NpcPool>(FindObjectsInactive.Exclude);
            if (!spawner) spawner = FindFirstObjectByType<CreatureSpawner>(FindObjectsInactive.Exclude);
            if (unloadRadius < loadRadius) unloadRadius = loadRadius + 8;

            StartCoroutine(Co_WaitAndRun());
        }

        private IEnumerator Co_WaitAndRun()
        {
            // ждЄм несколько кадров + готовность спавнера
            for (int i = 0; i < warmupFrames; i++) yield return null;
            while (spawner != null && !spawner.IsReady) yield return null;
            _running = true;
        }

        private void OnDisable()
        {
            s_tmp.Clear(); foreach (var id in _active.Keys) s_tmp.Add(id);
            for (int i = 0; i < s_tmp.Count; i++) Despawn(s_tmp[i]);
            _active.Clear();
            _running = false;
        }

        private void Update()
        {
            if (!_running || !player || !spawner || !pool) return;

            var center = WorldToCell(player.position);
            spawner.GatherPlannedCreaturesWithinRadius(center, unloadRadius, s_units);

            s_keep.Clear();
            int unload2 = unloadRadius * unloadRadius, load2 = loadRadius * loadRadius;

            for (int i = 0; i < s_units.Count; i++)
            {
                var u = s_units[i];
                if ((u.cell - center).sqrMagnitude <= unload2) s_keep.Add(u.id);
            }

            s_tmp.Clear();
            foreach (var kv in _active) if (!s_keep.Contains(kv.Key)) s_tmp.Add(kv.Key);
            for (int i = 0; i < s_tmp.Count; i++) Despawn(s_tmp[i]);

            for (int i = 0; i < s_units.Count; i++)
            {
                var u = s_units[i];
                if ((u.cell - center).sqrMagnitude > load2) continue;
                if (_active.ContainsKey(u.id)) continue;
                Spawn(u);
            }

            if (verbose) Debug.Log($"[PoolingCreatures] active={_active.Count}, planned={s_units.Count}");
        }

        private void Spawn(CreatureSpawner.PlannedUnit u)
        {
            var prefab = (u.profile && u.profile.prefab) ? u.profile.prefab : defaultCreaturePrefab;
            if (!prefab)
            {
                if (verbose) Debug.LogWarning($"[PoolingCreatures] No prefab/default for {u.id} (profile={(u.profile ? u.profile.name : "null")})");
                return;
            }

            var go = pool.Get(prefab);
            go.transform.SetParent(transform, false);
            go.transform.position = CellToWorld(u.cell);
            SetZByY(go.transform);

            var sr = go.GetComponentInChildren<SpriteRenderer>() ?? go.AddComponent<SpriteRenderer>();
            if (ReferenceEquals(prefab, defaultCreaturePrefab))
            {
                if (u.profile && u.profile.icon) sr.sprite = u.profile.icon; // всегда перезаписываем дл€ дефолта
            }
            else
            {
                if (!sr.sprite && u.profile && u.profile.icon) sr.sprite = u.profile.icon;
            }

            _active[u.id] = go;
        }

        private void Despawn(ulong id)
        {
            if (!_active.TryGetValue(id, out var go) || !go) { _active.Remove(id); return; }
            var key = NpcPool.GetPrefabKey(go) ?? defaultCreaturePrefab;

            if (ReferenceEquals(key, defaultCreaturePrefab))
            {
                var sr = go.GetComponentInChildren<SpriteRenderer>();
                if (sr) sr.sprite = null; // чистим следы профил€
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
