using UnityEngine;
using UnityEngine.Events;

public class HealthSystem : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 20f;
    [SerializeField] private float currentHealth;

    [Header("Health Events")]
    public UnityEvent<float> OnHealthChanged; // Passes current health
    public UnityEvent<float> OnHealthPercentChanged; // Passes health percentage (0-1)
    public UnityEvent OnDeath;
    public UnityEvent OnDamageTaken;
    public UnityEvent OnHealing;

    [Header("Debug Info")]
    [SerializeField] private bool isDead = false;
    [SerializeField] private bool isInvulnerable = false;
    [SerializeField] private float invulnerabilityDuration = 0f;

    // Properties for easy access
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public float HealthPercentage => currentHealth / maxHealth;
    public bool IsDead => isDead;
    public bool IsInvulnerable => isInvulnerable;

    void Start()
    {
        // Initialize health to max
        currentHealth = maxHealth;
        isDead = false;

        // Trigger initial health event
        OnHealthChanged?.Invoke(currentHealth);
        OnHealthPercentChanged?.Invoke(HealthPercentage);

        Debug.Log($"{gameObject.name} health initialized: {currentHealth}/{maxHealth}");
    }

    void Update()
    {
        // Handle invulnerability timer
        if (isInvulnerable && invulnerabilityDuration > 0f)
        {
            invulnerabilityDuration -= Time.deltaTime;
            if (invulnerabilityDuration <= 0f)
            {
                isInvulnerable = false;
                Debug.Log($"{gameObject.name} is no longer invulnerable");
            }
        }
    }

    public bool TakeDamage(float damage, string damageSource = "Unknown")
    {
        if (isDead || isInvulnerable || damage <= 0f) return false;

        currentHealth -= damage;
        currentHealth = Mathf.Max(0f, currentHealth); // Clamp to 0

        Debug.Log($"{gameObject.name} took {damage} damage from {damageSource}. Health: {currentHealth}/{maxHealth}");

        // Trigger events
        OnDamageTaken?.Invoke();
        OnHealthChanged?.Invoke(currentHealth);
        OnHealthPercentChanged?.Invoke(HealthPercentage);

        // Check for death
        if (currentHealth <= 0f && !isDead)
        {
            Die();
        }

        return true;
    }

    public bool Heal(float healAmount)
    {
        if (isDead || healAmount <= 0f) return false;

        float oldHealth = currentHealth;
        currentHealth += healAmount;
        currentHealth = Mathf.Min(maxHealth, currentHealth); // Clamp to max

        float actualHealing = currentHealth - oldHealth;

        if (actualHealing > 0f)
        {
            Debug.Log($"{gameObject.name} healed for {actualHealing}. Health: {currentHealth}/{maxHealth}");

            OnHealing?.Invoke();
            OnHealthChanged?.Invoke(currentHealth);
            OnHealthPercentChanged?.Invoke(HealthPercentage);
            return true;
        }

        return false;
    }

    public void SetInvulnerable(float duration)
    {
        isInvulnerable = true;
        invulnerabilityDuration = duration;
        Debug.Log($"{gameObject.name} is invulnerable for {duration} seconds");
    }

    public void SetHealth(float newHealth)
    {
        currentHealth = Mathf.Clamp(newHealth, 0f, maxHealth);
        OnHealthChanged?.Invoke(currentHealth);
        OnHealthPercentChanged?.Invoke(HealthPercentage);

        if (currentHealth <= 0f && !isDead)
        {
            Die();
        }
    }

    private void Die()
    {
        if (isDead) return;

        isDead = true;
        Debug.Log($"{gameObject.name} has died!");

        OnDeath?.Invoke();
    }

    public void Revive(float healthAmount = -1f)
    {
        if (healthAmount < 0f)
            healthAmount = maxHealth;

        isDead = false;
        currentHealth = Mathf.Clamp(healthAmount, 1f, maxHealth);

        OnHealthChanged?.Invoke(currentHealth);
        OnHealthPercentChanged?.Invoke(HealthPercentage);

        Debug.Log($"{gameObject.name} revived with {currentHealth} health!");
    }
}
