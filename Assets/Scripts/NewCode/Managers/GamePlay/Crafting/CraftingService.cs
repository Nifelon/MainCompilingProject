using Game.Items;

public static class CraftingService
{
    // Пример обмена: 1 Skin -> 3 LeatherPatch
    public static bool ExchangeSkinToPatches()
    {
        // Одним действием — безопаснее (без гонок между Count и Remove)
        if (!InventoryService.Remove(ItemId.Skin, 1)) return false;

        InventoryService.Add(ItemId.LeatherPatch, 3); // это поднимет QuestEventBus.RaiseCollect(...)
        return true;
    }
}
