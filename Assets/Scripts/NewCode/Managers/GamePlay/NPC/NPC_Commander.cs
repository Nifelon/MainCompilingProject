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
        var panel = Object.FindFirstObjectByType<DialogPanel>();
        if (!questManager)
        {
            DialogUtil.ShowLines(panel, "Командир", new[] { "Квестовый менеджер не найден в сцене." });
            return;
        }

        var active = questManager.Active;                   // текущий активный квест (или null)
        var q = questManager.quests.Find(x => x.id == questId); // запись нужного квеста

        // 1) Не активен и не выполнен — выдаём
        if (active == null && (q == null || !q.isDone))
        {
            DialogUtil.ShowLines(panel, "Командир", intro, onClose: () =>
            {
                questManager.ActivateById(questId);        // менеджер сам слушает QuestEventBus и двигает прогресс
                campBinder?.SetActive();                   // лагерь -> жёлтый
            });
            return;
        }

        // 2) Активен именно этот квест — прогресс/завершение
        if (active != null && active.id == questId)
        {
            if (active.isDone || active.progress >= active.targetCount)
            {
                DialogUtil.ShowLines(panel, "Командир", done, onClose: () =>
                {
                    campBinder?.SetDone();                 // лагерь -> зелёный
                });
            }
            else
            {
                DialogUtil.ShowLines(panel, "Командир", inProgress);
            }
            return;
        }

        // 3) Уже выполнен (Active == null, но запись помечена isDone)
        if (active == null && q != null && q.isDone)
        {
            campBinder?.SetDone();
            DialogUtil.ShowLines(panel, "Командир", done);
            return;
        }

        // 4) Другой активный квест
        DialogUtil.ShowLines(panel, "Командир", new[] { "Сначала разберись с текущей задачей." });
    }
}