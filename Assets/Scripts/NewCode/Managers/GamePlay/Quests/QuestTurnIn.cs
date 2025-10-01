using System.Collections.Generic;

public static class QuestTurnIn
{
    public static bool TrySubmit(Dictionary<ItemId, int> req)
    {
        foreach (var kv in req)
            if (InventoryService.Count(kv.Key) < kv.Value) return false;

        foreach (var kv in req)
            InventoryService.Remove(kv.Key, kv.Value);

        return true;
    }
}