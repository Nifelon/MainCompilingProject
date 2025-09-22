using System;
using UnityEngine;
using Game.World.Map.Biome;
using Game.World.Objects; // ObjectType

[CreateAssetMenu(menuName = "World/Settlement/Camp Profile", fileName = "CampProfile_Default")]
public class CampProfile : ScriptableObject
{
    [Header("Spawn Conditions")]
    [Tooltip("В каких биомах допустим лагерь.")]
    public BiomeType[] allowedBiomes = { BiomeType.Forest, BiomeType.Plains };

    [Tooltip("Целевое кол-во лагерей на мир (ориентир).")]
    public int targetCamps = 6;

    [Tooltip("Мин. дистанция между лагерями (клетки).")]
    public int minDistanceBetweenCamps = 80;

    [Tooltip("Использовать шум, чтобы не в каждом месте появлялся лагерь.")]
    public bool useNoiseGate = true;

    [Range(0.001f, 0.05f)] public float noiseScale = 0.01f;
    [Range(0f, 1f)] public float noiseThreshold = 0.50f;

    [Tooltip("Доп. соль к мировому сидy (чтобы разные типы лагерей получали разные паттерны).")]
    public int seedSalt = 137;

    [Header("Shape / Layout")]
    [Tooltip("Радиус лагеря в клетках (зона резервации и земля-оверрайд).")]
    [Min(2)] public int campRadius = 8;

    public LayoutMode layout = LayoutMode.Radial;
    public enum LayoutMode { Radial, Grid }

    [Tooltip("Минимальный отступ от центра/друг друга при раскладке (в клетках).")]
    [Min(0)] public int layoutPadding = 2;

    [Header("Ground Override (visual)")]
    [Tooltip("Спрайт 'протоптанной земли' на территории лагеря.")]
    public Sprite campGroundSprite;

    [Tooltip("Как наносить грунт: Круг по радиусу лагеря или Маской компоновки.")]
    public GroundMode groundMode = GroundMode.Circle;
    public enum GroundMode { Circle, LayoutMaskOnly }

    [Header("Composition (Structures)")]
    [Tooltip("Список 'каких объектов и сколько' ставить внутри лагеря.")]
    public CampStructure[] structures;

    [Header("NPC")]
    [Tooltip("Список ролей NPC (солдаты/командир и т.п.).")]
    public CampNpcRole[] npcRoles;

    [Header("Debug/Flags")]
    [Tooltip("Разрешить накладывать лагерь поверх уже сгенерированных природных объектов (обычно false).")]
    public bool allowOverlapWithNature = false;

    [Serializable]
    public class CampStructure
    {
        public ObjectType type = ObjectType.TentSmall;
        [Tooltip("Сколько штук; если min!=max — берём случайное в диапазоне.")]
        public Vector2Int countRange = new Vector2Int(4, 4);

        [Tooltip("Варианты спрайтов (индекс). -1 = случайный.")]
        public int variantIndex = -1;

        [Tooltip("Как распределять этот тип внутри лагеря.")]
        public Distribution dist = Distribution.Ring;
        public enum Distribution { Center, Ring, InnerRing, Grid, RandomScatter }

        [Tooltip("Смещение от центра для типа (в клетках).")]
        public int ringOffset = 0;

        [Tooltip("Минимальная дистанция до объектов того же типа (клетки).")]
        public float minDistanceSameType = 1.0f;
    }

    [Serializable]
    public class CampNpcRole
    {
        public string roleName = "Soldier";
        public GameObject prefab;
        public Vector2Int countRange = new Vector2Int(3, 3);

        [Tooltip("Распределение вокруг центра.")]
        public NpcDistribution dist = NpcDistribution.Ring;
        public enum NpcDistribution { Center, Ring, GuardPerTent, Random }

        [Tooltip("Радиус/отступ от центра (клетки).")]
        public int radius = 6;

        [Tooltip("Если GuardPerTent — отступ от палатки (в клетках).")]
        public Vector2Int guardOffset = new Vector2Int(1, 0);
    }
}