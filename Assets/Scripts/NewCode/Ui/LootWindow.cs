using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static UnityEditor.Progress;

public class LootWindow : MonoBehaviour
{
    public static LootWindow Instance;

    [Header("UI")]
    public GameObject root;
    public Transform content;
    public GameObject entryPrefab;
    public Button takeAllBtn, closeBtn;

    List<(string id, int count)> _items;

    void Awake()
    {
        Instance = this;
        if (!root) root = gameObject;
        if (takeAllBtn) takeAllBtn.onClick.AddListener(TakeAll);
        if (closeBtn) closeBtn.onClick.AddListener(() => root.SetActive(false));
        root.SetActive(false);
    }

    public void Show(List<(string id, int count)> items)
    {
        if (root == null || content == null || entryPrefab == null)
        {
            Debug.LogError("[LootWindow] Missing refs (root/content/entryPrefab).");
            return;
        }
        _items = items ?? new List<(string id, int count)>();
        root.SetActive(true);
        foreach (Transform c in content) Destroy(c.gameObject);
        for (int i = 0; i < _items.Count; i++)
        {
            var it = _items[i];
            var go = Instantiate(entryPrefab, content);
            var texts = go.GetComponentsInChildren<UnityEngine.UI.Text>(true);
            if (texts.Length > 0) texts[0].text = it.id;
            if (texts.Length > 1) texts[texts.Length - 1].text = "×" + it.count;
        }
    }

    void TakeAll()
    {
            if (_items != null)
                {
                    for (int i = 0; i < _items.Count; i++)
                        {
                var it = _items[i];
                InventoryService.TryAdd(it.id, it.count);
                        }
            _items.Clear();
                }
        root.SetActive(false);
    }


}