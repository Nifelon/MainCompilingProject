using UnityEngine;
using System.Collections.Generic;
using System;

[CreateAssetMenu(menuName = "Game/Items/Database")]
public class ItemDatabase : ScriptableObject
{
    public ItemSO[] items;
    private Dictionary<string, ItemSO> _byId;
    public ItemSO Get(string id) { _byId ??= Build(); return _byId.TryGetValue(id, out var so) ? so : null; }
    private Dictionary<string, ItemSO> Build()
    {
        var d = new Dictionary<string, ItemSO>(StringComparer.OrdinalIgnoreCase);
        foreach (var it in items) { if (string.IsNullOrEmpty(it.id) || d.ContainsKey(it.id)) Debug.LogError($"Duplicate/empty item id: {it?.displayName}"); else d[it.id] = it; }
        return d;
    }
}