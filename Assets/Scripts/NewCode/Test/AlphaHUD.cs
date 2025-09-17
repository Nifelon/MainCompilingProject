using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public class AlphaHUD : MonoBehaviour
    {
        [Header("World (optional)")]
        [SerializeField] private WorldManager world;   // ← притащи из инспектора ИЛИ оставь пустым

        [Header("UI Refs")]
        [SerializeField] private Text seedText;
        [SerializeField] private Text sizeText;
        [SerializeField] private Text timeText;
        [SerializeField] private Button rebuildSameBtn;
        [SerializeField] private Button rebuildNewBtn;
        [SerializeField] private Button savePngBtn;
        [SerializeField] private Button dsCycleBtn;
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

        public void Bind(WorldManager wm)   // ← чтобы LabStarter мог вколоть ссылку
        {
            world = wm;
            _wmCache = wm;
            RefreshLabels(true);
        }

        private readonly int[] _dsOptions = { 1, 2, 4 };
        private int _dsIndex;

        private void Awake()
        {
            if (rebuildSameBtn) rebuildSameBtn.onClick.AddListener(RebuildSame);
            if (rebuildNewBtn) rebuildNewBtn.onClick.AddListener(RebuildNew);
            if (savePngBtn) savePngBtn.onClick.AddListener(SavePng);
            if (dsCycleBtn) dsCycleBtn.onClick.AddListener(CycleDownscale);
        }

        private void Start()
        {
            // если мир ещё не найден — попробуем сейчас (до LabStarter это норм)
            _ = WM;

            // выставим текущий DS-индекс под MapPreview
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

        private void RebuildSame()
        {
            WM?.RebuildWorld();
            RefreshLabels(true);
        }

        private void RebuildNew()
        {
            var wm = WM;
            if (wm == null) return;
            int newSeed = Random.Range(int.MinValue, int.MaxValue);
            wm.RebuildWorld(newSeed: newSeed);
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
    }
}