// Game.World/Signals/WorldSignals.cs
using System;

namespace Game.World.Signals
{
    /// ������� ���������� ���� ��� ����.
    public static class WorldSignals
    {
        /// ����������� �����/�� ����� ����������� ���� (����� ���� � �.�.)
        public static event Action OnWorldRegen;

        public static void FireWorldRegen() => OnWorldRegen?.Invoke();
    }
}