using System.Collections.Generic;

public static class InventoryService
{
    static readonly Dictionary<string, int> _stacks = new();

    public static bool TryAdd(string id, int n)
    {
        if (string.IsNullOrEmpty(id) || n <= 0) return false;
        _stacks[id] = _stacks.GetValueOrDefault(id) + n;
        return true;
    }

    public static bool TryRemove(string id, int n)
    {
        if (string.IsNullOrEmpty(id) || n <= 0) return false;
        int have = _stacks.GetValueOrDefault(id);
        if (have < n) return false;
        have -= n;
        if (have == 0) _stacks.Remove(id);
        else _stacks[id] = have;
        return true;
    }

    public static int Count(string id) => _stacks.GetValueOrDefault(id);
    public static IReadOnlyDictionary<string, int> All => _stacks;
}