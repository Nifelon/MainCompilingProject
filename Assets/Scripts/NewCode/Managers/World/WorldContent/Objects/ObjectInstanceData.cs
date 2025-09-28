namespace Game.World.Objects
{
    /// Данные-инстанса (без GameObject): используются планировщиком и вью-адаптером.
    public struct ObjectInstanceData
    {
        public ulong id;
        public ObjectType type;
        public UnityEngine.Vector2Int cell;
        public int variantIndex;
        public UnityEngine.Vector2 worldPos;
        public UnityEngine.Vector2Int footprint;
    }

    // при желании тоже вынеси:
    public struct ObjectState { public bool isDestroyed; }

    public class WorldObjectRef : UnityEngine.MonoBehaviour
    {
        public ulong id;
        public ObjectType type;
    }
}