// Assets/Game/World/Objects/ObjectData.cs
using System.Collections.Generic;
using UnityEngine;

namespace Game.World.Objects
{
    [System.Serializable]
    public struct HarvestProfile
    {
        [Tooltip("Можно ли взаимодействовать и 'собирать' без разрушения.")]
        public bool harvestable;

        [Tooltip("Что выдать игроку при сборе (может быть пусто на альфе).")]
        public List<DropEntry> harvestDrops;

        [Tooltip("Удалить объект после сбора? Если false — можно трансформировать тип.")]
        public bool destroyOnHarvest;

        [Tooltip("Во что превратиться после сбора (напр. BerryBush → Bush).")]
        public ObjectType transformToType;

        [Tooltip("Через сколько секунд возродить урожай (0 = не респавнится на альфе).")]
        public float respawnSeconds;

        [Tooltip("Подсказка в UI при взаимодействии.")]
        public string interactPrompt;
    }

    [CreateAssetMenu(menuName = "World/Object Data", fileName = "ObjectData_")]
    public class ObjectData : ScriptableObject
    {
        [Header("Идентификация")]
        public ObjectType type;
        public string displayName = "Unnamed";

        [Header("Геометрия на карте")]
        [Tooltip("Размер в клетках (ширина×высота) для занятости/коллизии.")]
        public Vector2Int footprint = new(1, 1);

        [Tooltip("Смещение визуала относительно якорной клетки (в единицах мира).")]
        public Vector2 pivotOffsetWorld = Vector2.zero;

        [Tooltip("Поднять высокий спрайт над клеткой (в долях CellSize). 0 = не поднимать.")]
        [Min(0)] public float visualHeightUnits = 0f;

        [Header("Проходимость")]
        [Tooltip("0 = непроходимо, >0 — модификатор скорости (на альфе достаточно 0/1).")]
        public float movementModifier = 1f;

        [Header("Разрушаемость (выключено на альфе, но поля оставим)")]
        public bool destructible = false;
        [Min(1)] public int maxHP = 1;

        [Header("Лут при разрушении (позже)")]
        public bool dropOnDestroy = false;
        public List<DropEntry> drops = new();

        [Header("Визуал")]
        [Tooltip("Коллекция вариантов спрайта; при пуллинге выбираем variantIndex.")]
        public Sprite[] spriteVariants;

        [Tooltip("Если нужен особый префаб вместо базового 'ObjectSprite'.")]
        public GameObject prefabOverride;

        [Header("Интерактивность (урожай и т.п.)")]
        public HarvestProfile harvest;

        [Header("Теги поведения")]
        public ObjectTags tags = ObjectTags.None;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (footprint.x < 1) footprint.x = 1;
            if (footprint.y < 1) footprint.y = 1;

            // Синхронизация препятствия с проходимостью
            if (movementModifier <= 0f) tags |= ObjectTags.StaticObstacle;
            else tags &= ~ObjectTags.StaticObstacle;

            // Разрушаемость выключена — не даём случайно занизить maxHP/включить дроп
            if (!destructible)
            {
                maxHP = 1;
                dropOnDestroy = false;
            }

            // Флаг высокого спрайта
            if (visualHeightUnits > 0f) tags |= ObjectTags.HighSprite;
            else tags &= ~ObjectTags.HighSprite;

            // Безопасность для лута
            for (int i = 0; i < drops.Count; i++)
            {
                var d = drops[i];
                if (d.maxCount < d.minCount) d.maxCount = d.minCount;
                drops[i] = d;
            }
        }
#endif
    }
}
