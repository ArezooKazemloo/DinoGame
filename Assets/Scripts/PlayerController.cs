using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Side-scroller jump control. Uses the new Input System action API.</summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [SerializeField, Min(0f)] private float jumpForce = 8f;
    [SerializeField] private LayerMask groundLayers = ~0;
    [SerializeField] private Animator animator;

    private Rigidbody2D body;
    private InputAction jumpAction;
    private bool grounded;
    private Vector3 startingPosition;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        body.freezeRotation = true;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        SpriteRenderer playerRenderer = GetComponent<SpriteRenderer>();
        if (playerRenderer != null)
        {
            playerRenderer.enabled = true;
            playerRenderer.color = Color.white;
            playerRenderer.sortingOrder = 5;
        }
        BoxCollider2D playerBox = GetComponent<BoxCollider2D>();
        if (playerBox != null)
        {
            // Keep the hitbox slightly inside the art so a clear jump is not
            // punished by transparent edge pixels.
            playerBox.size = new Vector2(.72f, .78f);
            // The cartoon sprite has transparent breathing room to its right;
            // align the hitbox with the visible body instead of the full canvas.
            playerBox.offset = new Vector2(-.16f, 0f);
        }
        if (animator == null) animator = GetComponent<Animator>();
        jumpAction = new InputAction("Jump", InputActionType.Button, "<Keyboard>/space");
    }

    private void OnEnable() => jumpAction?.Enable();

    private void Start()
    {
        Collider2D playerCollider = GetComponent<Collider2D>();
        GameObject groundObject = GameObject.Find("Ground");
        Collider2D groundCollider = groundObject != null ? groundObject.GetComponent<Collider2D>() : null;
        if (playerCollider != null && groundCollider != null)
        {
            Vector3 position = transform.position;
            position.y = groundCollider.bounds.max.y + playerCollider.bounds.extents.y + .01f;
            transform.position = position;
            grounded = true;
        }
        startingPosition = transform.position;
    }

    public void ResetForNewRun()
    {
        body.linearVelocity = Vector2.zero;
        transform.position = startingPosition;
        grounded = true;
    }

    private void Update()
    {
        if (jumpAction != null && jumpAction.WasPressedThisFrame() && grounded && IsPlaying())
        {
            grounded = false;
            body.linearVelocity = new Vector2(body.linearVelocity.x, 0f);
            body.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            if (animator != null) animator.SetTrigger("Jump");
        }
        if (animator != null)
        {
            animator.SetBool("Grounded", grounded);
            animator.SetBool("Running", IsPlaying() && grounded);
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (!IsGround(collision.collider)) return;
        foreach (ContactPoint2D contact in collision.contacts)
        {
            if (contact.normal.y > .45f) { grounded = true; break; }
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (IsGround(collision.collider)) grounded = false;
    }

    private bool IsGround(Collider2D other)
    {
        if (other == null) return false;
        if (other.gameObject.name.Contains("Ground")) return true;
        return (groundLayers.value & (1 << other.gameObject.layer)) != 0;
    }

    private static bool IsPlaying() => GameManager.Instance == null || GameManager.Instance.State == GameManager.RunState.Playing;

    private void OnDisable() => jumpAction?.Disable();
    private void OnDestroy() => jumpAction?.Dispose();
}
