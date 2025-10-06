using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Game.Items;

public class LootWindow : MonoBehaviour
{
    public static LootWindow Instance;

    [Header("UI")]
    public GameObject root;
    public Transform content;
    public GameObject entryPrefab;
    public Button takeAllBtn, closeBtn;

    [Header("Data (optional)")]
    public ItemDatabase itemDatabase;     // для названий/иконок

    [Header("Hotkeys")]
    public KeyCode takeAllKey = KeyCode.E;
    public KeyCode closeKey = KeyCode.Escape;

    // внутренняя модель лута (нормализовано по ItemId)
    private readonly List<(ItemId id, int count)> _items = new();

    public bool IsOpen => root && root.activeSelf;

    void Awake()
    {
        Instance = this;
        if (!root) root = gameObject;

        if (takeAllBtn) takeAllBtn.onClick.AddListener(TakeAll);
        if (closeBtn) closeBtn.onClick.AddListener(Close);

        root.SetActive(false);
    }

    void Update()
    {
        if (!IsOpen) return;
        if (Input.GetKeyDown(takeAllKey)) TakeAll();
        if (Input.GetKeyDown(closeKey)) Close();
    }

    // ---------- ПУБЛИЧНЫЕ API ----------

    public static void ShowStatic(IEnumerable<(ItemId id, int count)> items) => Instance?.Show(items);
    public static void ShowStatic(IEnumerable<(string id, int count)> items) => Instance?.Show(items);

    public void Show(IEnumerable<(ItemId id, int count)> items)
    {
        if (!ValidateRefs()) return;

        _items.Clear();
        if (items != null)
        {
            foreach (var g in items.Where(x => x.count > 0).GroupBy(x => x.id))
                _items.Add((g.Key, g.Sum(x => x.count)));
        }

        Render();
        root.SetActive(true);
    }

    public void Show(IEnumerable<(string id, int count)> items)
    {
        if (!ValidateRefs()) return;

        _items.Clear();
        if (items != null)
        {
            var conv = new List<(ItemId id, int count)>();
            foreach (var it in items)
            {
                if (it.count <= 0 || string.IsNullOrWhiteSpace(it.id)) continue;

                if (TryResolveItemId(it.id, out var eid))
                    conv.Add((eid, it.count));
                else
                    Debug.LogWarning($"[LootWindow] Unknown item id '{it.id}'. Expected GUID/enum/displayName.");
            }

            foreach (var g in conv.GroupBy(x => x.id))
                _items.Add((g.Key, g.Sum(x => x.count)));
        }

        Render();
        root.SetActive(true);
    }

    // ---------- ЛОКАЛЬНАЯ ЛОГИКА ----------

    void Close() => root.SetActive(false);

    bool ValidateRefs()
    {
        if (!root || !content || !entryPrefab)
        {
            Debug.LogError("[LootWindow] Missing refs (root/content/entryPrefab).");
            return false;
        }
        return true;
    }

    void Render()
    {
        foreach (Transform c in content) Destroy(c.gameObject);

        foreach (var it in _items)
        {
            var go = Instantiate(entryPrefab, content);

            var texts = go.GetComponentsInChildren<Text>(true);
            if (texts.Length > 0) texts[0].text = ResolveDisplayName(it.id);
            if (texts.Length > 1) texts[^1].text = "×" + it.count;

            var icon = FindIcon(go);
            if (icon)
            {
                var so = GetItemSO(it.id);
                icon.sprite = so ? so.icon : null;
                icon.enabled = icon.sprite != null;
            }
        }

        if (takeAllBtn) takeAllBtn.interactable = _items.Count > 0;
    }

    public void TakeAll()
    {
        if (_items.Count > 0)
        {
            for (int i = 0; i < _items.Count; i++)
            {
                var it = _items[i];
                InventoryService.Add(it.id, it.count);
            }
            _items.Clear();
        }
        Close();
    }

    // ---------- HELPERS ----------

    string ResolveDisplayName(ItemId id)
    {
        var so = GetItemSO(id);
        if (so != null && !string.IsNullOrEmpty(so.displayName))
            return so.displayName;
        return id.ToString();
    }

    ItemSO GetItemSO(ItemId id)
    {
        if (!itemDatabase) return null;
        return itemDatabase.Get(id);
    }

    /// <summary>GUID → ItemId, enumName → ItemId, displayName → ItemId.</summary>
    bool TryResolveItemId(string s, out ItemId id)
    {
        // 1) GUID напрямую
        if (ItemMap.TryEnumByGuid(s, out id))
            return true;

        // 2) Имя enum (без учёта регистра)
        if (System.Enum.TryParse(s, true, out id))
            return true;

        // 3) Имя через базу (displayName / Unity name)
        if (itemDatabase)
        {
            var so = itemDatabase.GetByName(s);
            if (so && !string.IsNullOrWhiteSpace(so.Guid) &&
                ItemMap.TryEnumByGuid(so.Guid, out id))
                return true;
        }
        id = default;
        return false;
    }

    Image FindIcon(GameObject entry)
    {
        var imgs = entry.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < imgs.Length; i++)
            if (imgs[i].name.IndexOf("icon", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return imgs[i];
        return null;
    }
}
