using UnityEngine;
using System.Collections;

public class HeartCollectible : MonoBehaviour
{
    [Header("Heart Settings")]
    public float healthIncrease = 1f;
    public AudioClip collectSound;
    public GameObject collectEffect;

    [Header("Animation")]
    public float pulseSpeed = 2f;
    public float pulseScale = 0.2f;
    public float floatSpeed = 0.8f;
    public float floatHeight = 0.3f;

    private Vector3 startPosition;
    private Vector3 originalScale;
    private bool isCollected = false;

    void Start()
    {
        startPosition = transform.position;
        originalScale = transform.localScale;

        // Ensure trigger collider
        EnsureTriggerCollider();
    }

    void Update()
    {
        if (!isCollected)
        {
            // Floating animation
            float newY = startPosition.y + Mathf.Sin(Time.time * floatSpeed) * floatHeight;
            transform.position = new Vector3(startPosition.x, newY, startPosition.z);

            // Pulsing animation (heartbeat effect)
            float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseScale;
            transform.localScale = originalScale * pulse;
        }
    }

    void EnsureTriggerCollider()
    {
        Collider2D collider = GetComponent<Collider2D>();
        if (collider == null)
        {
            CircleCollider2D circleCollider = gameObject.AddComponent<CircleCollider2D>();
            circleCollider.isTrigger = true;
            circleCollider.radius = 0.5f;
        }
        else
        {
            collider.isTrigger = true;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isCollected) return;

        if (other.CompareTag("Player"))
        {
            HealthSystem health = other.GetComponent<HealthSystem>();
            if (health != null)
            {
                CollectHeart(health);
            }
        }
    }

    void CollectHeart(HealthSystem health)
    {
        isCollected = true;

        bool healed = health.Heal(healthIncrease);
        if (!healed)
        {
            Debug.Log("⚠️ Heart collected but healing failed (maybe full HP)");
        }

        // Play collect sound
        if (collectSound != null)
        {
            AudioSource.PlayClipAtPoint(collectSound, transform.position);
        }

        // UI feedback
        CollectibleUIManager.Instance?.ShowCollectMessage("+1 HP", CollectibleUIManager.MessageType.Health);

        // Collect effect
        if (collectEffect != null)
        {
            GameObject effect = Instantiate(collectEffect, transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }

        StartCoroutine(CollectAnimation());
    }

    IEnumerator CollectAnimation()
    {
        float animTime = 0.4f;
        float elapsed = 0f;

        while (elapsed < animTime)
        {
            float progress = elapsed / animTime;
            float scale = Mathf.Lerp(1f, 2f, progress);
            float alpha = Mathf.Lerp(1f, 0f, progress);

            transform.localScale = originalScale * scale;

            // Fade out sprite
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                Color color = sr.color;
                color.a = alpha;
                sr.color = color;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(gameObject);
    }
}
