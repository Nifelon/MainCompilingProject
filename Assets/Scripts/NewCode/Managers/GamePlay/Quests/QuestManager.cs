using UnityEngine;
public class QuestManager : MonoBehaviour
{
    public Quest[] quests;
    void OnEnable() { QuestEventBus.OnUnitKilled += OnKill; QuestEventBus.OnCollect += OnCollect; QuestEventBus.OnCraft += OnCraft; }
    void OnDisable() { QuestEventBus.OnUnitKilled -= OnKill; QuestEventBus.OnCollect -= OnCollect; QuestEventBus.OnCraft -= OnCraft; }
    public void ActivateAnyAvailable() { foreach (var q in quests) { if (q.state == QuestState.Inactive) { q.Activate(); Debug.Log($"[Quest] Activated: {q.title}"); return; } } }
    void OnKill(string kind) { foreach (var q in quests) { if (q.state == QuestState.Active && q.metric == $"Kill:{kind}") q.AddProgress(1); } }
    void OnCollect(string id, int amt) { foreach (var q in quests) { if (q.state == QuestState.Active && q.metric == $"Collect:{id}") q.AddProgress(amt); } }
    void OnCraft(string id, int amt) { foreach (var q in quests) { if (q.state == QuestState.Active && q.metric == $"Craft:{id}") q.AddProgress(amt); } }
}