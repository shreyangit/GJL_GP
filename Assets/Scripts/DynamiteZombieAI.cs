using UnityEngine;

public class DynamiteZombieAI : MonoBehaviour
{
    [Header("Detection Settings")]
    public float detectionRadius = 5f;
    public LayerMask playerLayerMask = -1;

    [Header("Movement Settings")]
    public float moveSpeed = 2f;
    public float rotationSpeed = 180f;

    [Header("Collision Avoidance")]
    public LayerMask obstacleLayerMask = 64;
    public float avoidanceRadius = 1f;
    public float avoidanceForce = 3f;

    [Header("Spawn Settings")]
    public Vector2 spawnAreaSize = new Vector2(20f, 15f);

    [Header("Despawn Settings")]
    public float despawnDistance = 25f;
    public float despawnTime = 10f;

    [Header("Dynamite Zombie Specific")]
    public float explosionRadius = 4f;       // Explosion range
    public float explosionDamage = 50f;      // Explosion damage
    public float fuseTime = 3f;              // Time before exploding

    [Header("References")]
    public Transform player;

    [Header("Debug Info (Read Only)")]
    [SerializeField] private float timeAwayFromPlayer = 0f;
    [SerializeField] private bool isAwayFromPlayer = false;
    [SerializeField] private bool isAvoidingObstacle = false;
    [SerializeField] private bool isAboutToExplode = false;
    [SerializeField] private float fuseTimer = 0f;

    private enum ZombieState
    {
        Idle,
        Chasing,
        AwaitingDespawn,
        AboutToExplode
    }

    private ZombieState currentState = ZombieState.Idle;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb2d;
    private bool isPlayerInRange = false;
    private MultiZombieSpawner spawner;
    private Vector2 avoidanceDirection = Vector2.zero;

    void Start()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb2d = GetComponent<Rigidbody2D>();
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

        SetState(ZombieState.Idle);
    }

    void Update()
    {
        if (player == null) return;

        CheckPlayerProximity();
        CheckDespawnConditions();
        UpdateExplosionLogic();
        UpdateBehavior();
    }

    void CheckPlayerProximity()
    {
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        bool wasPlayerInRange = isPlayerInRange;
        isPlayerInRange = distanceToPlayer <= detectionRadius;

        if (currentState != ZombieState.AwaitingDespawn && currentState != ZombieState.AboutToExplode)
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

    void UpdateExplosionLogic()
    {
        if (currentState == ZombieState.AboutToExplode)
        {
            fuseTimer += Time.deltaTime;

            // Flash effect as explosion approaches
            float flashSpeed = Mathf.Lerp(2f, 10f, fuseTimer / fuseTime);
            float alpha = (Mathf.Sin(Time.time * flashSpeed) + 1f) * 0.5f;
            spriteRenderer.color = new Color(1f, alpha, alpha, 1f);

            if (fuseTimer >= fuseTime)
            {
                Explode();
            }
        }
        else
        {
            // Check if close enough to player to start explosion
            float distanceToPlayer = Vector2.Distance(transform.position, player.position);
            if (distanceToPlayer <= explosionRadius && currentState == ZombieState.Chasing)
            {
                StartExplosion();
            }
        }
    }

    void CheckDespawnConditions()
    {
        if (currentState == ZombieState.AboutToExplode) return; // Don't despawn while exploding

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
            case ZombieState.AboutToExplode:
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

    void StartExplosion()
    {
        SetState(ZombieState.AboutToExplode);
        fuseTimer = 0f;
        isAboutToExplode = true;
        Debug.Log($"Dynamite Zombie starting explosion sequence!");
    }

    void Explode()
    {
        Debug.Log($"Dynamite Zombie exploded at {transform.position}!");

        // Create explosion effect
        GameObject explosionPrefab = CreateExplosionEffect();
        if (explosionPrefab != null)
        {
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        }

        // Notify spawner and destroy zombie
        if (spawner != null)
        {
            spawner.OnZombieDespawned(gameObject, MultiZombieSpawner.ZombieType.Dynamite);
        }

        Destroy(gameObject);
    }

    GameObject CreateExplosionEffect()
    {
        // Create explosion GameObject
        GameObject explosion = new GameObject("DynamiteExplosion");
        explosion.transform.position = transform.position;

        // Add SpriteRenderer
        SpriteRenderer explosionRenderer = explosion.AddComponent<SpriteRenderer>();
        explosionRenderer.sortingLayerName = "Default";
        explosionRenderer.sortingOrder = 10;

        // Add ExplosionEffect script
        ExplosionEffect explosionEffect = explosion.AddComponent<ExplosionEffect>();

        // Try to load explosion sprites
        Sprite[] explosionSprites = Resources.LoadAll<Sprite>("Effects/Explosion_Dynamite");
        if (explosionSprites.Length == 0)
        {
            // Fallback: try to load from the Effects folder
            explosionSprites = new Sprite[]
            {
            Resources.Load<Sprite>("Effects/Explosion_Dynamite/explosion1"),
            Resources.Load<Sprite>("Effects/Explosion_Dynamite/explosion2"),
            Resources.Load<Sprite>("Effects/Explosion_Dynamite/explosion3"),
            Resources.Load<Sprite>("Effects/Explosion_Dynamite/explosion4")
            };
        }

        explosionEffect.explosionSprites = explosionSprites;
        explosionEffect.explosionRadius = explosionRadius;
        explosionEffect.explosionDamage = explosionDamage;

        return explosion;
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
                case ZombieState.AboutToExplode:
                    animator.SetBool("IsWalking", false);
                    break;

                case ZombieState.Chasing:
                    animator.SetBool("IsWalking", true);
                    break;
            }
        }

        if (spriteRenderer != null && currentState != ZombieState.AboutToExplode)
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
            spawner.OnZombieDespawned(gameObject, MultiZombieSpawner.ZombieType.Dynamite);
        }

        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        // Detection radius
        Gizmos.color = Color.red;
        DrawWireCircle(transform.position, detectionRadius, 32);

        // Explosion radius
        Gizmos.color = isAboutToExplode ? Color.red : Color.orange;
        DrawWireCircle(transform.position, explosionRadius, 32);

        // Avoidance radius
        Gizmos.color = isAvoidingObstacle ? Color.yellow : Color.cyan;
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
