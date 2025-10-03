// Managers/World/WorldContent/Objects/HarvestRespawnScheduler.cs
using System.Collections.Generic;
using UnityEngine;

public class HarvestRespawnScheduler : MonoBehaviour
{
    struct Entry { public ObjectHarvestInteractable obj; public float at; }

    static readonly List<Entry> _queue = new();
    static HarvestRespawnScheduler _inst;

    void OnEnable() { TickManager.OnTick += Tick; }
    void OnDisable() { TickManager.OnTick -= Tick; }

    static void Ensure()
    {
        if (_inst) return;
        var go = new GameObject("__HarvestRespawnScheduler");
        _inst = go.AddComponent<HarvestRespawnScheduler>();
        DontDestroyOnLoad(go);
    }

    public static void Schedule(ObjectHarvestInteractable obj, float atTime)
    {
        Ensure();
        _queue.Add(new Entry { obj = obj, at = atTime });
    }

    void Tick()
    {
        if (_queue.Count == 0) return;
        float now = Time.time;
        for (int i = _queue.Count - 1; i >= 0; i--)
        {
            var e = _queue[i];
            if (!e.obj) { _queue.RemoveAt(i); continue; }
            if (now >= e.at)
            {
                _queue.RemoveAt(i);
                e.obj.RespawnNow();
            }
        }
    }
}
