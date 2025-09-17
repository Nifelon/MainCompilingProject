using System.Collections.Generic;
using Game.Core;
using Game.World.Map.Climate;
using UnityEngine;

namespace Game.World.Map.Biome
{
    public class BiomeManager : MonoBehaviour, IWorldSystem, IBiomeService
    {
        [SerializeField] private int order = 210;
        public int Order => order;

        [Header("Путь к SO биомов (Resources)")]
        [SerializeField] private string biomesPath = "ScriptAssets/Biomes";

        [Header("Шум для выбора внутри климат-зоны")]
        [SerializeField, Range(0.001f, 0.1f)] private float perlinScale = 0.015f; // ≈0.012–0.02

        // Сетка биомов
        private BiomeType[,] _grid;
        private int _w, _h, _ox, _oy;

        // Данные биомов (для временной карты)
        private readonly Dictionary<BiomeType, BiomeData> _biomeData = new();

        private IClimateService _climate;
        public bool IsBiomesReady { get; private set; }

        // === IWorldSystem ===
        public void Initialize(WorldContext ctx)
        {
            IsBiomesReady = false;

            // размер/смещения
            var half = ctx.Size;
            _w = half.x * 2 + 1; _h = half.y * 2 + 1;
            _ox = half.x; _oy = half.y;
            _grid = new BiomeType[_w, _h];

            // зависимости
            _climate = ctx.GetService<IClimateService>();
            if (_climate == null || !_climate.IsClimateReady)
            {
                Debug.LogError("[Biome] IClimateService не готов — проверь порядок инициализации.");
                return;
            }

            // данные биомов (для цвета и валидаций наличия)
            LoadAllBiomes();

            // валидация climate → biome-chances (покрытие [0..1) в каждой зоне + наличие defaultBiome)
            ValidateBiomeChances();

            // генерация одним проходом
            GenerateBiomesWithPerlinNoise(ctx);

            // лог статистики
            LogBiomeStats();

            // сервис наружу
            ctx.Services.Register<IBiomeService>(this);
            IsBiomesReady = true;
        }

        private void LoadAllBiomes()
        {
            _biomeData.Clear();
            var all = Resources.LoadAll<BiomeData>(biomesPath);
            for (int i = 0; i < all.Length; i++)
            {
                var b = all[i];
                if (!_biomeData.ContainsKey(b.Type)) _biomeData.Add(b.Type, b);
                else Debug.LogWarning($"[Biome] Дубликат BiomeData для {b.Type}");
            }
        }

        private void ValidateBiomeChances()
        {
            // пробегаем все зоны из климата
            int warnedMissing = 0, warnedCoverage = 0;

            // у нас нет общей коллекции всех зон, поэтому пройдёмся по enum'у (или можно взять из ассетов опять)
            foreach (ClimateZoneType zt in System.Enum.GetValues(typeof(ClimateZoneType)))
            {
                if (zt == ClimateZoneType.none) continue;
                var z = _climate.GetClimateZoneData(zt);
                if (z == null) continue;

                // проверяем наличие defaultBiome'а в BiomeData
                if (!_biomeData.ContainsKey(z.defaultBiome))
                {
                    warnedMissing++;
                    Debug.LogWarning($"[Biome] Нет BiomeData для defaultBiome {z.defaultBiome} (зона {zt})");
                }

                // проверка покрытия [0..1)
                if (z.biomeChances == null || z.biomeChances.Count == 0)
                {
                    warnedCoverage++;
                    Debug.LogWarning($"[Biome] В зоне {zt} список biomeChances пуст — будет всегда defaultBiome.");
                    continue;
                }

                z.biomeChances.Sort((a, b) => a.minPerlinValue.CompareTo(b.minPerlinValue));
                float cursor = 0f;

                for (int i = 0; i < z.biomeChances.Count; i++)
                {
                    var c = z.biomeChances[i];
                    if (c.minPerlinValue > cursor + 1e-6f)
                    {
                        warnedCoverage++;
                        Debug.LogWarning($"[Biome] Дыра {cursor:F2}..{c.minPerlinValue:F2} в зоне {zt}");
                    }
                    if (c.minPerlinValue < cursor - 1e-6f)
                    {
                        warnedCoverage++;
                        Debug.LogWarning($"[Biome] Перекрытие около {c.minPerlinValue:F2} в зоне {zt}");
                    }
                    if (c.maxPerlinValue <= c.minPerlinValue)
                    {
                        warnedCoverage++;
                        Debug.LogWarning($"[Biome] Неверный интервал {c.minPerlinValue:F2}..{c.maxPerlinValue:F2} в зоне {zt}");
                    }
                    cursor = Mathf.Max(cursor, c.maxPerlinValue);

                    // проверка наличия BiomeData для целевого биома
                    if (!_biomeData.ContainsKey(c.biome))
                    {
                        warnedMissing++;
                        Debug.LogWarning($"[Biome] Нет BiomeData для {c.biome} (используется в зоне {zt})");
                    }
                }

                if (cursor < 1f - 1e-6f)
                {
                    warnedCoverage++;
                    Debug.LogWarning($"[Biome] Зона {zt} не покрывает [0..1): до {cursor:F2}");
                }
            }

            if (warnedMissing == 0 && warnedCoverage == 0)
                Debug.Log("[Biome] Валидация биомов ОК");
        }

