using UnityEngine;

public class ExplosionEffect : MonoBehaviour
{
    [Header("Explosion Settings")]
    public float explosionDuration = 1f;
    public float explosionRadius = 3f;
    public float explosionDamage = 50f;

    [Header("Sprite Animation")]
    public Sprite[] explosionSprites;
    public float frameRate = 12f;

    private SpriteRenderer spriteRenderer;
    private int currentFrame = 0;
    private float frameTimer = 0f;
    private float explosionTimer = 0f;
    private bool hasExploded = false;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        // If no sprites assigned, try to load them from the explosion folder
        if (explosionSprites == null || explosionSprites.Length == 0)
        {
            LoadExplosionSprites();
        }

        // Start explosion animation
        if (explosionSprites.Length > 0)
        {
            spriteRenderer.sprite = explosionSprites[0];
        }
    }

    void Update()
    {
        explosionTimer += Time.deltaTime;

        // Animate explosion sprites
        AnimateExplosion();

        // Check if explosion is complete
        if (explosionTimer >= explosionDuration)
        {
            Destroy(gameObject);
        }
    }

    void AnimateExplosion()
    {
        if (explosionSprites.Length == 0) return;

        frameTimer += Time.deltaTime;

        if (frameTimer >= 1f / frameRate)
        {
            frameTimer = 0f;
            currentFrame++;

            if (currentFrame < explosionSprites.Length)
            {
                spriteRenderer.sprite = explosionSprites[currentFrame];
            }
            else
            {
                // Animation complete, trigger explosion damage if not done
                if (!hasExploded)
                {
                    TriggerExplosionDamage();
                    hasExploded = true;
                }
            }
        }
    }

    void LoadExplosionSprites()
    {
        // Try to load explosion sprites from Resources or manually assign them
        Sprite[] loadedSprites = Resources.LoadAll<Sprite>("Effects/Explosion_Dynamite");
        if (loadedSprites.Length > 0)
        {
            explosionSprites = loadedSprites;
            Debug.Log($"Loaded {explosionSprites.Length} explosion sprites");
        }
        else
        {
            Debug.LogWarning("Could not load explosion sprites. Please assign them manually.");
        }
    }

    void TriggerExplosionDamage()
    {
        Debug.Log($"Explosion damage triggered at {transform.position} with radius {explosionRadius}");

        // Here you would implement damage to player/other objects
        // For now, just log the explosion

        // Find all colliders in explosion radius
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, explosionRadius);

        foreach (Collider2D hit in hitColliders)
        {
            if (hit.CompareTag("Player"))
            {
                Debug.Log($"Player caught in explosion! Damage: {explosionDamage}");
                // Apply damage to player here
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        // Draw explosion radius
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
