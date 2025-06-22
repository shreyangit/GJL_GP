using UnityEngine;
using UnityEngine.UI;

public class MainMenuSetup : MonoBehaviour
{
    [Header("Auto Setup")]
    public bool autoSetupOnAwake = true;

    [Header("UI References")]
    public Canvas mainCanvas;
    public Camera menuCamera;

    void Awake()
    {
        if (autoSetupOnAwake)
        {
            SetupMainMenuUI();
        }
    }

    void Start()
    {
        ValidateSetup();
    }

    public void SetupMainMenuUI()
    {
        Debug.Log("🏠 Setting up Main Menu UI...");

        // Find or create camera
        SetupCamera();

        // Find or create canvas
        SetupCanvas();

        // Create UI elements
        CreateMainMenuUI();

        Debug.Log("✅ Main Menu UI setup complete!");
    }

    void SetupCamera()
    {
        if (menuCamera == null)
        {
            menuCamera = Camera.main;
            if (menuCamera == null)
            {
                GameObject cameraObj = GameObject.FindGameObjectWithTag("MainCamera");
                if (cameraObj != null)
                {
                    menuCamera = cameraObj.GetComponent<Camera>();
                }
            }
        }

        if (menuCamera != null)
        {
            // Configure camera for UI
            menuCamera.clearFlags = CameraClearFlags.SolidColor;
            menuCamera.backgroundColor = new Color(0.1f, 0.1f, 0.2f, 1f);
            menuCamera.orthographic = true;
            menuCamera.orthographicSize = 5f; // Much smaller for UI
            menuCamera.transform.position = new Vector3(0, 0, -10);

            Debug.Log($"📷 Camera configured: Orthographic Size = {menuCamera.orthographicSize}");
        }
        else
        {
            Debug.LogError("❌ No camera found for Main Menu!");
        }
    }

    void SetupCanvas()
    {
        if (mainCanvas == null)
        {
            mainCanvas = FindFirstObjectByType<Canvas>();
        }

        if (mainCanvas == null)
        {
            // Create new canvas
            GameObject canvasObj = new GameObject("Main Menu Canvas");
            mainCanvas = canvasObj.AddComponent<Canvas>();

            // Add required components
            CanvasScaler canvasScaler = canvasObj.AddComponent<CanvasScaler>();
            GraphicRaycaster raycaster = canvasObj.AddComponent<GraphicRaycaster>();

            // Configure the newly created scaler
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            canvasScaler.matchWidthOrHeight = 0.5f;
        }

        // Configure canvas for screen space overlay
        mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        mainCanvas.sortingOrder = 0;

        // Configure existing canvas scaler if it exists
        CanvasScaler existingScaler = mainCanvas.GetComponent<CanvasScaler>();
        if (existingScaler != null)
        {
            existingScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            existingScaler.referenceResolution = new Vector2(1920, 1080);
            existingScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            existingScaler.matchWidthOrHeight = 0.5f;
        }

        Debug.Log($"🖼️ Canvas configured: Render Mode = {mainCanvas.renderMode}");
    }

    void CreateMainMenuUI()
    {
        if (mainCanvas == null) return;

        // Check if UI already exists
        if (mainCanvas.transform.childCount > 0)
        {
            Debug.Log("📱 UI elements already exist, skipping creation");
            return;
        }

        // Create background panel
        CreateBackgroundPanel();

        // Create title
        CreateTitleText();

        // Create buttons
        CreateStartButton();
        CreateQuitButton();

        // Create version text
        CreateVersionText();
    }

    GameObject CreateBackgroundPanel()
    {
        GameObject panelObj = new GameObject("Background Panel");
        panelObj.transform.SetParent(mainCanvas.transform, false);

        Image panelImage = panelObj.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.7f); // Semi-transparent black

        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;
        panelRect.anchoredPosition = Vector2.zero;

