using UnityEngine;

namespace Game.World.Map.Biome
{
    public interface IBiomeService
    {
        BiomeType GetBiomeAtPosition(Vector2Int pos);
        Color GetBiomeColor(BiomeType type);
        Sprite GetBiomeSprite(BiomeType type); // ← добавить
        bool IsBiomesReady { get; }           // (опционально, но полезно)
    }
}