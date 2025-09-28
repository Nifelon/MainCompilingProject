using UnityEngine;

namespace Game.World.Services
{
    public interface IVisibilityService
    {
        int TileRadius { get; }                 // ������ ��������� � "�������", ��� � ������
        float CellSize { get; }                 // ������ ������ � world units
        Vector2 PlayerPosWorld { get; }         // ������� ������ � world
        float LoadRadiusWorld { get; }          // ������ ��� �������� (����������)
        float UnloadRadiusWorld { get; }        // ������ ��� �������� (����������)
        bool ChunkIntersectsLoad(Rect worldRect);
        bool ChunkIntersectsKeep(Rect worldRect);
        bool IsPointVisibleForLoad(Vector2 worldPos);
        bool IsPointVisibleForKeep(Vector2 worldPos);
    }
}