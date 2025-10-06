#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Game.Items;

namespace Game.Items
{
    /// <summary>
    /// Editor-only хелпер: резолвим ItemSO по GUID/ItemId.
    /// Никаких захардкоженных путей — сканируем проект и читаем ItemSO.Guid.
    /// </summary>
    public static class ItemMapEditor
    {
        private static Dictionary<string, ItemSO> _byGuid;
        private static bool _built;

        [InitializeOnLoadMethod]
        private static void OnDomainReload()
        {
            _built = false;
            _byGuid = null;
        }

        private static void Build()
        {
            _byGuid = new Dictionary<string, ItemSO>(StringComparer.OrdinalIgnoreCase);
            var guids = AssetDatabase.FindAssets("t:ItemSO");
            foreach (var assetGuid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(assetGuid);
                var so = AssetDatabase.LoadAssetAtPath<ItemSO>(path);
                if (!so) continue;

                var g = so.Guid;
                if (string.IsNullOrWhiteSpace(g)) continue;

                if (_byGuid.ContainsKey(g))
                    Debug.LogWarning($"[ItemMapEditor] Duplicate GUID '{g}' at {path}", so);
                else
                    _byGuid[g] = so;
            }
            _built = true;
        }

        public static ItemSO ByGuid(string guid)
        {
            if (string.IsNullOrWhiteSpace(guid)) return null;
            if (!_built) Build();
            return _byGuid.TryGetValue(guid, out var so) ? so : null;
        }

        public static ItemSO ById(ItemId id)
        {
            var guid = ItemMap.ConstByEnum(id);
            return guid != null ? ByGuid(guid) : null;
        }

        public static IEnumerable<ItemSO> All()
        {
            if (!_built) Build();
            return _byGuid.Values;
        }
    }
}
#endif
