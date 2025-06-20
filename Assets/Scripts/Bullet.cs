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
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Check if hit enemy
        if ((targetLayer.value & (1 << other.gameObject.layer)) > 0)
        {
            Debug.Log($"Bullet hit enemy: {other.name}");

            // Try to damage using HealthSystem first
            HealthSystem enemyHealth = other.GetComponent<HealthSystem>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(damage, "Bullet");
                Debug.Log($"Dealt {damage} damage to {other.name} using HealthSystem");
            }
            else
            {
                // Fallback: Destroy enemy without health system
                Debug.Log($"Enemy {other.name} has no HealthSystem, destroying directly");
                Destroy(other.gameObject);
            }

            DestroyBullet();
        }
        // Check if hit wall
        else if (other.gameObject.layer == 5) // Walls layer
        {
            Debug.Log("Bullet hit wall");
            DestroyBullet();
        }
    }

    void DestroyBullet()
    {
        // You can add bullet impact effects here
        Destroy(gameObject);
    }
}
