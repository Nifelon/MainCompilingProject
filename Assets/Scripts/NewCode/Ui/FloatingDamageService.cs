using UnityEngine;
using UnityEngine.UI;

public class FloatingDamageService : MonoBehaviour
{
    public Canvas canvas;                 // Main Canvas (Overlay)
    public GameObject damageTextPrefab;   // Prefab with Text + CanvasGroup
    public float floatUp = 40f;
    public float lifeTime = 0.8f;

    static FloatingDamageService _inst;
    void Awake()
    {
        _inst = this; if (!canvas) canvas = GetComponentInParent<Canvas>();
        if (damageTextPrefab && damageTextPrefab.scene.IsValid())
            Debug.LogError("[FDS] damageTextPrefab указывает на объект в СЦЕНЕ. Перетащи ПРЕФАБ из Project.");
    }

    public static void Show(Vector3 worldPos, int amount, Color color, bool crit = false)
    {
        if (_inst == null) { Debug.LogWarning("[FDS] No instance on Canvas."); return; }
        _inst.Spawn(worldPos, amount, color, crit);
    }

    void Spawn(Vector3 worldPos, int amount, Color color, bool crit)
    {
        if (!canvas || !damageTextPrefab) { Debug.LogWarning("[FDS] Missing refs."); return; }
        var cam = Camera.main; if (!cam) { Debug.LogWarning("[FDS] No Camera.main"); return; }

        Vector3 screen = cam.WorldToScreenPoint(worldPos);

        var go = Instantiate(damageTextPrefab, canvas.transform); // ← создаём КОПИЮ префаба
        var rt = go.transform as RectTransform;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform, screen, null, out var local);
        rt.anchoredPosition = local;
        go.gameObject.SetActive(true);
        var txt = go.GetComponentInChildren<Text>();
        if (txt)
        {
            txt.text = crit ? $"{amount}!" : amount.ToString();
            txt.color = color;
            txt.fontStyle = crit ? FontStyle.Bold : FontStyle.Normal;
        }

        var cg = go.GetComponent<CanvasGroup>();
        StartCoroutine(FadeAndRise(go, cg));
    }

    System.Collections.IEnumerator FadeAndRise(GameObject go, CanvasGroup cg)
    {
        float t = 0f; var rt = go.transform as RectTransform; Vector2 start = rt.anchoredPosition;
        while (t < lifeTime)
        {
            t += Time.deltaTime; float k = t / lifeTime;
            rt.anchoredPosition = start + Vector2.up * (floatUp * k);
            if (cg) cg.alpha = 1f - k;
            yield return null;
        }
        Destroy(go); // ← УДАЛЯЕМ ТОЛЬКО СОЗДАННЫЙ ЭЛЕМЕНТ, НЕ СЕРВИС!
    }
}