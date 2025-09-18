using UnityEngine;
using UnityEngine.UI;

public class QuestTrackerUI : MonoBehaviour
{
    [SerializeField] QuestManager questManager;
    [SerializeField] Text titleText;
    [SerializeField] Text progressText;

    void Reset()
    {
        questManager = FindAnyObjectByType<QuestManager>(FindObjectsInactive.Exclude);
        var texts = GetComponentsInChildren<Text>(true);
        if (texts.Length >= 2) { titleText = texts[0]; progressText = texts[1]; }
    }

    void OnEnable()
    {
        if (!questManager) questManager = FindAnyObjectByType<QuestManager>(FindObjectsInactive.Exclude);
        if (questManager) questManager.OnQuestChanged += Refresh;
        Refresh(null);
    }
    void OnDisable()
    {
        if (questManager) questManager.OnQuestChanged -= Refresh;
    }

    void Refresh(Quest _)
    {
        var q = questManager ? questManager.Active : null;
        if (q == null)
        {
            if (titleText) titleText.text = "Квест не выбран";
            if (progressText) progressText.text = "";
            return;
        }
        if (titleText) titleText.text = q.Title;
        if (progressText) progressText.text = $"{q.progress}/{q.targetCount}" + (q.isDone ? " ✓" : "");
    }
}