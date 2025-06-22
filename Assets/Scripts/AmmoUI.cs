using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AmmoUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI ammoText;
    public TextMeshProUGUI clipText;
    public TextMeshProUGUI totalAmmoText;
    public Image ammoBar;
    public GameObject[] clipIndicators = new GameObject[5]; // Visual clips

    [Header("Settings")]
    public Color fullClipColor = Color.green;
    public Color partialClipColor = Color.yellow;
    public Color emptyClipColor = Color.red;

    private PlayerController player;
    private Canvas canvas;

    void Start()
    {
        // Find player
        player = FindFirstObjectByType<PlayerController>();
        if (player == null)
        {
            Debug.LogError("AmmoUI: Could not find PlayerController!");
            return;
        }

        // Create UI if not assigned
        if (ammoText == null)
        {
            CreateAmmoUI();
        }
    }

    void Update()
    {
        if (player == null) return;

        UpdateAmmoDisplay();
    }

    void CreateAmmoUI()
    {
        // Create Canvas if not found
        canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("AmmoCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Create Ammo Panel
        GameObject ammoPanel = new GameObject("AmmoPanel");
        ammoPanel.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = ammoPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 1f); // Top-right corner
        panelRect.anchorMax = new Vector2(1f, 1f);
        panelRect.pivot = new Vector2(1f, 1f);
        panelRect.anchoredPosition = new Vector2(-20f, -20f); // Padding
        panelRect.sizeDelta = new Vector2(200f, 90f);

        // Background Panel
        Image panelBg = ammoPanel.AddComponent<Image>();
        panelBg.color = new Color(0f, 0f, 0f, 0.6f); // Semi-transparent black

        // === Ammo Text (Big) ===
        GameObject ammoTextObj = new GameObject("AmmoText");
        ammoTextObj.transform.SetParent(ammoPanel.transform, false);
        ammoText = ammoTextObj.AddComponent<TextMeshProUGUI>();
        ammoText.text = "10 / 10";
        ammoText.fontSize = 28;
        ammoText.color = Color.white;
        ammoText.alignment = TextAlignmentOptions.Center;

        RectTransform ammoTextRect = ammoText.GetComponent<RectTransform>();
        ammoTextRect.anchorMin = new Vector2(0f, 0.66f);
        ammoTextRect.anchorMax = new Vector2(1f, 1f);
        ammoTextRect.offsetMin = new Vector2(10f, 0f);
        ammoTextRect.offsetMax = new Vector2(-10f, -5f);

        // === Clip Info Text ===
        GameObject clipTextObj = new GameObject("ClipText");
        clipTextObj.transform.SetParent(ammoPanel.transform, false);
        clipText = clipTextObj.AddComponent<TextMeshProUGUI>();
        clipText.text = "Clip 1 / 5";
        clipText.fontSize = 18;
        clipText.color = Color.cyan;
        clipText.alignment = TextAlignmentOptions.Center;

        RectTransform clipTextRect = clipText.GetComponent<RectTransform>();
        clipTextRect.anchorMin = new Vector2(0f, 0.33f);
        clipTextRect.anchorMax = new Vector2(1f, 0.66f);
        clipTextRect.offsetMin = new Vector2(10f, 0f);
        clipTextRect.offsetMax = new Vector2(-10f, -5f);

        // === Total Ammo Text ===
        GameObject totalTextObj = new GameObject("TotalAmmoText");
        totalTextObj.transform.SetParent(ammoPanel.transform, false);
        totalAmmoText = totalTextObj.AddComponent<TextMeshProUGUI>();
        totalAmmoText.text = "Total: 50";
        totalAmmoText.fontSize = 16;
        totalAmmoText.color = Color.gray;
        totalAmmoText.alignment = TextAlignmentOptions.Center;

        RectTransform totalTextRect = totalAmmoText.GetComponent<RectTransform>();
        totalTextRect.anchorMin = new Vector2(0f, 0f);
        totalTextRect.anchorMax = new Vector2(1f, 0.33f);
        totalTextRect.offsetMin = new Vector2(10f, 0f);
        totalTextRect.offsetMax = new Vector2(-10f, -5f);

        Debug.Log("✅ Clean Ammo UI created successfully!");
    }


    void UpdateAmmoDisplay()
    {
        // Update ammo text
        if (ammoText != null)
        {
            ammoText.text = $"{player.GetCurrentAmmo()}/{player.bulletsPerClip}";

            // Color code based on ammo level
            float ammoPercentage = (float)player.GetCurrentAmmo() / player.bulletsPerClip;
            if (ammoPercentage > 0.5f)
                ammoText.color = Color.green;
            else if (ammoPercentage > 0.2f)
                ammoText.color = Color.yellow;
            else
                ammoText.color = Color.red;
        }

        // Update clip text
        if (clipText != null)
        {
            clipText.text = $"Clip {player.GetCurrentClip()}/{player.maxClips}";
        }

        // Update total ammo text
        if (totalAmmoText != null)
        {
            totalAmmoText.text = $"Total: {player.GetTotalAmmo()}";
        }
    }
}
