using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ClimatZones", menuName = "Data/Map/ClimatZone")]
public class ClimateZones : ScriptableObject
{
    public string zoneName;
    public ClimateZoneType zoneType;
    public Color zoneColor;
    public BiomeType defaultBiome;
    [Header("����� � �� ����� ���������")]
    public List<BiomeSpawnChance> biomeChances;
    [Header("�������� �� ����� (� ��������� ������)")]
    [Range(0, 100)] public float startPercent;   
    [Range(0, 100)] public float endPercent;     
}
[System.Serializable]
public class BiomeSpawnChance
{
    public BiomeType biome;
    [Range(0f, 1f)] public float minPerlinValue;
    [Range(0f, 1f)] public float maxPerlinValue; // ��� ��������� ����� ����� � �������� ������������� ����
}
