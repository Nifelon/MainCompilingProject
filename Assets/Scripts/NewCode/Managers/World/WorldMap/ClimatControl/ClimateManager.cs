using System.Collections.Generic;
using System.Linq;
using Game.Core;
using UnityEngine;

namespace Game.World.Map.Climate
{
    public class ClimateManager : MonoBehaviour, IWorldSystem, IClimateService
    {
        [SerializeField] private int order = 200;
        public int Order => order;

        [Header("Путь к SO-зонам (Resources)")]
        [SerializeField] private string climateZonesPath = "ScriptAssets/ClimatZones";

        [Header("Перлин для «растряски» границ")]
        [SerializeField, Range(0.001f, 1f)] private float noiseMinScale = 0.02f;
        [SerializeField, Range(0.001f, 1f)] private float noiseMaxScale = 0.05f;
        [SerializeField, Range(0f, 1f)] private float noiseStrength01 = 0.30f; // 0..1 → ±(strength*50) процентов

        [Header("Пост-обработка")]
        [SerializeField] private int minRegionSize = 5000;

        // Сетка
        private ClimateZoneType[,] _grid;
        private int _w, _h, _ox, _oy;

        // Данные зон
        private Dictionary<ClimateZoneType, ClimateZones> _byType;
        private ClimateZones[] _zonesSorted; // по startPercent

        public bool IsClimateReady { get; private set; }

        // === IWorldSystem ===
        public void Initialize(WorldContext ctx)
        {
            IsClimateReady = false;

            InitGrid(ctx);
            LoadSortValidateZones();
            FillBaseByLatitude(ctx);
            ApplyNoise(ctx);                 // детерминизм от ctx.Seed
            MergeSmallRegions(minRegionSize);

            // сервис для других систем (пригодится NPC и т.п.)
            ctx.Services.Register<IClimateService>(this);

            IsClimateReady = true;
        }

        private void InitGrid(WorldContext ctx)
        {
            var half = ctx.Size;
            _w = half.x * 2 + 1;
            _h = half.y * 2 + 1;
            _ox = half.x;
            _oy = half.y;
            _grid = new ClimateZoneType[_w, _h];
        }

        private void LoadSortValidateZones()
        {
            _byType = new Dictionary<ClimateZoneType, ClimateZones>();
            var all = Resources.LoadAll<ClimateZones>(climateZonesPath);

            for (int i = 0; i < all.Length; i++)
            {
                var z = all[i];
                if (!_byType.ContainsKey(z.zoneType)) _byType.Add(z.zoneType, z);
                else Debug.LogWarning($"[Climate] Дубликат пояса: {z.zoneType}");
            }

            _zonesSorted = _byType.Values.OrderBy(z => z.startPercent).ToArray();

            if (_zonesSorted.Length == 0)
            {
                Debug.LogError("[Climate] Не найдены ассеты зон (Resources/ScriptAssets/ClimatZones).");
                return;
            }

            for (int i = 0; i < _zonesSorted.Length; i++)
            {
                var z = _zonesSorted[i];
                if (z.startPercent < 0f || z.endPercent > 100f || z.startPercent > z.endPercent)
                    Debug.LogError($"[Climate] Диапазон у {z.name}: {z.startPercent}..{z.endPercent} некорректен.");

                if (i < _zonesSorted.Length - 1 && z.endPercent > _zonesSorted[i + 1].startPercent)
                    Debug.LogWarning($"[Climate] Перекрытие зон: {z.zoneType} → {_zonesSorted[i + 1].zoneType}");
            }
        }

        private void FillBaseByLatitude(WorldContext ctx)
        {
            var half = ctx.Size;
            float y2 = half.y * 2f;

            for (int iy = 0; iy < _h; iy++)
            {
                int y = iy - _oy;
                float pct = (y + half.y) / y2 * 100f; // 0..100
                for (int ix = 0; ix < _w; ix++)
                    _grid[ix, iy] = PickZoneByPercentile(pct);
            }
        }

        private void ApplyNoise(WorldContext ctx)
        {
            // детерминированные параметры от seed
            var (offX, offY) = NoiseUtility.MakeOffsetsFromSeed(ctx.Seed, salt: 101);
            float t = NoiseUtility.Hash01(ctx.Seed ^ 0xA3B1);
            float scale = Mathf.Lerp(noiseMinScale, noiseMaxScale, t);
            float strength = noiseStrength01 * 100f;

            var newGrid = new ClimateZoneType[_w, _h];

            var half = ctx.Size;
            float y2 = half.y * 2f;

            for (int iy = 0; iy < _h; iy++)
            {
                int y = iy - _oy;
                float basePct = (y + half.y) / y2 * 100f;

                for (int ix = 0; ix < _w; ix++)
                {
                    int x = ix - _ox;

                    float n = NoiseUtility.Perlin(x, y, scale, offX, offY); // 0..1
                    float pct = Mathf.Clamp(basePct + (n - 0.5f) * strength, 0f, 100f);

                    newGrid[ix, iy] = PickZoneByPercentile(pct);
                }
            }

            _grid = newGrid;
        }

