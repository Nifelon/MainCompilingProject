// Assets/Scripts/NPC/NPCAgent.cs
using UnityEngine;
using Game.Actors;          // ActorProfile (displayName/portrait)
using Game.World.NPC;       // NPCProfile
// using Game.World;        // ���� IInteractable ����� � ���� � ������ ���������� � �������� ���

/// <summary>
/// �������� ����� ��� NPC:
/// - ����������� � NPCService ��� ������ �� ����
/// - ���������� ������� (HP/��������/� �.�.)
/// - �������� UI/������� � ���� (��������/������)
/// - ������� IInteractable (E)
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

    [Header("Scene Services (optional, ����� ������ ����� Init/ConfigureUI)")]
    [SerializeField] private NPCService npcService;
    [SerializeField] private DialogPanelTMP dialogPanel;
    [SerializeField] private QuestManager questManager;

    // ���� (���� ����� �� �������)
    CommanderDialogController _commander;
    SoldierDialogController _soldier;

    // ------------------- ������������� -------------------

    /// <summary>��������� ������������� (����� Get �� ����): �������, ������, ������.</summary>
    public void Init(NPCProfile profile, int campId, NPCService service = null)
    {
        Profile = profile;
        CampId = campId;
        npcService = service ?? npcService;

        // ��������� ������� � GO (HP/��������/���� � �.�.)
        if (Profile) Profile.ApplyTo(gameObject);

        // �������������� � ������� (������� ���������� id)
        if (npcService) RegisteredId = npcService.Register(this, profile, campId);

        // ��� ����� (���� ����� �� �������)
        _commander = GetComponent<CommanderDialogController>();
        _soldier = GetComponent<SoldierDialogController>();
    }

    /// <summary>��������� UI/������ � ����. ���� ����� ����� Init.</summary>
    public void ConfigureUI(DialogPanelTMP panel, QuestManager quests)
    {
        dialogPanel = panel ?? dialogPanel;
        questManager = quests ?? questManager;

        // �������� (��� �����/�����)
        if (_commander != null)
        {
            var name = Profile ? Profile.displayName : "��������";
            var face = Profile ? Profile.portrait : null;
            _commander.Init(dialogPanel, questManager, name, face);
            _commander.enabled = true;
        }

        // ������ (�����)
        if (_soldier != null)
        {
            var name = Profile ? Profile.displayName : "������";
            var face = Profile ? Profile.portrait : null;
            _soldier.Init(dialogPanel, name, face, Profile ? Profile.dialogueRef : null);
            _soldier.enabled = true;
        }
    }

    // ------------------- IInteractable -------------------

    public string Hint =>
        (_commander && _commander.enabled) ? _commander.Hint :
        (_soldier && _soldier.enabled) ? _soldier.Hint :
        "E � ����������";

    public void Interact(GameObject actor)
    {
        if (_commander && _commander.enabled) _commander.Interact(actor);
        else if (_soldier && _soldier.enabled) _soldier.Interact(actor);
    }

    // ------------------- LIFECYCLE / POOL -------------------

    void OnDisable()
    {
        // ������� � ���/�������� � ������� �����������
        if (npcService && RegisteredId != 0)
        {
            npcService.Unregister(RegisteredId);
            RegisteredId = 0;
        }
    }
}
