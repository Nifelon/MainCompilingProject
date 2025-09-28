using System;
using UnityEngine;
using Game.Actors;


namespace Game.World.Creatures
{
    [CreateAssetMenu(menuName = "World/Creatures/CreatureProfile", fileName = "creature_")]
    public class CreatureProfile : ActorProfile
    {
        [Header("Visual & Prefab")]
        public Sprite icon;
        public GameObject prefab; // �������������� ������ ��������


        [Header("Stats")]
        public int maxHealth = 10;
        public float baseSpeed = 2f;


        [Header("Behavior (per-individual)")]
        public float agroRadius = 8f;
        [Range(0f, 1f)] public float fleeThresholdHp = 0.2f; // 20% HP � �����
        public float territorialRadius = 12f;
        public bool dayActive = true;
        public bool nightActive = false;


        // ��, ��� ������� � ������/�������/�������� � �������� � ��������� SO ����
    }
}