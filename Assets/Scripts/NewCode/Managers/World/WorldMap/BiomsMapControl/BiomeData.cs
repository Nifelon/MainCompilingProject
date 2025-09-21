using UnityEngine;

[CreateAssetMenu(fileName = "BiomeData", menuName = "Data/Map/Biome")]
public class BiomeData : ScriptableObject
{
    [Header("Основная информация")]
    public BiomeType Type;
    public string BiomeName;
    public ClimateZoneType ClimateType;
    public Color ColorMap;
    public Sprite SpriteMap;
    //[Header("Климатические параметры")]
    //public float TemperatureMin;       
    //public float TemperatureMax;
}
