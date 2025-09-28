using UnityEngine;

namespace Game.World.NPC
{
    /// <summary>
    /// Лёгкий компонент на общем префабе NPC.
    /// Вызывайте Init(profile, campId) сразу после инстанса в спавнере.
    /// </summary>
    [DisallowMultipleComponent]
    public class NPCAgent : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour npcServiceRef;   // INPCService (опционально через инспектор)
        private INPCService _service;

        [field: SerializeField, Tooltip("Профиль, с которым заспавнен NPC (ставится спавнером).")]
        public NPCProfile Profile { get; private set; }

        [field: SerializeField] public int CampId { get; private set; } = -1;

        /// <summary>Уникальный id в реестре (0 — не зарегистрирован).</summary>
        public ulong RegisteredId { get; internal set; }

        void Awake()
        {
            _service = npcServiceRef as INPCService ?? FindFirstObjectByType<NPCService>(FindObjectsInactive.Exclude);
            if (_service == null)
                Debug.LogError("[NPCAgent] INPCService не найден в сцене.");
        }

        void OnDisable()
        {
            if (_service != null && RegisteredId != 0)
            {
                _service.Unregister(RegisteredId);
                RegisteredId = 0;
            }
        }

        /// <summary>Внедрение сервиса извне (если используете DI/ServiceLocator).</summary>
        public void SetService(INPCService svc) => _service = svc;

        /// <summary>Вызывается спавнером сразу после инстанса.</summary>
        public void Init(NPCProfile profile, int campId)
        {
            Profile = profile;
            CampId = campId;

            if (_service == null)
            {
                Debug.LogError("[NPCAgent] Нет сервиса реестра — не зарегистрирован.");
                return;
            }

            if (RegisteredId == 0)
                RegisteredId = _service.Register(this, profile, campId);
        }
    }
}