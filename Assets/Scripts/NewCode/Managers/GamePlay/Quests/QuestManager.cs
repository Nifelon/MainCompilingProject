using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
    [Header("Quests")]
    public List<QuestConfig> quests = new();
    [SerializeField] private string activeQuestId;

    public event Action<QuestConfig> OnQuestChanged;

    QuestConfig Active => quests.FirstOrDefault(q => q.id == activeQuestId);

    void OnEnable()
    {
        QuestEventBus.OnCollect += HandleCollect;
        QuestEventBus.OnUnitKilled += HandleKill;
        QuestEventBus.OnCraft += HandleCraft;
    }
    void OnDisable()
    {
        QuestEventBus.OnCollect -= HandleCollect;
        QuestEventBus.OnUnitKilled -= HandleKill;
        QuestEventBus.OnCraft -= HandleCraft;
    }

    // UI: взять следующий доступный
    public void ActivateAnyAvailable()
    {
        var q = quests.FirstOrDefault(x => x.state == QuestProgressState.NotStarted);
        if (q == null) return;
        ActivateById(q.id);
    }

    public void ActivateById(string id)
    {
        var q = quests.FirstOrDefault(x => x.id == id);
        if (q == null) return;

        q.progress = 0;
        q.state = QuestProgressState.Active;
        activeQuestId = q.id;
        FireChanged(q);
    }

    public void CompleteActiveAndAdvance()
    {
        var q = Active;
        if (q == null) return;
        q.state = QuestProgressState.Completed;
        FireChanged(q);

        // автопереход на следующий NotStarted (не обязательно)
        var next = quests.FirstOrDefault(x => x.state == QuestProgressState.NotStarted);
        if (next != null)
        {
            ActivateById(next.id);
        }
    }

    // --- Event handlers ---

    void HandleCollect(string itemId, int amount)
    {
        var q = Active;
        if (q == null || q.state != QuestProgressState.Active) return;
        if (q.kind != QuestKind.Collect) return;
        if (!IdEq(q.targetId, itemId)) return;

        Bump(q, amount);
    }

    void HandleKill(string unitKind)
    {
        var q = Active;
        if (q == null || q.state != QuestProgressState.Active) return;
        if (q.kind != QuestKind.Kill) return;
        if (!IdEq(q.targetId, unitKind)) return;

        Bump(q, 1);
    }

    void HandleCraft(string itemId, int amount)
    {
        var q = Active;
        if (q == null || q.state != QuestProgressState.Active) return;
        if (q.kind != QuestKind.Craft) return;
        if (!IdEq(q.targetId, itemId)) return;

        Bump(q, amount);
    }

    void Bump(QuestConfig q, int delta)
    {
        q.progress = Mathf.Clamp(q.progress + Mathf.Max(0, delta), 0, Mathf.Max(1, q.targetCount));
        if (q.progress >= q.targetCount)
        {
            q.state = QuestProgressState.Completed;
        }
        FireChanged(q);
    }

    static bool IdEq(string a, string b) =>
        !string.IsNullOrWhiteSpace(a) && !string.IsNullOrWhiteSpace(b)
        && string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase);

    void FireChanged(QuestConfig q) => OnQuestChanged?.Invoke(q);

    // Вспомогательное API для UI
    public QuestConfig GetActive() => Active;
    public int GetActiveProgress() => Active?.progress ?? 0;
    public int GetActiveTarget() => Active?.targetCount ?? 0;
    public string GetActiveTitle() => Active?.title ?? "";
    public string GetActiveTargetId() => Active?.targetId ?? "";
    public bool IsActiveCompleted() => Active?.state == QuestProgressState.Completed;
}
