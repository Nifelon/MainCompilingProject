// Game/Combat/Health.cs
using JetBrains.Annotations;
using UnityEngine;

public class Health : MonoBehaviour
{
    [Header("Unit")]
    public UnitKind unitKind = UnitKind.Wolf;          // тип юнита (для квестов/логики)
    [SerializeField] public string questIdOverride = ""; // строковый ID цели для Kill-квестов (если нужно переопределить)
    public bool countsForKillQuests = true;            // учитывать смерть в Kill-квестах

    /// <summary>Публичный доступ к квестовому ID (для профилей/спавнера).</summary>
    public string QuestIdOverride
    {
        get => questIdOverride;
        set => questIdOverride = value ?? string.Empty;
    }

    [Header("HP")]
    public int maxHP = 100;
    public int currentHP = 100;
    [Tooltip("HP за тик (0 — регена нет)")]
    public int regenPerTick = 0;

    [Header("Respawn (для игрока)")]
    public Transform respawnPoint;

    private bool _dead;

    // ===== Жизненный цикл =====
    private void OnEnable()
    {
        _dead = false;
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);
        TickManager.OnTick += OnTick;   // подписка на общий тик
    }

    private void OnDisable()
    {
        TickManager.OnTick -= OnTick;
    }

    private void OnTick()
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

        Vector3 pos = info.worldHitPos != Vector3.zero
            ? info.worldHitPos
            : (transform.position + Vector3.up * 0.5f);

        FloatingDamageService.Show(pos, Mathf.Max(0, info.amount), c, info.isCrit);

        currentHP = Mathf.Max(0, currentHP - Mathf.Max(0, info.amount));
        if (currentHP == 0)
            Die(info);
    }

    private void Die(DamageInfo lastHit)
    {
        if (_dead) return;
        _dead = true;

        // Kill-квест
        if (countsForKillQuests)
            QuestEventBus.RaiseUnitKilled(QuestTargetId);

        // Игрок: мгновенный респаун
        if (unitKind == UnitKind.Player || respawnPoint != null)
        {
            currentHP = maxHP;
            if (respawnPoint)
                transform.position = respawnPoint.position;

            _bleedTicksLeft = 0;
            _bleedDamagePerTick = 0;
            _dead = false;
            return;
        }

        // Для пула — просто выключаем
        gameObject.SetActive(false);
    }

    /// <summary>Какой ID пойдёт в квесты при смерти юнита.</summary>
    public string QuestTargetId =>
        string.IsNullOrEmpty(questIdOverride) ? unitKind.ToString() : questIdOverride;

    // ===== Эффекты/DoT =====
    private int _bleedTicksLeft;
    private int _bleedDamagePerTick;

    private void TickEffects()
    {
        if (_dead || currentHP <= 0) return;

        if (_bleedTicksLeft > 0)
        {
            _bleedTicksLeft--;
            int dmg = Mathf.Min(_bleedDamagePerTick, currentHP);
            FloatingDamageService.Show(transform.position + Vector3.up * 0.5f, dmg, new Color(0.85f, 0.2f, 0.2f), false);
            currentHP -= dmg;

            if (currentHP <= 0)
                Die(new DamageInfo { target = transform, amount = dmg });
        }
    }

    public void ApplyBleedFromBaseDamage(int baseDamage)
    {
        _bleedDamagePerTick = Mathf.Max(1, Mathf.RoundToInt(baseDamage * 0.10f));
        _bleedTicksLeft = 5;
    }

    // ===== Конфиг под пул/спавн =====
    /// <summary>Единая инициализация параметров для префаба при спавне/реюзе.</summary>
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

    /// <summary>Сбросить временные флаги/эффекты перед возвратом в пул.</summary>
    public void ResetForPool()
    {
        _dead = false;
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);
        _bleedTicksLeft = 0;
        _bleedDamagePerTick = 0;
    }
}
