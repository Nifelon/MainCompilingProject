// PoolingNPC.cs — streaming of NPCs around the player using external NpcPool
// - Reads data-only NPC plan from CampManager (PlannedNpc)
// - Spawns/despawns via NpcPool with default prefab fallback
// - Applies visual (sprite) from NPCProfile if available

using System.Collections.Generic;
using UnityEngine;
using Game.World.NPC;

[DefaultExecutionOrder(-205)]
public class PoolingNPC : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform player;
    [SerializeField] private CampManager campManager;
    [SerializeField] private NPCService npcService;   // optional; will FindFirstObjectByType if null
    [SerializeField] private NpcPool npcPool;         // external pool component

    [Header("Prefabs")]
    [Tooltip("Общий дефолтный префаб для NPC, если у профиля нет своего префаба.")]
    [SerializeField] private GameObject defaultNpcPrefab;

    [Header("Streaming (cells)")]
    [SerializeField, Min(1)] private int loadRadius = 48;    // радиус появления NPC
    [SerializeField, Min(1)] private int unloadRadius = 56;  // радиус исчезновения (гистерезис)
    [SerializeField] private float cellSize = 1f;            // 1 клетка = 1 юнит

    [Header("Debug")]
    [SerializeField] private bool verboseLogs = false;

    private readonly Dictionary<ulong, GameObject> _active = new(); // plannedId -> instance
    private Vector2Int _lastCenter;

    // temp buffers to minimize allocations
    private static readonly List<CampManager.PlannedNpc> s_planned = new(256);
    private static readonly HashSet<ulong> s_shouldKeep = new();
    private static readonly List<ulong> s_tmpIds = new(128);

    private void Awake()
    {

        if (!npcService) npcService = FindFirstObjectByType<NPCService>(FindObjectsInactive.Exclude);
        if (!campManager) campManager = FindFirstObjectByType<CampManager>(FindObjectsInactive.Exclude);
        if (!npcPool) npcPool = FindFirstObjectByType<NpcPool>(FindObjectsInactive.Exclude);

        if (unloadRadius < loadRadius)
            unloadRadius = loadRadius + 8; // минимальный гистерезис, чтобы не мигало на границе
    }

    private void OnDisable()
    {
        // вернуть всё в пул при выключении
        s_tmpIds.Clear();
        foreach (var id in _active.Keys) s_tmpIds.Add(id);
        for (int i = 0; i < s_tmpIds.Count; i++) Despawn(s_tmpIds[i]);
        _active.Clear();
    }

    private void Update()
    {
        if (!player || !campManager || !npcPool) return;

        var center = WorldToCell(player.position);
        if (verboseLogs && center != _lastCenter)
        {
            _lastCenter = center;
            Debug.Log($"[PoolingNPC] center={center} active={_active.Count}");
        }

        // 1) Запрос: все плановые NPC в радиусе UNLOAD
        campManager.GatherPlannedNpcsWithinRadius(center, unloadRadius, s_planned);

        // 2) Кого держать (<= UNLOAD)?
        s_shouldKeep.Clear();
        int unloadR2 = unloadRadius * unloadRadius;
        int loadR2 = loadRadius * loadRadius;

        for (int i = 0; i < s_planned.Count; i++)
        {
            var p = s_planned[i];
            var d2 = (p.cell - center).sqrMagnitude;
            if (d2 <= unloadR2)
                s_shouldKeep.Add(p.id);
        }

        // 3) Деспавним всё, что вышло за UNLOAD
        s_tmpIds.Clear();
        foreach (var kv in _active)
            if (!s_shouldKeep.Contains(kv.Key)) s_tmpIds.Add(kv.Key);
        for (int i = 0; i < s_tmpIds.Count; i++) Despawn(s_tmpIds[i]);

        // 4) Спавним тех, кто <= LOAD и ещё не активен
        for (int i = 0; i < s_planned.Count; i++)
        {
            var p = s_planned[i];
            var d2 = (p.cell - center).sqrMagnitude;
            if (d2 > loadR2) continue;
            if (_active.ContainsKey(p.id)) continue;
            Spawn(p);
        }
    }

    private void Spawn(CampManager.PlannedNpc p)
    {
        var prefab = (p.profile && p.profile.prefab) ? p.profile.prefab : defaultNpcPrefab;
        if (!prefab)
        {
            if (verboseLogs)
                Debug.LogWarning($"[PoolingNPC] No prefab and no default for NPC id={p.id}. Skipped.");
            return;
        }

        var go = npcPool.Get(prefab);
        go.transform.SetParent(transform, false);
        go.transform.position = CellToWorld(p.cell);
        go.name = p.profile.id;
        SetZByY(go.transform);

        // агент логики
        var agent = go.GetComponent<NPCAgent>();
        if (!agent) agent = go.AddComponent<NPCAgent>();
        agent.Init(p.profile, p.campId);

        // визуал
        ApplyNpcAppearance(go, p.profile, usedDefaultPrefab: (prefab == defaultNpcPrefab));

        _active[p.id] = go;

        if (verboseLogs)
            Debug.Log($"[PoolingNPC] Spawn id={p.id} at {p.cell} prefab={(prefab == defaultNpcPrefab ? "DEFAULT" : prefab.name)}");
    }

    private void Despawn(ulong id)
    {
        if (!_active.TryGetValue(id, out var go) || !go)
        {
            _active.Remove(id); return;
        }
        var prefabKey = NpcPool.GetPrefabKey(go) ?? defaultNpcPrefab; // подстраховка
        npcPool.Release(prefabKey, go);
        _active.Remove(id);
    }

    private void ApplyNpcAppearance(GameObject go, NPCProfile profile, bool usedDefaultPrefab)
    {
        if (!profile) return;

        // Специализированный апплаер — если присутсвует в проекте
        var applier = go.GetComponent<NpcVisualApplier>();
        if (applier) { applier.Apply(profile); return; }

        // Базовый: ставим спрайт
        var sr = go.GetComponentInChildren<SpriteRenderer>() ?? go.AddComponent<SpriteRenderer>();

        // 1) Из скина
        if (profile.skin && profile.skin.baseSprite)
        {
            sr.sprite = profile.skin.baseSprite;
            return;
        }

        // 2) Из референс-префаба профиля
        if (profile.prefab)
        {
            var refSr = profile.prefab.GetComponentInChildren<SpriteRenderer>();
            if (refSr && refSr.sprite)
            {
                sr.sprite = refSr.sprite;
                return;
            }
        }

        // 3) Иначе остаётся спрайт из дефолтного префаба
    }

    private Vector2Int WorldToCell(Vector3 w) => new(Mathf.RoundToInt(w.x / cellSize), Mathf.RoundToInt(w.y / cellSize));
    private Vector3 CellToWorld(Vector2Int c) => new(c.x * cellSize, c.y * cellSize, 0);

    private static void SetZByY(Transform t, float zScale = 0.001f, float baseZ = 0f)
    {
        var p = t.position;
        p.z = baseZ - p.y * zScale;
        t.position = p;
    }
}
