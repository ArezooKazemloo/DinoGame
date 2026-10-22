using UnityEngine;

/// <summary>Airborne obstacle with the same interaction contract as ground hazards.</summary>
public sealed class FlyingObstacle : Interactable
{
    [Min(1)] public int damage = 1;
    public override void Interact(PlayerHealth playerHealth)
    {
        if (playerHealth != null) playerHealth.TakeDamage(damage);
    }
}
