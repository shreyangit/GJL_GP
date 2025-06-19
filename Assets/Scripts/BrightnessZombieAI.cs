using UnityEngine;

public class BrightnessZombieAI : MonoBehaviour
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

    [Header("Brightness Zombie Specific")]
    public float brightnessRadius = 8f;      // Special brightness effect radius
    public float brightnessIntensity = 2f;   // Light intensity

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
    private UnityEngine.Rendering.Universal.Light2D zombieLight;
    private bool isPlayerInRange = false;
    private MultiZombieSpawner spawner;
    private Vector2 avoidanceDirection = Vector2.zero;

    void Start()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb2d = GetComponent<Rigidbody2D>();
        zombieLight = GetComponent<UnityEngine.Rendering.Universal.Light2D>();
        spawner = FindObjectOfType<MultiZombieSpawner>();

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

        // Configure brightness light
        if (zombieLight != null)
        {
            zombieLight.lightType = UnityEngine.Rendering.Universal.Light2D.LightType.Point;
            zombieLight.intensity = brightnessIntensity;
            zombieLight.pointLightOuterRadius = brightnessRadius;
        }

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
                if (rb2d != null)
                    rb2d.linearVelocity = Vector2.zero;
                break;

            case ZombieState.Chasing:
                ChasePlayerWithCollisionAvoidance();
                break;

            case ZombieState.AwaitingDespawn:
                if (rb2d != null)
                    rb2d.linearVelocity = Vector2.zero;
                break;
        }
    }

    void ChasePlayerWithCollisionAvoidance()
    {
        if (player == null) return;

        Vector2 directionToPlayer = (player.position - transform.position).normalized;
        Vector2 finalDirection = directionToPlayer;
        isAvoidingObstacle = false;

        Vector2 avoidanceDir = GetAvoidanceDirection(directionToPlayer);
        if (avoidanceDir != Vector2.zero)
        {
            finalDirection = (directionToPlayer + avoidanceDir * avoidanceForce).normalized;
            isAvoidingObstacle = true;
        }

        if (rb2d != null)
        {
            rb2d.linearVelocity = finalDirection * moveSpeed;
        }

        if (finalDirection.x < 0)
            spriteRenderer.flipX = true;
        else if (finalDirection.x > 0)
            spriteRenderer.flipX = false;
    }

    Vector2 GetAvoidanceDirection(Vector2 desiredDirection)
    {
        Vector2 avoidanceDir = Vector2.zero;

        Vector2[] checkDirections = new Vector2[]
        {
            desiredDirection,
            desiredDirection + Vector2.right * 0.5f,
            desiredDirection + Vector2.left * 0.5f,
            Vector2.right,
            Vector2.left,
        };

        foreach (Vector2 direction in checkDirections)
        {
            Vector2 checkPosition = (Vector2)transform.position + direction.normalized * avoidanceRadius;
            Collider2D hit = Physics2D.OverlapCircle(checkPosition, 0.3f, obstacleLayerMask);

            if (hit != null)
            {
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

        // Add null check and parameter existence check
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            // Check if the parameter exists before trying to set it
            foreach (AnimatorControllerParameter param in animator.parameters)
            {
                if (param.name == "IsWalking")
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
                    break; // Exit the loop once we find the parameter
                }
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
        if (spawner != null)
        {
            spawner.OnZombieDespawned(gameObject, MultiZombieSpawner.ZombieType.Brightness);
        }

        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        // Detection radius
        Gizmos.color = Color.red;
        DrawWireCircle(transform.position, detectionRadius, 32);

        // Brightness radius
        Gizmos.color = Color.yellow;
        DrawWireCircle(transform.position, brightnessRadius, 32);

        // Avoidance radius
        Gizmos.color = isAvoidingObstacle ? Color.orange : Color.cyan;
        DrawWireCircle(transform.position, avoidanceRadius, 16);

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
