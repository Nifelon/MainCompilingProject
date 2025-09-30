using System;
using UnityEngine;

[Flags]
public enum ReservationMask
{
    None = 0,
    Nature = 1 << 0,   // деревья/камни/кусты
    Camps = 1 << 1,   // сам лагерь
    Creatures = 1 << 2,   // ← НОВОЕ: запрет спавна существ (олени/волки и т.п.)
    All = ~0
}

// IReservationService.cs
public interface IReservationService
{
    int ReserveCircle(Vector2Int center, int radius, ReservationMask mask);
    void ReleaseCircle(Vector2Int center, int radius, ReservationMask mask);
    bool IsReserved(Vector2Int cell, ReservationMask mask);

    // НОВОЕ: полный сброс (на WorldRegen/смену сида)
    void ClearAll();
}