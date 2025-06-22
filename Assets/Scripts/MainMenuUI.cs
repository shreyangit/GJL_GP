using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenuController : MonoBehaviour
{
    [Header("UI References")]
    public Button startButton;
    public Button exitButton;
    public TextMeshProUGUI titleText;

    [Header("Scene Settings")]
    public string gameSceneName = "GameScene";

    [Header("Audio")]
    public AudioClip buttonClickSound;
    public AudioClip menuMusic;
    public AudioSource audioSource;

    [Header("Visual Effects")]
    public bool enableButtonHoverEffects = true;
    public Color buttonNormalColor = Color.white;
    public Color buttonHoverColor = Color.cyan;
    public Color buttonClickColor = Color.yellow;

    void Start()
    {
        SetupMainMenu();
        SetupAudio();
        SetupButtonReferences();
        SetupButtonEvents();

        Debug.Log("🏠 Main Menu Controller initialized");
    }

    void SetupMainMenu()
    {
        // Setup title text if available
        if (titleText != null)
        {
            titleText.text = "ZOMBIE SURVIVAL";
            titleText.color = Color.white;
        }

        // Ensure canvas is properly configured
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;
        }
    }

    void SetupAudio()
    {
        // Setup audio source
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        audioSource.playOnAwake = false;
        audioSource.loop = true;
        audioSource.volume = 0.3f;

        // Play menu music
        if (menuMusic != null)
        {
            audioSource.clip = menuMusic;
            audioSource.Play();
            Debug.Log("🎵 Playing menu music");
        }
    }

    void SetupButtonReferences()
    {
        // Auto-find buttons if not assigned
        if (startButton == null)
        {
            GameObject startButtonObj = GameObject.Find("StartButton");
            if (startButtonObj != null)
            {
                startButton = startButtonObj.GetComponent<Button>();
            }
        }

        if (exitButton == null)
        {
            GameObject exitButtonObj = GameObject.Find("ExitButton");
            if (exitButtonObj != null)
            {
                exitButton = exitButtonObj.GetComponent<Button>();
            }
        }

        if (titleText == null)
        {
            GameObject titleObj = GameObject.Find("Title");
            if (titleObj != null)
            {
                titleText = titleObj.GetComponent<TextMeshProUGUI>();
            }
        }
    }

    void SetupButtonEvents()
    {
        // Setup Start button
        if (startButton != null)
        {
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(OnStartButtonClicked);

            if (enableButtonHoverEffects)
            {
                SetupButtonHoverEffects(startButton);
            }

            Debug.Log("✅ Start button configured"); // FIXED: Capital L
        }
        else
        {
            Debug.LogWarning("⚠️ Start button not found!");
        }

        // Setup Exit button
        if (exitButton != null)
        {
            exitButton.onClick.RemoveAllListeners();
            exitButton.onClick.AddListener(OnExitButtonClicked);

            if (enableButtonHoverEffects)
            {
                SetupButtonHoverEffects(exitButton);
            }

            Debug.Log("✅ Exit button configured");
        }
        else
        {
            Debug.LogWarning("⚠️ Exit button not found!");
        }
    }

    void SetupButtonHoverEffects(Button button)
    {
        // Get or add ColorBlock for button color transitions
        var colors = button.colors;
        colors.normalColor = buttonNormalColor;
        colors.highlightedColor = buttonHoverColor;
        colors.pressedColor = buttonClickColor;
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.2f;
        button.colors = colors;

        // Ensure button transition is set to ColorTint
        button.transition = Selectable.Transition.ColorTint;
    }

    // 🎮 BUTTON EVENT HANDLERS

    public void OnStartButtonClicked()
    {
        Debug.Log("🎮 START GAME button clicked!");

        PlayButtonSound();

        // Disable buttons to prevent double-clicking
        DisableButtons();

        // Start the game through GameManager or directly load scene
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartNewGame();
        }
        else
        {
            SceneManager.LoadScene(gameSceneName);
        }
    }

    public void OnExitButtonClicked()
    {
        Debug.Log("👋 EXIT GAME button clicked!");

        PlayButtonSound();

        // Disable buttons
        DisableButtons();

        // Quit the game
        if (GameManager.Instance != null)
        {
            GameManager.Instance.QuitGame();
        }
        else
        {
            QuitGame();
        }
    }

    void PlayButtonSound()
    {
        if (buttonClickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(buttonClickSound);
            Debug.Log("🔊 Playing button click sound");
        }
    }

    void DisableButtons()
    {
        if (startButton != null) startButton.interactable = false;
        if (exitButton != null) exitButton.interactable = false;
    }

    void EnableButtons()
    {
        if (startButton != null) startButton.interactable = true;
        if (exitButton != null) exitButton.interactable = true;
    }

    void QuitGame()
    {
        Debug.Log("🚪 Quitting application...");

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // 🎮 PUBLIC UTILITY METHODS

    public void SetButtonsInteractable(bool interactable)
    {
        if (interactable)
        {
            EnableButtons();
        }
        else
        {
            DisableButtons();
        }
    }

    public void UpdateTitle(string newTitle)
    {
        if (titleText != null)
        {
            titleText.text = newTitle;
        }
    }

    // Handle ESC key for alternative exit
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            OnExitButtonClicked();
        }
    }

    void OnDestroy()
    {
        // Stop music when main menu is destroyed
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }
}
