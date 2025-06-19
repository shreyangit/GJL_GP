using UnityEngine;

public class ZombieAI : MonoBehaviour
{
    [Header("Detection Settings")]
    public float detectionRadius = 5f;
    public LayerMask playerLayerMask = -1;

    [Header("Movement Settings")]
    public float moveSpeed = 2f;
    public float rotationSpeed = 180f;

    [Header("Collision Avoidance")]
    public LayerMask obstacleLayerMask = 64;  // Layer mask for walls/obstacles
    public float avoidanceRadius = 1f;       // How close to walls before avoiding
    public float avoidanceForce = 3f;        // Strength of avoidance

    [Header("Spawn Settings")]
    public Vector2 spawnAreaSize = new Vector2(20f, 15f);

    [Header("Despawn Settings")]
    public float despawnDistance = 25f;
    public float despawnTime = 10f;

    [Header("References")]
    public Transform player;

    [Header("Debug Info (Read Only)")]
    [SerializeField] private float timeAwayFromPlayer = 0f;
    [SerializeField] private bool isAwayFromPlayer = false;
    [SerializeField] private bool isAvoidingObstacle = false;

    private enum ZombieState
    {
        Idle,
        Chasing,
        AwaitingDespawn
    }

    private ZombieState currentState = ZombieState.Idle;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb2d;
    private bool isPlayerInRange = false;
    private MultiZombieSpawner spawner;  // Changed from ZombieSpawner to MultiZombieSpawner
    private Vector2 avoidanceDirection = Vector2.zero;

    void Start()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb2d = GetComponent<Rigidbody2D>();
        spawner = FindObjectOfType<MultiZombieSpawner>();  // Changed from ZombieSpawner to MultiZombieSpawner

