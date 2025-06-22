using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CollectibleUIManager : MonoBehaviour
{
    public static CollectibleUIManager Instance;

    [Header("UI References")]
    public Text feedbackText;
    public Transform feedbackParent;

    [Header("Message Settings")]
    public float messageDuration = 2f;
    public float fadeInTime = 0.3f;
    public float fadeOutTime = 0.7f;
    public float moveDistance = 50f;

    [Header("Message Colors")]
    public Color healthColor = Color.green;
    public Color brightnessColor = Color.yellow;

    public enum MessageType { Health, Brightness }

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
    }

    void SetupUI()
    {
        // Create UI if it doesn't exist
        if (feedbackText == null)
        {
            // Find Canvas or create one
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("CollectibleUI Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;

                CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
            }

            // Create feedback text
            GameObject textObj = new GameObject("Collectible Feedback Text");
            textObj.transform.SetParent(canvas.transform, false);

            feedbackText = textObj.AddComponent<Text>();
            feedbackText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            feedbackText.fontSize = 36;
            feedbackText.alignment = TextAnchor.MiddleCenter;
            feedbackText.color = Color.white;

            // Position at top center
            RectTransform rectTransform = textObj.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.8f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.8f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(400, 100);

            feedbackParent = textObj.transform;
        }

        // Hide text initially
        if (feedbackText != null)
        {
            feedbackText.color = new Color(feedbackText.color.r, feedbackText.color.g, feedbackText.color.b, 0f);
        }
    }

    public void ShowCollectMessage(string message, MessageType messageType)
    {
        if (feedbackText == null) return;

        StopAllCoroutines();
        StartCoroutine(ShowMessageCoroutine(message, messageType));
    }

    IEnumerator ShowMessageCoroutine(string message, MessageType messageType)
    {
        // Set message and color
        feedbackText.text = message;
        Color targetColor = messageType == MessageType.Health ? healthColor : brightnessColor;

        // Fade in
        float elapsed = 0f;
        Vector3 startPos = feedbackParent.position;

        while (elapsed < fadeInTime)
        {
            float progress = elapsed / fadeInTime;
            float alpha = Mathf.Lerp(0f, 1f, progress);

            feedbackText.color = new Color(targetColor.r, targetColor.g, targetColor.b, alpha);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Full visibility
        feedbackText.color = new Color(targetColor.r, targetColor.g, targetColor.b, 1f);

        // Wait
        yield return new WaitForSeconds(messageDuration - fadeInTime - fadeOutTime);

        // Fade out and move up
        elapsed = 0f;
        Vector3 endPos = startPos + Vector3.up * moveDistance;

        while (elapsed < fadeOutTime)
        {
            float progress = elapsed / fadeOutTime;
            float alpha = Mathf.Lerp(1f, 0f, progress);

            feedbackText.color = new Color(targetColor.r, targetColor.g, targetColor.b, alpha);
            feedbackParent.position = Vector3.Lerp(startPos, endPos, progress);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Reset position
        feedbackParent.position = startPos;
        feedbackText.color = new Color(targetColor.r, targetColor.g, targetColor.b, 0f);
    }
}
