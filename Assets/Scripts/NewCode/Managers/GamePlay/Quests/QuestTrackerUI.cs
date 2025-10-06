using UnityEngine;
using UnityEngine.UI;

public class QuestTrackerUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] QuestManager quests;

    [Header("UI")]
    [SerializeField] Text titleText;
    [SerializeField] Text progressText;
    [SerializeField] Slider progressBar;
    [SerializeField] GameObject completedMark; // галочка / булавка

    void OnEnable()
    {
        if (quests != null) quests.OnQuestChanged += Refresh;
        Refresh(quests != null ? quests.GetActive() : null);
    }

    void OnDisable()
    {
        if (quests != null) quests.OnQuestChanged -= Refresh;
    }

    public void Refresh(QuestConfig q)
    {
        if (q == null)
        {
            SetEmpty();
            return;
        }

        int cur = q.progress;
        int max = Mathf.Max(1, q.targetCount);

        if (titleText) titleText.text = string.IsNullOrEmpty(q.title) ? q.id : q.title;
        if (progressText) progressText.text = $"{cur}/{max} ({q.GetTargetLabel()})";

        if (progressBar)
        {
            progressBar.minValue = 0;
            progressBar.maxValue = max;
            progressBar.value = cur;
        }

        if (completedMark) completedMark.SetActive(q.state == QuestProgressState.Completed);
    }

    void SetEmpty()
    {
        if (titleText) titleText.text = "Нет активного квеста";
        if (progressText) progressText.text = "";
        if (progressBar) progressBar.value = 0;
        if (completedMark) completedMark.SetActive(false);
    }
}
