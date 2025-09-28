// Assets/Game/World/NPC/NPCSpawnList.cs
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.World.NPC
{
    [CreateAssetMenu(menuName = "World/NPC/NPC Spawn List", fileName = "npc_pack_")]
    public class NPCSpawnList : ScriptableObject
    {
        public List<Entry> entries = new();

        [Serializable]
        public class Entry
        {
            public NPCProfile profile;              // ���
            public Vector2Int count = new(1, 1);   // ������� (min..max)
            public SpawnAnchor anchor = SpawnAnchor.Perimeter; // ���
            [Min(0)] public float radiusFromCenter = 0f;       // 0 = campRadius - 1
            [Min(0)] public float spacing = 1.5f;              // ����� NPC (� �������)
        }

        public enum SpawnAnchor { Center, Perimeter, Tents, Fire, Gate, Any }
    }
}