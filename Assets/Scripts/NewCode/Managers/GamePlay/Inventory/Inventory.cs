using Game.Items;
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public event System.Action OnChanged { add => InventoryService.OnChanged += value; remove => InventoryService.OnChanged -= value; }
    public bool Add(ItemId id, int n) { InventoryService.Add(id, n); return true; }
    public bool Remove(ItemId id, int n) => InventoryService.Remove(id, n);
    public int Count(ItemId id) => InventoryService.Count(id);
    public IReadOnlyDictionary<ItemId, int> All => InventoryService.All;
}