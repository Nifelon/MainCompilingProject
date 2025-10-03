using UnityEngine;

/// Командир: выдаёт/завершает квест «волки» и меняет цвет маркера лагеря.
public class NPC_Commander : MonoBehaviour, IInteractable
{
    [Header("IDs")]
    [SerializeField] private string questId = "wolf";   // id записи в QuestManager.quests

    [Header("Ссылки (можно оставить пустыми)")]
    [SerializeField] private QuestManager questManager;         // найдётся автоматически, если пусто
    [SerializeField] private MapCampMarkerBinder campBinder;    // найдётся автоматически, если пусто

    [Header("Диалоги")]
    [TextArea]
    public string[] intro =
    {
        "Наши люди страдают от волков. Возьми эту задачу.",
        "Очисти рощу и доложи мне."
    };
    [TextArea]
    public string[] inProgress =
    {
        "Как продвигается зачистка? Вернись, когда будет безопасно."
    };
    [TextArea]
    public string[] done =
    {
        "Отличная работа. Лагерь обязан тебе."
    };

    [SerializeField] private string hint = "E — Поговорить (Командир)";
    public string Hint => hint;                                  // Требуемое свойство интерфейса

    private void Awake()
    {
        if (!questManager) questManager = Object.FindFirstObjectByType<QuestManager>();
        if (!campBinder) campBinder = Object.FindFirstObjectByType<MapCampMarkerBinder>();

    }

    public void Interact(GameObject actor)
    {
        var panel = Object.FindObjectOfType<DialogPanel>();
        if (panel == null) { Debug.LogWarning("Командир: не найден DialogPanel."); return; }
        if (questManager == null) { DialogUtil.ShowLines(panel, "Командир", new[] { "Квестовый менеджер не найден." }); return; }

        // 1) Получаем активный квест и тот, что нам нужен по id
        var active = questManager.GetActive(); // вместо questManager.Active
        var q = questManager.quests.Find(x => x.id == questId); // нужный квест
        if (q == null)
        {
            DialogUtil.ShowLines(panel, "Командир", new[] { $"Квест '{questId}' не найден." });
            return;
        }

        // 2) Если не активен или активен другой — предложить взять
        if (active == null || active.id != q.id)
        {
            DialogUtil.ShowLines(panel, "Командир", intro, onClose: () =>
            {
                questManager.ActivateById(q.id);  // имя метода из QuestManager
                campBinder?.SetActive();          // если нужно перекрасить лагерь в жёлтый
            });
            return;
        }

        // 3) Активен именно этот: проверяем прогресс/завершение
        bool isDone = (q.state == QuestProgressState.Completed) || (q.progress >= q.targetCount);
        if (isDone)
        {
            DialogUtil.ShowLines(panel, "Командир", done, onClose: () =>
            {
                questManager.CompleteActiveAndAdvance(); // помечаем и берём следующий (если есть)
                campBinder?.SetDone();                   // лагерь → зелёный
            });
        }
        else
        {
            DialogUtil.ShowLines(panel, "Командир", inProgress);
        }
    }
}