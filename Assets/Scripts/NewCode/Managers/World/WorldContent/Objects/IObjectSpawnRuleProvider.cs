using Game.World.Map.Biome;

namespace Game.World.Objects.Spawning
{
    /// ����� ������� ������ ��� ������ ���� (�, ��� �������, ��� �������� ������).
    public interface IObjectSpawnRuleProvider
    {
        BiomeSpawnProfile GetProfile(BiomeType biome);
        bool TryGetProfile(BiomeType biome, out BiomeSpawnProfile profile);
    }
}