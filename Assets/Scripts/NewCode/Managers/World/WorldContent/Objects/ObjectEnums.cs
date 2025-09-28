using UnityEngine;

namespace Game.World.Objects
{
    public enum ObjectType
    {
        None,
        Palm,
        Oak,
        Spruce,
        Rock,
        Cactus,
        Bush,        // обычный (без €год)
        BerryBush,    // куст с €годами (интерактивный)
        TentSmall,
        TentLarge,
        Campfire,
        WorkBench,
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
        public string itemId;
        [Min(0)] public int minCount;
        [Min(0)] public int maxCount;
        [Range(0, 1)] public float chance;
    }
}