        private void GenerateBiomesWithPerlinNoise(WorldContext ctx)
        {
            // детерминированный перлин от seed (отдельная соль, чтобы не совпадал с климатом)
            var (offX, offY) = NoiseUtility.MakeOffsetsFromSeed(ctx.Seed, salt: 202);
            float scale = Mathf.Max(0.0001f, perlinScale);

            int w = _w, h = _h, ox = _ox, oy = _oy;
            var grid = _grid; // локальная ссылка

            var pos = new Vector2Int();

            for (int iy = 0; iy < h; iy++)
            {
                int y = iy - oy;
                pos.y = y;

                for (int ix = 0; ix < w; ix++)
                {
                    int x = ix - ox;
                    pos.x = x;

                    // читаем климат
                    var zt = _climate.GetClimateZoneAtPosition(pos);
                    var z = _climate.GetClimateZoneData(zt);

                    // перлин 0..1
                    float p = NoiseUtility.Perlin(x, y, scale, offX, offY);

                    // выбор биома по шансам; если что-то не так — default
                    var bt = PickBiome(z, p);
                    grid[ix, iy] = bt;
                }
            }
        }

        private static BiomeType PickBiome(ClimateZones z, float perlin01)
        {
            if (z == null || z.biomeChances == null || z.biomeChances.Count == 0)
                return z != null ? z.defaultBiome : default;

            // список уже отсортирован в ValidateBiomeChances(), но на всякий случай — линейный поиск
            for (int i = 0; i < z.biomeChances.Count; i++)
            {
                var c = z.biomeChances[i];
                if (perlin01 >= c.minPerlinValue && perlin01 < c.maxPerlinValue)
                    return c.biome;
            }
            return z.defaultBiome;
        }

        private void LogBiomeStats()
        {
            int w = _w, h = _h;
            var counts = new Dictionary<BiomeType, int>(16);
            for (int iy = 0; iy < h; iy++)
                for (int ix = 0; ix < w; ix++)
                {
                    var t = _grid[ix, iy];
                    counts[t] = counts.TryGetValue(t, out var c) ? c + 1 : 1;
                }

            int total = w * h;
            System.Text.StringBuilder sb = new System.Text.StringBuilder(256);
            sb.Append("[Biome] Stats: ");
            bool first = true;
            foreach (var kv in counts)
            {
                if (!first) sb.Append(", "); first = false;
                float pct = 100f * kv.Value / total;
                sb.Append($"{kv.Key}:{pct:F1}%");
            }
            Debug.Log(sb.ToString());
        }

        // === Публичное API ===
        public BiomeType GetBiomeAtPosition(Vector2Int pos)
        {
            int ix = pos.x + _ox, iy = pos.y + _oy;
            if ((uint)ix >= (uint)_w || (uint)iy >= (uint)_h) return default;
            return _grid[ix, iy];
        }

        public Color GetBiomeColor(BiomeType type)
        {
            return _biomeData.TryGetValue(type, out var d) ? d.ColorMap : Color.magenta;
        }

        public BiomeType[,] Grid => _grid; // для временной карты
    }
}