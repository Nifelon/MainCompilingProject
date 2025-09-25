using System;
using UnityEngine;

[Flags]
public enum ReservationMask
{
    None = 0,
    Nature = 1 << 0,   // �������/�����/�����
    Camps = 1 << 1,   // ��� ������
    All = ~0
}

public interface IReservationService
{
    // �������� ���� ������ ������� mask. ���������� ���-�� ���������� ������.
    int ReserveCircle(Vector2Int center, int radius, ReservationMask mask);

    // ����� ����� mask � �����. (������� ����, ���� ����� ����� ������.)
    void ReleaseCircle(Vector2Int center, int radius, ReservationMask mask);

    // ���������, ���� �� ����������� mask � ������.
    bool IsReserved(Vector2Int cell, ReservationMask mask);
}