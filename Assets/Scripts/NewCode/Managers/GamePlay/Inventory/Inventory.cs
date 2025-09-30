using UnityEngine;
using System.Collections.Generic;
public class Inventory : MonoBehaviour
{
    [SerializeField] private ItemDatabase db;
    private readonly Dictionary<string, int> _stacks = new();
    public event System.Action OnChanged;

    public bool Add(string id, int n) { if (n <= 0) return false; _stacks[id] = _stacks.GetValueOrDefault(id) + n; OnChanged?.Invoke(); return true; }
    public bool Remove(string id, int n) { int have = _stacks.GetValueOrDefault(id); if (have < n) return false; have -= n; if (have == 0) _stacks.Remove(id); else _stacks[id] = have; OnChanged?.Invoke(); return true; }
    public int Count(string id) => _stacks.GetValueOrDefault(id);
    public IReadOnlyDictionary<string, int> All => _stacks;
}