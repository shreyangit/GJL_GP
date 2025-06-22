using UnityEngine;

public class ZombieAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    public float attackDamage = 1f;
    public float attackCooldown = 1f;
    public float attackRange = 1.5f;
    public string zombieType = "Normal"; // "Normal", "Brightness", "Dynamite"

    [Header("Brightness Zombie Settings")]
    public float lightDecreaseAmount = 1f;

    [Header("Dynamite Zombie Settings")]
    public float explosionRadius = 5f;
    public float veryNearRadius = 1.5f; // -5 HP
    public float midNearRadius = 3f;    // -3 HP
    public float farRadius = 5f;        // -1 HP
    public int veryNearDamage = 5;
    public int midNearDamage = 3;
    public int farDamage = 1;

    [Header("Collision Detection")]
    public LayerMask playerLayerMask = 128; // ✅ Player layer 7 (2^7 = 128)
    public bool useExistingCollider = true; // ✅ Use existing collider instead of creating new one

    [Header("Debug Info")]
    [SerializeField] private float lastAttackTime = 0f;
    [SerializeField] private bool playerInRange = false;
    [SerializeField] private float currentDistanceToPlayer = 0f;

    private Transform player;
    private PlayerController playerController;
    private Collider2D attackCollider;

    void Start()
    {
        // Find player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerController = playerObj.GetComponent<PlayerController>();
            Debug.Log($"✅ {gameObject.name} found player: {playerObj.name}");
        }
        else
        {
            Debug.LogError($"❌ {gameObject.name} could not find player with 'Player' tag!");
            return;
        }

        // ✅ Setup attack collider - use existing or create new
        SetupAttackCollider();

        Debug.Log($"✅ {gameObject.name} ({zombieType} zombie) attack system initialized");
    }

    void SetupAttackCollider()
    {
        if (useExistingCollider)
        {
            // ✅ Use existing collider and modify it for attack detection
            attackCollider = GetComponent<Collider2D>();

            if (attackCollider != null)
            {
                attackCollider.isTrigger = true;

                // ✅ Adjust size based on collider type
                if (attackCollider is CapsuleCollider2D capsule)
                {
                    capsule.size = new Vector2(attackRange * 2f, attackRange * 2f);
                }
                else if (attackCollider is CircleCollider2D circle)
                {
                    circle.radius = attackRange;
                }

                Debug.Log($"✅ Using existing {attackCollider.GetType().Name} for attack detection");
            }
            else
            {
                CreateNewAttackCollider();
            }
        }
        else
        {
            CreateNewAttackCollider();
        }
    }

    void CreateNewAttackCollider()
    {
        // ✅ Create new attack collider
        GameObject attackTrigger = new GameObject("AttackTrigger");
        attackTrigger.transform.SetParent(transform);
        attackTrigger.transform.localPosition = Vector3.zero;

        CircleCollider2D newCollider = attackTrigger.AddComponent<CircleCollider2D>();
        newCollider.radius = attackRange;
        newCollider.isTrigger = true;

        // ✅ Add this script to the trigger object
        ZombieAttackTrigger trigger = attackTrigger.AddComponent<ZombieAttackTrigger>();
        trigger.parentAttack = this;

        attackCollider = newCollider;
        Debug.Log($"✅ Created new attack trigger for {gameObject.name}");
    }

    void Update()
    {
        // ✅ ENHANCED: Distance-based backup detection
        if (player != null)
        {
            currentDistanceToPlayer = Vector2.Distance(transform.position, player.position);

            // ✅ Backup detection: Check distance even if trigger fails
            if (currentDistanceToPlayer <= attackRange)
            {
                if (!playerInRange)
                {
                    Debug.Log($"🔍 {zombieType} zombie backup detection: Player in range ({currentDistanceToPlayer:F2})");
                    playerInRange = true;
                }
            }
            else if (currentDistanceToPlayer > attackRange * 1.2f) // Add buffer to prevent flickering
            {
                if (playerInRange)
                {
                    Debug.Log($"🔍 {zombieType} zombie backup detection: Player out of range ({currentDistanceToPlayer:F2})");
                    playerInRange = false;
                }
            }
        }

        // ✅ Attack logic
        if (playerInRange && Time.time >= lastAttackTime + attackCooldown)
        {
            AttackPlayer();
        }
    }

    // ✅ Make these public so external trigger can call them
    public void OnPlayerEnterRange(Collider2D playerCollider)
    {
        if (playerCollider.CompareTag("Player") || IsPlayerLayer(playerCollider.gameObject.layer))
        {
            playerInRange = true;
            Debug.Log($"✅ {zombieType} zombie detected player in attack range via trigger");
        }
    }

    public void OnPlayerExitRange(Collider2D playerCollider)
    {
        if (playerCollider.CompareTag("Player") || IsPlayerLayer(playerCollider.gameObject.layer))
        {
            playerInRange = false;
            Debug.Log($"✅ {zombieType} zombie lost player from attack range via trigger");
        }
    }

    // ✅ Built-in trigger detection (for when using existing collider)
    void OnTriggerEnter2D(Collider2D other)
    {
        OnPlayerEnterRange(other);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        OnPlayerExitRange(other);
    }

    // ✅ Helper method to check player layer
    bool IsPlayerLayer(int layer)
    {
        return (playerLayerMask.value & (1 << layer)) > 0;
    }

    void AttackPlayer()
    {
        if (playerController == null) return;

        lastAttackTime = Time.time;

        switch (zombieType.ToLower())
        {
            case "normal":
                AttackNormalZombie();
                break;
            case "brightness":
                AttackBrightnessZombie();
                break;
            case "dynamite":
                AttackDynamiteZombie();
                break;
            default:
                AttackNormalZombie();
                break;
        }
    }

    void AttackNormalZombie()
    {
        playerController.TakeDamage(attackDamage, $"{gameObject.name} (Normal Zombie)");
        Debug.Log($"🗡️ Normal zombie {gameObject.name} attacked player for {attackDamage} damage");
    }

    void AttackBrightnessZombie()
    {
        // Brightness zombies don't deal HP damage, they decrease light intensity
        if (playerController != null)
        {
            playerController.DecreaseLightIntensity(lightDecreaseAmount);
            Debug.Log($"💡 Brightness zombie {gameObject.name} decreased player's light intensity by {lightDecreaseAmount}");

            // ✅ Also show current light intensity
            if (playerController.playerLight != null)
            {
                Debug.Log($"💡 Player light intensity now: {playerController.playerLight.intensity}");
            }
        }
    }


    void AttackDynamiteZombie()
    {
        // Dynamite zombies explode and deal distance-based damage
        ExplodeDynamiteZombie();
    }

    public void ExplodeDynamiteZombie()
    {
        if (player == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        int explosionDamage = 0;
        string damageRange = "";

        // Determine damage based on distance
        if (distanceToPlayer <= veryNearRadius)
        {
            explosionDamage = veryNearDamage;
            damageRange = "Very Near";
        }
        else if (distanceToPlayer <= midNearRadius)
        {
            explosionDamage = midNearDamage;
            damageRange = "Mid Near";
        }
        else if (distanceToPlayer <= farRadius)
        {
            explosionDamage = farDamage;
            damageRange = "Far";
        }
        else
        {
            explosionDamage = 0;
            damageRange = "Out of Range";
        }

        // Deal damage if in range
        if (explosionDamage > 0)
        {
            playerController.TakeDamage(explosionDamage, $"{gameObject.name} (Dynamite Explosion - {damageRange})");
        }

        Debug.Log($"💥 Dynamite zombie {gameObject.name} exploded! Distance: {distanceToPlayer:F1}, Range: {damageRange}, Damage: {explosionDamage}");

        // Destroy the dynamite zombie after explosion
        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        // Draw attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Draw current distance to player
        if (player != null)
        {
            Gizmos.color = playerInRange ? Color.green : Color.yellow;
            Gizmos.DrawLine(transform.position, player.position);

            // Show distance text would be nice, but Gizmos doesn't support text
        }

        // Draw explosion ranges for dynamite zombies
        if (zombieType.ToLower() == "dynamite")
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, veryNearRadius);

            Gizmos.color = Color.orange;
            Gizmos.DrawWireSphere(transform.position, midNearRadius);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, farRadius);
        }
    }
}
