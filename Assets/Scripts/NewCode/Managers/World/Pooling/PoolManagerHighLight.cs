using System.Collections.Generic;
using UnityEngine;

public class PoolManagerHighLight : MonoBehaviour
{
    //public static PoolManagerHighLight Instance { get; private set; }
    //private void Awake()
    //{
    //    if (Instance == null)
    //    {
    //        Instance = this;
    //    }
    //    else
    //    {
    //        Destroy(gameObject);
    //    }
    //}
    //public static int PoolHighLightTile = 300;
    //public static Queue<GameObject> HighLightpool = new Queue<GameObject>();
    //public List<GameObject> activeHighlights = new List<GameObject>();
    //public void GenerateHighLightTilePool()
    //{

    //    if (GlobalCore.Instance.MainGameObjects.HighLightTile == null)
    //    {
    //        Debug.LogError(" squarePrefab не установлен в SquarePool! Добавь его в инспекторе.");
    //        return;
    //    }
    //    // Заполняем пул квадратами
    //    for (int i = 0; i < PoolHighLightTile; i++)
    //    {
    //        GameObject square = Instantiate(GlobalCore.Instance.MainGameObjects.HighLightTile);
    //        square.SetActive(false);
    //        var renderer = square.GetComponent<SpriteRenderer>();
    //        if (renderer != null)
    //        {
    //            renderer.sortingLayerName = "Effects";
    //            renderer.sortingOrder = 0;
    //        }
    //        square.transform.SetParent(GlobalCore.Instance.MainGameObjects.WorldRoot.transform);
    //        HighLightpool.Enqueue(square);
    //    }
    //}
    //public GameObject GetHighlightTile()
    //{
    //    if (HighLightpool.Count > 0)
    //        return HighLightpool.Dequeue();
    //    GameObject obj = Instantiate(GlobalCore.Instance.MainGameObjects.HighLightTile);
    //    obj.GetComponent<SpriteRenderer>().color = new Color(1f, 0f, 0f, 0.4f);
    //    obj.transform.SetParent(GlobalCore.Instance.MainGameObjects.WorldRoot.transform);
    //    return obj;
    //}
    //public void ReturnAllHighlights()
    //{
    //    foreach (var tile in activeHighlights)
    //    {
    //        tile.SetActive(false);
    //        tile.GetComponent<SpriteRenderer>().color = new Color(1f, 0f, 0f, 0.4f);
    //        HighLightpool.Enqueue(tile);
    //    }
    //    activeHighlights.Clear();
    //}
}
