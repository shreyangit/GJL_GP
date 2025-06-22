using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class PoisonMessageUI : MonoBehaviour
{
    public static PoisonMessageUI Instance; // Singleton access

    [Header("UI Settings")]
    public Canvas canvas;
    public TextMeshProUGUI messageText;
    public float messageDuration = 2.5f;
    public Color poisonColor = new Color(0f, 0.4f, 0f); // Dark green

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            CreateUI();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void CreateUI()
    {
        // Create canvas if not assigned
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("PoisonMessageCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;

            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Create text object
        GameObject msgObj = new GameObject("PoisonMessageText");
        msgObj.transform.SetParent(canvas.transform, false);

        messageText = msgObj.AddComponent<TextMeshProUGUI>();
        messageText.fontSize = 28;
        messageText.alignment = TextAlignmentOptions.Center;
        messageText.color = poisonColor;
        messageText.text = "";
        messageText.enableWordWrapping = false;

        // Position it at the top center
        RectTransform rect = messageText.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -30f);
        rect.sizeDelta = new Vector2(600f, 60f);
    }

    public void ShowPoisonMessage()
    {
        if (messageText != null)
            StartCoroutine(DisplayPoisonMessage());
    }

    IEnumerator DisplayPoisonMessage()
    {
        messageText.text = "You were poisoned!";
        messageText.alpha = 1f;

        float t = 0f;
        float fadeStart = messageDuration * 0.6f;

        while (t < messageDuration)
        {
            t += Time.deltaTime;

            if (t > fadeStart)
            {
                float fadeAmount = 1f - ((t - fadeStart) / (messageDuration - fadeStart));
                messageText.alpha = fadeAmount;
            }

            yield return null;
        }

        messageText.text = "";
        messageText.alpha = 1f;
    }
}
