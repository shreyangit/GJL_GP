using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BurstNotificationUI : MonoBehaviour
{
    public static BurstNotificationUI Instance;

    [Header("UI References")]
    public Text warningText;
    public Transform warningParent;

    [Header("Warning Settings")]
    public float warningDisplayTime = 3f;
    public float warningBeforeStartTime = 5f;  // Show "SURGE INCOMING" 5 seconds before burst
    public float fadeInTime = 0.5f;
    public float fadeOutTime = 0.8f;

    [Header("Warning Messages")]
    [TextArea(2, 4)]
    public string surgeIncomingMessage = "⚠️ SURGE INCOMING ⚠️";
    [TextArea(2, 4)]
    public string surgeActiveMessage = "🚨 ZOMBIE SURGE ACTIVE! 🚨";
    [TextArea(2, 4)]
    public string surgeEndMessage = "✅ SURGE ENDED";

    [Header("Visual Effects")]
    public Color warningColor = Color.yellow;
    public Color activeColor = Color.red;
    public Color endColor = Color.green;
    public float pulseSpeed = 3f;
    public float pulseMagnitude = 0.3f;

    [Header("Audio")]
    public AudioClip warningSound;
    public AudioClip activeSound;
    public AudioClip endSound;

    private AudioSource audioSource;
    private bool isDisplaying = false;
    private Vector3 originalScale;

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        SetupUI();
        SetupAudio();
    }

    void SetupAudio()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.volume = 0.7f;
        }
    }

    void SetupUI()
    {
        // Create UI if it doesn't exist
        if (warningText == null)
        {
            // Find Canvas or create one
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("BurstNotification Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 1000; // Very high priority

                CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);

                GraphicRaycaster raycaster = canvasObj.AddComponent<GraphicRaycaster>();
            }

            // Create warning text
            GameObject textObj = new GameObject("Burst Warning Text");
            textObj.transform.SetParent(canvas.transform, false);

            warningText = textObj.AddComponent<Text>();
            warningText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            warningText.fontSize = 48;
            warningText.fontStyle = FontStyle.Bold;
            warningText.alignment = TextAnchor.MiddleCenter;
            warningText.color = warningColor;

            // Add outline for better readability
            Outline outline = textObj.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(3, 3);

            // Add shadow for extra depth
            Shadow shadow = textObj.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.8f);
            shadow.effectDistance = new Vector2(5, -5);

            // Position at top center
            RectTransform rectTransform = textObj.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0f, 0.85f);  // Top area
            rectTransform.anchorMax = new Vector2(1f, 0.95f);  // Full width
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = Vector2.zero; // Use anchor size

            warningParent = textObj.transform;
            originalScale = warningParent.localScale;
        }

        // Hide text initially
        if (warningText != null)
        {
            warningText.color = new Color(warningText.color.r, warningText.color.g, warningText.color.b, 0f);
        }

        Debug.Log($"✅ BurstNotificationUI setup complete");
    }

    // 🚨 PUBLIC METHODS called by MultiZombieSpawner

    public void ShowSurgeIncoming(float timeUntilBurst)
    {
        if (isDisplaying) return;

        Debug.Log($"⚠️ Showing SURGE INCOMING warning. Burst in {timeUntilBurst}s");
        StartCoroutine(ShowWarningCoroutine(surgeIncomingMessage, warningColor, warningSound, warningDisplayTime));
    }

    public void ShowSurgeActive(float duration)
    {
        Debug.Log($"🚨 Showing SURGE ACTIVE notification. Duration: {duration}s");
        StopAllCoroutines(); // Stop any ongoing warnings
        StartCoroutine(ShowWarningCoroutine(surgeActiveMessage, activeColor, activeSound, duration));
    }

    public void ShowSurgeEnded()
    {
        Debug.Log($"✅ Showing SURGE ENDED notification");
        StopAllCoroutines(); // Stop any ongoing notifications
        StartCoroutine(ShowWarningCoroutine(surgeEndMessage, endColor, endSound, 2f));
    }

    IEnumerator ShowWarningCoroutine(string message, Color color, AudioClip sound, float displayTime)
    {
        if (warningText == null) yield break;

        isDisplaying = true;

        // Set message and color
        warningText.text = message;

        // Play sound effect
        if (sound != null && audioSource != null)
        {
            audioSource.PlayOneShot(sound);
        }

        // Fade in with scale effect
        float elapsed = 0f;
        Vector3 startScale = originalScale * 0.5f; // Start smaller
        Vector3 targetScale = originalScale;

        while (elapsed < fadeInTime)
        {
            float progress = elapsed / fadeInTime;
            float alpha = Mathf.Lerp(0f, 1f, progress);
            Vector3 currentScale = Vector3.Lerp(startScale, targetScale, progress);

            warningText.color = new Color(color.r, color.g, color.b, alpha);
            warningParent.localScale = currentScale;

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Full visibility
        warningText.color = new Color(color.r, color.g, color.b, 1f);
        warningParent.localScale = targetScale;

        // Display with pulsing effect
        float displayElapsed = 0f;
        float displayDuration = displayTime - fadeInTime - fadeOutTime;

        while (displayElapsed < displayDuration)
        {
            // Pulsing scale effect
            float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseMagnitude;
            warningParent.localScale = originalScale * pulse;

            // Pulsing color intensity
            float colorPulse = 0.8f + Mathf.Sin(Time.time * pulseSpeed * 1.5f) * 0.2f;
            warningText.color = new Color(color.r * colorPulse, color.g * colorPulse, color.b * colorPulse, 1f);

            displayElapsed += Time.deltaTime;
            yield return null;
        }

        // Fade out
        elapsed = 0f;
        Vector3 endScale = originalScale * 1.2f; // End slightly larger

        while (elapsed < fadeOutTime)
        {
            float progress = elapsed / fadeOutTime;
            float alpha = Mathf.Lerp(1f, 0f, progress);
            Vector3 currentScale = Vector3.Lerp(originalScale, endScale, progress);

            warningText.color = new Color(color.r, color.g, color.b, alpha);
            warningParent.localScale = currentScale;

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Reset
        warningParent.localScale = originalScale;
        warningText.color = new Color(color.r, color.g, color.b, 0f);

        isDisplaying = false;
    }

    // 🎮 Additional utility methods

    public void ShowCustomMessage(string message, Color color, float duration = 3f)
    {
        if (!isDisplaying)
        {
            StartCoroutine(ShowWarningCoroutine(message, color, null, duration));
        }
    }

    public void ForceHide()
    {
        StopAllCoroutines();
        if (warningText != null)
        {
            warningText.color = new Color(warningText.color.r, warningText.color.g, warningText.color.b, 0f);
        }
        isDisplaying = false;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
