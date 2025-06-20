using UnityEngine;

public class ZombieAI : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 2f;
    public float detectionRange = 5f;

    [Header("Debug Info")]
    [SerializeField] private bool playerDetected = false;
    [SerializeField] private float distanceToPlayer = 0f;

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

        Debug.Log($"{gameObject.name} zombie AI initialized");
    }

    void Update()
    {
        if (playerTransform == null) return;

        CheckForPlayer();
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

    void UpdateMovement()
    {
        if (rb2d == null) return;

        if (isMoving && playerDetected)
        {
            // Move towards player
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
        animator.SetBool("IsMoving", isMoving);
        animator.SetFloat("MoveX", movementDirection.x);
        animator.SetFloat("MoveY", movementDirection.y);
    }

    void OnDrawGizmosSelected()
    {
        // Draw detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Draw line to player when detected
        if (playerDetected && playerTransform != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, playerTransform.position);
        }
    }
}
