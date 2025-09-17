using UnityEngine;

namespace Game.World.Map.Climate
{
    public interface IClimateService
    {
        ClimateZoneType GetClimateZoneAtPosition(Vector2Int pos);
        ClimateZones GetClimateZoneData(ClimateZoneType type);
        bool IsClimateReady { get; }
    }
}