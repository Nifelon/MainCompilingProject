using Game.UI;
using UnityEngine;
using UnityEngine.UI;

public class MapPanelController : MonoBehaviour
{
    [Header("UI")]
    public GameObject root;        // MapPanel (контейнер)
    public RawImage mapImage;      // RawImage внутри Frame/Map
    public Text title;             // optional

    [Header("Source")]
    public Texture2D explicitTexture;  // если хотим вручную указать текстуру

    void Awake()
    {
        if (!root) root = gameObject;
        Hide();
    }

    public void Show()
    {
        EnsureTexture();
        root.SetActive(true);
    }

    public void Hide() => root.SetActive(false);

    void EnsureTexture()
    {
        if (!mapImage) return;

        // 1) €вна€ текстура из инспектора
        if (explicitTexture != null) { mapImage.texture = explicitTexture; return; }

        // 2) попытка достать из WorldGen (если он загружен в этой сцене)
        var wm = GlobalCore.Instance ? GlobalCore.Instance.WorldManager : null;
        if (wm != null)
        {
            // попытаемс€ найти MapPreview в сцене
            var preview = FindAnyObjectByType<MapPreview>(FindObjectsInactive.Include);
            if (preview && preview.CurrentTexture != null)
            {
                mapImage.texture = preview.CurrentTexture;
                return;
            }
        }

        // 3) фолбэк Ч сгенерировать простую УшахматкуФ
        if (mapImage.texture == null)
            mapImage.texture = MakeChecker(512, 512, 32);
    }

    Texture2D MakeChecker(int w, int h, int cell)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        var a = new Color32(210, 210, 210, 255);
        var b = new Color32(235, 235, 235, 255);
        var px = new Color32[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                bool c = ((x / cell) + (y / cell)) % 2 == 0;
                px[y * w + x] = c ? a : b;
            }
        tex.SetPixels32(px);
        tex.Apply(false, false);
        return tex;
    }
}