using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
    }

    public void Show(List<(string id, int count)> items)
    {
        if (root == null || content == null || entryPrefab == null)
        {
            Debug.LogError("[LootWindow] Missing refs (root/content/entryPrefab).");
            return;
        }

        _items = items;
        root.SetActive(true);

        foreach (Transform c in content) Destroy(c.gameObject);

        foreach (var it in items)
        {
            var go = Instantiate(entryPrefab, content);
            var texts = go.GetComponentsInChildren<UnityEngine.UI.Text>(true);
            if (texts.Length > 0) texts[0].text = it.id;
            if (texts.Length > 1) texts[texts.Length - 1].text = "×" + it.count;
        }
    }

    void TakeAll()
    {
        // TODO: положить предметы в инвентарь
        root.SetActive(false);
    }
}