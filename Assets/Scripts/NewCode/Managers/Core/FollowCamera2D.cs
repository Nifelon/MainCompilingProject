// CameraFollow2D.cs
using UnityEngine;

public class CameraFollow2D : MonoBehaviour
{
    public Transform target;
    public GridTestField clampToField;   // твой генератор сетки
    public Vector3 offset = new(0, 0, -10);
    public float smooth = 12f;

    void LateUpdate()
    {
        if (!target) return;

        var desired = target.position + offset;

        if (clampToField)
        {
            var b = clampToField.GetWorldBounds();

            // учёт размера камеры (ортографической)
            var cam = GetComponent<Camera>();
            float halfH = cam != null && cam.orthographic ? cam.orthographicSize : 0f;
            float halfW = halfH * (cam ? cam.aspect : 1f);

            // чтобы камера НЕ выходила за край поля
            desired.x = Mathf.Clamp(desired.x, b.xMin + halfW, b.xMax - halfW);
            desired.y = Mathf.Clamp(desired.y, b.yMin + halfH, b.yMax - halfH);
        }

        transform.position = Vector3.Lerp(
            transform.position, desired,
            1f - Mathf.Exp(-smooth * Time.deltaTime)
        );
    }
}