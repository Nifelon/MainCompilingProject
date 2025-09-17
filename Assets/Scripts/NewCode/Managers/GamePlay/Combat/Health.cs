using UnityEngine;

public class Health : MonoBehaviour
{
    public int maxHP = 100;
    public int currentHP = 100;
    public int regenPerTick = 3;
    public Transform respawnPoint;
    public string unitKind = "Unit";

    void OnEnable() { TickManager.OnTick += OnTick; }
    void OnDisable() { TickManager.OnTick -= OnTick; }

    void Start() { currentHP = Mathf.Clamp(currentHP, 0, maxHP); }
    void OnTick()
    {
        if (currentHP > 0 && currentHP < maxHP)
            currentHP = Mathf.Min(maxHP, currentHP + Mathf.Max(0, regenPerTick));
    }

    public void ApplyDamage(DamageInfo info)
    {
        if (currentHP <= 0) return;
        currentHP = Mathf.Max(0, currentHP - Mathf.Max(0, info.amount));
        FloatingDamageService.Raise(info); // цифры урона (см. ниже)
        if (currentHP == 0)
        {
            // смерть → «моментальный» респаун
            if (respawnPoint)
            {
                transform.position = respawnPoint.position;
                currentHP = maxHP;
            }
            QuestEventBus.RaiseUnitKilled(unitKind); // для квеста Kill:Wolf
        }
    }
}