        // Find player if not assigned
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }

        // Set up Rigidbody2D if missing
        if (rb2d == null)
        {
            rb2d = gameObject.AddComponent<Rigidbody2D>();
            rb2d.gravityScale = 0f;
            rb2d.freezeRotation = true;
        }

        // Spawn at random position around player
        SpawnAroundPlayer();
        SetState(ZombieState.Idle);
    }

    void Update()
    {
        if (player == null) return;

        CheckPlayerProximity();
        CheckDespawnConditions();
        UpdateBehavior();
    }

    void CheckPlayerProximity()
    {
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        bool wasPlayerInRange = isPlayerInRange;
        isPlayerInRange = distanceToPlayer <= detectionRadius;

        if (currentState != ZombieState.AwaitingDespawn)
        {
            if (isPlayerInRange && currentState == ZombieState.Idle)
            {
                SetState(ZombieState.Chasing);
            }
            else if (!isPlayerInRange && currentState == ZombieState.Chasing)
            {
                SetState(ZombieState.Idle);
            }
        }
    }

    void CheckDespawnConditions()
    {
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (distanceToPlayer > despawnDistance)
        {
            if (!isAwayFromPlayer)
            {
                isAwayFromPlayer = true;
                timeAwayFromPlayer = 0f;
                SetState(ZombieState.AwaitingDespawn);
                Debug.Log($"Zombie {name} starting despawn timer.");
            }
            else
            {
                timeAwayFromPlayer += Time.deltaTime;

                if (timeAwayFromPlayer >= despawnTime)
                {
                    DespawnZombie();
                }
            }
        }
        else
        {
            if (isAwayFromPlayer)
            {
                isAwayFromPlayer = false;
                timeAwayFromPlayer = 0f;
                Debug.Log($"Zombie {name} back in range. Canceling despawn.");

                if (isPlayerInRange)
                    SetState(ZombieState.Chasing);
                else
                    SetState(ZombieState.Idle);
            }
        }
    }

    void UpdateBehavior()
    {
        switch (currentState)
        {
            case ZombieState.Idle:
                // Stop movement
                if (rb2d != null)
                    rb2d.linearVelocity = Vector2.zero;
                break;

            case ZombieState.Chasing:
                ChasePlayerWithCollisionAvoidance();
                break;

            case ZombieState.AwaitingDespawn:
                // Stop movement while awaiting despawn
                if (rb2d != null)
                    rb2d.linearVelocity = Vector2.zero;
                break;
        }
    }

    void ChasePlayerWithCollisionAvoidance()
    {
        if (player == null) return;

        // Calculate direction to player
        Vector2 directionToPlayer = (player.position - transform.position).normalized;

        // Check for obstacles ahead
        Vector2 finalDirection = directionToPlayer;
        isAvoidingObstacle = false;

        // Perform collision detection in multiple directions
        Vector2 avoidanceDir = GetAvoidanceDirection(directionToPlayer);
        if (avoidanceDir != Vector2.zero)
        {
            // Blend player direction with avoidance direction
            finalDirection = (directionToPlayer + avoidanceDir * avoidanceForce).normalized;
            isAvoidingObstacle = true;
        }

        // Move using Rigidbody2D for better physics interaction
        if (rb2d != null)
        {
            rb2d.linearVelocity = finalDirection * moveSpeed;
        }
        else
        {
            // Fallback to transform movement
            transform.Translate(finalDirection * moveSpeed * Time.deltaTime, Space.World);
        }

        // Flip sprite based on movement direction
        if (finalDirection.x < 0)
            spriteRenderer.flipX = true;
        else if (finalDirection.x > 0)
            spriteRenderer.flipX = false;
    }

    // Calculate avoidance direction based on nearby obstacles
    Vector2 GetAvoidanceDirection(Vector2 desiredDirection)
    {
        Vector2 avoidanceDir = Vector2.zero;

        // Check multiple directions around the zombie
        Vector2[] checkDirections = new Vector2[]
        {
            desiredDirection,                    // Forward
            desiredDirection + Vector2.right * 0.5f,   // Forward-right
            desiredDirection + Vector2.left * 0.5f,    // Forward-left
            Vector2.right,                       // Right
            Vector2.left,                        // Left
        };

        foreach (Vector2 direction in checkDirections)
        {
            Vector2 checkPosition = (Vector2)transform.position + direction.normalized * avoidanceRadius;

            // Check for collision at this position
            Collider2D hit = Physics2D.OverlapCircle(checkPosition, 0.3f, obstacleLayerMask);

            if (hit != null)
            {
                // Calculate avoidance direction (perpendicular to obstacle)
                Vector2 awayFromObstacle = ((Vector2)transform.position - checkPosition).normalized;
                avoidanceDir += awayFromObstacle;
            }
        }

        return avoidanceDir.normalized;
    }

    void SetState(ZombieState newState)
    {
        if (currentState == newState) return;

        currentState = newState;

        if (animator != null)
        {
            switch (currentState)
            {
                case ZombieState.Idle:
                case ZombieState.AwaitingDespawn:
                    animator.SetBool("IsWalking", false);
                    break;

                case ZombieState.Chasing:
                    animator.SetBool("IsWalking", true);
                    break;
            }
        }

        if (spriteRenderer != null)
        {
            switch (currentState)
            {
                case ZombieState.AwaitingDespawn:
                    spriteRenderer.color = new Color(1f, 1f, 1f, 0.7f);
                    break;

                default:
                    spriteRenderer.color = new Color(1f, 1f, 1f, 1f);
                    break;
            }
        }
    }

    void DespawnZombie()
    {
        Debug.Log($"Despawning zombie {name} after {timeAwayFromPlayer:F1} seconds away from player.");

        // Updated to work with MultiZombieSpawner
        if (spawner != null)
        {
            spawner.OnZombieDespawned(gameObject, MultiZombieSpawner.ZombieType.Normal);
        }

        Destroy(gameObject);
    }

    void SpawnAroundPlayer()
    {
        if (player == null) return;

        Vector2 randomOffset = new Vector2(
            Random.Range(-spawnAreaSize.x / 2f, spawnAreaSize.x / 2f),
            Random.Range(-spawnAreaSize.y / 2f, spawnAreaSize.y / 2f)
        );

        Vector3 spawnPosition = player.position + new Vector3(randomOffset.x, randomOffset.y, 0f);
        transform.position = spawnPosition;
    }

    // Visualize detection, avoidance, and despawn areas
    void OnDrawGizmosSelected()
    {
        // Detection radius
        Gizmos.color = Color.red;
        DrawWireCircle(transform.position, detectionRadius, 32);

        // Avoidance radius
        Gizmos.color = isAvoidingObstacle ? Color.orange : Color.yellow;
        DrawWireCircle(transform.position, avoidanceRadius, 16);

        // Despawn distance
        Gizmos.color = Color.cyan;
        DrawWireCircle(transform.position, despawnDistance, 32);

        // Spawn area around player
        if (player != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(player.position, new Vector3(spawnAreaSize.x, spawnAreaSize.y, 0f));
        }

        // Line to player
        if (player != null)
        {
            Gizmos.color = isAwayFromPlayer ? Color.red : Color.blue;
            Gizmos.DrawLine(transform.position, player.position);
        }
    }

    void DrawWireCircle(Vector3 center, float radius, int segments = 32)
    {
        if (segments < 3) segments = 3;

        float angleStep = 360f / segments;
        Vector3 prevPoint = center + new Vector3(radius, 0, 0);

        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 newPoint = center + new Vector3(
                Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * radius,
                0
            );

            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
    }
}
