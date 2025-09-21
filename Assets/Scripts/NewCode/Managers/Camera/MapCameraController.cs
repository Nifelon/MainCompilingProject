using UnityEngine;

[RequireComponent(typeof(Camera))]
public class MapCameraController : MonoBehaviour
{
    [Header("Панорамирование (WASD/стрелки/ПКМ-drag)")]
    public float moveSpeed = 10f;      // базовая скорость
    public float fastMul = 2f;         // при Shift
    public float dragMul = 1f;         // чувствительность ПКМ перетаскивания

    [Header("Зум (колёсико)")]
    public float zoomMin = 3f;
    public float zoomMax = 30f;
    public float zoomStep = 3f;
    public float zoomSmooth = 10f;

    [Header("Прочее")]
    public bool clampToBounds = false;
    public Rect worldBounds = new Rect(-200, -200, 400, 400); // xmin,ymin,w,h

    Camera _cam;
    Vector3 _targetPos;
    float _targetOrtho;
    bool _dragging;
    Vector3 _dragStartWorld;

    void Awake()
    {
        _cam = GetComponent<Camera>();
        if (!_cam.orthographic) _cam.orthographic = true;

        _targetPos = transform.position;
        _targetOrtho = Mathf.Clamp(_cam.orthographicSize, zoomMin, zoomMax);
        _cam.orthographicSize = _targetOrtho;
    }

    void Update()
    {
        // Не реагируем, если UI блокирует ввод
        var gm = GlobalCore.Instance?.GameManager;
        if (gm != null && gm.IsUiBlocked) return;

        HandleZoom();
        HandlePan();
        Apply();
    }

    void HandleZoom()
    {
        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            _targetOrtho = Mathf.Clamp(_targetOrtho - scroll * zoomStep, zoomMin, zoomMax);
        }
    }

    void HandlePan()
    {
        // ПКМ drag
        if (Input.GetMouseButtonDown(1))
        {
            _dragging = true;
            _dragStartWorld = ScreenToWorld(Input.mousePosition);
        }
        if (Input.GetMouseButtonUp(1)) _dragging = false;

        if (_dragging)
        {
            var cur = ScreenToWorld(Input.mousePosition);
            var delta = (_dragStartWorld - cur) * dragMul;
            _targetPos += delta;
            _dragStartWorld = ScreenToWorld(Input.mousePosition);
        }

        // WASD / стрелки
        float mult = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? fastMul : 1f;

        // скорость масштабируем от зума (чтобы на высоких уровнях двигалась быстрее)
        float zoomFactor = Mathf.Lerp(0.5f, 2.0f, Mathf.InverseLerp(zoomMin, zoomMax, _targetOrtho));
        float spd = moveSpeed * mult * zoomFactor * Time.deltaTime;

        Vector3 dir = Vector3.zero;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) dir += Vector3.up;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) dir += Vector3.down;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) dir += Vector3.left;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) dir += Vector3.right;

        _targetPos += dir * spd;
    }

    void Apply()
    {
        // плавный зум
        _cam.orthographicSize = Mathf.Lerp(_cam.orthographicSize, _targetOrtho, Time.deltaTime * zoomSmooth);

        // клампы по границам
        if (clampToBounds)
        {
            float halfH = _cam.orthographicSize;
            float halfW = halfH * _cam.aspect;

            float minX = worldBounds.xMin + halfW;
            float maxX = worldBounds.xMax - halfW;
            float minY = worldBounds.yMin + halfH;
            float maxY = worldBounds.yMax - halfH;

            _targetPos.x = Mathf.Clamp(_targetPos.x, minX, maxX);
            _targetPos.y = Mathf.Clamp(_targetPos.y, minY, maxY);
        }

        transform.position = new Vector3(_targetPos.x, _targetPos.y, transform.position.z);
    }

    Vector3 ScreenToWorld(Vector3 screen)
    {
        var w = _cam.ScreenToWorldPoint(screen);
        w.z = 0;
        return w;
    }
}