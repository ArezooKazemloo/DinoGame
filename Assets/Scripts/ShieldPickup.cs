using UnityEngine;

/// <summary>A collectible that grants temporary collision immunity.</summary>
public sealed class ShieldPickup : Interactable
{
    [Min(.1f)] public float shieldDuration = 4f;
    public override void Interact(PlayerHealth playerHealth)
    {
        if (playerHealth == null) return;
        playerHealth.ActivateShield(shieldDuration);
        if (GameManager.Instance != null) GameManager.Instance.AddScore(10);
        if (AudioManager.Instance != null) AudioManager.Instance.PlayShield();
        Destroy(gameObject);
    }
}
