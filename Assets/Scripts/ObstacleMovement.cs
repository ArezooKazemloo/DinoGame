
using UnityEngine;

public class ObstacleMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float leftLimit = -12f;
    private Rigidbody2D body;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        if (body == null) body = gameObject.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    private void FixedUpdate()
    {
        if (GameManager.Instance != null && GameManager.Instance.State != GameManager.RunState.Playing) return;
        float speed = moveSpeed * PlayerDash.GameSpeedMultiplier;
        Vector2 nextPosition = body.position + Vector2.left * speed * Time.fixedDeltaTime;
        body.MovePosition(nextPosition);

        if (nextPosition.x < leftLimit)
        {
            Destroy(gameObject);
        }
    }
}
