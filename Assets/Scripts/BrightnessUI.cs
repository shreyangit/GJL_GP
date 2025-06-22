using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BrightnessUI : MonoBehaviour
{
    [Header("Player Reference")]
    public PlayerController playerController;

    [Header("UI References")]
    private Canvas canvas;
    private GameObject brightnessPanel;
    private TextMeshProUGUI brightnessText;

    void Start()
    {
        if (playerController == null)
        {
            playerController = FindObjectOfType<PlayerController>();
        }

        CreateBrightnessUI();
        UpdateBrightnessDisplay();
    }

    void Update()
    {
        UpdateBrightnessDisplay();
    }

    void CreateBrightnessUI()
    {
        // Create Canvas if not found
        canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("BrightnessCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Create panel in bottom-right
        brightnessPanel = new GameObject("BrightnessPanel");
        brightnessPanel.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = brightnessPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 0f); // Bottom-right
        panelRect.anchorMax = new Vector2(1f, 0f);
        panelRect.pivot = new Vector2(1f, 0f);
        panelRect.anchoredPosition = new Vector2(-20f, 20f);
        panelRect.sizeDelta = new Vector2(180f, 40f);

        // Panel background
        Image panelBg = brightnessPanel.AddComponent<Image>();
        panelBg.color = new Color(0f, 0f, 0f, 0.6f);

        // Create text object
        GameObject textObj = new GameObject("BrightnessText");
        textObj.transform.SetParent(brightnessPanel.transform, false);

        brightnessText = textObj.AddComponent<TextMeshProUGUI>();
        brightnessText.text = "Light: 3.0 / 3.0";
        brightnessText.fontSize = 18;
        brightnessText.color = Color.yellow;
        brightnessText.alignment = TextAlignmentOptions.Center;

        RectTransform textRect = brightnessText.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10f, 5f);
        textRect.offsetMax = new Vector2(-10f, -5f);

        Debug.Log("✅ Brightness UI created!");
    }

    void UpdateBrightnessDisplay()
    {
        if (playerController != null && brightnessText != null)
        {
            float current = playerController.playerLight.intensity;
            float max = playerController.initialLightIntensity;

            brightnessText.text = $"Light: {current:F1} / {max:F1}";
        }
    }
}