        private void MergeSmallRegions(int minSize)
        {
            var visited = new bool[_w, _h];

            for (int iy = 0; iy < _h; iy++)
            {
                for (int ix = 0; ix < _w; ix++)
                {
                    if (visited[ix, iy]) continue;

                    var zt = _grid[ix, iy];
                    var region = new List<(int x, int y)>(64);
                    var q = new Queue<(int x, int y)>();
                    visited[ix, iy] = true; q.Enqueue((ix, iy));

                    while (q.Count > 0)
                    {
                        var (cx, cy) = q.Dequeue();
                        region.Add((cx, cy));

                        if (cy + 1 < _h && !visited[cx, cy + 1] && _grid[cx, cy + 1] == zt) { visited[cx, cy + 1] = true; q.Enqueue((cx, cy + 1)); }
                        if (cy - 1 >= 0 && !visited[cx, cy - 1] && _grid[cx, cy - 1] == zt) { visited[cx, cy - 1] = true; q.Enqueue((cx, cy - 1)); }
                        if (cx + 1 < _w && !visited[cx + 1, cy] && _grid[cx + 1, cy] == zt) { visited[cx + 1, cy] = true; q.Enqueue((cx + 1, cy)); }
                        if (cx - 1 >= 0 && !visited[cx - 1, cy] && _grid[cx - 1, cy] == zt) { visited[cx - 1, cy] = true; q.Enqueue((cx - 1, cy)); }
                    }

                    if (region.Count < minSize)
                    {
                        var counts = new Dictionary<ClimateZoneType, int>(4);
                        for (int i = 0; i < region.Count; i++)
                        {
                            var (rx, ry) = region[i];
                            CountNeighbor(rx, ry + 1, zt, counts);
                            CountNeighbor(rx, ry - 1, zt, counts);
                            CountNeighbor(rx + 1, ry, zt, counts);
                            CountNeighbor(rx - 1, ry, zt, counts);
                        }

                        if (counts.Count > 0)
                        {
                            ClimateZoneType dom = zt; int best = -1;
                            foreach (var kv in counts) if (kv.Value > best) { best = kv.Value; dom = kv.Key; }
                            for (int i = 0; i < region.Count; i++) _grid[region[i].x, region[i].y] = dom;
                        }
                    }
                }
            }
        }

        private void CountNeighbor(int nx, int ny, ClimateZoneType regionType, Dictionary<ClimateZoneType, int> counts)
        {
            if (nx < 0 || ny < 0 || nx >= _w || ny >= _h) return;
            var t = _grid[nx, ny];
            if (t == regionType) return;
            counts[t] = counts.TryGetValue(t, out var c) ? c + 1 : 1;
        }

        private ClimateZoneType PickZoneByPercentile(float p)
        {
            if (_zonesSorted == null || _zonesSorted.Length == 0) return ClimateZoneType.none;
            p = Mathf.Clamp(p, 0f, 100f);

            int n = _zonesSorted.Length;
            for (int i = 0; i < n - 1; i++)
            {
                var z = _zonesSorted[i];
                if (p >= z.startPercent && p < z.endPercent) return z.zoneType;
            }
            var last = _zonesSorted[n - 1];
            if (p >= last.startPercent && p <= last.endPercent) return last.zoneType;

            return ClimateZoneType.none;
        }

        // === Публичное API (как договорились) ===
        public ClimateZoneType GetClimateZoneAtPosition(Vector2Int pos)
        {
            int ix = pos.x + _ox, iy = pos.y + _oy;
            if ((uint)ix >= (uint)_w || (uint)iy >= (uint)_h) return ClimateZoneType.none;
            return _grid[ix, iy];
        }

        public ClimateZones GetClimateZoneData(ClimateZoneType type)
        {
            return (_byType != null && _byType.TryGetValue(type, out var z)) ? z : null;
        }

        // временно для UI
        public Color GetClimateColorAtPosition(Vector2Int pos)
        {
            var z = GetClimateZoneAtPosition(pos);
            var data = GetClimateZoneData(z);
            return data != null ? data.zoneColor : Color.white;
        }
    }
}