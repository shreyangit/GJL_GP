using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Bullet Settings")]
    public float damage = 10f;
    public float lifetime = 5f;
    public float speed = 10f;

    [Header("Debug Info")]
    [SerializeField] private Vector2 direction;
    [SerializeField] private LayerMask targetLayer;

    private Rigidbody2D rb2d;

    void Start()
    {
        rb2d = GetComponent<Rigidbody2D>();

        // Destroy bullet after lifetime
        Destroy(gameObject, lifetime);
    }

    public void Initialize(Vector2 shootDirection, float bulletSpeed, LayerMask enemyLayer)
    {
        direction = shootDirection.normalized;
        speed = bulletSpeed;
        targetLayer = enemyLayer;

        // Set velocity
        if (rb2d == null)
            rb2d = GetComponent<Rigidbody2D>();

        if (rb2d != null)
        {
            rb2d.linearVelocity = direction * speed;
        }

        // Rotate bullet to face movement direction
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        Debug.Log($"Bullet initialized: Direction={direction}, Speed={speed}, TargetLayer={targetLayer.value}");
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"🔥 BULLET COLLISION: {other.name} (layer {other.gameObject.layer}={LayerMask.LayerToName(other.gameObject.layer)}) - My layer: {gameObject.layer}");
        Debug.Log($"🔥 Target layer mask: {targetLayer.value} - Layer check: {(targetLayer.value & (1 << other.gameObject.layer)) > 0}");

        // ✅ ENHANCED COMPONENT CHECK
        HealthSystem healthCheck = other.GetComponent<HealthSystem>();
        Debug.Log($"🔍 Target has HealthSystem: {healthCheck != null}");

        // Skip self-collision (bullets hitting bullets)
        if (other.gameObject.layer == 8) // Bullets layer
        {
            Debug.Log("⚠️ Bullet hit another bullet - ignoring");
            return;
        }

        // Check if hit enemy (layer 6 - Zombies)
        if ((targetLayer.value & (1 << other.gameObject.layer)) > 0)
        {
            Debug.Log($"✅ Bullet hit enemy: {other.name} on layer {other.gameObject.layer}");

            HealthSystem enemyHealth = other.GetComponent<HealthSystem>();
            if (enemyHealth != null)
            {
                bool damaged = enemyHealth.TakeDamage(damage, "Bullet");
                Debug.Log($"✅ Dealt {damage} damage to {other.name}. Success: {damaged}. Health: {enemyHealth.CurrentHealth}/{enemyHealth.MaxHealth}");
            }
            else
            {
                Debug.LogError($"❌ Enemy {other.name} has no HealthSystem component!");
            }

            DestroyBullet();
        }
        // Check if hit wall (layer 5 - Walls)
        else if (other.gameObject.layer == 5)
        {
            Debug.Log($"🧱 Bullet hit wall: {other.name}");
            DestroyBullet();
        }
        else
        {
            Debug.Log($"❓ Bullet hit unknown object: {other.name} on layer {other.gameObject.layer}");
        }
    }




    void DestroyBullet()
    {
        Debug.Log("💥 Bullet destroyed");
        Destroy(gameObject);
    }
}
