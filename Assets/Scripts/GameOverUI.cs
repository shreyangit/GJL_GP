using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class GameOverController : MonoBehaviour
{
    [Header("UI References")]
    public Button startGameAgainButton;
    public Button mainMenuBackButton;
    public Button quitButton;
    public TextMeshProUGUI gameOverTitle;
    public TextMeshProUGUI survivalTimeText;
    public TextMeshProUGUI finalScoreText;

    [Header("Scene Settings")]
    public string gameSceneName = "GameScene";
    public string mainMenuSceneName = "MainMenu";

    [Header("Game Over Display")]
    public string gameOverTitleText = "GAME OVER";
    public string survivalTimePrefix = "Survival Time: ";
    public string finalScorePrefix = "Final Score: ";

    [Header("Audio")]
    public AudioClip buttonClickSound;
    public AudioClip gameOverMusic;
    public AudioSource audioSource;

    [Header("Visual Effects")]
    public bool enableButtonHoverEffects = true;
    public bool enableFadeInEffect = true;
    public Color buttonNormalColor = Color.white;
    public Color buttonHoverColor = Color.cyan;
    public Color buttonClickColor = Color.yellow;

    [Header("Animation Settings")]
    public float fadeInDuration = 2f;
    public float buttonDelayBetween = 0.3f;

    // Private variables
    private float survivalTime = 0f;
    private int finalScore = 0;
    private bool buttonsEnabled = false;

    void Start()
    {
        LoadGameOverData();
        SetupGameOverUI();
        SetupAudio();
        SetupButtonReferences();
        SetupButtonEvents();

        if (enableFadeInEffect)
        {
            StartCoroutine(FadeInGameOverScreen());
        }
        else
        {
            ShowAllElements();
            EnableButtons();
        }

        Debug.Log("💀 Game Over Controller initialized");
    }

    void LoadGameOverData()
    {
        // Load survival time from PlayerPrefs (set by GameManager)
        survivalTime = PlayerPrefs.GetFloat("SurvivalTime", 0f);

        // Calculate final score based on survival time (you can customize this)
        finalScore = Mathf.RoundToInt(survivalTime * 10f); // 10 points per second

        Debug.Log($"💀 Game Over Data - Survival Time: {survivalTime}s, Score: {finalScore}");
    }

    void SetupGameOverUI()
    {
        // Setup game over title
        if (gameOverTitle != null)
        {
            gameOverTitle.text = gameOverTitleText;
            gameOverTitle.color = Color.red;
        }

        // Setup survival time display
        if (survivalTimeText != null)
        {
            string formattedTime = GameManager.FormatTime(survivalTime);
            survivalTimeText.text = survivalTimePrefix + formattedTime;
            survivalTimeText.color = Color.white;
        }

        // Setup final score display
        if (finalScoreText != null)
        {
            finalScoreText.text = finalScorePrefix + finalScore.ToString("N0");
            finalScoreText.color = Color.yellow;
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
        audioSource.volume = 0.4f;

        // Play game over music
        if (gameOverMusic != null)
        {
            audioSource.clip = gameOverMusic;
            audioSource.Play();
            Debug.Log("🎵 Playing game over music");
        }
    }

    void SetupButtonReferences()
    {
        // Auto-find buttons if not assigned
        if (startGameAgainButton == null)
        {
            GameObject startAgainObj = GameObject.Find("StartGameAgain");
            if (startAgainObj != null)
            {
                startGameAgainButton = startAgainObj.GetComponent<Button>();
            }
        }

        if (mainMenuBackButton == null)
        {
            GameObject mainMenuObj = GameObject.Find("MainMenuBack");
            if (mainMenuObj != null)
            {
                mainMenuBackButton = mainMenuObj.GetComponent<Button>();
            }
        }

        if (quitButton == null)
        {
            GameObject quitObj = GameObject.Find("Quit");
            if (quitObj != null)
            {
                quitButton = quitObj.GetComponent<Button>();
            }
        }

        // Auto-find text elements if not assigned
        if (gameOverTitle == null)
        {
            GameObject titleObj = GameObject.Find("GameOverTitle");
            if (titleObj != null)
            {
                gameOverTitle = titleObj.GetComponent<TextMeshProUGUI>();
            }
        }

        if (survivalTimeText == null)
        {
            GameObject timeObj = GameObject.Find("SurvivalTimeText");
            if (timeObj != null)
            {
                survivalTimeText = timeObj.GetComponent<TextMeshProUGUI>();
            }
        }

        if (finalScoreText == null)
        {
            GameObject scoreObj = GameObject.Find("FinalScoreText");
            if (scoreObj != null)
            {
                finalScoreText = scoreObj.GetComponent<TextMeshProUGUI>();
            }
        }
    }

    void SetupButtonEvents()
    {
        // Setup Start Game Again button
        if (startGameAgainButton != null)
        {
            startGameAgainButton.onClick.RemoveAllListeners();
            startGameAgainButton.onClick.AddListener(OnStartGameAgainClicked);

            if (enableButtonHoverEffects)
            {
                SetupButtonHoverEffects(startGameAgainButton);
            }

            Debug.Log("✅ Start Game Again button configured");
        }
        else
        {
            Debug.LogWarning("⚠️ Start Game Again button not found!");
        }

        // Setup Main Menu Back button
        if (mainMenuBackButton != null)
        {
            mainMenuBackButton.onClick.RemoveAllListeners();
            mainMenuBackButton.onClick.AddListener(OnMainMenuBackClicked);

            if (enableButtonHoverEffects)
            {
                SetupButtonHoverEffects(mainMenuBackButton);
            }

            Debug.Log("✅ Main Menu Back button configured");
        }
        else
        {
            Debug.LogWarning("⚠️ Main Menu Back button not found!");
        }

        // Setup Quit button
        if (quitButton != null)
        {
            quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(OnQuitClicked);

            if (enableButtonHoverEffects)
            {
                SetupButtonHoverEffects(quitButton);
            }

            Debug.Log("✅ Quit button configured");
        }
        else
        {
            Debug.LogWarning("⚠️ Quit button not found!");
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

    // 🎬 FADE IN ANIMATION

    IEnumerator FadeInGameOverScreen()
    {
        Debug.Log("🎬 Starting Game Over screen fade-in animation");

        // Initially hide all UI elements
        SetAllElementsAlpha(0f);
        DisableButtons();

        // Fade in title first
        if (gameOverTitle != null)
        {
            yield return StartCoroutine(FadeInElement(gameOverTitle, fadeInDuration * 0.3f));
        }

        yield return new WaitForSeconds(0.5f);

        // Fade in survival time
        if (survivalTimeText != null)
        {
            yield return StartCoroutine(FadeInElement(survivalTimeText, fadeInDuration * 0.3f));
        }

        yield return new WaitForSeconds(0.3f);

        // Fade in final score
        if (finalScoreText != null)
        {
            yield return StartCoroutine(FadeInElement(finalScoreText, fadeInDuration * 0.3f));
        }

        yield return new WaitForSeconds(0.5f);

        // Fade in buttons one by one
        yield return StartCoroutine(FadeInButtonsSequentially());

        // Enable button interactions
        EnableButtons();

        Debug.Log("✅ Game Over screen animation complete");
    }

    IEnumerator FadeInButtonsSequentially()
    {
        Button[] buttons = { startGameAgainButton, mainMenuBackButton, quitButton };

        foreach (Button button in buttons)
        {
            if (button != null)
            {
                yield return StartCoroutine(FadeInButton(button, fadeInDuration * 0.2f));
                yield return new WaitForSeconds(buttonDelayBetween);
            }
        }
    }

    IEnumerator FadeInElement(TextMeshProUGUI textElement, float duration)
    {
        if (textElement == null) yield break;

        Color originalColor = textElement.color;
        Color transparentColor = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);

        textElement.color = transparentColor;

        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsedTime / duration);
            textElement.color = Color.Lerp(transparentColor, originalColor, alpha);
            yield return null;
        }

        textElement.color = originalColor;
    }

    IEnumerator FadeInButton(Button button, float duration)
    {
        if (button == null) yield break;

        Image buttonImage = button.GetComponent<Image>();
        TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();

        // Store original colors
        Color originalImageColor = buttonImage != null ? buttonImage.color : Color.white;
        Color originalTextColor = buttonText != null ? buttonText.color : Color.white;

        // Set transparent
        if (buttonImage != null)
            buttonImage.color = new Color(originalImageColor.r, originalImageColor.g, originalImageColor.b, 0f);
        if (buttonText != null)
            buttonText.color = new Color(originalTextColor.r, originalTextColor.g, originalTextColor.b, 0f);

        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsedTime / duration);

            if (buttonImage != null)
                buttonImage.color = Color.Lerp(new Color(originalImageColor.r, originalImageColor.g, originalImageColor.b, 0f), originalImageColor, alpha);
            if (buttonText != null)
                buttonText.color = Color.Lerp(new Color(originalTextColor.r, originalTextColor.g, originalTextColor.b, 0f), originalTextColor, alpha);

            yield return null;
        }

        // Restore original colors
        if (buttonImage != null) buttonImage.color = originalImageColor;
        if (buttonText != null) buttonText.color = originalTextColor;
    }

    void SetAllElementsAlpha(float alpha)
    {
        // Set text elements alpha
        if (gameOverTitle != null)
        {
            Color color = gameOverTitle.color;
            gameOverTitle.color = new Color(color.r, color.g, color.b, alpha);
        }

        if (survivalTimeText != null)
        {
            Color color = survivalTimeText.color;
            survivalTimeText.color = new Color(color.r, color.g, color.b, alpha);
        }

        if (finalScoreText != null)
        {
            Color color = finalScoreText.color;
            finalScoreText.color = new Color(color.r, color.g, color.b, alpha);
        }

        // Set button elements alpha
        Button[] buttons = { startGameAgainButton, mainMenuBackButton, quitButton };
        foreach (Button button in buttons)
        {
            if (button != null)
            {
                Image buttonImage = button.GetComponent<Image>();
                TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();

                if (buttonImage != null)
                {
                    Color color = buttonImage.color;
                    buttonImage.color = new Color(color.r, color.g, color.b, alpha);
                }

                if (buttonText != null)
                {
                    Color color = buttonText.color;
                    buttonText.color = new Color(color.r, color.g, color.b, alpha);
                }
            }
        }
    }

    void ShowAllElements()
    {
        SetAllElementsAlpha(1f);
    }

    // 🎮 BUTTON EVENT HANDLERS

    public void OnStartGameAgainClicked()
    {
        Debug.Log("🔄 START GAME AGAIN button clicked!");

        PlayButtonSound();
        DisableButtons();

        // Clear any existing game over data
        PlayerPrefs.DeleteKey("SurvivalTime");

        // Start new game through GameManager or directly load scene
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartNewGame();
        }
        else
        {
            SceneManager.LoadScene(gameSceneName);
        }
    }

    public void OnMainMenuBackClicked()
    {
        Debug.Log("🏠 MAIN MENU BACK button clicked!");

        PlayButtonSound();
        DisableButtons();

        // Go back to main menu through GameManager or directly load scene
        if (GameManager.Instance != null)
        {
            GameManager.Instance.GoToMainMenu();
        }
        else
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }

    public void OnQuitClicked()
    {
        Debug.Log("👋 QUIT GAME button clicked!");

        PlayButtonSound();
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
        if (startGameAgainButton != null) startGameAgainButton.interactable = false;
        if (mainMenuBackButton != null) mainMenuBackButton.interactable = false;
        if (quitButton != null) quitButton.interactable = false;
        buttonsEnabled = false;
    }

    void EnableButtons()
    {
        if (startGameAgainButton != null) startGameAgainButton.interactable = true;
        if (mainMenuBackButton != null) mainMenuBackButton.interactable = true;
        if (quitButton != null) quitButton.interactable = true;
        buttonsEnabled = true;
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

    public void UpdateSurvivalTime(float newSurvivalTime)
    {
        survivalTime = newSurvivalTime;
        finalScore = Mathf.RoundToInt(survivalTime * 10f);

        if (survivalTimeText != null)
        {
            string formattedTime = GameManager.FormatTime(survivalTime);
            survivalTimeText.text = survivalTimePrefix + formattedTime;
        }

        if (finalScoreText != null)
        {
            finalScoreText.text = finalScorePrefix + finalScore.ToString("N0");
        }
    }

    public void UpdateGameOverTitle(string newTitle)
    {
        gameOverTitleText = newTitle;
        if (gameOverTitle != null)
        {
            gameOverTitle.text = newTitle;
        }
    }

    // Handle keyboard shortcuts
    void Update()
    {
        if (!buttonsEnabled) return;

        // Space or Enter to restart game
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            OnStartGameAgainClicked();
        }

        // ESC to go to main menu
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            OnMainMenuBackClicked();
        }

        // Q to quit
        if (Input.GetKeyDown(KeyCode.Q))
        {
            OnQuitClicked();
        }
    }

    void OnDestroy()
    {
        // Stop music when game over screen is destroyed
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }
}
