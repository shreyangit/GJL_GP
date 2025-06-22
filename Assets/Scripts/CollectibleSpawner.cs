using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CollectibleSpawner : MonoBehaviour
{
    [Header("Spawning Settings")]
    public float spawnInterval = 15f;           // Spawn every 15 seconds
    public int maxCoinsOnMap = 5;               // Maximum coins at once
    public int maxHeartsOnMap = 3;              // Maximum hearts at once
    public float coinSpawnChance = 0.7f;        // 70% chance to spawn coin vs heart

    [Header("Prefabs")]
    public GameObject coinPrefab;
    public GameObject heartPrefab;

    [Header("Spawn Area")]
    public Vector2 spawnAreaCenter = Vector2.zero;
    public Vector2 spawnAreaSize = new Vector2(50f, 50f);

    [Header("Collision Avoidance")]
    public LayerMask obstacleLayerMask = 32;    // Walls layer
    public LayerMask zombieLayerMask = 64;      // Zombies layer
    public float collisionCheckRadius = 1f;
    public int maxSpawnAttempts = 20;

    [Header("Debug")]
    public bool showDebugInfo = true;
    public bool showSpawnArea = true;

    private List<GameObject> activeCoins = new List<GameObject>();
    private List<GameObject> activeHearts = new List<GameObject>();
    private Transform collectiblesParent;

    void Start()
    {
        // Create parent object for organization
        GameObject parentObj = new GameObject("Active Collectibles");
        collectiblesParent = parentObj.transform;

        // Create prefabs if not assigned
        CreatePrefabsIfNeeded();

        // Start spawning
        StartCoroutine(SpawnRoutine());

        Debug.Log($"✅ Collectible spawner initialized. Spawning every {spawnInterval}s");
    }

    void CreatePrefabsIfNeeded()
    {
        // Create coin prefab if missing
        if (coinPrefab == null)
        {
            coinPrefab = CreateCoinPrefab();
        }

        // Create heart prefab if missing
        if (heartPrefab == null)
        {
            heartPrefab = CreateHeartPrefab();
        }
    }

    GameObject CreateCoinPrefab()
    {
        // Try to find coin sprite
        Sprite coinSprite = Resources.Load<Sprite>("Items/Coin") ??
                           LoadSpriteFromPath("Assets/Items/Coin");

        GameObject coin = new GameObject("Coin Collectible");

        // Add sprite renderer
        SpriteRenderer sr = coin.AddComponent<SpriteRenderer>();
        sr.sprite = coinSprite;
        sr.sortingOrder = 10;

        // Add collectible script
        coin.AddComponent<CoinCollectible>();

        // Make it a prefab (in memory)
        coin.SetActive(false);

        Debug.Log($"✅ Created coin prefab with sprite: {(coinSprite != null ? coinSprite.name : "default")}");
        return coin;
    }

    GameObject CreateHeartPrefab()
    {
        // Try to find heart sprite
        Sprite heartSprite = Resources.Load<Sprite>("Items/Heart") ??
                            LoadSpriteFromPath("Assets/Items/Heart");

        GameObject heart = new GameObject("Heart Collectible");

        // Add sprite renderer
        SpriteRenderer sr = heart.AddComponent<SpriteRenderer>();
        sr.sprite = heartSprite;
        sr.sortingOrder = 10;
        sr.color = Color.red;

        // Add collectible script
        heart.AddComponent<HeartCollectible>();

        // Make it a prefab (in memory)
        heart.SetActive(false);

        Debug.Log($"✅ Created heart prefab with sprite: {(heartSprite != null ? heartSprite.name : "default")}");
        return heart;
    }

    Sprite LoadSpriteFromPath(string path)
    {
        // This is a fallback - in practice, assign sprites in Inspector
        return null;
    }

    IEnumerator SpawnRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);

            // Clean up destroyed collectibles
            CleanupDestroyedCollectibles();

            // Try to spawn collectible
            TrySpawnCollectible();
        }
    }

    void TrySpawnCollectible()
    {
        // Check if we need to spawn anything
        bool canSpawnCoin = activeCoins.Count < maxCoinsOnMap;
        bool canSpawnHeart = activeHearts.Count < maxHeartsOnMap;

        if (!canSpawnCoin && !canSpawnHeart)
        {
            if (showDebugInfo) Debug.Log("⏸️ Max collectibles reached, skipping spawn");
            return;
        }

        // Decide what to spawn
        bool spawnCoin = canSpawnCoin && (Random.value < coinSpawnChance || !canSpawnHeart);

        if (spawnCoin)
        {
            SpawnCoin();
        }
        else if (canSpawnHeart)
        {
            SpawnHeart();
        }
    }

    void SpawnCoin()
    {
        Vector2 spawnPos = FindValidSpawnPosition();
        if (spawnPos != Vector2.zero)
        {
            GameObject coin = Instantiate(coinPrefab, spawnPos, Quaternion.identity, collectiblesParent);
            coin.SetActive(true);
            activeCoins.Add(coin);

            if (showDebugInfo) Debug.Log($"🪙 Spawned coin at {spawnPos}. Active coins: {activeCoins.Count}");
        }
    }

    void SpawnHeart()
    {
        Vector2 spawnPos = FindValidSpawnPosition();
        if (spawnPos != Vector2.zero)
        {
            GameObject heart = Instantiate(heartPrefab, spawnPos, Quaternion.identity, collectiblesParent);
            heart.SetActive(true);
            activeHearts.Add(heart);

            if (showDebugInfo) Debug.Log($"💖 Spawned heart at {spawnPos}. Active hearts: {activeHearts.Count}");
        }
    }

    Vector2 FindValidSpawnPosition()
    {
        for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
        {
            // Random position within spawn area
            Vector2 randomPos = new Vector2(
                Random.Range(spawnAreaCenter.x - spawnAreaSize.x / 2f, spawnAreaCenter.x + spawnAreaSize.x / 2f),
                Random.Range(spawnAreaCenter.y - spawnAreaSize.y / 2f, spawnAreaCenter.y + spawnAreaSize.y / 2f)
            );

            // Check for collisions
            bool hasCollision = Physics2D.OverlapCircle(randomPos, collisionCheckRadius, obstacleLayerMask) != null ||
                               Physics2D.OverlapCircle(randomPos, collisionCheckRadius, zombieLayerMask) != null;

            if (!hasCollision)
            {
                return randomPos;
            }
        }

        if (showDebugInfo) Debug.LogWarning($"⚠️ Could not find valid spawn position after {maxSpawnAttempts} attempts");
        return Vector2.zero;
    }

    void CleanupDestroyedCollectibles()
    {
        // Remove null references from lists
        activeCoins.RemoveAll(coin => coin == null);
        activeHearts.RemoveAll(heart => heart == null);
    }

    void OnDrawGizmosSelected()
    {
        if (!showSpawnArea) return;

        // Draw spawn area
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(spawnAreaCenter, spawnAreaSize);

        // Draw collision check radius at center
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(spawnAreaCenter, collisionCheckRadius);
    }
}
