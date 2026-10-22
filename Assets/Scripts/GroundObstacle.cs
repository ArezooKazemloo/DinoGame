
using UnityEngine;

public class GroundObstacle : Interactable
{
    public int damage = 1;

    public override void Interact(PlayerHealth playerHealth)
    {
        if (playerHealth == null) return;
        Collider2D playerCollider = playerHealth.GetComponent<Collider2D>();
        Collider2D obstacleCollider = GetComponent<Collider2D>();
        // A shallow overlap while the dinosaur clears the top of a ground
        // hazard is a successful jump, not a hit.
        if (playerCollider != null && obstacleCollider != null &&
            playerCollider.bounds.min.y >= obstacleCollider.bounds.max.y - .04f) return;
        Debug.Log("Obstacle hit the player!");
        playerHealth.TakeDamage(damage);
    }
}
