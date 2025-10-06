#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;

public static class ItemGuidTools
{
    [MenuItem("Tools/Items/Validate & Fix ItemSO GUIDs")]
    public static void ValidateAndFix()
    {
        var guids = AssetDatabase.FindAssets("t:ItemSO");
        var map = new Dictionary<string, List<ItemSO>>(StringComparer.OrdinalIgnoreCase);

        foreach (var g in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(g);
            var so = AssetDatabase.LoadAssetAtPath<ItemSO>(path);
            if (!so) continue;

            var key = string.IsNullOrWhiteSpace(so.Guid) ? "(empty)" : so.Guid;
            if (!map.TryGetValue(key, out var list)) map[key] = list = new List<ItemSO>();
            list.Add(so);
        }

        int fixedCount = 0;
        foreach (var kv in map.Where(kv => kv.Key == "(empty)" || kv.Value.Count > 1))
        {
            // оставляем первую копию как есть; всем остальным перегенерим guid
            foreach (var so in kv.Value.Skip(kv.Key == "(empty)" ? 0 : 1))
            {
                var soSer = new SerializedObject(so);
                soSer.FindProperty("guid").stringValue = System.Guid.NewGuid().ToString("N");
                soSer.ApplyModifiedProperties();
                EditorUtility.SetDirty(so);
                fixedCount++;
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[ItemGuidTools] Fixed {fixedCount} duplicate/empty GUID(s).");
    }
}
#endif