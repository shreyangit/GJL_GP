using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MultiZombieSpawner : MonoBehaviour
{
    [Header("Global Zombie Limits")]
    public int maxZombieCountAllMap = 20;
    public int maxZombieCountPlayer = 8;

    [Header("Zombie Prefabs")]
    public GameObject normalZombiePrefab;
    public GameObject brightnessZombiePrefab;
    public GameObject dynamiteZombiePrefab;

    [Header("Spawn Rates (%)")]
    [Range(0f, 100f)] public float normalZombieChance = 60f;
    [Range(0f, 100f)] public float brightnessZombieChance = 25f;
    [Range(0f, 100f)] public float dynamiteZombieChance = 15f;

    [Header("Normal Spawning Settings")]
    public float spawnRate = 2f;
    public Vector2 spawnAreaSize = new Vector2(20f, 15f);

    [Header("🚨 BURST SPAWNING SYSTEM")]
    [Space(10)]
    [Tooltip("Enable periodic spawn rate bursts")]
    public bool enableSpawnBursts = true;

    [Tooltip("Time between burst events (in seconds)")]
    public float burstInterval = 120f; // Every 2 minutes

    [Tooltip("Duration of each burst (in seconds)")]
    public float burstDuration = 20f;   // 20 seconds

    [Tooltip("Spawn rate during burst (how often to spawn during burst)")]
    public float burstSpawnRate = 0.5f; // Much faster spawning

    [Tooltip("Multiplier for zombie limits during burst")]
    [Range(1f, 3f)] public float burstLimitMultiplier = 1.5f;

    [Header("🎯 BURST SPAWN CHANCES")]
    [Space(5)]
    [Tooltip("Normal zombie chance during burst")]
    [Range(0f, 100f)] public float burstNormalZombieChance = 40f;

    [Tooltip("Brightness zombie chance during burst")]
    [Range(0f, 100f)] public float burstBrightnessZombieChance = 35f;

    [Tooltip("Dynamite zombie chance during burst")]
    [Range(0f, 100f)] public float burstDynamiteZombieChance = 25f;

    [Header("Collision Detection")]
    public LayerMask obstacleLayerMask = 32; // Walls layer 5 (2^5 = 32)
    public float zombieRadius = 0.5f;
    public int maxSpawnAttempts = 30;

    [Header("References")]
    public Transform player;

    [Header("🎮 GAME CONTROL")]
    [Space(10)]
    [Tooltip("Scene names where spawning should be active")]
    public string[] gameSceneNames = { "GameScene", "Game", "MainGame" };

    [Tooltip("Only spawn when game is explicitly started")]
    public bool waitForGameStart = true;

    [Header("🔥 BURST STATUS (Read Only)")]
    [Space(10)]
    [SerializeField] private bool isBurstActive = false;
    [SerializeField] private float timeUntilNextBurst = 0f;
    [SerializeField] private float burstTimeRemaining = 0f;
    [SerializeField] private int burstsCompleted = 0;

    [Header("Debug Info (Read Only)")]
    [SerializeField] private bool isGameActive = false;
    [SerializeField] private bool isSpawningEnabled = false;
    [SerializeField] private string currentSceneName = "";
    [SerializeField] private int currentTotalZombies = 0;
    [SerializeField] private int currentNearbyZombies = 0;
    [SerializeField] private int normalZombieCount = 0;
    [SerializeField] private int brightnessZombieCount = 0;
    [SerializeField] private int dynamiteZombieCount = 0;

    [Header("🎬 BURST UI NOTIFICATIONS")]
    [Space(10)]
    [Tooltip("Enable burst warning notifications")]
    public bool enableBurstNotifications = true;

    [Tooltip("Show warning this many seconds before burst starts")]
    public float warningTimeBeforeBurst = 5f;

    [Tooltip("Audio clip to play when warning appears")]
    public AudioClip warningSound;

    [Tooltip("Audio clip to play when burst starts")]
    public AudioClip burstStartSound;

    [Tooltip("Audio clip to play when burst ends")]
    public AudioClip burstEndSound;

    public enum ZombieType
    {
        Normal,
        Brightness,
        Dynamite
    }

    // Private variables
    private Dictionary<ZombieType, List<GameObject>> zombiesByType = new Dictionary<ZombieType, List<GameObject>>();
    private Vector3 lastPlayerPosition;
    private bool hasStartedSpawning = false;

    void Awake()
    {
        // Initialize zombie tracking lists early (before Start)
        zombiesByType = new Dictionary<ZombieType, List<GameObject>>();
        zombiesByType[ZombieType.Normal] = new List<GameObject>();
        zombiesByType[ZombieType.Brightness] = new List<GameObject>();
        zombiesByType[ZombieType.Dynamite] = new List<GameObject>();

        // Get current scene name
        currentSceneName = SceneManager.GetActiveScene().name;

        Debug.Log($"🎮 MultiZombieSpawner awakened in scene: {currentSceneName}");
    }

    void Start()
    {
        // Check if we should be active in this scene
        CheckSceneValidity();

        if (!isSpawningEnabled)
        {
            Debug.Log($"🛑 Spawning disabled - not in game scene or game not started");
            return;
        }

        // Initialize if we're in the right scene and conditions
        InitializeSpawner();
    }

    void CheckSceneValidity()
    {
        currentSceneName = SceneManager.GetActiveScene().name;

        // Check if current scene is a game scene
        bool isValidGameScene = false;
        foreach (string sceneName in gameSceneNames)
        {
            if (currentSceneName.Equals(sceneName, System.StringComparison.OrdinalIgnoreCase))
            {
                isValidGameScene = true;
                break;
            }
        }

        // Check if game is active (if GameManager exists)
        bool gameManagerActive = false;
        if (GameManager.Instance != null)
        {
            gameManagerActive = GameManager.Instance.isGameActive;
        }
        else if (!waitForGameStart)
        {
            gameManagerActive = true; // Allow without GameManager if not waiting
        }

        // Enable spawning only if both conditions are met
        isSpawningEnabled = isValidGameScene && (!waitForGameStart || gameManagerActive);
        isGameActive = gameManagerActive;

        Debug.Log($"🎮 Scene check: Scene='{currentSceneName}', ValidGameScene={isValidGameScene}, GameActive={gameManagerActive}, SpawningEnabled={isSpawningEnabled}");
    }

    void InitializeSpawner()
    {
        // Find player if not assigned
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }

        if (player == null)
        {
            Debug.LogWarning("⚠️ MultiZombieSpawner: No player found! Spawning will be disabled.");
            isSpawningEnabled = false;
            return;
        }

        lastPlayerPosition = player.position;

        // Initialize burst system
        if (enableSpawnBursts)
        {
            timeUntilNextBurst = burstInterval;
            Debug.Log($"🚀 Burst system enabled! First burst in {burstInterval} seconds");
        }

        // Start spawning systems
        StartSpawningCoroutines();
    }

    void StartSpawningCoroutines()
    {
        if (!isSpawningEnabled || hasStartedSpawning) return;

        hasStartedSpawning = true;

        StartCoroutine(SpawnZombiesOverTime());
        StartCoroutine(CheckPlayerMovement());

        if (enableSpawnBursts)
        {
            StartCoroutine(BurstController());
        }

        Debug.Log($"✅ Zombie spawning systems started in {currentSceneName}");
    }

    void Update()
    {
        // Always update counters for debugging
        UpdateZombieCounters();
        CleanUpDestroyedZombies();

        // Only update timers if spawning is active
        if (isSpawningEnabled)
        {
            UpdateBurstTimers();
        }

        // Check for game state changes
        CheckGameStateChanges();
    }

    void CheckGameStateChanges()
    {
        // Check if scene changed
        string newSceneName = SceneManager.GetActiveScene().name;
        if (newSceneName != currentSceneName)
        {
            Debug.Log($"🔄 Scene changed from {currentSceneName} to {newSceneName}");
            currentSceneName = newSceneName;
            CheckSceneValidity();

            // Stop spawning if we're no longer in a valid scene
            if (!isSpawningEnabled && hasStartedSpawning)
            {
                StopAllSpawning();
            }
            // Start spawning if we entered a valid scene
            else if (isSpawningEnabled && !hasStartedSpawning)
            {
                InitializeSpawner();
            }
        }

        // Check if game manager state changed
        if (GameManager.Instance != null)
        {
            bool newGameState = GameManager.Instance.isGameActive;
            if (newGameState != isGameActive)
            {
                Debug.Log($"🎮 Game state changed: {isGameActive} → {newGameState}");
                isGameActive = newGameState;
                CheckSceneValidity();

                if (isSpawningEnabled && !hasStartedSpawning)
                {
                    InitializeSpawner();
                }
                else if (!isSpawningEnabled && hasStartedSpawning)
                {
                    StopAllSpawning();
                }
            }
        }
    }

    void StopAllSpawning()
    {
        Debug.Log($"🛑 Stopping all zombie spawning");

        hasStartedSpawning = false;
        StopAllCoroutines();

        // Optionally destroy all existing zombies
        DestroyAllZombies();
    }

    void DestroyAllZombies()
    {
        Debug.Log($"💀 Destroying all existing zombies");

        foreach (var zombieList in zombiesByType.Values)
        {
            for (int i = zombieList.Count - 1; i >= 0; i--)
            {
                if (zombieList[i] != null)
                {
                    Destroy(zombieList[i]);
                }
            }
            zombieList.Clear();
        }
    }

    // 🚀 PUBLIC METHODS for external control

    public void StartGame()
    {
        Debug.Log($"🎮 StartGame() called - enabling spawning");
        isGameActive = true;
        CheckSceneValidity();

        if (isSpawningEnabled && !hasStartedSpawning)
        {
            InitializeSpawner();
        }
    }

    public void StopGame()
    {
        Debug.Log($"🛑 StopGame() called - disabling spawning");
        isGameActive = false;
        isSpawningEnabled = false;

        if (hasStartedSpawning)
        {
            StopAllSpawning();
        }
    }

    void UpdateBurstTimers()
    {
        if (!enableSpawnBursts || !isSpawningEnabled) return;

        if (isBurstActive)
        {
            burstTimeRemaining -= Time.deltaTime;
        }
        else
        {
            timeUntilNextBurst -= Time.deltaTime;
        }
    }

    // 🚀 ENHANCED: Burst Controller with UI notifications
    IEnumerator BurstController()
    {
        while (enableSpawnBursts && isSpawningEnabled)
        {
            // Wait for most of the burst interval
            float waitTime = burstInterval - warningTimeBeforeBurst;
            if (waitTime > 0)
            {
                yield return new WaitForSeconds(waitTime);
            }

            // Check if still active before proceeding
            if (!isSpawningEnabled) yield break;

            // Show warning notification
            if (enableBurstNotifications && BurstNotificationUI.Instance != null)
            {
                BurstNotificationUI.Instance.ShowSurgeIncoming(warningTimeBeforeBurst);
            }

            // Wait for warning duration
            yield return new WaitForSeconds(warningTimeBeforeBurst);

            // Check if still active before proceeding
            if (!isSpawningEnabled) yield break;

            // Start burst
            StartBurst();

            // Wait for burst duration
            yield return new WaitForSeconds(burstDuration);

            // End burst
            EndBurst();
        }
    }

    void StartBurst()
    {
        if (!enableSpawnBursts || isBurstActive || !isSpawningEnabled) return;

        isBurstActive = true;
        burstTimeRemaining = burstDuration;
        burstsCompleted++;

        Debug.Log($"🚨 SPAWN BURST #{burstsCompleted} STARTED! Duration: {burstDuration}s");
        Debug.Log($"🔥 Burst settings: Rate={burstSpawnRate}s, Limits=x{burstLimitMultiplier}");

        // Show burst active notification
        if (enableBurstNotifications && BurstNotificationUI.Instance != null)
        {
            BurstNotificationUI.Instance.ShowSurgeActive(burstDuration);
        }

        // Play burst start sound
        if (burstStartSound != null && player != null)
        {
            AudioSource.PlayClipAtPoint(burstStartSound, player.position);
        }
    }

    void EndBurst()
    {
        if (!isBurstActive) return;

        isBurstActive = false;
        timeUntilNextBurst = burstInterval;
        burstTimeRemaining = 0f;

        Debug.Log($"✅ SPAWN BURST #{burstsCompleted} ENDED! Next burst in {burstInterval}s");

        // Show burst ended notification
        if (enableBurstNotifications && BurstNotificationUI.Instance != null)
        {
            BurstNotificationUI.Instance.ShowSurgeEnded();
        }

        // Play burst end sound
        if (burstEndSound != null && player != null)
        {
            AudioSource.PlayClipAtPoint(burstEndSound, player.position);
        }
    }

    // 🚀 ENHANCED: Smart spawning with burst support
    IEnumerator SpawnZombiesOverTime()
    {
        while (isSpawningEnabled)
        {
            // Use burst spawn rate if burst is active, otherwise normal spawn rate
            float currentSpawnRate = isBurstActive ? burstSpawnRate : spawnRate;
            yield return new WaitForSeconds(currentSpawnRate);

            // Check if still enabled before spawning
            if (isSpawningEnabled && CanSpawnZombie())
            {
                SpawnRandomZombie();
            }
        }
    }

    IEnumerator CheckPlayerMovement()
    {
        while (isSpawningEnabled)
        {
            yield return new WaitForSeconds(1f);

            if (player != null)
            {
                float distanceMoved = Vector3.Distance(player.position, lastPlayerPosition);
                if (distanceMoved > 5f)
                {
                    lastPlayerPosition = player.position;
                }
            }
            else
            {
                // Try to find player again
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                    player = playerObj.transform;
            }
        }
    }

    // 🚀 ENHANCED: Burst-aware spawn limits
    bool CanSpawnZombie()
    {
        if (player == null || !isSpawningEnabled) return false;

        // Apply burst multiplier to limits if burst is active
        int maxTotal = isBurstActive ?
            Mathf.RoundToInt(maxZombieCountAllMap * burstLimitMultiplier) :
            maxZombieCountAllMap;

        int maxNearby = isBurstActive ?
            Mathf.RoundToInt(maxZombieCountPlayer * burstLimitMultiplier) :
            maxZombieCountPlayer;

        if (currentTotalZombies >= maxTotal) return false;
        if (currentNearbyZombies >= maxNearby) return false;
        return true;
    }

    void SpawnRandomZombie()
    {
        if (!isSpawningEnabled) return;

        ZombieType typeToSpawn = GetRandomZombieType();
        GameObject prefabToSpawn = GetZombiePrefab(typeToSpawn);

        if (prefabToSpawn == null)
        {
            Debug.LogWarning($"No prefab assigned for {typeToSpawn} zombie!");
            return;
        }

        Vector3 validSpawnPosition = FindValidSpawnPosition();

        if (validSpawnPosition != Vector3.zero)
        {
            GameObject newZombie = Instantiate(prefabToSpawn, validSpawnPosition, Quaternion.identity);

            SetupZombieAI(newZombie, typeToSpawn);

            // Add to tracking list AFTER setup
            zombiesByType[typeToSpawn].Add(newZombie);

            string burstStatus = isBurstActive ? "🚨 BURST" : "🔄 NORMAL";
            Debug.Log($"✅ {burstStatus} Spawned {typeToSpawn} zombie at {validSpawnPosition}! Total: {currentTotalZombies + 1}");
        }
    }

    // 🚀 ENHANCED: Burst-aware zombie type selection
    ZombieType GetRandomZombieType()
    {
        // Use burst spawn chances if burst is active, otherwise normal chances
        float normalChance = isBurstActive ? burstNormalZombieChance : normalZombieChance;
        float brightnessChance = isBurstActive ? burstBrightnessZombieChance : brightnessZombieChance;
        float dynamiteChance = isBurstActive ? burstDynamiteZombieChance : dynamiteZombieChance;

        float totalChance = normalChance + brightnessChance + dynamiteChance;
        float randomValue = Random.Range(0f, totalChance);

        if (randomValue <= normalChance)
            return ZombieType.Normal;
        else if (randomValue <= normalChance + brightnessChance)
            return ZombieType.Brightness;
        else
            return ZombieType.Dynamite;
    }

    GameObject GetZombiePrefab(ZombieType type)
    {
        switch (type)
        {
            case ZombieType.Normal: return normalZombiePrefab;
            case ZombieType.Brightness: return brightnessZombiePrefab;
            case ZombieType.Dynamite: return dynamiteZombiePrefab;
            default: return normalZombiePrefab;
        }
    }

    void SetupZombieAI(GameObject zombie, ZombieType type)
    {
        Debug.Log($"🔧 Setting up spawned {type} zombie: {zombie.name}");

        // FORCE CORRECT LAYER ASSIGNMENT
        int zombieLayer = LayerMask.NameToLayer("Zombies");
        if (zombieLayer == -1)
        {
            Debug.LogError("❌ 'Zombies' layer not found! Check your layer setup.");
            return;
        }
        zombie.layer = zombieLayer;

        // VERIFY AND ADD HEALTHSYSTEM IF MISSING
        HealthSystem healthSystem = zombie.GetComponent<HealthSystem>();
        if (healthSystem == null)
        {
            healthSystem = zombie.AddComponent<HealthSystem>();
            healthSystem.maxHealth = 20f; // Default zombie health
        }

        // VERIFY AND CONFIGURE COLLIDER
        Collider2D collider = zombie.GetComponent<Collider2D>();
        if (collider == null)
        {
            CapsuleCollider2D capsuleCollider = zombie.AddComponent<CapsuleCollider2D>();
            capsuleCollider.isTrigger = true;
            capsuleCollider.size = new Vector2(0.5f, 1f);
        }
        else
        {
            collider.isTrigger = true;
        }

        // VERIFY TAG
        try
        {
            if (!zombie.CompareTag("Enemy"))
            {
                zombie.tag = "Enemy";
            }
        }
        catch (UnityException e)
        {
            Debug.LogError($"❌ Cannot set Enemy tag - Tag not defined! Error: {e.Message}");
        }

        // SETUP AI COMPONENTS
        switch (type)
        {
            case ZombieType.Normal:
                ZombieAI normalAI = zombie.GetComponent<ZombieAI>();
                if (normalAI != null)
                {
                    normalAI.player = FindFirstObjectByType<PlayerController>();
                }
                break;

            case ZombieType.Brightness:
                BrightnessZombieAI brightnessAI = zombie.GetComponent<BrightnessZombieAI>();
                if (brightnessAI != null)
                {
                    brightnessAI.player = FindFirstObjectByType<PlayerController>();
                }
                break;

            case ZombieType.Dynamite:
                DynamiteZombieAI dynamiteAI = zombie.GetComponent<DynamiteZombieAI>();
                if (dynamiteAI != null)
                {
                    dynamiteAI.player = FindFirstObjectByType<PlayerController>();
                }
                break;
        }
    }

    Vector3 FindValidSpawnPosition()
    {
        for (int attempts = 0; attempts < maxSpawnAttempts; attempts++)
        {
            Vector2 randomOffset = new Vector2(
                Random.Range(-spawnAreaSize.x / 2f, spawnAreaSize.x / 2f),
                Random.Range(-spawnAreaSize.y / 2f, spawnAreaSize.y / 2f)
            );

            Vector3 potentialPosition = player.position + new Vector3(randomOffset.x, randomOffset.y, 0f);

            if (IsPositionValid(potentialPosition))
            {
                return potentialPosition;
            }
        }

        return Vector3.zero;
    }

    bool IsPositionValid(Vector3 position)
    {
        Collider2D hit = Physics2D.OverlapCircle(position, zombieRadius, obstacleLayerMask);
        if (hit != null) return false;

        // Check distance from other zombies
        foreach (var zombieList in zombiesByType.Values)
        {
            foreach (GameObject zombie in zombieList)
            {
                if (zombie != null)
                {
                    float distance = Vector2.Distance(position, zombie.transform.position);
                    if (distance < zombieRadius * 2f)
                        return false;
                }
            }
        }

        return true;
    }

    void UpdateZombieCounters()
    {
        normalZombieCount = zombiesByType[ZombieType.Normal].Count;
        brightnessZombieCount = zombiesByType[ZombieType.Brightness].Count;
        dynamiteZombieCount = zombiesByType[ZombieType.Dynamite].Count;

        currentTotalZombies = normalZombieCount + brightnessZombieCount + dynamiteZombieCount;
        currentNearbyZombies = GetNearbyZombieCount();
    }

    int GetNearbyZombieCount()
    {
        if (player == null) return 0;

        int nearbyCount = 0;
        float maxDistance = Mathf.Max(spawnAreaSize.x, spawnAreaSize.y) / 2f;

        foreach (var zombieList in zombiesByType.Values)
        {
            foreach (GameObject zombie in zombieList)
            {
                if (zombie != null)
                {
                    float distance = Vector2.Distance(zombie.transform.position, player.position);
                    if (distance <= maxDistance)
                        nearbyCount++;
                }
            }
        }

        return nearbyCount;
    }

    void CleanUpDestroyedZombies()
    {
        foreach (var zombieType in zombiesByType.Keys)
        {
            var zombieList = zombiesByType[zombieType];
            for (int i = zombieList.Count - 1; i >= 0; i--)
            {
                if (zombieList[i] == null)
                {
                    zombieList.RemoveAt(i);
                }
            }
        }
    }

    public void OnZombieDespawned(GameObject zombie, ZombieType type)
    {
        if (zombiesByType[type].Contains(zombie))
        {
            zombiesByType[type].Remove(zombie);
            Debug.Log($"{type} zombie despawned. Remaining: {currentTotalZombies - 1}");
        }
    }

    // 🚀 NEW: Public methods for external control
    public void TriggerManualBurst()
    {
        if (!isBurstActive && isSpawningEnabled)
        {
            StartCoroutine(ManualBurstCoroutine());
        }
    }

    IEnumerator ManualBurstCoroutine()
    {
        StartBurst();
        yield return new WaitForSeconds(burstDuration);
        EndBurst();
    }

    public void SetBurstEnabled(bool enabled)
    {
        enableSpawnBursts = enabled;
        if (!enabled && isBurstActive)
        {
            EndBurst();
        }

        Debug.Log($"🚀 Burst system {(enabled ? "enabled" : "disabled")}");
    }

    void OnDrawGizmosSelected()
    {
        if (player != null && isSpawningEnabled)
        {
            // Spawn area (different color during burst)
            Gizmos.color = isBurstActive ? Color.red : Color.green;
            Gizmos.DrawWireCube(player.position, new Vector3(spawnAreaSize.x, spawnAreaSize.y, 0f));

            // Burst status indicator
            if (isBurstActive)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(player.position, 2f);
            }

            // Draw zombies by type with different colors
            if (zombiesByType != null && zombiesByType.Count > 0)
            {
                if (zombiesByType.ContainsKey(ZombieType.Normal))
                {
                    Gizmos.color = Color.red;
                    foreach (GameObject zombie in zombiesByType[ZombieType.Normal])
                    {
                        if (zombie != null)
                            Gizmos.DrawWireSphere(zombie.transform.position, zombieRadius);
                    }
                }

                if (zombiesByType.ContainsKey(ZombieType.Brightness))
                {
                    Gizmos.color = Color.yellow;
                    foreach (GameObject zombie in zombiesByType[ZombieType.Brightness])
                    {
                        if (zombie != null)
                            Gizmos.DrawWireSphere(zombie.transform.position, zombieRadius);
                    }
                }

                if (zombiesByType.ContainsKey(ZombieType.Dynamite))
                {
                    Gizmos.color = Color.orange;
                    foreach (GameObject zombie in zombiesByType[ZombieType.Dynamite])
                    {
                        if (zombie != null)
                            Gizmos.DrawWireSphere(zombie.transform.position, zombieRadius);
                    }
                }
            }
        }
        else if (!isSpawningEnabled)
        {
            // Draw disabled indicator
            Gizmos.color = Color.gray;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 2f);
        }
    }
}
