using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoolManagerUISuppWindow : MonoBehaviour
{
    //private Dictionary<Vector2Int, float> floatingTextDelays = new();
    //private float minDelayBetweenTexts = 0.35f;
    //private struct FloatingTextRequest
    //{
    //    public Vector2 worldPos;
    //    public string value;
    //    public Color color;

    //    public FloatingTextRequest(Vector2 worldPos, string value, Color color)
    //    {
    //        this.worldPos = worldPos;
    //        this.value = value;
    //        this.color = color;
    //    }
    //}
    //public static Queue<GameObject> UISupppool = new Queue<GameObject>();
    //public Dictionary<Vector2Int, GameObject> activeUISupp = new Dictionary<Vector2Int, GameObject>();
    //public static int UISuppcount = 500;
    //public static PoolManagerUISuppWindow Instance { get; private set; }
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
    //public void GenerateUISuppPool()
    //{

    //    if (GlobalCore.Instance.MainGameObjects.UISuppWindow == null)
    //    {
    //        Debug.LogError(" squarePrefab не установлен в SquarePool! Добавь его в инспекторе.");
    //        return;
    //    }
    //    // Заполняем пул квадратами
    //    for (int i = 0; i < UISuppcount; i++)
    //    {
    //        GameObject square = Instantiate(GlobalCore.Instance.MainGameObjects.UISuppWindow);
    //        square.SetActive(false);
    //        square.transform.name = "FloatingText";
    //        square.transform.SetParent(GlobalCore.Instance.MainGameObjects.Canvas.transform);
    //        UISupppool.Enqueue(square);
    //    }
    //}
    //public GameObject GetFloatingText()
    //{
    //    if (UISupppool.Count > 0)
    //        return UISupppool.Dequeue();

    //    return Instantiate(GlobalCore.Instance.MainGameObjects.UISuppWindow, GlobalCore.Instance.MainGameObjects.Canvas.transform);
    //}

    //public void ReturnFloatingText(GameObject go)
    //{
    //    go.SetActive(false);
    //    UISupppool.Enqueue(go);
    //}

    //public void ShowFloatingText(Vector2 worldPos, string value, Color color)
    //{
    //    Vector2Int key = new(Mathf.RoundToInt(worldPos.x), Mathf.RoundToInt(worldPos.y));
    //    float currentTime = Time.time;

    //    // Проверка: была ли недавно вспышка в этой точке
    //    if (floatingTextDelays.TryGetValue(key, out float lastTime))
    //    {
    //        float elapsed = currentTime - lastTime;

    //        if (elapsed < minDelayBetweenTexts)
    //        {
    //            float delay = minDelayBetweenTexts - elapsed;
    //            StartCoroutine(DelayedFloatingText(worldPos, value, color, delay));
    //            return;
    //        }
    //    }

    //    // Показ сразу
    //    ShowNow(worldPos, value, color);
    //    floatingTextDelays[key] = currentTime;
    //}
    //private void ShowNow(Vector2 worldPos, string value, Color color)
    //{
    //    GameObject go = GetFloatingText();
    //    RectTransform rt = go.GetComponent<RectTransform>();
    //    Vector2 screenPoint = Camera.main.WorldToScreenPoint(worldPos);
    //    rt.position = screenPoint;

    //    go.SetActive(true);
    //    var controller = go.GetComponent<FloatingTextController>();
    //    controller.Setup(value, color);
    //}
    //private IEnumerator DelayedFloatingText(Vector2 worldPos, string value, Color color, float delay)
    //{
    //    yield return new WaitForSeconds(delay);
    //    ShowNow(worldPos, value, color);

    //    Vector2Int key = new(Mathf.RoundToInt(worldPos.x), Mathf.RoundToInt(worldPos.y));
    //    floatingTextDelays[key] = Time.time;
    //}
}
