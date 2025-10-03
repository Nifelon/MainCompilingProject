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
        // 0 - ���������

        None = 0,

        //0xx ��������� �������

        Palm = 1,
        Oak =2,
        Spruce=3,
        Rock=4,
        Cactus=5,
        Bush=6,        // ������� (��� ����)
        BerryBush=7,    // ���� � ������� (�������������)

        //1�� ������� ���������

        TentSmall=101,
        TentLarge=102,
        Campfire=103,
        WorkBench=104,
    }

    // �������� ��� ��������� (�� �������, ������ ��� Flags)
    [System.Flags]
    public enum ObjectTags
    {
        None = 0,
        Destructible = 1 << 0,  // ����� ������ (���� HP)
        Harvestable = 1 << 1,  // ����� ���������� (�����/������ ��� ����������)
        StaticObstacle = 1 << 2,  // ����������� ��� �������
        HighSprite = 1 << 3,  // ������� ������ (����� ��������� �� Y)
    }

    [System.Serializable]
    public struct DropEntry
    {
        [Tooltip("ID �������� � ����� ������� ��������� (���� string ��� int).")]
        public ItemId itemId;
        [Min(0)] public int minCount;
        [Min(0)] public int maxCount;
        [Range(0, 1)] public float chance;
    }
}