using UnityEngine;

public class ExplosionEffect : MonoBehaviour
{
    [Header("Explosion Settings")]
    public Sprite[] explosionSprites;
    public float explosionRadius = 5f;
    public float explosionDamage = 50f;
    public float animationSpeed = 10f;
    public float effectDuration = 1f;

    private SpriteRenderer spriteRenderer;
    private int currentSpriteIndex = 0;
    private float timer = 0f;
    private float spriteTimer = 0f;
    private bool hasDealtDamage = false;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Deal explosion damage immediately
        if (!hasDealtDamage)
        {
            DealExplosionDamage();
            hasDealtDamage = true;
        }

        // Start animation
        if (explosionSprites != null && explosionSprites.Length > 0)
        {
            spriteRenderer.sprite = explosionSprites[0];
        }

        // Destroy after duration
        Destroy(gameObject, effectDuration);
    }

    void Update()
    {
        timer += Time.deltaTime;
        spriteTimer += Time.deltaTime;

        // Animate explosion sprites
        if (explosionSprites != null && explosionSprites.Length > 0)
        {
            if (spriteTimer >= 1f / animationSpeed)
            {
                currentSpriteIndex = (currentSpriteIndex + 1) % explosionSprites.Length;
                spriteRenderer.sprite = explosionSprites[currentSpriteIndex];
                spriteTimer = 0f;
            }
        }
    }

    void DealExplosionDamage()
    {
        // Find player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        PlayerController playerController = player.GetComponent<PlayerController>();
        if (playerController == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.transform.position);
        int damage = 0;
        string range = "";

        // Determine damage based on distance (same logic as ZombieAttack)
        if (distanceToPlayer <= 1.5f)
        {
            damage = 5;
            range = "Very Near";
        }
        else if (distanceToPlayer <= 3f)
        {
            damage = 3;
            range = "Mid Near";
        }
        else if (distanceToPlayer <= 5f)
        {
            damage = 1;
            range = "Far";
        }

        if (damage > 0)
        {
            playerController.TakeDamage(damage, $"Dynamite Explosion ({range})");
            Debug.Log($"Explosion dealt {damage} damage to player at {range} range (distance: {distanceToPlayer:F1})");
        }
        else
        {
            Debug.Log($"Player out of explosion range (distance: {distanceToPlayer:F1})");
        }
    }
}
