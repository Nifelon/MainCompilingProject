// Assets/Scripts/NPC/NPCAgent.cs
using UnityEngine;
using Game.Actors;          // ActorProfile (displayName/portrait)
using Game.World.NPC;       // NPCProfile
// using Game.World;        // если IInteractable лежит у тебя в другом неймспейсе — подключи его

/// <summary>
/// Сквозной агент для NPC:
/// - Регистрация в NPCService при выдаче из пула
/// - Применение профиля (HP/скорости/и т.п.)
/// - Прокидка UI/квестов в роли (Командир/Солдат)
/// - Роутинг IInteractable (E)
/// </summary>
[DisallowMultipleComponent]
public sealed class NPCAgent : MonoBehaviour, IInteractable
{
    [Header("Runtime")]


    public ulong RegisteredId
    {
        get; internal set;
    }
    public NPCProfile Profile { get; private set; }
    public int CampId { get; private set; }

    [Header("Scene Services (optional, можно задать через Init/ConfigureUI)")]
    [SerializeField] private NPCService npcService;
    [SerializeField] private DialogPanelTMP dialogPanel;
    [SerializeField] private QuestManager questManager;

    // Роли (если висят на префабе)
    CommanderDialogController _commander;
    SoldierDialogController _soldier;

    // ------------------- ИНИЦИАЛИЗАЦИЯ -------------------

    /// <summary>Первичная инициализация (после Get из пула): профиль, лагерь, реестр.</summary>
    public void Init(NPCProfile profile, int campId, NPCService service = null)
    {
        Profile = profile;
        CampId = campId;
        npcService = service ?? npcService;

        // Применяем профиль к GO (HP/скорости/мозг и т.п.)
        if (Profile) Profile.ApplyTo(gameObject);

        // Регистрируемся в реестре (получим уникальный id)
        if (npcService) RegisteredId = npcService.Register(this, profile, campId);

        // Кеш ролей (если висят на префабе)
        _commander = GetComponent<CommanderDialogController>();
        _soldier = GetComponent<SoldierDialogController>();
    }

    /// <summary>Прокинуть UI/квесты в роли. Зови сразу после Init.</summary>
    public void ConfigureUI(DialogPanelTMP panel, QuestManager quests)
    {
        dialogPanel = panel ?? dialogPanel;
        questManager = quests ?? questManager;

        // Командир (даёт квест/крафт)
        if (_commander != null)
        {
            var name = Profile ? Profile.displayName : "Командир";
            var face = Profile ? Profile.portrait : null;
            _commander.Init(dialogPanel, questManager, name, face);
            _commander.enabled = true;
        }

        // Солдат (слухи)
        if (_soldier != null)
        {
            var name = Profile ? Profile.displayName : "Солдат";
            var face = Profile ? Profile.portrait : null;
            _soldier.Init(dialogPanel, name, face, Profile ? Profile.dialogueRef : null);
            _soldier.enabled = true;
        }
    }

    // ------------------- IInteractable -------------------

    public string Hint =>
        (_commander && _commander.enabled) ? _commander.Hint :
        (_soldier && _soldier.enabled) ? _soldier.Hint :
        "E — Поговорить";

    public void Interact(GameObject actor)
    {
        if (_commander && _commander.enabled) _commander.Interact(actor);
        else if (_soldier && _soldier.enabled) _soldier.Interact(actor);
    }

    // ------------------- LIFECYCLE / POOL -------------------

    void OnDisable()
    {
        // Возврат в пул/выгрузка — снимаем регистрацию
        if (npcService && RegisteredId != 0)
        {
            npcService.Unregister(RegisteredId);
            RegisteredId = 0;
        }
    }
}