        return panelObj;
    }

    GameObject CreateTitleText()
    {
        GameObject titleObj = new GameObject("Title Text");
        titleObj.transform.SetParent(mainCanvas.transform, false);

        Text titleText = titleObj.AddComponent<Text>();
        titleText.text = "ZOMBIE SURVIVAL";
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 60;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = Color.white;

        // Add outline
        Outline outline = titleObj.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(3, 3);

        // Position at top center
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 0.7f);
        titleRect.anchorMax = new Vector2(1f, 0.9f);
        titleRect.anchoredPosition = Vector2.zero;
        titleRect.sizeDelta = Vector2.zero;

        return titleObj;
    }

    GameObject CreateStartButton()
    {
        GameObject buttonObj = CreateMenuButton("Start Game", new Vector2(0.5f, 0.5f), new Vector2(300, 80));

        Button button = buttonObj.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(OnStartButtonClicked);
        }

        return buttonObj;
    }

    GameObject CreateQuitButton()
    {
        GameObject buttonObj = CreateMenuButton("Quit Game", new Vector2(0.5f, 0.35f), new Vector2(300, 80));

        Button button = buttonObj.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(OnQuitButtonClicked);
        }

        return buttonObj;
    }

    GameObject CreateMenuButton(string buttonText, Vector2 anchorPosition, Vector2 size)
    {
        // Create button GameObject
        GameObject buttonObj = new GameObject($"{buttonText} Button");
        buttonObj.transform.SetParent(mainCanvas.transform, false);

        // Add Button component
        Button button = buttonObj.AddComponent<Button>();

        // Add Image component for button background
        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.3f, 0.8f, 0.9f); // Blue background

        // Setup RectTransform
        RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
        buttonRect.anchorMin = buttonRect.anchorMax = anchorPosition;
        buttonRect.anchoredPosition = Vector2.zero;
        buttonRect.sizeDelta = size;

        // Create text child
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);

        Text text = textObj.AddComponent<Text>();
        text.text = buttonText;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 28;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;

        // Setup text RectTransform to fill button
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.anchoredPosition = Vector2.zero;
        textRect.sizeDelta = Vector2.zero;

        return buttonObj;
    }

    GameObject CreateVersionText()
    {
        GameObject versionObj = new GameObject("Version Text");
        versionObj.transform.SetParent(mainCanvas.transform, false);

        Text versionText = versionObj.AddComponent<Text>();
        versionText.text = "v1.0";
        versionText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        versionText.fontSize = 20;
        versionText.alignment = TextAnchor.LowerRight;
        versionText.color = new Color(1f, 1f, 1f, 0.7f);

        // Position at bottom right
        RectTransform versionRect = versionObj.GetComponent<RectTransform>();
        versionRect.anchorMin = new Vector2(0.8f, 0f);
        versionRect.anchorMax = new Vector2(1f, 0.2f);
        versionRect.anchoredPosition = new Vector2(-20, 20);
        versionRect.sizeDelta = Vector2.zero;

        return versionObj;
    }

    void ValidateSetup()
    {
        // Check canvas visibility
        if (mainCanvas != null && mainCanvas.gameObject.activeInHierarchy)
        {
            Debug.Log($"✅ Canvas is active and visible");
        }
        else
        {
            Debug.LogWarning($"⚠️ Canvas is not active or visible!");
        }

        // Check camera
        if (menuCamera != null)
        {
            Debug.Log($"✅ Camera found: {menuCamera.name}, Orthographic Size: {menuCamera.orthographicSize}");
        }
        else
        {
            Debug.LogWarning($"⚠️ No camera assigned!");
        }

        // Count UI elements
        int uiElementCount = mainCanvas != null ? mainCanvas.transform.childCount : 0;
        Debug.Log($"📱 UI elements in canvas: {uiElementCount}");
    }

    // Button callbacks
    public void OnStartButtonClicked()
    {
        Debug.Log("🎮 Start Game clicked!");

        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartNewGame();
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("GameScene");
        }
    }

    public void OnQuitButtonClicked()
    {
        Debug.Log("👋 Quit Game clicked!");

        if (GameManager.Instance != null)
        {
            GameManager.Instance.QuitGame();
        }
        else
        {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }

    // Public method to force refresh
    [ContextMenu("Force Setup UI")]
    public void ForceSetupUI()
    {
        SetupMainMenuUI();
    }
}
