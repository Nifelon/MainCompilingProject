using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.World.Objects
{
    /// <summary>
    /// Лёгкий реестр ObjectData: лениво грузит все SO из Resources/ScriptAssets/Objects и даёт доступ по ObjectType.
    /// </summary>
    public static class ObjectDataDB
    {
        // Куда положить ассеты: Assets/Resources/ScriptAssets/Objects/*.asset
        const string ResourcesPath = "ScriptAssets/Objects";

        static Dictionary<ObjectType, ObjectData> _byType;

        static void Ensure()
        {
            if (_byType != null) return;
            var all = Resources.LoadAll<ObjectData>(ResourcesPath);
            _byType = all.Where(x => x != null)
                         .GroupBy(x => x.type)
                         .ToDictionary(g => g.Key, g => g.First());
#if UNITY_EDITOR
            if (_byType.Count == 0)
                Debug.LogWarning($"[ObjectDataDB] No ObjectData found at Resources/{ResourcesPath}. " +
                                 "Make sure your SO are under that folder.");
#endif
        }

        public static ObjectData Get(ObjectType t)
        {
            Ensure();
            _byType.TryGetValue(t, out var d);
            return d;
        }
    }
}