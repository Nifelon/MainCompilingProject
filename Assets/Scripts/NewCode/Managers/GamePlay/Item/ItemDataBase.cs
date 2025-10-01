using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Items/ItemDatabase", fileName = "ItemDatabase")]
public class ItemDatabase : ScriptableObject
{
    public List<ItemSO> items = new();
    Dictionary<ItemId, ItemSO> _byId;
    Dictionary<string, ItemSO> _byName; // для моста со строчными id

    void Ensure()
    {
        if (_byId == null) _byId = items.Where(i => i).ToDictionary(i => i.id, i => i);
        if (_byName == null) _byName = items.Where(i => i).ToDictionary(i => i.name, i => i);
    }

    public ItemSO Get(ItemId id) { Ensure(); return _byId.TryGetValue(id, out var so) ? so : null; }
    public ItemSO GetByName(string name) { Ensure(); return _byName.TryGetValue(name, out var so) ? so : null; }
}