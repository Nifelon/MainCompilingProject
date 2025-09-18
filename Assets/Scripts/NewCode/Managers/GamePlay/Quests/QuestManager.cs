using System;
using System.Collections.Generic;
using UnityEngine;

public enum QuestType { Kill, Collect, Craft }

[Serializable]
public class Quest
{
    public string id;
    public QuestType type;
    public string targetId;         // "Wolf", "Berry", "Patch"
    public int targetCount = 1;
    [NonSerialized] public int progress = 0;
    [NonSerialized] public bool isActive = false;
    [NonSerialized] public bool isDone = false;

    public string Title =>
        type switch
        {
            QuestType.Kill => $"Убить {targetId}",
            QuestType.Collect => $"Собрать {targetId}",
            QuestType.Craft => $"Скрафтить {targetId}",
            _ => id
        };
}

public class QuestManager : MonoBehaviour
{
    [Tooltip("Список доступных квестов (заполняем в инспекторе)")]
    public List<Quest> quests = new();

    public Quest Active { get; private set; }

    public event Action<Quest> OnQuestChanged;   // активирован/завершён/прогресс

    void OnEnable()
    {
        QuestEventBus.OnUnitKilled += HandleKill;
        QuestEventBus.OnCollect += HandleCollect;
        QuestEventBus.OnCraft += HandleCraft;
    }
    void OnDisable()
    {
        QuestEventBus.OnUnitKilled -= HandleKill;
        QuestEventBus.OnCollect -= HandleCollect;
        QuestEventBus.OnCraft -= HandleCraft;
    }

    public void ActivateAnyAvailable()
    {
        if (Active != null && !Active.isDone) return;
        foreach (var q in quests)
        {
            if (!q.isDone)
            {
                Activate(q);
                return;
            }
        }
        Debug.Log("[Quests] Нет доступных квестов.");
    }

    public void ActivateById(string id)
    {
        var q = quests.Find(x => x.id == id);
        if (q != null) Activate(q);
    }

    void Activate(Quest q)
    {
        Active = q;
        q.isActive = true;
        q.progress = 0;
        OnQuestChanged?.Invoke(q);
        Debug.Log($"[Quests] Активирован: {q.Title} ({q.progress}/{q.targetCount})");
    }

    void CompleteActiveIfReady()
    {
        if (Active != null && Active.progress >= Active.targetCount)
        {
            Active.isDone = true;
            Active.isActive = false;
            OnQuestChanged?.Invoke(Active);
            Debug.Log($"[Quests] Завершён: {Active.Title}");
            Active = null;
        }
    }

    void Bump(QuestType type, string id, int count)
    {
        if (Active == null || Active.isDone) return;
        if (Active.type != type) return;
        if (!string.Equals(Active.targetId, id, StringComparison.OrdinalIgnoreCase)) return;

        Active.progress = Mathf.Min(Active.progress + count, Active.targetCount);
        OnQuestChanged?.Invoke(Active);
        CompleteActiveIfReady();
    }

    void HandleKill(string unitKind) => Bump(QuestType.Kill, unitKind, 1);
    void HandleCollect(string id, int c) => Bump(QuestType.Collect, id, c);
    void HandleCraft(string id, int c) => Bump(QuestType.Craft, id, c);
}