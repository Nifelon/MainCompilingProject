using UnityEngine;

public class SimpleSpawner2D : MonoBehaviour
{
    public Transform player; public GameObject wolfPrefab;
    public int count = 3; public float radius = 8f;

    void Start()
    {
        if (!player) { var p = GameObject.FindWithTag("Player"); if (p) player = p.transform; }
        for (int i = 0; i < count; i++)
        {
            var ang = Random.value * Mathf.PI * 2f;
            var pos = player.position + new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f) * radius;
            var go =Instantiate(wolfPrefab, pos, Quaternion.identity);
            go.SetActive(true);
            
        
        }
    }
}