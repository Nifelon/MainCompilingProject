public static class QuestEventBus
{
    public static System.Action<string> OnUnitKilled;     // kind
    public static System.Action<string, int> OnCollect;    // id, amt
    public static System.Action<string, int> OnCraft;      // id, amt
    public static void RaiseUnitKilled(string kind) => OnUnitKilled?.Invoke(kind);
    public static void RaiseCollect(string id, int amt) => OnCollect?.Invoke(id, amt);
    public static void RaiseCraft(string id, int amt) => OnCraft?.Invoke(id, amt);
}