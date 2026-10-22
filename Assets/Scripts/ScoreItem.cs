
using UnityEngine;

public class ScoreItem : Interactable
{
    public int scoreValue = 10;

    public override void Interact(PlayerHealth playerHealth)
    {
        if (GameManager.Instance != null) GameManager.Instance.AddScore(scoreValue);
        if (AudioManager.Instance != null) AudioManager.Instance.PlayScore();
        Destroy(gameObject);
    }
}
