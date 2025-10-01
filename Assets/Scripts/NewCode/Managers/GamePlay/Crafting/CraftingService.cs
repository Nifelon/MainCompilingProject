using UnityEngine;

public static class CraftingService
{
    // ����� 1 Skin -> 3 LeatherPatch
    public static bool ExchangeSkinToPatches()
    {
        if (InventoryService.Count(ItemId.Skin) < 1) return false;

        InventoryService.Remove(ItemId.Skin, 1);
        InventoryService.Add(ItemId.LeatherPatch, 3);

        // �����: ������ �����
        QuestEventBus.RaiseCraft(ItemId.LeatherPatch.ToString(), 3);
        return true;
    }
}