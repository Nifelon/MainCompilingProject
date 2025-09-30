// ReservationService.cs
using System.Collections.Generic;
using UnityEngine;
using Game.World.Signals; // ← чтобы слушать WorldRegen

public class ReservationService : MonoBehaviour, IReservationService
{
    private readonly Dictionary<Vector2Int, int> _cells = new();

    // НОВОЕ: полный сброс
    public void ClearAll() => _cells.Clear();

    // НОВОЕ: подписка на реген мира
    private void OnEnable() => WorldSignals.OnWorldRegen += HandleWorldRegen;
    private void OnDisable() => WorldSignals.OnWorldRegen -= HandleWorldRegen;
    private void HandleWorldRegen() => ClearAll();

    public int ReserveCircle(Vector2Int c, int r, ReservationMask mask)
    {
        if (mask == ReservationMask.None) return 0;
        int wrote = 0;
        int m = (int)mask;
        int r2 = r * r;

        for (int dx = -r; dx <= r; dx++)
            for (int dy = -r; dy <= r; dy++)
            {
                if (dx * dx + dy * dy > r2) continue;
                var p = new Vector2Int(c.x + dx, c.y + dy);
                _cells.TryGetValue(p, out int cur);
                _cells[p] = cur | m;
                wrote++;
            }
        return wrote;
    }

    public void ReleaseCircle(Vector2Int c, int r, ReservationMask mask)
    {
        if (mask == ReservationMask.None) return;
        int clear = ~((int)mask);
        int r2 = r * r;

        for (int dx = -r; dx <= r; dx++)
            for (int dy = -r; dy <= r; dy++)
            {
                if (dx * dx + dy * dy > r2) continue;
                var p = new Vector2Int(c.x + dx, c.y + dy);
                if (_cells.TryGetValue(p, out int cur))
                {
                    cur &= clear;
                    if (cur == 0) _cells.Remove(p);
                    else _cells[p] = cur;
                }
            }
    }

    public bool IsReserved(Vector2Int cell, ReservationMask mask)
        => _cells.TryGetValue(cell, out int cur) && (cur & (int)mask) != 0;

#if UNITY_EDITOR
    [SerializeField] float cellSize = 1f; // опционально подхватывай из PoolManager
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 0, 0, 0.12f);
        foreach (var kv in _cells)
        {
            var c = kv.Key;
            Gizmos.DrawCube(new Vector3((c.x + 0.5f) * cellSize, (c.y + 0.5f) * cellSize, 0),
                            new Vector3(cellSize, cellSize, 1));
        }
    }
#endif
}
