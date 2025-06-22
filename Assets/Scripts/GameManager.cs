using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Game State")]
    public bool isGameActive = false;
    public bool isPaused = false;
    private bool hasGameEnded = false;

    [Header("Game Statistics")]
    private float gameStartTime;
    private float survivalTime;

    [Header("Scene Names")]
    public string mainMenuSceneName = "MainMenu";
    public string gameSceneName = "GameScene";
    public string gameOverSceneName = "GameOver";

    [Header("Player References")]
    public PlayerController player;
    public HealthSystem playerHealth;

    [Header("Audio")]
    public AudioClip gameOverSound;
    public AudioClip backgroundMusic;
    private AudioSource audioSource;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SetupAudio();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Check current scene and initialize accordingly
        string currentScene = SceneManager.GetActiveScene().name;

        Debug.Log($"🎮 GameManager started in scene: {currentScene}");

        if (currentScene == mainMenuSceneName)
        {
            InitializeMainMenu();
        }
        else if (currentScene == gameSceneName)
        {
            // Wait a frame then start game to let scene load
            StartCoroutine(StartGameAfterDelay());
        }
    }

    void Update()
    {
        // Update survival time if game is active
        if (isGameActive && !isPaused && !hasGameEnded)
        {
            survivalTime = Time.time - gameStartTime;
        }

        // Handle ESC key for quitting to main menu (only in game scene)
        if (Input.GetKeyDown(KeyCode.Escape) && isGameActive && !hasGameEnded)
        {
            GoToMainMenu();
        }
    }

    void SetupAudio()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.loop = true;
        audioSource.volume = 0.5f;
    }

    IEnumerator StartGameAfterDelay()
    {
        yield return new WaitForSeconds(0.1f); // Let scene fully load
        StartGameInScene();
    }

    void StartGameInScene()
    {
        FindPlayerReferences();

        if (player == null)
        {
            Debug.LogWarning("⚠️ No player found in game scene!");
        }

        isGameActive = true;
        isPaused = false;
        hasGameEnded = false;
        gameStartTime = Time.time;
        survivalTime = 0f;

        Debug.Log($"🎮 Game started! Time: {gameStartTime}");

        // Play background music
        if (backgroundMusic != null && audioSource != null)
        {
            audioSource.clip = backgroundMusic;
            audioSource.Play();
        }

        // Subscribe to player death event
        SubscribeToPlayerEvents();
    }

    void FindPlayerReferences()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.GetComponent<PlayerController>();
                playerHealth = playerObj.GetComponent<HealthSystem>();
            }
        }
    }

    void SubscribeToPlayerEvents()
    {
        if (playerHealth != null)
        {
            // Subscribe to death event (make sure this doesn't double-subscribe)
            playerHealth.OnDeath.RemoveListener(OnPlayerDeath);
            playerHealth.OnDeath.AddListener(OnPlayerDeath);
            Debug.Log($"✅ Subscribed to player death events");
        }
        else
        {
            Debug.LogWarning("⚠️ No player HealthSystem found for death subscription");
        }
    }

    public void OnPlayerDeath()
    {
        if (hasGameEnded) return;

        hasGameEnded = true;
        isGameActive = false;

        Debug.Log($"💀 Player died! Survival time: {GetFormattedSurvivalTime()}");

        // Play game over sound
        if (gameOverSound != null && audioSource != null)
        {
            audioSource.Stop(); // Stop background music
            audioSource.PlayOneShot(gameOverSound);
        }

        // Save survival time and go to game over scene
        StartCoroutine(GoToGameOverAfterDelay(2f));
    }

    IEnumerator GoToGameOverAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        EndGame();
    }

    // 🎯 PUBLIC SCENE TRANSITION METHODS

    public void StartGame()
    {
        Debug.Log($"🎮 Starting new game - Loading {gameSceneName}");
        SceneManager.LoadScene(gameSceneName);
    }

    public void StartNewGame()
    {
        Debug.Log($"🎮 Starting new game - Loading {gameSceneName}");
        SceneManager.LoadScene(gameSceneName);
    }

    public void EndGame()
    {
        Debug.Log($"💀 Ending game - Going to game over");

        // Store survival time for game over scene
        PlayerPrefs.SetFloat("SurvivalTime", survivalTime);
        PlayerPrefs.Save();

        isGameActive = false;
        hasGameEnded = true;

        SceneManager.LoadScene(gameOverSceneName);
    }

    public void GoToMainMenu()
    {
        Debug.Log($"🏠 Going to main menu - Loading {mainMenuSceneName}");

        isGameActive = false;
        hasGameEnded = false;

        // Stop any playing audio
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void QuitGame()
    {
        Debug.Log($"👋 Quitting game application");

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // 🏠 MAIN MENU INITIALIZATION

    public void InitializeMainMenu()
    {
        Debug.Log("🏠 Initializing Main Menu");

        // Ensure game is not active in main menu
        isGameActive = false;
        isPaused = false;
        hasGameEnded = false;

        // Stop any existing game audio
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        // Find and setup main menu controller
        MainMenuController menuController = FindFirstObjectByType<MainMenuController>();
        if (menuController != null)
        {
            Debug.Log("✅ Main Menu Controller found and ready");
        }
        else
        {
            Debug.Log("⚠️ No Main Menu Controller found - UI will still work");
        }
    }


    // 🕐 TIME FORMATTING METHODS

    public string GetFormattedSurvivalTime()
    {
        return FormatTime(survivalTime);
    }

    public static string FormatTime(float timeInSeconds)
    {
        int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
        int milliseconds = Mathf.FloorToInt((timeInSeconds * 100f) % 100f);

        return $"{minutes:00}:{seconds:00}.{milliseconds:00}";
    }

    public float GetCurrentSurvivalTime()
    {
        return survivalTime;
    }

    // 🎮 GAME STATE METHODS

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;
        Debug.Log($"⏸️ Game paused");
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        Debug.Log($"▶️ Game resumed");
    }

    // 🎵 AUDIO METHODS

    public void PlayBackgroundMusic(AudioClip music)
    {
        if (audioSource != null && music != null)
        {
            audioSource.clip = music;
            audioSource.Play();
        }
    }

    public void StopBackgroundMusic()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
