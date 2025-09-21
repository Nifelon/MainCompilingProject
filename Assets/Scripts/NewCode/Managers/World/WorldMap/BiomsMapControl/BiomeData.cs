using UnityEngine;

[CreateAssetMenu(fileName = "BiomeData", menuName = "Data/Map/Biome")]
public class BiomeData : ScriptableObject
{
    [Header("�������� ����������")]
    public BiomeType Type;
    public string BiomeName;
    public ClimateZoneType ClimateType;
    public Color ColorMap;
    public Sprite SpriteMap;
    //[Header("������������� ���������")]
    //public float TemperatureMin;       
    //public float TemperatureMax;
}
