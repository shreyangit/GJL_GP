using UnityEngine;

public class ZombieAnimationController : MonoBehaviour
{
    [Header("Animation References")]
    public Animator animator;
    public string zombieType = "Normal"; // "Normal", "Brightness", "Dynamite"

    [Header("Death Settings")]
    public float deathAnimationDuration = 2f;
    public GameObject deathEffect;
    public AudioClip deathSound;

    private HealthSystem healthSystem;
    private AudioSource audioSource;
    private bool isDead = false;

    void Start()
    {
        // Get components
        if (animator == null)
            animator = GetComponent<Animator>();

        healthSystem = GetComponent<HealthSystem>();
        audioSource = GetComponent<AudioSource>();

        // Subscribe to health events
        if (healthSystem != null)
        {
            healthSystem.OnDamageTaken.AddListener(OnTakeDamage);
            healthSystem.OnDeath.AddListener(OnDeath);
        }

        Debug.Log($"{gameObject.name} animation controller initialized for {zombieType} zombie");
    }

    void Update()
    {
        if (isDead) return; // Don't update animations if dead

        UpdateMovementAnimations();
    }

    void UpdateMovementAnimations()
    {
        if (animator == null) return;

        // Get movement from AI components
        Vector2 velocity = Vector2.zero;
        bool isMoving = false;

        // Get velocity from appropriate AI component
        switch (zombieType)
        {
            case "Normal":
                ZombieAI normalAI = GetComponent<ZombieAI>();
                if (normalAI != null)
                {
                    Rigidbody2D rb = GetComponent<Rigidbody2D>();
                    if (rb != null) velocity = rb.linearVelocity;
                }
                break;

            case "Brightness":
                BrightnessZombieAI brightnessAI = GetComponent<BrightnessZombieAI>();
                if (brightnessAI != null)
                {
                    Rigidbody2D rb = GetComponent<Rigidbody2D>();
                    if (rb != null) velocity = rb.linearVelocity;
                }
                break;

            case "Dynamite":
                DynamiteZombieAI dynamiteAI = GetComponent<DynamiteZombieAI>();
                if (dynamiteAI != null)
                {
                    Rigidbody2D rb = GetComponent<Rigidbody2D>();
                    if (rb != null) velocity = rb.linearVelocity;
                }
                break;
        }

        isMoving = velocity.magnitude > 0.1f;

        // Update animator parameters
        animator.SetBool("IsMoving", isMoving);
        animator.SetFloat("MoveX", velocity.x);
        animator.SetFloat("MoveY", velocity.y);
    }

    public void OnTakeDamage()
    {
        if (isDead || animator == null) return;

        // Trigger hurt animation
        animator.SetTrigger("TakeDamage");

        Debug.Log($"{gameObject.name} played hurt animation");
    }

    public void OnDeath()
    {
        if (isDead) return;

        isDead = true;

        Debug.Log($"{gameObject.name} starting death sequence");

        // Stop AI movement
        StopAIMovement();

        // Play death animation
        if (animator != null)
        {
            animator.SetBool("IsDead", true);
        }

        // Play death sound
        if (audioSource != null && deathSound != null)
        {
            audioSource.PlayOneShot(deathSound);
        }

        // Spawn death effect
        if (deathEffect != null)
        {
            Instantiate(deathEffect, transform.position, Quaternion.identity);
        }

        // Handle zombie-specific death behavior
        HandleSpecificDeathBehavior();

        // Destroy after animation completes
        Destroy(gameObject, deathAnimationDuration);
    }

    void StopAIMovement()
    {
        // Stop all AI components
        ZombieAI normalAI = GetComponent<ZombieAI>();
        BrightnessZombieAI brightnessAI = GetComponent<BrightnessZombieAI>();
        DynamiteZombieAI dynamiteAI = GetComponent<DynamiteZombieAI>();

        if (normalAI != null) normalAI.enabled = false;
        if (brightnessAI != null) brightnessAI.enabled = false;
        if (dynamiteAI != null) dynamiteAI.enabled = false;

        // Stop rigidbody movement
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }
    }

    void HandleSpecificDeathBehavior()
    {
        switch (zombieType)
        {
            case "Normal":
                // Normal zombies just die
                Debug.Log($"Normal zombie {gameObject.name} died");
                break;

            case "Brightness":
                // Brightness zombies could create a light flash
                Debug.Log($"Brightness zombie {gameObject.name} died with light effect");
                // Add light flash effect here
                break;

            case "Dynamite":
                // Dynamite zombies explode (handled by ZombieAttack component)
                Debug.Log($"Dynamite zombie {gameObject.name} died and will explode");
                ZombieAttack zombieAttack = GetComponent<ZombieAttack>();
                if (zombieAttack != null)
                {
                    zombieAttack.ExplodeDynamiteZombie();
                }
                break;
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from events
        if (healthSystem != null)
        {
            healthSystem.OnDamageTaken.RemoveListener(OnTakeDamage);
            healthSystem.OnDeath.RemoveListener(OnDeath);
        }
    }
}
