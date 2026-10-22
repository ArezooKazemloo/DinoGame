using UnityEngine;

/// <summary>A collectible that restores one missing life.</summary>
public sealed class HealthPickup : Interactable
{
    [Min(1)] public int healAmount = 1;
    public override void Interact(PlayerHealth playerHealth)
    {
        if (playerHealth == null) return;
        bool restoresLife = playerHealth.currentHealth < playerHealth.maxHealth;
        if (restoresLife)
            playerHealth.Heal(healAmount);
        if (GameManager.Instance != null) GameManager.Instance.AddScore(5);
        if (!restoresLife && AudioManager.Instance != null) AudioManager.Instance.PlayScore();
        Destroy(gameObject);
    }
}
