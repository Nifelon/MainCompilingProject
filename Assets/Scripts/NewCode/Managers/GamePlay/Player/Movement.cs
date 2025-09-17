// TopDownMove2D.cs
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class TopDownMove2D : MonoBehaviour
{
    public float speed = 6f;
    public float acceleration = 30f;
    public float deceleration = 40f;
    public bool faceMoveDirection = false;
    public GridTestField clampToField;     // ← опционально: ограничить перемещение полем

    Rigidbody2D rb;
    Vector2 vel, input;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    void Update()
    {
        input = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        ).normalized;
    }

    void FixedUpdate()
    {
        // целевая скорость
        Vector2 targetVel = input * speed;
        float accel = (targetVel.sqrMagnitude > 0.001f) ? acceleration : deceleration;
        vel = Vector2.MoveTowards(vel, targetVel, accel * Time.fixedDeltaTime);

        Vector2 next = rb.position + vel * Time.fixedDeltaTime;

        // Кламп в пределах поля (если задано)
        if (clampToField)
        {
            var bounds = clampToField.GetWorldBounds();
            next.x = Mathf.Clamp(next.x, bounds.xMin, bounds.xMax);
            next.y = Mathf.Clamp(next.y, bounds.yMin, bounds.yMax);
        }

        rb.MovePosition(next);

        if (faceMoveDirection && vel.sqrMagnitude > 0.001f)
            transform.up = vel.normalized; // или right, если спрайт «смотрит вправо»
    }
}