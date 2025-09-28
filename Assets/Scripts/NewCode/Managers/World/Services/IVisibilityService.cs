using UnityEngine;

namespace Game.World.Services
{
    public interface IVisibilityService
    {
        int TileRadius { get; }                 // радиус видимости в "клетках", как у тайлов
        float CellSize { get; }                 // размер клетки в world units
        Vector2 PlayerPosWorld { get; }         // позиция игрока в world
        float LoadRadiusWorld { get; }          // радиус для загрузки (гистерезис)
        float UnloadRadiusWorld { get; }        // радиус для выгрузки (гистерезис)
        bool ChunkIntersectsLoad(Rect worldRect);
        bool ChunkIntersectsKeep(Rect worldRect);
        bool IsPointVisibleForLoad(Vector2 worldPos);
        bool IsPointVisibleForKeep(Vector2 worldPos);
    }
}