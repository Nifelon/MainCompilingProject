using System;
using System.Collections.Generic;

public static class InventoryService
{
    public static event Action OnChanged;

    static readonly Dictionary<ItemId, int> _stacks = new();
    public static IReadOnlyDictionary<ItemId, int> All => _stacks;

    public static int Count(ItemId id) => _stacks.TryGetValue(id, out var v) ? v : 0;

    public static void Add(ItemId id, int amt)
    {
        if (amt <= 0) return;
        _stacks[id] = Count(id) + amt;
        OnChanged?.Invoke();
    }

    public static bool Remove(ItemId id, int amt)
    {
        if (amt <= 0) return true;
        var cur = Count(id);
        if (cur < amt) return false;
        cur -= amt;
        if (cur <= 0) _stacks.Remove(id);
        else _stacks[id] = cur;
        OnChanged?.Invoke();
        return true;
    }

    // Мост для старых мест, где пока приходят строковые id (например из ObjectData.harvest)
    public static bool TryAddByName(string itemName, int amt, ItemDatabase db)
    {
        var so = db ? db.GetByName(itemName) : null;
        if (!so) return false;
        Add(so.id, amt);
        return true;
    }
}