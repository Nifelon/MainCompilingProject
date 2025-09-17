using System.Collections.Generic;
using System.Linq;
using Game.Core;
using UnityEngine;
namespace Game.World.Content
{
    public class WorldContentManager : MonoBehaviour, IWorldComposite
    {
        [SerializeField] private int order = 100;
        public int Order => order;

        [SerializeField] private MonoBehaviour[] systems;

        private readonly List<IWorldSystem> _children = new();
        public IReadOnlyList<IWorldSystem> Children => _children;

        public void Initialize(WorldContext ctx)
        {
            Debug.Log($"[WorldContentManager] Initialize. systems length={(systems?.Length ?? 0)}");

            _children.Clear();

            if (systems != null)
            {
                for (int i = 0; i < systems.Length; i++)
                {
                    var mb = systems[i];
                    if (mb == null)
                    {
                        Debug.LogWarning($"[WorldContentManager] systems[{i}] is NULL");
                        continue;
                    }

                    var asSys = mb as IWorldSystem;
                    Debug.Log($"[WorldContentManager] systems[{i}] {mb.GetType().Name} → is IWorldSystem? {(asSys != null)}");

                    if (asSys != null) _children.Add(asSys);
                    else Debug.LogWarning($"[WorldContentManager] {mb.name} НЕ реализует IWorldSystem");
                }
            }

            foreach (var s in _children.OrderBy(s => s.Order))
            {
                Debug.Log($"[WorldContentManager] → Initialize child: {s.GetType().Name} (Order={s.Order})");
                s.Initialize(ctx);
            }
        }
    }
}