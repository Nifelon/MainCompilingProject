using UnityEngine;

namespace Game.World.Services
{
    public class VisibilityService : MonoBehaviour, IVisibilityService
    {
        [Header("Source")]
        [SerializeField] private Transform player;          // Player root
        [SerializeField] private int tileRadius = 64;       // ��� � ��������� ������
        [SerializeField] private float cellSize = 1f;       // ����� ��������� �� Units
        [Tooltip("����� ��� ��������, � ������� (����������)")]
        [SerializeField] private int loadPaddingCells = 8;
        [Tooltip("����� ��� ��������, � ������� (����������)")]
        [SerializeField] private int keepPaddingCells = 12;

        public int TileRadius => tileRadius;
        public float CellSize => cellSize;

        public Vector2 PlayerPosWorld { get; private set; }
        public float LoadRadiusWorld => (tileRadius + loadPaddingCells) * cellSize;
        public float UnloadRadiusWorld => (tileRadius + keepPaddingCells) * cellSize;

        void Awake()
        {
            if (!player)
            {
                // �������� ����� ������ �������������
                var p = GameObject.FindWithTag("Player");
                if (p) player = p.transform;
            }
        }

        void LateUpdate()
        {
            if (player) PlayerPosWorld = player.position;
        }

        public bool ChunkIntersectsLoad(Rect r) => CircleIntersectsRect(PlayerPosWorld, LoadRadiusWorld, r);
        public bool ChunkIntersectsKeep(Rect r) => CircleIntersectsRect(PlayerPosWorld, UnloadRadiusWorld, r);
        public bool IsPointVisibleForLoad(Vector2 p) => (p - PlayerPosWorld).sqrMagnitude <= LoadRadiusWorld * LoadRadiusWorld;
        public bool IsPointVisibleForKeep(Vector2 p) => (p - PlayerPosWorld).sqrMagnitude <= UnloadRadiusWorld * UnloadRadiusWorld;

        static bool CircleIntersectsRect(Vector2 c, float radius, Rect r)
        {
            var closest = new Vector2(
                Mathf.Clamp(c.x, r.xMin, r.xMax),
                Mathf.Clamp(c.y, r.yMin, r.yMax)
            );
            return (closest - c).sqrMagnitude <= radius * radius;
        }

        // ������������ ��� �������
        void OnDrawGizmosSelected()
        {
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            Gizmos.DrawWireSphere(player ? player.position : (Vector3)PlayerPosWorld, LoadRadiusWorld);
            Gizmos.color = new Color(1, 1, 0, 0.3f);
            Gizmos.DrawWireSphere(player ? player.position : (Vector3)PlayerPosWorld, UnloadRadiusWorld);
        }

        // ��������������� API, ���� ����� �������� ��������� �� Units � ��������
        public void ConfigureFromUnits(int tileRadiusCells, float cellSizeWorld)
        {
            tileRadius = tileRadiusCells;
            cellSize = cellSizeWorld;
        }
    }
}