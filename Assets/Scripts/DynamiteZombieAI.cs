using UnityEngine;

public class DynamiteZombieAI : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 4f; // ✅ Slightly faster than normal zombies
    public float detectionRange = 8f; // ✅ INCREASED detection range

    [Header("Explosion Settings")]
    public float explosionRange = 4f; // ✅ INCREASED explosion range
    public GameObject explosionEffectPrefab;

    [Header("Debug Info")]
    [SerializeField] private bool playerDetected = false;
    [SerializeField] private float distanceToPlayer = 0f;
    [SerializeField] private bool isExploding = false;

    [HideInInspector] public PlayerController player;
    private Transform playerTransform;
    private Rigidbody2D rb2d;
    private Animator animator;
    private Vector2 movementDirection;
    private bool isMoving = false;

    void Start()
    {
        rb2d = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        if (rb2d != null)
        {
            rb2d.gravityScale = 0f;
            rb2d.freezeRotation = true;
        }

        if (player == null)
        {
            player = FindFirstObjectByType<PlayerController>();
        }

        if (player != null)
        {
            playerTransform = player.transform;
        }

        Debug.Log($"✅ {gameObject.name} dynamite zombie AI initialized - Explosion range: {explosionRange}");
    }

    void Update()
    {
        if (playerTransform == null || isExploding) return;

        CheckForPlayer();
        CheckForExplosion();
        UpdateMovement();
        UpdateAnimations();
    }

    void CheckForPlayer()
    {
        distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
        playerDetected = distanceToPlayer <= detectionRange;

        if (playerDetected)
        {
            movementDirection = (playerTransform.position - transform.position).normalized;
            isMoving = true;
        }
        else
        {
            movementDirection = Vector2.zero;
            isMoving = false;
        }
    }

    void CheckForExplosion()
    {
        if (playerDetected && distanceToPlayer <= explosionRange)
        {
            Debug.Log($"🧨 Dynamite zombie {gameObject.name} triggered explosion at distance {distanceToPlayer:F1}");
            TriggerExplosion();
        }
    }

    void TriggerExplosion()
    {
        if (isExploding) return;

        isExploding = true;

        Debug.Log($"💥 Dynamite zombie {gameObject.name} is exploding!");

        if (explosionEffectPrefab != null)
        {
            Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
        }

        // ✅ ENHANCED: Try multiple methods to deal damage
        ZombieAttack zombieAttack = GetComponent<ZombieAttack>();
        if (zombieAttack != null)
        {
            Debug.Log("✅ Found ZombieAttack component - calling ExplodeDynamiteZombie()");
            zombieAttack.ExplodeDynamiteZombie();
        }
        else
        {
            Debug.LogWarning("❌ No ZombieAttack component found - applying direct damage");
            // ✅ FALLBACK: Deal damage directly
            ApplyExplosionDamageDirectly();
            Destroy(gameObject);
        }
    }

    // ✅ NEW: Fallback damage method
    void ApplyExplosionDamageDirectly()
    {
        if (player == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
        int explosionDamage = 0;
        string damageRange = "";

        // Damage based on distance (same logic as ZombieAttack)
        if (distanceToPlayer <= 1.5f) // Very near
        {
            explosionDamage = 5;
            damageRange = "Very Near";
        }
        else if (distanceToPlayer <= 3f) // Mid near
        {
            explosionDamage = 3;
            damageRange = "Mid Near";
        }
        else if (distanceToPlayer <= 5f) // Far
        {
            explosionDamage = 1;
            damageRange = "Far";
        }

        if (explosionDamage > 0)
        {
            player.TakeDamage(explosionDamage, $"{gameObject.name} (Dynamite Explosion - {damageRange})");
            Debug.Log($"💥 DIRECT DAMAGE: Dynamite zombie dealt {explosionDamage} damage ({damageRange}) at distance {distanceToPlayer:F1}");
        }
        else
        {
            Debug.Log($"💥 Explosion too far: Distance {distanceToPlayer:F1} > 5f - no damage");
        }
    }

    void UpdateMovement()
    {
        if (rb2d == null || isExploding) return;

        if (isMoving && playerDetected)
        {
            rb2d.linearVelocity = movementDirection * moveSpeed;
        }
        else
        {
            rb2d.linearVelocity = Vector2.zero;
        }
    }

    void UpdateAnimations()
    {
        if (animator == null) return;

        animator.SetBool("IsMoving", isMoving && !isExploding);
        animator.SetBool("IsExploding", isExploding);
        animator.SetFloat("MoveX", movementDirection.x);
        animator.SetFloat("MoveY", movementDirection.y);
    }

    void OnDrawGizmosSelected()
    {
        // Draw detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Draw explosion range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRange);

        // ✅ Draw damage ranges
        Gizmos.color = Color.orange;
        Gizmos.DrawWireSphere(transform.position, 1.5f); // Very near (5 damage)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 3f);   // Mid near (3 damage)
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 5f);   // Far (1 damage)

        if (playerDetected && playerTransform != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, playerTransform.position);
        }
    }
}
