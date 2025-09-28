using System.Collections.Generic;
using UnityEngine;

namespace Game.World.Objects.Spawning
{
    /// ������� ���� �������� ��� ����� (������-������, ��� ���/����).
    public interface IObjectSpawnPlanner
    {
        /// ������� ����������������� ������ ��������� ��� �����.
        /// originCell � �����-������ ���� ����� � �������.
        /// chunkKey   � ((uint)cx | ((ulong)cy << 32)) � ����� ��� ���������� id.
        List<ObjectInstanceData> PlanChunk(Vector2Int originCell, int chunkSize, int worldSeed, ulong chunkKey);
    }
}