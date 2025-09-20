using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

public enum MarkerType { Player, Camp }

[Serializable]
public class MarkerDef
{
    public MarkerType type;
    public Transform worldTarget;
    [HideInInspector] public Image uiIcon;
    public Color color = Color.white;
    public bool visible = true;
}

public class MapMarkersController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] RectTransform mapImage;
    [SerializeField] RectTransform iconsRoot;
    [SerializeField] GameObject iconPrefab;

    [Header("Markers")]
    [SerializeField] List<MarkerDef> markers = new();

    [Header("World size (cells half-extent)")]
    [Tooltip("Если WorldManager не найден или не даёт размер — используем это значение.")]
    [SerializeField] Vector2Int fallbackHalfSize = new(25, 25); // для 50x50

    // ✅ КЭШ: найдём WorldManager один раз (без статического Instance)
    WorldManager _wmCache;

    Vector2Int HalfSize
    {
        get
        {
            // Unity 6: предпочтительно FindFirstObjectByType; можно и FindObjectOfType
#if UNITY_2023_1_OR_NEWER
            if (_wmCache == null) _wmCache = FindFirstObjectByType<WorldManager>();
#else
            if (_wmCache == null) _wmCache = FindObjectOfType<WorldManager>();
#endif
            if (_wmCache != null)
            {
                // предполагаем, что SizeMap — полный размер (W,H)
                var full = _wmCache.SizeMap; // Vector2Int
                return new Vector2Int(Mathf.Max(1, full.x / 2), Mathf.Max(1, full.y / 2));
            }
            return fallbackHalfSize;
        }
    }

    void Awake()
    {
        if (mapImage == null) mapImage = GetComponentInChildren<RawImage>(true)?.rectTransform;
    }

    void Start()
    {
        foreach (var m in markers)
        {
            if (m.uiIcon == null && iconPrefab != null)
            {
                var go = Instantiate(iconPrefab, iconsRoot);
                m.uiIcon = go.GetComponent<Image>();
                m.uiIcon.color = m.color;
                go.name = $"Icon_{m.type}";
            }
        }
    }

    void LateUpdate()
    {
        if (mapImage == null) return;

        foreach (var m in markers)
        {
            if (m.uiIcon == null) continue;
            if (!m.visible || !m.worldTarget) { m.uiIcon.enabled = false; continue; }

            var uv = WorldToUV(m.worldTarget.localPosition);
            var local = UVToLocal(uv);
            var rt = m.uiIcon.rectTransform;

            if (rt.parent != mapImage) rt.SetParent(mapImage, false);
            rt.anchoredPosition = local;
            m.uiIcon.enabled = true;
        }
    }

    Vector2 WorldToUV(Vector3 worldPos)
    {
        // Если твой мир в 0..W/0..H, а не -W..W/-H..H — просто замени на:
        // float u = Mathf.Clamp01(worldPos.x / (HalfSize.x * 2f));
        // float v = Mathf.Clamp01(worldPos.y / (HalfSize.y * 2f));
        var half = HalfSize;
        float u = Mathf.InverseLerp(-half.x, half.x, worldPos.x);
        float v = Mathf.InverseLerp(-half.y, half.y, worldPos.y);
        return new Vector2(u, v);
    }

    Vector2 UVToLocal(Vector2 uv)
    {
        var r = mapImage.rect;
        float x = Mathf.Lerp(r.xMin, r.xMax, uv.x);
        float y = Mathf.Lerp(r.yMin, r.yMax, uv.y);
        return new Vector2(x, y);
    }

    public void SetMarkerColor(MarkerType type, Color c)
    {
        foreach (var m in markers)
            if (m.type == type && m.uiIcon != null) { m.color = c; m.uiIcon.color = c; }
    }

    public void SetMarkerVisible(MarkerType type, bool visible)
    {
        foreach (var m in markers)
            if (m.type == type)
            {
                m.visible = visible;
                if (m.uiIcon != null) m.uiIcon.enabled = visible;
            }
    }

    public void SetMarkerTarget(MarkerType type, Transform target)
    {
        foreach (var m in markers)
            if (m.type == type) m.worldTarget = target;
    }
}