using System.Collections.Generic;
using UnityEngine;
public interface IGroundSpriteService
{
    void SetSpriteCircle(Vector2Int center, int radius, Sprite sprite);
    void ClearSpriteCircle(Vector2Int center, int radius);
    bool TryGetSprite(Vector2Int cell, out Sprite sprite);
}

public class GroundSpriteService : MonoBehaviour, IGroundSpriteService
{
    private readonly Dictionary<Vector2Int, Sprite> _map = new();

    public void SetSpriteCircle(Vector2Int c, int r, Sprite s)
    {
        if (!s) return;
        for (int x = -r; x <= r; x++)
            for (int y = -r; y <= r; y++)
            {
                if (x * x + y * y > r * r) continue;
                _map[new Vector2Int(c.x + x, c.y + y)] = s;
            }
    }

    public void ClearSpriteCircle(Vector2Int c, int r)
    {
        for (int x = -r; x <= r; x++)
            for (int y = -r; y <= r; y++)
            {
                if (x * x + y * y > r * r) continue;
                _map.Remove(new Vector2Int(c.x + x, c.y + y));
            }
    }

    public bool TryGetSprite(Vector2Int cell, out Sprite sprite) => _map.TryGetValue(cell, out sprite);
}