using UnityEngine;
using System.Collections;

public class CoinCollectible : MonoBehaviour
{
    [Header("Coin Settings")]
    public float brightnessIncrease = 1f;
    public AudioClip collectSound;
    public GameObject collectEffect;

    [Header("Animation")]
    public float floatSpeed = 1f;
    public float floatHeight = 0.5f;
    public float rotationSpeed = 90f;

    private Vector3 startPosition;
    private AudioSource audioSource;
    private bool isCollected = false;

    void Start()
    {
        startPosition = transform.position;

        // Setup audio
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        // Ensure trigger collider
        EnsureTriggerCollider();

        Debug.Log($"✅ Coin collectible {gameObject.name} initialized");
    }

    void Update()
    {
        if (!isCollected)
        {
            // Floating animation
            float newY = startPosition.y + Mathf.Sin(Time.time * floatSpeed) * floatHeight;
            transform.position = new Vector3(startPosition.x, newY, startPosition.z);

            // Rotation animation
            transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
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
        if (other.CompareTag("Player") && !isCollected)
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                CollectCoin(player);
            }
        }
    }

    void CollectCoin(PlayerController player)
    {
        if (isCollected) return;

        isCollected = true;

        Debug.Log($"🪙 Player collected coin: {gameObject.name}");

        // Increase player brightness
        player.IncreaseLightIntensity(brightnessIncrease);

        // Play collect sound
        if (collectSound != null)
        {
            AudioSource.PlayClipAtPoint(collectSound, transform.position);
        }

        // Show UI feedback
        CollectibleUIManager.Instance?.ShowCollectMessage("+1 Brightness", CollectibleUIManager.MessageType.Brightness);

        // Spawn collect effect
        if (collectEffect != null)
        {
            GameObject effect = Instantiate(collectEffect, transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }

        // Animate collection and destroy
        StartCoroutine(CollectAnimation());
    }

    IEnumerator CollectAnimation()
    {
        Vector3 originalScale = transform.localScale;
        float animTime = 0.3f;
        float elapsed = 0f;

        while (elapsed < animTime)
        {
            float progress = elapsed / animTime;
            float scale = Mathf.Lerp(1f, 1.5f, progress);
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
