using System.Collections.Generic;
using UnityEngine;

namespace Game.World.Objects.Spawning
{
    /// —читает план объектов дл€ чанка (данные-только, без вью/пула).
    public interface IObjectSpawnPlanner
    {
        /// ¬ернуть детерминированный список инстансов дл€ чанка.
        /// originCell Ч левый-нижний угол чанка в клетках.
        /// chunkKey   Ч ((uint)cx | ((ulong)cy << 32)) Ч нужен дл€ стабильных id.
        List<ObjectInstanceData> PlanChunk(Vector2Int originCell, int chunkSize, int worldSeed, ulong chunkKey);
    }
}