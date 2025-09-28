using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-206)]
public class NpcPool : MonoBehaviour
{
    [Tooltip("Необязательный прогрев пула: пары (префаб, количество)")]
    [SerializeField] private List<WarmupItem> warmup = new();

    [System.Serializable]
    public struct WarmupItem
    {
        public GameObject prefab;
        public int count;
    }

    private readonly Dictionary<GameObject, Stack<GameObject>> _pools = new();

    void Start()
    {
        // Прогреем, если задано
        if (warmup != null)
        {
            for (int i = 0; i < warmup.Count; i++)
            {
                var w = warmup[i];
                if (!w.prefab || w.count <= 0) continue;
                WarmUp(w.prefab, w.count);
            }
        }
    }

    public void WarmUp(GameObject prefab, int count)
    {
        if (!prefab || count <= 0) return;
        if (!_pools.TryGetValue(prefab, out var st))
        {
            st = new Stack<GameObject>();
            _pools[prefab] = st;
        }
        for (int i = 0; i < count; i++)
        {
            var go = Instantiate(prefab);
            EnsurePrefabKey(go, prefab);
            go.SetActive(false);
            go.transform.SetParent(transform, false);
            st.Push(go);
        }
    }

    public GameObject Get(GameObject prefab)
    {
        if (!prefab) return null;
        if (!_pools.TryGetValue(prefab, out var st))
        {
            st = new Stack<GameObject>();
            _pools[prefab] = st;
        }
        if (st.Count > 0)
        {
            var go = st.Pop();
            go.SetActive(true);
            return go;
        }
        var inst = Instantiate(prefab);
        EnsurePrefabKey(inst, prefab);
        return inst;
    }

    public void Release(GameObject prefab, GameObject instance)
    {
        if (!instance) return;
        if (!prefab)
        {
            // попытка взять ключ с инстанса
            prefab = GetPrefabKey(instance);
            if (!prefab)
            {
                Destroy(instance); // нечего кэшировать — нет ключа
                return;
            }
        }
        if (!_pools.TryGetValue(prefab, out var st))
        {
            st = new Stack<GameObject>();
            _pools[prefab] = st;
        }
        instance.SetActive(false);
        instance.transform.SetParent(transform, false);
        st.Push(instance);
    }

    // ===== Helpers =====

    private static void EnsurePrefabKey(GameObject instance, GameObject prefab)
    {
        var key = instance.GetComponent<NpcPrefabKey>();
        if (!key) key = instance.AddComponent<NpcPrefabKey>();
        key.source = prefab;
    }

    public static GameObject GetPrefabKey(GameObject instance)
    {
        return instance ? instance.GetComponent<NpcPrefabKey>()?.source : null;
    }
}

// Вспомогательный компонент — хранит ссылку на исходный префаб
public class NpcPrefabKey : MonoBehaviour
{
    public GameObject source;
}
