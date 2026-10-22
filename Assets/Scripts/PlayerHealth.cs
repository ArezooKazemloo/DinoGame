using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    public static PlayerHealth Instance { get; private set; }

    [Min(1)] public int maxHealth = 3;
    public int currentHealth;
    public Image healthBarFill;
    public TMP_Text healthText;
    [FormerlySerializedAs("invulnerabilitySeconds"), Min(0)] public float hitFlashSeconds = .8f;
    [Min(0)] public float flashSeconds = .12f;

    private bool isDead;
    private bool shieldActive;
    private Coroutine hitFeedbackRoutine;
    private Coroutine shieldRoutine;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private Color originalColor = Color.white;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        if (spriteRenderer != null) originalColor = spriteRenderer.color;
    }

    private void Start()
    {
        if (healthText == null)
        {
            TMP_Text[] texts = FindObjectsByType<TMP_Text>(FindObjectsSortMode.None);
            foreach (TMP_Text text in texts) if (text.name == "Health") { healthText = text; break; }
        }
        if (healthBarFill == null)
        {
            Image[] images = FindObjectsByType<Image>(FindObjectsSortMode.None);
            foreach (Image image in images) if (image.name == "HealthBarFill") { healthBarFill = image; break; }
        }
        if (healthBarFill != null) healthBarFill.gameObject.SetActive(true);
        GameObject healthBarBackground = GameObject.Find("HealthBarBackground");
        if (healthBarBackground != null) healthBarBackground.SetActive(true);
        if (healthBarFill != null)
        {
            healthBarFill.type = Image.Type.Filled;
            healthBarFill.fillMethod = Image.FillMethod.Horizontal;
            healthBarFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        }
        ResetHealth();
    }

    public void ResetHealth()
    {
        isDead = false;
        shieldActive = false;
        if (hitFeedbackRoutine != null) StopCoroutine(hitFeedbackRoutine);
        if (shieldRoutine != null) StopCoroutine(shieldRoutine);
        hitFeedbackRoutine = null;
        shieldRoutine = null;
        currentHealth = maxHealth;
        if (spriteRenderer != null) spriteRenderer.color = originalColor;
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.ResetTrigger("Jump");
            animator.ResetTrigger("Hit");
            animator.ResetTrigger("Death");
            animator.ResetTrigger("Dash");
            animator.Play("Run", 0, 0f);
            animator.Update(0f);
        }
        UpdateHealthUI();
    }

    public void TakeDamage(int damage)
    {
        if (isDead || shieldActive || damage <= 0) return;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayHit();
        currentHealth = Mathf.Max(0, currentHealth - damage);
        UpdateHealthUI();
        if (currentHealth == 0)
        {
            isDead = true;
            if (animator != null) animator.SetTrigger("Death");
            if (GameManager.Instance != null) GameManager.Instance.EndGame();
            return;
        }
        if (animator != null) animator.SetTrigger("Hit");
        if (isActiveAndEnabled)
        {
            if (hitFeedbackRoutine != null) StopCoroutine(hitFeedbackRoutine);
            hitFeedbackRoutine = StartCoroutine(HitFeedback());
        }
    }

    public void Heal(int amount)
    {
        if (isDead || amount <= 0) return;
        int previousHealth = currentHealth;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        UpdateHealthUI();
        if (currentHealth > previousHealth && AudioManager.Instance != null)
            AudioManager.Instance.PlayLifeUp();
    }

    public void ActivateShield(float duration)
    {
        if (isDead || duration <= 0f) return;
        if (shieldRoutine != null) StopCoroutine(shieldRoutine);
        shieldActive = true;
        shieldRoutine = StartCoroutine(ShieldRoutine(duration));
    }

    private IEnumerator ShieldRoutine(float duration)
    {
        shieldActive = true;
        float elapsed = 0f;
        while (elapsed < duration && !isDead)
        {
            if (spriteRenderer != null)
                spriteRenderer.color = Color.Lerp(originalColor, new Color(.35f, .9f, 1f), .5f + .5f * Mathf.Sin(Time.time * 12f));
            yield return null;
            elapsed += Time.deltaTime;
        }
        if (spriteRenderer != null) spriteRenderer.color = originalColor;
        shieldActive = false;
        shieldRoutine = null;
    }

    private IEnumerator HitFeedback()
    {
        float flashInterval = Mathf.Max(.02f, flashSeconds);
        float elapsed = 0f;
        while (elapsed < hitFlashSeconds)
        {
            if (spriteRenderer != null && !shieldActive)
            {
                spriteRenderer.color = (Mathf.FloorToInt(elapsed / flashInterval) % 2 == 0)
                    ? new Color(1f, .34f, .29f, 1f) : originalColor;
            }
            yield return new WaitForSeconds(flashInterval);
            elapsed += flashInterval;
        }
        if (spriteRenderer != null && !shieldActive) spriteRenderer.color = originalColor;
        hitFeedbackRoutine = null;
    }

    private void UpdateHealthUI()
    {
        if (healthText != null)
        {
            healthText.text = $"LIVES   {new string('\u2665', currentHealth)}{new string('\u2661', Mathf.Max(0, maxHealth - currentHealth))}";
            healthText.color = new Color(1f, .57f, .43f);
            healthText.fontSize = 25f;
        }
        if (healthBarFill != null) healthBarFill.fillAmount = maxHealth <= 0 ? 0f : (float)currentHealth / maxHealth;
    }

    private void OnDestroy() { if (Instance == this) Instance = null; }
}
