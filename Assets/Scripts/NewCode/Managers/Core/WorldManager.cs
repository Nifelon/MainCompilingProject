using UnityEngine;
using Game.Core;
using Game.World.Content;
using Stopwatch = System.Diagnostics.Stopwatch;

[DefaultExecutionOrder(-900)]
public class WorldManager : MonoBehaviour
{
    [Header("Матрешки мира (проставь в инспекторе)")]
    [SerializeField] private WorldMapManager map;
    [SerializeField] private WorldContentManager content;

    [Header("Источник правды (Size/Seed)")]
    [SerializeField] private Vector2Int sizeMap = new(256, 256);
    [SerializeField] private int seed = 12345;

    [Header("PlayerPrefs")]
    [SerializeField] private bool usePlayerPrefs = true;
    [SerializeField] private string prefsKeyPrefix = "World"; // World_Seed, World_SizeX, World_SizeY

    public Vector2Int SizeMap
    {
        get => sizeMap;
        set => sizeMap = new Vector2Int(Mathf.Max(1, value.x), Mathf.Max(1, value.y));
    }
    public int Seed { get => seed; set => seed = value; }
    public int Width => sizeMap.x * 2 + 1;
    public int Height => sizeMap.y * 2 + 1;

    public bool IsWorldReady { get; private set; }
    public long LastBuildMs { get; private set; } // время последней сборки

    private Vector2Int _lastSize;
    private int _lastSeed;
    private bool _hasBuilt;

    private void Awake()
    {
        GlobalCore.Instance?.Register(this);
    }

    // === Save/Load конфигурации ===
    public void LoadFromPlayerPrefs()
    {
        if (!usePlayerPrefs) return;
        if (PlayerPrefs.HasKey(prefsKeyPrefix + "_Seed"))
        {
            seed = PlayerPrefs.GetInt(prefsKeyPrefix + "_Seed", seed);
            int sx = PlayerPrefs.GetInt(prefsKeyPrefix + "_SizeX", sizeMap.x);
            int sy = PlayerPrefs.GetInt(prefsKeyPrefix + "_SizeY", sizeMap.y);
            SizeMap = new Vector2Int(Mathf.Max(1, sx), Mathf.Max(1, sy));
            Debug.Log($"[WorldManager] Loaded from prefs: Seed={seed}, Size={SizeMap}");
        }
    }

    private void SaveToPlayerPrefs()
    {
        if (!usePlayerPrefs) return;
        PlayerPrefs.SetInt(prefsKeyPrefix + "_Seed", seed);
        PlayerPrefs.SetInt(prefsKeyPrefix + "_SizeX", sizeMap.x);
        PlayerPrefs.SetInt(prefsKeyPrefix + "_SizeY", sizeMap.y);
        PlayerPrefs.Save();
    }

    // === Основная сборка ===
    public void InitializeWorld(bool force = false)
    {
        // 1) анти-дубль
        if (!force && _hasBuilt && _lastSeed == seed && _lastSize == sizeMap)
        {
            Debug.Log("[WorldManager] InitializeWorld skipped — same Seed/Size.");
            return;
        }

        // 2) мир НЕ готов
        IsWorldReady = false;

        // 3) валидация источника правды
        if (!ValidateSourceOfTruth())
            return;

        // 4) контекст
        var ctx = new WorldContext(this);

        // 5) тайминг
        var sw = Stopwatch.StartNew();

        // 6) матрёшка карты
        if (map != null) map.Initialize(ctx);
        else Debug.LogWarning("[WorldManager] WorldMapManager НЕ назначен.");

        // 7) матрёшка контента
        if (content != null) content.Initialize(ctx);
        else Debug.LogWarning("[WorldManager] WorldContentManager НЕ назначен.");

        // 8) лог/время
        sw.Stop();
        LastBuildMs = sw.ElapsedMilliseconds;
        Debug.Log($"[WorldManager] InitializeWorld done in {LastBuildMs} ms. Map: {Width}x{Height}, Seed: {seed}");

        // 9) кешируем конфиг
        _lastSeed = seed;
        _lastSize = sizeMap;
        _hasBuilt = true;

        // 10) мир готов + сохраняем конфиг
        IsWorldReady = true;
        SaveToPlayerPrefs();
    }

    // Всегда пересобираем (даже если Seed/Size не менялись)
    public void RebuildWorld(int? newSeed = null, Vector2Int? newHalfSize = null)
    {
        if (newSeed.HasValue) seed = newSeed.Value;
        if (newHalfSize.HasValue) SizeMap = newHalfSize.Value;
        InitializeWorld(force: true); // ← важное отличие
    }

    private bool ValidateSourceOfTruth()
    {
        if (sizeMap.x < 1 || sizeMap.y < 1)
        {
            Debug.LogError("[WorldManager] SizeMap должен быть >= (1,1) (половинные размеры).");
            return false;
        }
        return true;
    }
    private void OnValidate()
    {
        sizeMap = new Vector2Int(Mathf.Max(1, sizeMap.x), Mathf.Max(1, sizeMap.y));
    }
}