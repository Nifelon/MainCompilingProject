// Game/Combat/Health.cs
using UnityEngine;

public class Health : MonoBehaviour
{
    [Header("Unit")]
    public UnitKind unitKind = UnitKind.Wolf;      // ← вместо string
    [SerializeField] string questIdOverride = "";  // опционально: если нужно иное имя цели в квесте
    public bool countsForKillQuests = true;

    [Header("HP")]
    public int maxHP = 100;
    public int currentHP = 100;
    [Tooltip("HP за тик (0 — регена нет)")]
    public int regenPerTick = 0;

    [Header("Respawn (для игрока)")]
    public Transform respawnPoint;

    bool _dead;

    // ===== Жизненный цикл =====
    void OnEnable()
    {
        _dead = false;
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);
        TickManager.OnTick += OnTick;   // как у тебя было
    }

    void OnDisable()
    {
        TickManager.OnTick -= OnTick;   // как у тебя было
    }

    void OnTick()
    {
        if (!_dead && currentHP > 0 && currentHP < maxHP && regenPerTick > 0)
            currentHP = Mathf.Min(maxHP, currentHP + regenPerTick);

        TickEffects();
    }

    // ===== Бой =====
    public void ApplyDamage(DamageInfo info)
    {
        if (_dead) return;

        bool isPlayer = (unitKind == UnitKind.Player) || respawnPoint != null;
        Color c = isPlayer ? Color.red : (info.isCrit ? new Color(1f, 0.84f, 0f) : Color.white);
        Vector3 pos = info.worldHitPos != Vector3.zero ? info.worldHitPos : (transform.position + Vector3.up * 0.5f);
        FloatingDamageService.Show(pos, Mathf.Max(0, info.amount), c, info.isCrit);

        currentHP = Mathf.Max(0, currentHP - Mathf.Max(0, info.amount));
        if (currentHP == 0) Die(info);
    }

    void Die(DamageInfo lastHit)
    {
        if (_dead) return;
        _dead = true;

        if (countsForKillQuests)
            QuestEventBus.RaiseUnitKilled(QuestTargetId); // строка для QuestManager

        // Игрок: мгновенный респаун
        if (unitKind == UnitKind.Player || respawnPoint != null)
        {
            currentHP = maxHP;
            if (respawnPoint) transform.position = respawnPoint.position;
            _bleedTicksLeft = 0; _bleedDamagePerTick = 0;
            _dead = false;
            return;
        }

        // (по желанию) тестовый лут — оставить как было у тебя или убрать
        // gameObject.SetActive(false) — для пуллинга
        gameObject.SetActive(false);
    }

    public string QuestTargetId =>
        string.IsNullOrEmpty(questIdOverride) ? unitKind.ToString() : questIdOverride;

    // ===== Эффекты/DoT (как у тебя) =====
    int _bleedTicksLeft;
    int _bleedDamagePerTick;

    void TickEffects()
    {
        if (_dead || currentHP <= 0) return;
        if (_bleedTicksLeft > 0)
        {
            _bleedTicksLeft--;
            var dmg = Mathf.Min(_bleedDamagePerTick, currentHP);
            FloatingDamageService.Show(transform.position + Vector3.up * 0.5f, dmg, new Color(0.85f, 0.2f, 0.2f), false);
            currentHP -= dmg;
            if (currentHP <= 0) Die(new DamageInfo { target = transform, amount = dmg });
        }
    }

    public void ApplyBleedFromBaseDamage(int baseDamage)
    {
        _bleedDamagePerTick = Mathf.Max(1, Mathf.RoundToInt(baseDamage * 0.10f));
        _bleedTicksLeft = 5;
    }

    // ===== Конфиг под пул/спавн =====
    public void ApplyConfig(UnitKind kind, int maxHp, int regen = 0, Transform respawn = null, bool forQuests = true)
    {
        unitKind = kind;
        maxHP = Mathf.Max(1, maxHp);
        currentHP = Mathf.Min(currentHP, maxHP);
        regenPerTick = Mathf.Max(0, regen);
        respawnPoint = respawn;
        countsForKillQuests = forQuests;
        _dead = false;
    }

    public void ResetForPool()
    {
        _dead = false;
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);
        _bleedTicksLeft = 0; _bleedDamagePerTick = 0;
    }
}
