using System;
using UnityEngine;
using Game.World.Map.Biome;   // BiomeType
using Game.World.Objects;     // ObjectType
using Game.World.NPC;         // NPCSpawnList

namespace Game.World.Camps
{
    [CreateAssetMenu(menuName = "World/Settlement/Camp Profile", fileName = "CampProfile_Default")]
    public class CampProfile : ScriptableObject
    {
        // ---------- Spawn Conditions ----------
        [Header("Spawn Conditions")]
        [Tooltip("В каких биомах допустим лагерь.")]
        public BiomeType[] allowedBiomes = { BiomeType.Forest, BiomeType.Plains };

        [Tooltip("Ориентир по количеству лагерей в мире (нежёсткое).")]
        public int targetCamps = 6;

        [Tooltip("Мин. дистанция между лагерями (в клетках).")]
        public int minDistanceBetweenCamps = 80;

        [Tooltip("Использовать шумовую маску, чтобы лагерь появлялся не в каждой подходящей точке.")]
        public bool useNoiseGate = true;

        [Range(0.001f, 0.05f)] public float noiseScale = 0.01f;
        [Range(0f, 1f)] public float noiseThreshold = 0.50f;

        [Tooltip("Дополнительная соль к мировому сидy (для разведения паттернов разных профилей).")]
        public int seedSalt = 137;

        // ---------- Shape / Layout ----------
        [Header("Shape / Layout")]
        [Tooltip("Радиус лагеря в клетках (зона резервации, грунт, раскладка).")]
        [Min(2)] public int campRadius = 8;

        [Header("Wildlife / Spawn Control")]
        [Min(0)] public int creaturesNoSpawnRadius = 12;

        public LayoutMode layout = LayoutMode.Radial;
        public enum LayoutMode { Radial, Grid }

        [Tooltip("Минимальный отступ при раскладке (в клетках).")]
        [Min(0)] public int layoutPadding = 2;

        // ---------- Ground Override ----------
        [Header("Ground Override (visual)")]
        [Tooltip("Спрайт 'протоптанной земли' внутри лагеря.")]
        public Sprite campGroundSprite;

        [Tooltip("Как наносить грунт: круг по радиусу лагеря или только по маске раскладки.")]
        public GroundMode groundMode = GroundMode.Circle;
        public enum GroundMode { Circle, LayoutMaskOnly }

        // ---------- Structures ----------
        [Header("Composition (Structures)")]
        [Tooltip("Какие объекты и в каком количестве разместить внутри лагеря.")]
        public CampStructure[] structures;

        [Serializable]
        public class CampStructure
        {
            public ObjectType type = ObjectType.TentSmall;

            [Tooltip("Сколько штук (min..max, включительно).")]
            public Vector2Int countRange = new(4, 4);

            [Tooltip("Фиксированный вариант спрайта (-1 = случайный).")]
            public int variantIndex = -1;

            [Tooltip("Схема раскладки данного типа.")]
            public Distribution dist = Distribution.Ring;
            public enum Distribution { Center, Ring, InnerRing, Grid, RandomScatter }

            [Tooltip("Смещение кольца от центра (в клетках).")]
            public int ringOffset = 0;

            [Tooltip("Мин. дистанция до объектов этого же типа (в клетках).")]
            public float minDistanceSameType = 1f;
        }

        // ---------- NPC (Assets-first) ----------
        [Header("NPC (Assets)")]
        [Tooltip("Новый способ: состав лагеря через ассет-пак. Если заполнен — используется он.")]
        public NPCSpawnList npcPack;

        // ---------- NPC (Legacy fallback) ----------
        [Header("NPC (Legacy)")]
        [Tooltip("Старый способ — прямой список ролей с префабами. Используется, если npcPack не задан.")]
        public CampNpcRole[] npcRoles;

        [Serializable]
        public class CampNpcRole
        {
            public string roleName = "Soldier";
            public GameObject prefab;
            public Vector2Int countRange = new(3, 3);

            [Tooltip("Схема раскладки вокруг центра.")]
            public NpcDistribution dist = NpcDistribution.Ring;
            public enum NpcDistribution { Center, Ring, GuardPerTent, Random }

            [Tooltip("Радиус/отступ от центра (в клетках) для Ring.")]
            public int radius = 6;

            [Tooltip("Смещение от палатки для GuardPerTent (в клетках).")]
            public Vector2Int guardOffset = new(1, 0);
        }

        // ---------- Debug / Flags ----------
        [Header("Debug/Flags")]
        [Tooltip("Позволять накладывать лагерь поверх природы (обычно false — мы чистим природу и резервируем клетки).")]
        public bool allowOverlapWithNature = false;
    }
}