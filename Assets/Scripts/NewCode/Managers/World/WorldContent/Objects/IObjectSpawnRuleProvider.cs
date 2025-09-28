using Game.World.Map.Biome;

namespace Game.World.Objects.Spawning
{
    /// Отдаёт профиль спавна под нужный биом (и, при желании, под контекст клетки).
    public interface IObjectSpawnRuleProvider
    {
        BiomeSpawnProfile GetProfile(BiomeType biome);
        bool TryGetProfile(BiomeType biome, out BiomeSpawnProfile profile);
    }
}