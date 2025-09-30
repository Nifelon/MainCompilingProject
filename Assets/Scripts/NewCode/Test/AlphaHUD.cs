using System.IO;
using UnityEngine;
using UnityEngine.UI;
using Game.World.Signals;
using Game.World.Objects;
using Game.World; // если тут WorldManager
// using Game.World.Tiles; // если PoolManagerMainTile в другом неймспейсе
// using Game.World.Content.Services; // если ReservationService в другом неймспейсе

namespace Game.UI
{
    public class AlphaHUD : MonoBehaviour
    {
        [SerializeField] PoolManager poolManager;
        [Header("World (optional)")]
        [SerializeField] private WorldManager world;   // ← можно оставить пустым, подхватим из GlobalCore

        [Header("World visuals/services (optional)")]
        [SerializeField] private ObjectManager objectManager;
        [SerializeField] private PoolManagerMainTile mainTilePool;
        [SerializeField] private ReservationService reservation; // или IReservationService, если удобнее

        [Header("UI Refs")]
        [SerializeField] private Text seedText;
        [SerializeField] private Text sizeText;
        [SerializeField] private Text timeText;
        [SerializeField] private Button rebuildSameBtn;
        [SerializeField] private Button rebuildNewBtn;
        [SerializeField] private Button savePngBtn;
        [SerializeField] private Button dsCycleBtn;
        [SerializeField] private Button Close_OpenBtn;
        [SerializeField] private Text dsCycleLabel;
        [SerializeField] private MapPreview mapPreview;

        // кэш с ленивой инициализацией
        private WorldManager _wmCache;
        private WorldManager WM
        {
            get
            {
                if (_wmCache) return _wmCache;
                _wmCache = world
                           ?? GlobalCore.Instance?.WorldManager
                           ?? FindAnyObjectByType<WorldManager>(FindObjectsInactive.Exclude);
                return _wmCache;
            }
        }

        public void Bind(WorldManager wm)
        {
            world = wm;
            _wmCache = wm;
            RefreshLabels(true);
        }

        private readonly int[] _dsOptions = { 1, 2, 4 };
        private int _dsIndex;

        void Awake()
        {
            // подхватим сервисы, если не проставлены в инспекторе
            if (!objectManager) objectManager = FindAnyObjectByType<ObjectManager>(FindObjectsInactive.Exclude);
            if (!mainTilePool) mainTilePool = FindAnyObjectByType<PoolManagerMainTile>(FindObjectsInactive.Exclude);
            if (!reservation) reservation = FindAnyObjectByType<ReservationService>(FindObjectsInactive.Exclude);

            if (rebuildSameBtn) rebuildSameBtn.onClick.AddListener(RebuildSame);
            if (rebuildNewBtn) rebuildNewBtn.onClick.AddListener(RebuildNew);
            if (savePngBtn) savePngBtn.onClick.AddListener(SavePng);
            if (dsCycleBtn) dsCycleBtn.onClick.AddListener(CycleDownscale);
            if (Close_OpenBtn) Close_OpenBtn.onClick.AddListener(сlose_OpenWindow);
        }

        void Start()
        {
            _ = WM;

            _dsIndex = 0;
            if (mapPreview != null)
                for (int i = 0; i < _dsOptions.Length; i++)
                    if (_dsOptions[i] == mapPreview.Downscale) { _dsIndex = i; break; }

            RefreshLabels(true);
        }

        private void RefreshLabels(bool force = false)
        {
            var wm = WM;
            if (wm != null)
            {
                if (seedText) seedText.text = $"Seed: {wm.Seed}";
                if (sizeText) sizeText.text = $"Half: {wm.SizeMap.x}×{wm.SizeMap.y}  (Map {wm.Width}×{wm.Height})";
                if (timeText) timeText.text = $"Build: {wm.LastBuildMs} ms";
            }
            if (dsCycleLabel)
            {
                int ds = _dsOptions[Mathf.Clamp(_dsIndex, 0, _dsOptions.Length - 1)];
                dsCycleLabel.text = $"DS: {ds}x";
            }
        }

        private void RebuildSame() => DoWorldRegen(null);

        private void RebuildNew()
        {
            int newSeed = Random.Range(int.MinValue, int.MaxValue);
            DoWorldRegen(newSeed);
        }

        /// <summary>
        /// Полный реген: очистка визуала/резерваций → сигнал → пересборка мира → обновление UI.
        /// </summary>
        private void DoWorldRegen(int? newSeed)
        {
            // 1) очистка визуала/резервов
            objectManager?.DespawnAll();
            mainTilePool?.ClearAll();
            reservation?.ClearAll();

            // 2) СНАЧАЛА пересобрать мир (биомы/карта)
            var wm = WM;
            if (wm != null)
            {
                if (newSeed.HasValue) wm.RebuildWorld(newSeed.Value);
                else wm.RebuildWorld();
            }

            // 3) ТЕПЕРЬ оповестить всех
            WorldSignals.FireWorldRegen();

            // 4) И сразу “ткнуть” тайловый менеджер
            poolManager?.ForceRefresh();

            // 5) миникарта
            mapPreview?.RebuildTexture();
            RefreshLabels(true);
        }

        private void SavePng()
        {
            var tex = mapPreview != null ? mapPreview.CurrentTexture : null;
            if (tex == null) { Debug.LogWarning("[AlphaHUD] Нет текстуры карты для сохранения."); return; }

            byte[] png = tex.EncodeToPNG();
            string dir = Path.Combine(Application.persistentDataPath, "Maps");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            string file = Path.Combine(dir, $"map_{WM?.Seed ?? 0}_{tex.width}x{tex.height}.png");
            File.WriteAllBytes(file, png);
            Debug.Log($"[AlphaHUD] Saved: {file}");
        }

        private void CycleDownscale()
        {
            _dsIndex = (_dsIndex + 1) % _dsOptions.Length;
            int ds = _dsOptions[_dsIndex];
            if (mapPreview != null) mapPreview.SetDownscale(ds);
            RefreshLabels();
        }

        void сlose_OpenWindow() => mapPreview?.Close_OpenWindow();
    }
}
