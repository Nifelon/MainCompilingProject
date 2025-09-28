namespace Game.World.Objects
{
    /// ������-�������� (��� GameObject): ������������ ������������� � ���-���������.
    public struct ObjectInstanceData
    {
        public ulong id;
        public ObjectType type;
        public UnityEngine.Vector2Int cell;
        public int variantIndex;
        public UnityEngine.Vector2 worldPos;
        public UnityEngine.Vector2Int footprint;
    }

    // ��� ������� ���� ������:
    public struct ObjectState { public bool isDestroyed; }

    public class WorldObjectRef : UnityEngine.MonoBehaviour
    {
        public ulong id;
        public ObjectType type;
    }
}