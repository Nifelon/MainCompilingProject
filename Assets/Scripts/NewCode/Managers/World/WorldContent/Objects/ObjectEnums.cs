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
        Bush,        // ������� (��� ����)
        BerryBush,    // ���� � ������� (�������������)
        TentSmall,
        TentLarge,
        Campfire,
        WorkBench,
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
        public string itemId;
        [Min(0)] public int minCount;
        [Min(0)] public int maxCount;
        [Range(0, 1)] public float chance;
    }
}