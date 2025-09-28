using System.Collections.Generic;
using UnityEngine;

public class ReservationService : MonoBehaviour, IReservationService
{
    // Храним битовую маску на клетку
    private readonly Dictionary<Vector2Int, int> _cells = new();

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
    {
        return _cells.TryGetValue(cell, out int cur) && (cur & (int)mask) != 0;
    }
}