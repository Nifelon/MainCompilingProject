using System;

public static class QuestEventBus
{
    // ���� ��������� (id, ����������)
    public static event Action<string, int> OnCollect;
    // �������� �������� (���/���)
    public static event Action<string> OnUnitKilled;
    // ����� (id ��������, ����������)
    public static event Action<string, int> OnCraft;

    public static void RaiseCollect(string itemId, int amount)
    {
        if (amount <= 0 || string.IsNullOrWhiteSpace(itemId)) return;
        OnCollect?.Invoke(itemId, amount);
    }

    public static void RaiseUnitKilled(string unitKind)
    {
        if (string.IsNullOrWhiteSpace(unitKind)) return;
        OnUnitKilled?.Invoke(unitKind);
    }

    public static void RaiseCraft(string itemId, int amount)
    {
        if (amount <= 0 || string.IsNullOrWhiteSpace(itemId)) return;
        OnCraft?.Invoke(itemId, amount);
    }
}