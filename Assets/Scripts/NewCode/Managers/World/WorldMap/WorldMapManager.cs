using System.Collections.Generic;
using Game.Core;
using System.Linq;
using UnityEngine;

public class WorldMapManager : MonoBehaviour, IWorldComposite
{
    [SerializeField] private int order = 0;               // карта идёт первой
    public int Order => order;

    [Header("Подсистемы карты (Height/Liquid/Climate/Biome/Roads/Structures/Towns...)")]
    [SerializeField] private MonoBehaviour[] systems;     // кинь сюда компоненты, которые реализуют IWorldSystem

    private readonly List<IWorldSystem> _children = new();
    public IReadOnlyList<IWorldSystem> Children => _children;

    public void Initialize(WorldContext ctx)
    {
        _children.Clear();

        if (systems != null)
        {
            for (int i = 0; i < systems.Length; i++)
            {
                var mb = systems[i];
                if (mb is IWorldSystem sys) _children.Add(sys);
                else if (mb != null)
                    Debug.LogWarning($"[WorldMapManager] {mb.name} не реализует IWorldSystem");
            }
        }

        foreach (var s in _children.OrderBy(s => s.Order))
            s.Initialize(ctx);
    }
}
