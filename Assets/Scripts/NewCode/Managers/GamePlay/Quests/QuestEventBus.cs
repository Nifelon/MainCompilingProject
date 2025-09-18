using System;

public static class QuestEventBus
{
    // kill
    public static event Action<string> OnUnitKilled;
    public static void RaiseUnitKilled(string unitKind) => OnUnitKilled?.Invoke(unitKind);

    // collect
    public static event Action<string, int> OnCollect;
    public static void RaiseCollect(string itemId, int count) => OnCollect?.Invoke(itemId, count);

    // craft
    public static event Action<string, int> OnCraft;
    public static void RaiseCraft(string itemId, int count) => OnCraft?.Invoke(itemId, count);
}