using UnityEngine;

namespace Game.World.Objects
{
    public enum ObjectCategory
    {
        Natural = 0, // 0xx
        Settlement = 1, // 1xx
        Structure = 2, // 2xx
        Rare = 3, // 3xx
    }
    public enum ObjectType
    {
        // 0 - служебное

        None = 0,

        //0xx природные объекты

        Palm = 1,
        Oak =2,
        Spruce=3,
        Rock=4,
        Cactus=5,
        Bush=6,        // обычный (без €год)
        BerryBush=7,    // куст с €годами (интерактивный)

        //1хх объекты поселений

        TentSmall=101,
        TentLarge=102,
        Campfire=103,
        WorkBench=104,
    }

    // ”словный тэг поведени€ (на будущее, удобно как Flags)
    [System.Flags]
    public enum ObjectTags
    {
        None = 0,
        Destructible = 1 << 0,  // можно ломать (есть HP)
        Harvestable = 1 << 1,  // можно Ђсобиратьї (€годы/ресурс без разрушени€)
        StaticObstacle = 1 << 2,  // преп€тствие дл€ прохода
        HighSprite = 1 << 3,  // высокий спрайт (нужно поднимать по Y)
    }

    [System.Serializable]
    public struct DropEntry
    {
        [Tooltip("ID предмета в твоей системе предметов (пока string или int).")]
        public ItemId itemId;
        [Min(0)] public int minCount;
        [Min(0)] public int maxCount;
        [Range(0, 1)] public float chance;
    }
}