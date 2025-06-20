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

    [Header("Debug Info")]
    [SerializeField] private float lastAttackTime = 0f;
    [SerializeField] private bool playerInRange = false;

    private Transform player;
    private PlayerController playerController;
    private CircleCollider2D attackCollider;

    void Start()
    {
        // Find player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerController = playerObj.GetComponent<PlayerController>();
        }

        // Create attack range collider
        attackCollider = gameObject.AddComponent<CircleCollider2D>();
        attackCollider.radius = attackRange;
        attackCollider.isTrigger = true;

        Debug.Log($"{gameObject.name} ({zombieType} zombie) attack system initialized");
    }

    void Update()
    {
        // Try to attack if player is in range and cooldown is over
        if (playerInRange && Time.time >= lastAttackTime + attackCooldown)
        {
            AttackPlayer();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            Debug.Log($"{zombieType} zombie detected player in attack range");
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            Debug.Log($"{zombieType} zombie lost player from attack range");
        }
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
        Debug.Log($"Normal zombie {gameObject.name} attacked player for {attackDamage} damage");
    }

    void AttackBrightnessZombie()
    {
        // Brightness zombies don't deal HP damage, they decrease light intensity
        playerController.DecreaseLightIntensity(lightDecreaseAmount);
        Debug.Log($"Brightness zombie {gameObject.name} decreased player's light intensity by {lightDecreaseAmount}");
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

        Debug.Log($"Dynamite zombie {gameObject.name} exploded! Distance: {distanceToPlayer:F1}, Range: {damageRange}, Damage: {explosionDamage}");

        // Destroy the dynamite zombie after explosion
        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        // Draw attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

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
