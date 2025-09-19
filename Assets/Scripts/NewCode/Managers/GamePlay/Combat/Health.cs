// Health.cs (фрагменты)
using UnityEngine;

public class Health : MonoBehaviour
{
    [Header("HP")]
    public int maxHP = 100;
    public int currentHP = 100;

    [Tooltip("HP за тик (0 — регена нет)")]
    public int regenPerTick = 0;

    [Header("Quest/Kill tracking")]
    public string unitKind = "Wolf";
    public bool countsForKillQuests = true;

    [Header("Respawn (для игрока)")]
    public Transform respawnPoint;

    bool _dead;

    void OnEnable()
    {
        _dead = false;
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);
        TickManager.OnTick += OnTick;      // ← подписка
    }

    void OnDisable()
    {
        TickManager.OnTick -= OnTick;      // ← отписка (важно!)
    }

    void OnTick()
    {
        // реген только живых и неполных
        if (!_dead && currentHP > 0 && currentHP < maxHP && regenPerTick > 0)
            currentHP = Mathf.Min(maxHP, currentHP + regenPerTick);

        // тикаем DoT/эффекты (см. п.2)
        TickEffects();
    }

    public void ApplyDamage(DamageInfo info)
    {
        if (_dead) return;

        // всплывающий урон (как делали ранее)
        bool isPlayer = respawnPoint != null;
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

        if (countsForKillQuests && !string.IsNullOrEmpty(unitKind))
            QuestEventBus.RaiseUnitKilled(unitKind);

        // Показ лута (для теста — только волк)
        if (LootWindow.Instance != null)
        {
            if (unitKind == "Wolf")
            {
                // без target-typed new — чтобы компилилось на любом C#
                var drop = new System.Collections.Generic.List<(string, int)> { ("Skin", 1) };
                LootWindow.Instance.Show(drop);
            }
        }
        else
        {
            Debug.LogWarning("[Health] LootWindow.Instance == null");
        }

        if (respawnPoint) { /* игрок → респаун */ return; }
        gameObject.SetActive(false);
    }

    // ===== эффекты (кровотечение) =====
    int _bleedTicksLeft;       // сколько тиков осталось
    int _bleedDamagePerTick;   // урон за тик

    void TickEffects()
    {
        if (_dead || currentHP <= 0) return;

        if (_bleedTicksLeft > 0)
        {
            _bleedTicksLeft--;
            var dmg = Mathf.Min(_bleedDamagePerTick, currentHP);
            // показываем красновато-бордовый урон для DoT
            FloatingDamageService.Show(transform.position + Vector3.up * 0.5f, dmg, new Color(0.85f, 0.2f, 0.2f), false);
            currentHP -= dmg;
            if (currentHP <= 0) Die(new DamageInfo { target = transform, amount = dmg });
        }
    }

    // вызвать из способности “Рваная рана”: 70% урона сразу + кровотечение 10% * 5 тиков
    public void ApplyBleedFromBaseDamage(int baseDamage)
    {
        int totalBleed = Mathf.RoundToInt(baseDamage * 0.10f * 5); // 10% * 5 тиков = 50% от базового
        _bleedDamagePerTick = Mathf.Max(1, Mathf.RoundToInt(baseDamage * 0.10f));
        _bleedTicksLeft = 5;
    }
}