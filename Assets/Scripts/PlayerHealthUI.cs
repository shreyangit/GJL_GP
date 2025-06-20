using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHealthUI : MonoBehaviour
{
    [Header("UI References")]
    public Slider healthBar;
    public TextMeshProUGUI healthText;
    public Image healthBarFill;
    public GameObject healthPanel;

    [Header("Health Bar Colors")]
    public Color fullHealthColor = Color.green;
    public Color midHealthColor = Color.yellow;
    public Color lowHealthColor = Color.red;
    public Color criticalHealthColor = new Color(1f, 0.2f, 0.2f); // Bright red

    [Header("Animation Settings")]
    public bool animateHealthBar = true;
    public float animationSpeed = 5f;

    private HealthSystem playerHealth;
    private Canvas canvas;
    private float targetHealthPercentage = 1f;
    private float currentDisplayedHealth = 1f;

    void Start()
    {
        // Find player health system
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerHealth = player.GetComponent<HealthSystem>();
            if (playerHealth == null)
            {
                Debug.LogError("PlayerHealthUI: Player doesn't have HealthSystem component!");
                return;
            }
        }
        else
        {
            Debug.LogError("PlayerHealthUI: Could not find player with 'Player' tag!");
            return;
        }

        // Create UI if not assigned
        if (healthPanel == null)
        {
            CreateHealthUI();
        }

        // Subscribe to health events
        playerHealth.OnHealthPercentChanged.AddListener(UpdateHealthBar);
        playerHealth.OnDeath.AddListener(OnPlayerDeath);

        // Initialize display
        UpdateHealthBar(playerHealth.HealthPercentage);
    }

    void Update()
    {
        if (animateHealthBar && Mathf.Abs(currentDisplayedHealth - targetHealthPercentage) > 0.01f)
        {
            // Smoothly animate health bar
            currentDisplayedHealth = Mathf.Lerp(currentDisplayedHealth, targetHealthPercentage, animationSpeed * Time.deltaTime);

            if (healthBar != null)
            {
                healthBar.value = currentDisplayedHealth;
            }

            UpdateHealthBarColor(currentDisplayedHealth);
        }
    }

    void CreateHealthUI()
    {
        // Create Canvas if it doesn't exist
        canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("HealthCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Create health panel in bottom-right
        healthPanel = new GameObject("HealthPanel");
        healthPanel.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = healthPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 0f); // Bottom-right anchor
        panelRect.anchorMax = new Vector2(1f, 0f);
        panelRect.pivot = new Vector2(1f, 0f);
        panelRect.anchoredPosition = new Vector2(-20f, 20f); // 20px from edges
        panelRect.sizeDelta = new Vector2(200f, 80f);

        // Add background
        Image panelBg = healthPanel.AddComponent<Image>();
        panelBg.color = new Color(0f, 0f, 0f, 0.8f); // Semi-transparent black

        // Create health text
        GameObject healthTextObj = new GameObject("HealthText");
        healthTextObj.transform.SetParent(healthPanel.transform, false);

        healthText = healthTextObj.AddComponent<TextMeshProUGUI>();
        healthText.text = "Health: 20/20";
        healthText.fontSize = 16;
        healthText.color = Color.white;
        healthText.alignment = TextAlignmentOptions.Center;

        RectTransform healthTextRect = healthText.GetComponent<RectTransform>();
        healthTextRect.anchorMin = Vector2.zero;
        healthTextRect.anchorMax = Vector2.one;
        healthTextRect.offsetMin = new Vector2(10f, 45f);
        healthTextRect.offsetMax = new Vector2(-10f, -5f);

        // Create health bar background
        GameObject healthBarBg = new GameObject("HealthBarBackground");
        healthBarBg.transform.SetParent(healthPanel.transform, false);

        Image barBg = healthBarBg.AddComponent<Image>();
        barBg.color = new Color(0.2f, 0.2f, 0.2f, 1f); // Dark gray

        RectTransform barBgRect = healthBarBg.GetComponent<RectTransform>();
        barBgRect.anchorMin = Vector2.zero;
        barBgRect.anchorMax = Vector2.one;
        barBgRect.offsetMin = new Vector2(15f, 15f);
        barBgRect.offsetMax = new Vector2(-15f, 45f);

        // Create health bar slider
        GameObject healthBarObj = new GameObject("HealthBar");
        healthBarObj.transform.SetParent(healthBarBg.transform, false);

        healthBar = healthBarObj.AddComponent<Slider>();
        healthBar.minValue = 0f;
        healthBar.maxValue = 1f;
        healthBar.value = 1f;

        RectTransform sliderRect = healthBar.GetComponent<RectTransform>();
        sliderRect.anchorMin = Vector2.zero;
        sliderRect.anchorMax = Vector2.one;
        sliderRect.offsetMin = Vector2.zero;
        sliderRect.offsetMax = Vector2.zero;

        // Create health bar fill
        GameObject fillArea = new GameObject("FillArea");
        fillArea.transform.SetParent(healthBarObj.transform, false);

        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = Vector2.zero;
        fillAreaRect.offsetMax = Vector2.zero;

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);

        healthBarFill = fill.AddComponent<Image>();
        healthBarFill.color = fullHealthColor;
        healthBarFill.type = Image.Type.Filled;

        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        // Connect slider to fill
        healthBar.fillRect = fillRect;

        Debug.Log("Player Health UI created successfully!");
    }

    public void UpdateHealthBar(float healthPercentage)
    {
        targetHealthPercentage = healthPercentage;

        if (!animateHealthBar && healthBar != null)
        {
            healthBar.value = healthPercentage;
            currentDisplayedHealth = healthPercentage;
        }

        // Update health text
        if (healthText != null && playerHealth != null)
        {
            healthText.text = $"Health: {Mathf.Ceil(playerHealth.CurrentHealth)}/{playerHealth.MaxHealth}";
        }

        UpdateHealthBarColor(healthPercentage);
    }

    void UpdateHealthBarColor(float healthPercentage)
    {
        if (healthBarFill == null) return;

        Color targetColor;

        if (healthPercentage > 0.75f) // 75-100%
        {
            targetColor = fullHealthColor;
        }
        else if (healthPercentage > 0.5f) // 50-75%
        {
            targetColor = Color.Lerp(midHealthColor, fullHealthColor, (healthPercentage - 0.5f) / 0.25f);
        }
        else if (healthPercentage > 0.25f) // 25-50%
        {
            targetColor = Color.Lerp(lowHealthColor, midHealthColor, (healthPercentage - 0.25f) / 0.25f);
        }
        else // 0-25%
        {
            targetColor = Color.Lerp(criticalHealthColor, lowHealthColor, healthPercentage / 0.25f);
        }

        healthBarFill.color = targetColor;
    }

    void OnPlayerDeath()
    {
        if (healthText != null)
        {
            healthText.text = "DEAD";
            healthText.color = criticalHealthColor;
        }

        Debug.Log("Player has died!");
    }

    void OnDestroy()
    {
        // Unsubscribe from events
        if (playerHealth != null)
        {
            playerHealth.OnHealthPercentChanged.RemoveListener(UpdateHealthBar);
            playerHealth.OnDeath.RemoveListener(OnPlayerDeath);
        }
    }
}
