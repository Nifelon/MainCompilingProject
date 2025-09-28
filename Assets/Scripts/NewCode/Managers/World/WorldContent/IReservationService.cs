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

public interface IReservationService
{
    // Пометить круг клеток флагами mask. Возвращает кол-во помеченных клеток.
    int ReserveCircle(Vector2Int center, int radius, ReservationMask mask);

    // Снять флаги mask в круге. (Удаляет ключ, если маска стала пустой.)
    void ReleaseCircle(Vector2Int center, int radius, ReservationMask mask);

    // Проверить, есть ли пересечение mask в клетке.
    bool IsReserved(Vector2Int cell, ReservationMask mask);
}