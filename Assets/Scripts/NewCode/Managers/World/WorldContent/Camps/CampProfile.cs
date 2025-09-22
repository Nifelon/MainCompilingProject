using System;
using UnityEngine;
using Game.World.Map.Biome;
using Game.World.Objects; // ObjectType

[CreateAssetMenu(menuName = "World/Settlement/Camp Profile", fileName = "CampProfile_Default")]
public class CampProfile : ScriptableObject
{
    [Header("Spawn Conditions")]
    [Tooltip("� ����� ������ �������� ������.")]
    public BiomeType[] allowedBiomes = { BiomeType.Forest, BiomeType.Plains };

    [Tooltip("������� ���-�� ������� �� ��� (��������).")]
    public int targetCamps = 6;

    [Tooltip("���. ��������� ����� �������� (������).")]
    public int minDistanceBetweenCamps = 80;

    [Tooltip("������������ ���, ����� �� � ������ ����� ��������� ������.")]
    public bool useNoiseGate = true;

    [Range(0.001f, 0.05f)] public float noiseScale = 0.01f;
    [Range(0f, 1f)] public float noiseThreshold = 0.50f;

    [Tooltip("���. ���� � �������� ���y (����� ������ ���� ������� �������� ������ ��������).")]
    public int seedSalt = 137;

    [Header("Shape / Layout")]
    [Tooltip("������ ������ � ������� (���� ���������� � �����-��������).")]
    [Min(2)] public int campRadius = 8;

    public LayoutMode layout = LayoutMode.Radial;
    public enum LayoutMode { Radial, Grid }

    [Tooltip("����������� ������ �� ������/���� ����� ��� ��������� (� �������).")]
    [Min(0)] public int layoutPadding = 2;

    [Header("Ground Override (visual)")]
    [Tooltip("������ '������������ �����' �� ���������� ������.")]
    public Sprite campGroundSprite;

    [Tooltip("��� �������� �����: ���� �� ������� ������ ��� ������ ����������.")]
    public GroundMode groundMode = GroundMode.Circle;
    public enum GroundMode { Circle, LayoutMaskOnly }

    [Header("Composition (Structures)")]
    [Tooltip("������ '����� �������� � �������' ������� ������ ������.")]
    public CampStructure[] structures;

    [Header("NPC")]
    [Tooltip("������ ����� NPC (�������/�������� � �.�.).")]
    public CampNpcRole[] npcRoles;

    [Header("Debug/Flags")]
    [Tooltip("��������� ����������� ������ ������ ��� ��������������� ��������� �������� (������ false).")]
    public bool allowOverlapWithNature = false;

    [Serializable]
    public class CampStructure
    {
        public ObjectType type = ObjectType.TentSmall;
        [Tooltip("������� ����; ���� min!=max � ���� ��������� � ���������.")]
        public Vector2Int countRange = new Vector2Int(4, 4);

        [Tooltip("�������� �������� (������). -1 = ���������.")]
        public int variantIndex = -1;

        [Tooltip("��� ������������ ���� ��� ������ ������.")]
        public Distribution dist = Distribution.Ring;
        public enum Distribution { Center, Ring, InnerRing, Grid, RandomScatter }

        [Tooltip("�������� �� ������ ��� ���� (� �������).")]
        public int ringOffset = 0;

        [Tooltip("����������� ��������� �� �������� ���� �� ���� (������).")]
        public float minDistanceSameType = 1.0f;
    }

    [Serializable]
    public class CampNpcRole
    {
        public string roleName = "Soldier";
        public GameObject prefab;
        public Vector2Int countRange = new Vector2Int(3, 3);

        [Tooltip("������������� ������ ������.")]
        public NpcDistribution dist = NpcDistribution.Ring;
        public enum NpcDistribution { Center, Ring, GuardPerTent, Random }

        [Tooltip("������/������ �� ������ (������).")]
        public int radius = 6;

        [Tooltip("���� GuardPerTent � ������ �� ������� (� �������).")]
        public Vector2Int guardOffset = new Vector2Int(1, 0);
    }
}