using UnityEngine;

public class PooledTile : MonoBehaviour
{
    public SpriteRenderer sr;

    void Reset()
    {
        if (!sr) sr = GetComponentInChildren<SpriteRenderer>(true);
    }

    public void EnsureSpriteRenderer()
    {
        if (!sr) sr = GetComponentInChildren<SpriteRenderer>(true);
    }
}