using UnityEngine;

public class DynamiteZombieAI : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 3f;
    public float detectionRange = 6f;

    [Header("Explosion Settings")]
    public float explosionRange = 2f;
    public GameObject explosionEffectPrefab;

    [Header("Debug Info")]
    [SerializeField] private bool playerDetected = false;
    [SerializeField] private float distanceToPlayer = 0f;
    [SerializeField] private bool isExploding = false;

    // FIXED: Made player public so MultiZombieSpawner can access it
    [HideInInspector] public PlayerController player;
    private Transform playerTransform;
    private Rigidbody2D rb2d;
    private Animator animator;
    private Vector2 movementDirection;
    private bool isMoving = false;

    void Start()
    {
        // Get components
        rb2d = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        // Initialize Rigidbody2D settings
        if (rb2d != null)
        {
            rb2d.gravityScale = 0f;
            rb2d.freezeRotation = true;
        }

        // Find player if not already set by spawner
        if (player == null)
        {
            player = FindFirstObjectByType<PlayerController>();
        }

        if (player != null)
        {
            playerTransform = player.transform;
        }

        Debug.Log($"{gameObject.name} dynamite zombie AI initialized");
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
            // Calculate direction to player
            movementDirection = (playerTransform.position - transform.position).normalized;
            isMoving = true;
        }
        else
        {
            // Stop moving when player is out of range
            movementDirection = Vector2.zero;
            isMoving = false;
        }
    }

    void CheckForExplosion()
    {
        // Explode when close to player
        if (playerDetected && distanceToPlayer <= explosionRange)
        {
            TriggerExplosion();
        }
    }

    void TriggerExplosion()
    {
        if (isExploding) return;

        isExploding = true;

        Debug.Log($"Dynamite zombie {gameObject.name} is exploding!");

        // Create explosion effect if prefab exists
        if (explosionEffectPrefab != null)
        {
            Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
        }

        // Get the ZombieAttack component and trigger explosion damage
        ZombieAttack zombieAttack = GetComponent<ZombieAttack>();
        if (zombieAttack != null)
        {
            zombieAttack.ExplodeDynamiteZombie();
        }
        else
        {
            // Fallback: Just destroy the zombie
            Destroy(gameObject);
        }
    }

    void UpdateMovement()
    {
        if (rb2d == null || isExploding) return;

        if (isMoving && playerDetected)
        {
            // Move towards player (faster than normal zombies)
            rb2d.linearVelocity = movementDirection * moveSpeed;
        }
        else
        {
            // Stop movement
            rb2d.linearVelocity = Vector2.zero;
        }
    }

    void UpdateAnimations()
    {
        if (animator == null) return;

        // Set animation parameters
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

        // Draw line to player when detected
        if (playerDetected && playerTransform != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, playerTransform.position);
        }
    }
}
