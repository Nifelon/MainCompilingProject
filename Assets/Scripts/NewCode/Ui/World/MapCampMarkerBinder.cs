using UnityEngine;

public class MapCampMarkerBinder : MonoBehaviour
{
    [SerializeField] MapMarkersController markers;
    [Header("Colors")]
    [SerializeField] Color idleColor = new Color(1f, 1f, 1f, 1f);
    [SerializeField] Color activeColor = new Color(1f, 0.85f, 0f, 1f);
    [SerializeField] Color doneColor = new Color(0.2f, 0.85f, 0.2f, 1f);

    void Reset()
    {
        markers = FindAnyObjectByType<MapMarkersController>();
    }

    public void SetIdle() => markers?.SetMarkerColor(MarkerType.Camp, idleColor);
    public void SetActive() => markers?.SetMarkerColor(MarkerType.Camp, activeColor);
    public void SetDone() => markers?.SetMarkerColor(MarkerType.Camp, doneColor);
}