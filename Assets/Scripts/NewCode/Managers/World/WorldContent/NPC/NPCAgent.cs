using UnityEngine;

namespace Game.World.NPC
{
    /// <summary>
    /// ˸���� ��������� �� ����� ������� NPC.
    /// ��������� Init(profile, campId) ����� ����� �������� � ��������.
    /// </summary>
    [DisallowMultipleComponent]
    public class NPCAgent : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour npcServiceRef;   // INPCService (����������� ����� ���������)
        private INPCService _service;

        [field: SerializeField, Tooltip("�������, � ������� ��������� NPC (�������� ���������).")]
        public NPCProfile Profile { get; private set; }

        [field: SerializeField] public int CampId { get; private set; } = -1;

        /// <summary>���������� id � ������� (0 � �� ���������������).</summary>
        public ulong RegisteredId { get; internal set; }

        void Awake()
        {
            _service = npcServiceRef as INPCService ?? FindFirstObjectByType<NPCService>(FindObjectsInactive.Exclude);
            if (_service == null)
                Debug.LogError("[NPCAgent] INPCService �� ������ � �����.");
        }

        void OnDisable()
        {
            if (_service != null && RegisteredId != 0)
            {
                _service.Unregister(RegisteredId);
                RegisteredId = 0;
            }
        }

        /// <summary>��������� ������� ����� (���� ����������� DI/ServiceLocator).</summary>
        public void SetService(INPCService svc) => _service = svc;

        /// <summary>���������� ��������� ����� ����� ��������.</summary>
        public void Init(NPCProfile profile, int campId)
        {
            Profile = profile;
            CampId = campId;

            if (_service == null)
            {
                Debug.LogError("[NPCAgent] ��� ������� ������� � �� ���������������.");
                return;
            }

            if (RegisteredId == 0)
                RegisteredId = _service.Register(this, profile, campId);
        }
    }
}