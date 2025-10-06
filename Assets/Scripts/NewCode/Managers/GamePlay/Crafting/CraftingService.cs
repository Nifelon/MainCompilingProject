using Game.Items;

public static class CraftingService
{
    // ������ ������: 1 Skin -> 3 LeatherPatch
    public static bool ExchangeSkinToPatches()
    {
        // ����� ��������� � ���������� (��� ����� ����� Count � Remove)
        if (!InventoryService.Remove(ItemId.Skin, 1)) return false;

        InventoryService.Add(ItemId.LeatherPatch, 3); // ��� �������� QuestEventBus.RaiseCollect(...)
        return true;
    }
}
