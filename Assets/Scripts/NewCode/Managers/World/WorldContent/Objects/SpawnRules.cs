// Assets/Game/World/Objects/SpawnRule.cs
using UnityEngine;
using Game.World.Map.Biome;
using Game.World.Objects;

//public enum SpawnMode { BlueNoise, Clustered, Uniform }

[CreateAssetMenu(menuName = "World/Spawn Rule", fileName = "SpawnRule_")]
public class SpawnRule : ScriptableObject
{
    [Header("Что спавним")]
    public ObjectType objectType;
    public Vector2Int footprint = new(1, 1);
    public int variants = 1;

    [Header("Где спавним")]
    public BiomeType[] allowedBiomes;

    [Header("Сколько спавним (на чанк 64×64)")]
    public int targetPerChunk = 20;

    [Header("Режим раскладки")]
    public SpawnMode mode = SpawnMode.Clustered;
    public Vector2Int clusterCountRange = new(3, 6);  // для Clustered
    public float minDistanceSameType = 2f;
    public float minDistanceAny = 1f;
    public bool avoidOtherObjects = true;

    [Header("Шумовая маска")]
    public bool useNoiseGate = true;
    [Range(0, 1)] public float noiseThreshold = 0.55f;
    public float noiseScale = 0.06f;
}
