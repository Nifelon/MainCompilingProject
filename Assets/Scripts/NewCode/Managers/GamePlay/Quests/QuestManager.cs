using Game.Actors;
using Game.Items;
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

    // Текущий активный квест (или null)
    QuestConfig Active => quests.FirstOrDefault(q => q.id == activeQuestId);

    // Активен ли сейчас квест нужного типа?
    static bool IsActiveOf(QuestConfig q, QuestKind kind)
    {
        return q != null
            && q.state == QuestProgressState.Active
            && q.kind == kind;
    }

    void OnEnable()
    {
        // Kill (типизированный) + legacy для плавной миграции
        QuestEventBus.OnUnitKilled += HandleKillTyped;
        QuestEventBus.OnUnitKilledLegacy += HandleKillLegacy;

        // Collect считаем по НАЛИЧИЮ в инвентаре → слушаем инвентарь
        InventoryService.OnChanged += HandleInventoryChanged;

        // Эти события — лишь триггеры пересчёта (реальный прогресс берём из InventoryService)
        QuestEventBus.OnCollect += HandleCollectTyped;
        QuestEventBus.OnCollectLegacy += HandleCollectLegacy;
    }

    void OnDisable()
    {
        QuestEventBus.OnUnitKilled -= HandleKillTyped;
        QuestEventBus.OnUnitKilledLegacy -= HandleKillLegacy;

        InventoryService.OnChanged -= HandleInventoryChanged;

        QuestEventBus.OnCollect -= HandleCollectTyped;
        QuestEventBus.OnCollectLegacy -= HandleCollectLegacy;
    }

    // === API ===

    // Взять первый доступный квест
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

        // Для Collect сразу посчитать прогресс по текущему инвентарю
        if (q.kind == QuestKind.Collect)
            RecalcCollectProgress(q);

        FireChanged(q);
    }

    public void CompleteActiveAndAdvance()
    {
        var q = Active;
        if (q == null) return;

        q.state = QuestProgressState.Completed;
        FireChanged(q);

        var next = quests.FirstOrDefault(x => x.state == QuestProgressState.NotStarted);
        if (next != null) ActivateById(next.id);
    }

    // === Kill ===

    // Типизированное событие убийства
    void HandleKillTyped(ActorIdKind kind, NPCEnums npc, CreatureEnums creature)
    {
        var q = Active;
        if (!IsActiveOf(q, QuestKind.Kill)) return;
        if (!q.killTarget.Matches(kind, npc, creature)) return;
        Bump(q, 1);
    }

    // Legacy (строка) — на переходный период
    void HandleKillLegacy(string unitKind)
    {
        var q = Active;
        if (!IsActiveOf(q, QuestKind.Kill)) return;
        if (!IdEq(q.killTarget.Label, unitKind)) return;
        Bump(q, 1);
    }

    // === Collect — по наличию предмета в инвентаре ===

    // Любое изменение инвентаря → пересчёт активного Collect
    void HandleInventoryChanged()
    {
        var q = Active;
        if (q == null || q.state != QuestProgressState.Active) return;
        if (q.kind != QuestKind.Collect) return;

        RecalcCollectProgress(q);
        FireChanged(q);
    }

    // Эти два — просто триггеры пересчёта (на случай прямых RaiseCollect)
    void HandleCollectTyped(ItemId id, int amount)
    {
        HandleInventoryChanged();
    }

    void HandleCollectLegacy(string id, int amount)
    {
        HandleInventoryChanged();
    }

    void RecalcCollectProgress(QuestConfig q)
    {
        int have = InventoryService.Count(q.itemTarget);
        q.progress = Mathf.Clamp(have, 0, Mathf.Max(1, q.targetCount));
        if (q.progress >= q.targetCount)
            q.state = QuestProgressState.Completed;
    }

    // === Helpers ===

    void Bump(QuestConfig q, int delta)
    {
        q.progress = Mathf.Clamp(q.progress + Mathf.Max(0, delta), 0, Mathf.Max(1, q.targetCount));
        if (q.progress >= q.targetCount)
            q.state = QuestProgressState.Completed;
        FireChanged(q);
    }

    static bool IdEq(string a, string b) =>
        !string.IsNullOrWhiteSpace(a) && !string.IsNullOrWhiteSpace(b)
        && string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase);

    void FireChanged(QuestConfig q) => OnQuestChanged?.Invoke(q);

    // === Удобное API для UI ===
    public QuestConfig GetActive() => Active;
    public int GetActiveProgress() => Active?.progress ?? 0;
    public int GetActiveTarget() => Active?.targetCount ?? 0;
    public string GetActiveTitle() => Active?.title ?? "";
    public string GetActiveTargetLabel() => Active?.GetTargetLabel() ?? "";
    public bool IsActiveCompleted() => Active != null && Active.state == QuestProgressState.Completed;
}
