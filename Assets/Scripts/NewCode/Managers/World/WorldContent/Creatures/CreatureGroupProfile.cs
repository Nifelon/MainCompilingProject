using System.Collections.Generic;
using UnityEngine;


namespace Game.World.Creatures
{
    [CreateAssetMenu(menuName = "World/Creatures/Creature Group Profile", fileName = "creature_group_")]
    public class CreatureGroupProfile : ScriptableObject
    {
        [System.Serializable]
        public struct Member
        {
            public CreatureProfile profile; // ���
            public Vector2Int count; // ������� (min..max)
        }


        [Header("Composition")]
        public List<Member> members = new();


        [Header("Group Layout")]
        [Tooltip("������� ��������� ������: ������ ��������� (������).")]
        public float cohesionRadius = 4f;
        [Tooltip("��������� ���������� ������� ���������.")]
        public float formationJitter = 1.25f;
    }
}