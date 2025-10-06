using Game.Items;
using UnityEngine;

namespace Game.World.Objects
{
    public enum ObjectCategory { Natural = 0, Settlement = 1, Structure = 2, Rare = 3 }

    public enum ObjectType
    {
        None = 0,
        Palm = 1, Oak = 2, Spruce = 3, Rock = 4, Cactus = 5, Bush = 6,
        BerryBush = 7,
        TentSmall = 101, TentLarge = 102, Campfire = 103, WorkBench = 104,
    }

    [System.Flags]
    public enum ObjectTags
    {
        None = 0,
        Destructible = 1 << 0,
        Harvestable = 1 << 1,
        StaticObstacle = 1 << 2,
        HighSprite = 1 << 3,
    }

    [System.Serializable]
    public struct DropEntry
    {
        [Header("Новая схема (рекомендуется)")]
        [Tooltip("Ссылка на ItemSO — дальше маппинг по GUID в ItemId.")]
        public ItemSO item;                 // <-- НОВОЕ поле (опционально)

        [Header("Легаси (оставляем для совместимости)")]
        [Tooltip("Старый enum. Используется, если ItemSO не задан.")]
        public ItemId itemId;               // <-- как было

        [Min(0)] public int minCount;
        [Min(0)] public int maxCount;
        [Range(0, 1)] public float chance;
    }
}
