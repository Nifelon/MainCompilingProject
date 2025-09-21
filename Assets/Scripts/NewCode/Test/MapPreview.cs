using Game.Core;
using Game.World.Map.Biome;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    [RequireComponent(typeof(RawImage))]
    public class MapPreview : MonoBehaviour, IWorldSystem
    {
        [SerializeField] private int order = 1000;
        public int Order => order;
        [SerializeField, Min(1)] private int downscale = 2;

        public Texture2D CurrentTexture { get; private set; }
        public int Downscale => downscale;

        private RawImage _img;
        private WorldContext _ctx;
        private IBiomeService _biomes;

        private void Awake() => _img = GetComponent<RawImage>();
        public void Close_OpenWindow() =>gameObject.SetActive(!gameObject.activeSelf);
        public void Initialize(WorldContext ctx)
        {
            _ctx = ctx;
            _biomes = ctx.GetService<IBiomeService>();
            RebuildTexture();
        }

        public void SetDownscale(int ds)
        {
            ds = Mathf.Max(1, ds);
            if (ds == downscale) return;
            downscale = ds;
            if (_ctx != null) RebuildTexture();
        }

        public void RebuildTexture()
        {
            if (_ctx == null) return;

            int w = _ctx.Width, h = _ctx.Height;
            int ds = Mathf.Max(1, downscale);
            int tw = Mathf.Max(1, w / ds);
            int th = Mathf.Max(1, h / ds);

            var tex = new Texture2D(tw, th, TextureFormat.RGBA32, false);
            var pixels = new Color32[tw * th];

            int halfX = _ctx.Size.x, halfY = _ctx.Size.y;
            var pos = new Vector2Int();
            int idx = 0;

            for (int y = 0; y < th; y++)
            {
                int sy = Mathf.Min(h - 1, y * ds);
                pos.y = sy - halfY;

                for (int x = 0; x < tw; x++, idx++)
                {
                    int sx = Mathf.Min(w - 1, x * ds);
                    pos.x = sx - halfX;

                    if (_biomes != null)
                    {
                        var t = _biomes.GetBiomeAtPosition(pos);
                        pixels[idx] = _biomes.GetBiomeColor(t);
                    }
                    else
                    {
                        // fallback-градиент, если сервис не найден
                        float u = (sx % 256) / 255f, v = (sy % 256) / 255f;
                        pixels[idx] = new Color(u, v, 1f - 0.5f * u, 1f);
                    }
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, false);

            _img.color = Color.white;
            _img.texture = tex;
            _img.SetNativeSize();
            CurrentTexture = tex;
        }
    }
}