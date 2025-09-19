using UnityEngine;

public class ObjectSpawner : MonoBehaviour
{
    [System.Serializable]
    public struct SpawnEntry
    {
        public GameObject prefab;
        public int count;
        public Vector2 areaSize; // ширина/высота прямоугольника для спавна
    }

    [Header("Prefabs and counts")]
    public SpawnEntry[] entries;

    [Header("Random seed (0 = рандом каждый запуск)")]
    public int seed;

    void Start()
    {
        if (seed != 0) Random.InitState(seed);

        foreach (var e in entries)
        {
            if (!e.prefab || e.count <= 0) continue;

            for (int i = 0; i < e.count; i++)
            {
                Vector3 pos = transform.position +
                              new Vector3(Random.Range(-e.areaSize.x / 2, e.areaSize.x / 2),
                                          Random.Range(-e.areaSize.y / 2, e.areaSize.y / 2),
                                          0f);

                Instantiate(e.prefab, pos, Quaternion.identity, transform);
            }
        }
    }
}