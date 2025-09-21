// Assets/Game/World/Objects/BiomeSpawnProfile.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using Game.World.Map.Biome;
using Game.World.Objects;

public enum SpawnMode { BlueNoise, Clustered, Uniform }

[Serializable]
public class BiomeObjectRule
{
    [Header("Что")]
    public ObjectType objectType;
    [Min(0)] public int targetPerChunk = 0;   // на чанк 64×64

    [Header("Как")]
    public SpawnMode mode = SpawnMode.Clustered;
    public Vector2Int clusterCountRange = new(3, 6); // для Clustered

    [Header("Дистанции (в клетках)")]
    public float minDistanceSameType = 2f;
    public float minDistanceAny = 1f;
    public bool avoidOtherObjects = true;

    [Header("Шум")]
    public bool useNoiseGate = true;
    [Range(0, 1)] public float noiseThreshold = 0.55f;
    public float noiseScale = 0.06f;

    [Header("Вариативность визуала")]
    public int variants = 1; // обычно = числу спрайтов в ObjectData
}

[CreateAssetMenu(menuName = "World/Biome Spawn Profile", fileName = "BiomeSpawn_")]
public class BiomeSpawnProfile : ScriptableObject
{
    public BiomeType biome;
    [Tooltip("Правила спавна объектов в данном биоме.")]
    public List<BiomeObjectRule> rules = new();

    // (опц.) Глобальные множители для LOD/масштаба карты
    [Header("Глобальные множители (опционально)")]
    [Range(0.1f, 3f)] public float densityMultiplier = 1f;
}