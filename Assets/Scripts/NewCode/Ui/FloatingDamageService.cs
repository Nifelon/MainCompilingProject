using UnityEngine;
using UnityEngine.UI;

public class FloatingDamageService : MonoBehaviour
{
    public static FloatingDamageService Instance { get; private set; }
    public static void Raise(DamageInfo info) { Instance?._OnDamage(info); }

    public Text damageTextPrefab; public Canvas canvas;

    void Awake() { if (Instance == null) Instance = this; else Destroy(gameObject); }

    void _OnDamage(DamageInfo info)
    {
        if (!damageTextPrefab || !canvas) return;
        var t = Instantiate(damageTextPrefab, canvas.transform);
        t.gameObject.SetActive(true);
        t.text = info.amount.ToString();
        t.color = info.isCrit ? new Color(1f, 0.84f, 0f) : Color.white;
        Vector3 screen = Camera.main ? Camera.main.WorldToScreenPoint(info.worldHitPos) : new Vector3(Screen.width / 2, Screen.height / 2);
        t.rectTransform.position = screen;
        t.CrossFadeAlpha(1f, 0f, true); t.CrossFadeAlpha(0f, 1f, false);
        Destroy(t.gameObject, 1.1f);
    }
}