using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public class MapPanelController : MonoBehaviour
{
    [Header("UI")]
    public GameObject root;            // контейнер панели (MapPanel)
    public RawImage mapImage;          // куда рисуем карту
    public Text title;                 // (опционально)

    [Header("Source (priority order)")]
    [Tooltip("1) Жёстко заданная текстура (если указана — берём её).")]
    public Texture explicitTexture;

    [Tooltip("2) Любой объект, у которого есть свойство/метод, возвращающий Texture/RenderTexture.")]
    public Object mapTextureProvider;

    [Tooltip("Имя свойства или метода у провайдера (пример: \"CurrentTexture\", \"PreviewTexture\", \"GetMapTexture\").")]
    public string providerMemberName = "CurrentTexture";

    [Header("Auto update")]
    [Tooltip("Периодически опрашивать провайдера (на случай, если карта строится позже).")]
    public bool autoUpdate = true;

    [Tooltip("Интервал опроса провайдера, сек.")]
    [Min(0.05f)] public float updateInterval = 0.25f;

    [Header("Fallback")]
    [Tooltip("Если ничего не нашли — сгенерировать шахматку для визуальной заглушки.")]
    public bool useCheckerFallback = true;
    public int checkerSize = 512;
    public int checkerCell = 32;

    Coroutine _pollRoutine;

    void Awake()
    {
        if (!root) root = gameObject;
        Hide();
    }

    public void Show()
    {
        EnsureTextureOnce();               // быстрый попытка сразу
        if (autoUpdate && _pollRoutine == null)
            _pollRoutine = StartCoroutine(Co_PollProvider());

        root.SetActive(true);
    }

    public void Hide()
    {
        root.SetActive(false);
        if (_pollRoutine != null) { StopCoroutine(_pollRoutine); _pollRoutine = null; }
    }

    /// <summary>Позволяет внешнему коду «впихнуть» текстуру напрямую.</summary>
    public void SetTexture(Texture tex)
    {
        if (!mapImage) return;
        if (tex != null) mapImage.texture = tex;
    }

    /// <summary>Позволяет привязать провайдера и указать имя члена в рантайме.</summary>
    public void BindProvider(Object provider, string member = null)
    {
        mapTextureProvider = provider;
        if (!string.IsNullOrEmpty(member)) providerMemberName = member;
    }

    // ---------------- internals ----------------

    void EnsureTextureOnce()
    {
        if (!mapImage) return;

        // 1) Явная текстура
        if (explicitTexture != null) { mapImage.texture = explicitTexture; return; }

        // 2) Провайдер из инспектора (через рефлексию)
        var tex = TryGetTextureFromProvider(mapTextureProvider, providerMemberName);
        if (tex != null) { mapImage.texture = tex; return; }

        // 3) Попытка найти подходящий провайдер в сцене автоматически
        var auto = AutoFindLikelyProvider();
        tex = TryGetTextureFromProvider(auto, providerMemberName)
              ?? TryGetTextureFromProvider(auto, "PreviewTexture")
              ?? TryGetTextureFromProvider(auto, "GetMapTexture");
        if (tex != null) { mapImage.texture = tex; return; }

        // 4) Фолбэк
        if (useCheckerFallback && mapImage.texture == null)
            mapImage.texture = MakeChecker(checkerSize, checkerSize, checkerCell);
    }

    IEnumerator Co_PollProvider()
    {
        var wait = new WaitForSeconds(updateInterval);
        while (true)
        {
            if (mapImage)
            {
                // если уже есть картинка — всё равно можно обновлять (например, RenderTexture обновился)
                var tex = (explicitTexture != null) ? explicitTexture
                    : TryGetTextureFromProvider(mapTextureProvider, providerMemberName)
                      ?? TryGetTextureFromProvider(AutoFindLikelyProvider(), providerMemberName)
                      ?? TryGetTextureFromProvider(AutoFindLikelyProvider(), "PreviewTexture")
                      ?? TryGetTextureFromProvider(AutoFindLikelyProvider(), "GetMapTexture");

                if (tex != null && mapImage.texture != tex)
                    mapImage.texture = tex;
            }
            yield return wait;
        }
    }

    // Пытается достать Texture/RenderTexture свойством или методом провайдера.
    static Texture TryGetTextureFromProvider(Object provider, string memberName)
    {
        if (!provider || string.IsNullOrEmpty(memberName)) return null;

        var t = provider.GetType();
        // 1) Свойство
        var prop = t.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (prop != null && typeof(Texture).IsAssignableFrom(prop.PropertyType))
        {
            var v = prop.GetValue(provider) as Texture;
            return v;
        }
        // 2) Метод без аргументов
        var m = t.GetMethod(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (m != null && typeof(Texture).IsAssignableFrom(m.ReturnType) && m.GetParameters().Length == 0)
        {
            var v = m.Invoke(provider, null) as Texture;
            return v;
        }
        return null;
    }

    // Небольшой хелпер: пытаемся угадать «правильный» компонент в сцене
    static Object AutoFindLikelyProvider()
    {
        // приоритет: WorldMapManager → MapPreview → любой MonoBehaviour с подходящими свойствами
        var worldMapMgr = FindObjectOfTypeByName("WorldMapManager");
        if (worldMapMgr) return worldMapMgr;

        var preview = FindObjectOfTypeByName("MapPreview");
        if (preview) return preview;

        // fallback: ищем любой компонент с CurrentTexture/PreviewTexture
        var all = Object.FindObjectsOfType<MonoBehaviour>(includeInactive: true);
        foreach (var mb in all)
        {
            var t = mb.GetType();
            if (t.GetProperty("CurrentTexture") != null || t.GetProperty("PreviewTexture") != null)
                return mb;
        }
        return null;
    }

    static Object FindObjectOfTypeByName(string typeName)
    {
        // Без жёсткой ссылки на тип (чтобы не ломать сборку, если класса нет)
        var all = Object.FindObjectsOfType<MonoBehaviour>(includeInactive: true);
        foreach (var mb in all) if (mb && mb.GetType().Name == typeName) return mb;
        return null;
    }

    static Texture2D MakeChecker(int w, int h, int cell)
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
