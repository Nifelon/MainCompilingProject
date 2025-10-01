using System.Linq;
using UnityEngine;

public class InventoryPanelBinder : MonoBehaviour
{
    [SerializeField] ItemDatabase itemDb;
    [SerializeField] InventoryCellView[] cells = new InventoryCellView[40];

    void OnEnable() { InventoryService.OnChanged += Rebuild; Rebuild(); }
    void OnDisable() { InventoryService.OnChanged -= Rebuild; }

    public void Rebuild()
    {
        foreach (var c in cells) c?.SetEmpty();

        var all = InventoryService.All;
        if (all == null || all.Count == 0) return;

        int i = 0;
        foreach (var kv in all.OrderBy(k => k.Key))
        {
            if (i >= cells.Length) break;
            var so = itemDb ? itemDb.Get(kv.Key) : null;
            cells[i++]?.Set(so ? so.icon : null, kv.Value);
        }
    }
}