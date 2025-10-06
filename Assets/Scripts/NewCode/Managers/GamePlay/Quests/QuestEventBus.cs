using System;
using Game.Items;   // <-- ДОБАВИЛИ
using Game.Actors; // ActorIdKind, NPCEnums, CreatureEnums

public static class QuestEventBus
{
    public static event Action<ItemId, int> OnCollect;
    public static event Action<ActorIdKind, NPCEnums, CreatureEnums> OnUnitKilled;

    // legacy — на время миграции
    public static event Action<string, int> OnCollectLegacy;
    public static event Action<string> OnUnitKilledLegacy;

    public static void RaiseCollect(ItemId itemId, int amount)
    {
        if (amount <= 0) return;
        if (!ItemMap.IsValid(itemId)) return;
        OnCollect?.Invoke(itemId, amount);
    }

    public static void RaiseCollectGuid(string guid, int amount)
    {
        if (amount <= 0 || string.IsNullOrWhiteSpace(guid)) return;
        if (ItemMap.TryEnumByGuid(guid, out var id))
            OnCollect?.Invoke(id, amount);
        else
            OnCollectLegacy?.Invoke(guid, amount);
    }

    public static void RaiseUnitKilled(ActorIdKind kind, NPCEnums npc, CreatureEnums creature)
    {
        OnUnitKilled?.Invoke(kind, npc, creature);
    }

    // legacy
    public static void RaiseCollect(string itemId, int amount)
    {
        if (amount <= 0 || string.IsNullOrWhiteSpace(itemId)) return;
        OnCollectLegacy?.Invoke(itemId, amount);
    }

    public static void RaiseUnitKilled(string unitKind)
    {
        if (string.IsNullOrWhiteSpace(unitKind)) return;
        OnUnitKilledLegacy?.Invoke(unitKind);
    }

    [Obsolete("Craft больше не используется: предметные квесты считаются по Collect/инвентарю.")]
    public static void RaiseCraft(string itemId, int amount) { /* no-op */ }
}