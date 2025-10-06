using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Game.Items;   // <-- ДОБАВИЛИ

[CreateAssetMenu(menuName = "Game/Items/ItemDatabase", fileName = "ItemDatabase")]
public class ItemDatabase : ScriptableObject
{
    public List<ItemSO> items = new();

    Dictionary<ItemId, ItemSO> _byEnum;
    Dictionary<string, ItemSO> _byGuid;
    Dictionary<string, ItemSO> _byName;

    void OnValidate() => Invalidate();
    void Invalidate() { _byEnum = null; _byGuid = null; _byName = null; }

    void Ensure()
    {
        if (_byEnum != null) return;

        _byEnum = new Dictionary<ItemId, ItemSO>();
        _byGuid = new Dictionary<string, ItemSO>(StringComparer.OrdinalIgnoreCase);
        _byName = new Dictionary<string, ItemSO>(StringComparer.OrdinalIgnoreCase);

        foreach (var so in items)
        {
            if (!so) continue;

            if (string.IsNullOrWhiteSpace(so.Guid))
            {
                Debug.LogWarning($"[ItemDatabase] Item '{so.name}' has empty GUID.", this);
            }
            else if (_byGuid.ContainsKey(so.Guid))
            {
                Debug.LogWarning($"[ItemDatabase] Duplicate GUID '{so.Guid}' -> {so.name}", this);
            }
            else
            {
                _byGuid[so.Guid] = so;
            }

            if (!string.IsNullOrWhiteSpace(so.Guid)
                && ItemMap.TryEnumByGuid(so.Guid, out var enumId))
            {
                if (_byEnum.ContainsKey(enumId))
                    Debug.LogWarning($"[ItemDatabase] Duplicate ItemId '{enumId}' -> {so.name}", this);
                else
                    _byEnum[enumId] = so;
            }
            else
            {
                Debug.LogWarning($"[ItemDatabase] No ItemId mapping for '{so.name}' (GUID {so.Guid}).", this);
            }

            var key = NormalizeName(string.IsNullOrWhiteSpace(so.displayName) ? so.name : so.displayName);
            if (!string.IsNullOrEmpty(key))
            {
                if (_byName.ContainsKey(key))
                    Debug.LogWarning($"[ItemDatabase] Duplicate name '{key}' -> {so.name}", this);
                else
                    _byName[key] = so;
            }
        }
    }

    static string NormalizeName(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        return s.Trim().ToLowerInvariant().Replace(" ", "").Replace("_", "");
    }

    public ItemSO Get(ItemId id) { Ensure(); return _byEnum.TryGetValue(id, out var so) ? so : null; }
    public ItemSO GetByGuid(string guid) { Ensure(); return string.IsNullOrWhiteSpace(guid) ? null : (_byGuid.TryGetValue(guid, out var so) ? so : null); }
    public ItemSO GetByName(string name)
    {
        Ensure();
        var key = NormalizeName(name);
        if (string.IsNullOrEmpty(key)) return null;
        return _byName.TryGetValue(key, out var so) ? so : null;
    }
}