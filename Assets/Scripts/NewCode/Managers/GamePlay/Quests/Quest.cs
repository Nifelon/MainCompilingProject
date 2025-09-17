using UnityEngine;
[System.Serializable]
public class Quest
{
    public string id, title; [TextArea] public string description;
    public QuestState state = QuestState.Inactive;
    public int targetAmount = 1, progress = 0;
    public string metric; // "Kill:Wolf" / "Collect:Berry" / "Craft:Patch"
    public void Activate() { if (state == QuestState.Inactive) { state = QuestState.Active; progress = 0; } }
    public void AddProgress(int amt) { if (state != QuestState.Active) return; progress += amt; if (progress >= targetAmount) state = QuestState.Done; }
}