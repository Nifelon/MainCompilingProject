using System;
using System.Collections.Generic;
using Game.Items;     // <-- ДОБАВИЛИ
using UnityEngine;    // <-- ДОБАВИЛИ (для ItemSO)

public static class InventoryService
{
    public static event Action OnChanged;
    public static event Action<ItemId, int> OnItemChanged;

    private static readonly Dictionary<ItemId, int> _stacks = new();
    public static IReadOnlyDictionary<ItemId, int> All => _stacks;

    public static int Count(ItemId id) =>
        _stacks.TryGetValue(id, out var v) ? v : 0;

    #region Add
    public static void Add(ItemId id, int amount)
    {
        if (amount <= 0) return;
        var cur = Count(id) + amount;
        _stacks[id] = cur;

        OnItemChanged?.Invoke(id, amount);
        OnChanged?.Invoke();

        // Квест-событие — ItemId
        QuestEventBus.RaiseCollect(id, amount);
    }

    public static bool Add(string guid, int amount)
    {
        if (string.IsNullOrWhiteSpace(guid) || amount <= 0) return false;
        if (!ItemMap.TryEnumByGuid(guid, out var id)) return false;
        Add(id, amount);
        return true;
    }

    public static bool Add(ItemSO so, int amount)
    {
        if (!so || amount <= 0) return false;
        return Add(so.Guid, amount);
    }
    #endregion

    #region Remove
    public static bool Remove(ItemId id, int amount)
    {
        if (amount <= 0) return true;
        var cur = Count(id);
        if (cur < amount) return false;

        cur -= amount;
        if (cur <= 0) _stacks.Remove(id);
        else _stacks[id] = cur;

        OnItemChanged?.Invoke(id, -amount);
        OnChanged?.Invoke();
        return true;
    }

    public static bool Remove(string guid, int amount)
    {
        if (string.IsNullOrWhiteSpace(guid) || amount <= 0) return false;
        if (!ItemMap.TryEnumByGuid(guid, out var id)) return false;
        return Remove(id, amount);
    }

    public static bool Remove(ItemSO so, int amount)
    {
        if (!so || amount <= 0) return false;
        return Remove(so.Guid, amount);
    }
    #endregion

    #region Bridges / Legacy
    public static bool TryAddByGuidOrName(string input, int amount, ItemDatabase db = null)
    {
        if (string.IsNullOrWhiteSpace(input) || amount <= 0) return false;

        // 1) как GUID
        if (Add(input, amount)) return true;

        // 2) имя через базу
        if (db != null)
        {
            var so = db.GetByName(input);
            if (so) return Add(so, amount);
        }

        return false;
    }
    #endregion
}