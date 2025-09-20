using UnityEngine;

public class MapToggle : MonoBehaviour
{
    public MapPanelController map;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            if (!map) return;
            if (map.gameObject.activeSelf) map.Hide(); else map.Show();
        }

        if (map && map.gameObject.activeSelf && (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1)))
            map.Hide();
    }
}