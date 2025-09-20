using UnityEngine;

public class PlayerMeleeController : MonoBehaviour
{
    public Transform attackOrigin; public float attackRadius = 1.5f;
    public int minDamage = 8, maxDamage = 14; [Range(0, 1)] public float critChance = 0.2f;
    public float attackCooldown = 0.3f; float _cd;

    void Update()
    {
        _cd -= Time.deltaTime;
        if (Input.GetMouseButtonDown(0)) TryAttackSingle();  // ËÊÌ
        if (Input.GetKeyDown(KeyCode.Alpha2)) TryAttackWhirl(); // «2»
    }

    void TryAttackSingle()
    {
        if (_cd > 0f) return; _cd = attackCooldown;
        var t = FindNearestTarget(); if (!t) return;
        var dmg = RollDamage(out bool crit);
        var hit = t.transform.position + Vector3.up * 0.5f;
        t.ApplyDamage(new DamageInfo { source = transform, target = t.transform, amount = dmg, isCrit = crit, worldHitPos = hit });
    }

    void TryAttackWhirl()
    {
        if (_cd > 0f) return; _cd = attackCooldown;
        var all = Object.FindObjectsByType<Health>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var h in all)
        {
            if (h.currentHP <= 0 || h.transform == transform) continue;
            if ((h.transform.position - (attackOrigin ? attackOrigin.position : transform.position)).sqrMagnitude <= attackRadius * attackRadius)
            {
                var dmg = Mathf.CeilToInt(RollDamage(out bool crit) * 0.8f);
                var hit = h.transform.position + Vector3.up * 0.5f;
                h.ApplyDamage(new DamageInfo { source = transform, target = h.transform, amount = dmg, isCrit = crit, worldHitPos = hit });
            }
        }
    }

    Health FindNearestTarget()
    {
        Health best = null; float bestSqr = float.MaxValue;
        var all = Object.FindObjectsByType<Health>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var h in all)
        {
            if (h.currentHP <= 0 || h.transform == transform) continue;
            float sqr = (h.transform.position - (attackOrigin ? attackOrigin.position : transform.position)).sqrMagnitude;
            if (sqr <= attackRadius * attackRadius && sqr < bestSqr) { best = h; bestSqr = sqr; }
        }
        return best;
    }
    int RollDamage(out bool crit) { int baseD = Random.Range(minDamage, maxDamage + 1); crit = Random.value < critChance; return crit ? Mathf.RoundToInt(baseD * 1.5f) : baseD; }
}