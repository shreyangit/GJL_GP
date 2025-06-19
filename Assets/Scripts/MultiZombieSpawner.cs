using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    [Header("Spawning Settings")]
    public float spawnRate = 2f;
    public Vector2 spawnAreaSize = new Vector2(20f, 15f);

    [Header("Collision Detection")]
    public LayerMask obstacleLayerMask = 64;
    public float zombieRadius = 0.5f;
    public int maxSpawnAttempts = 30;

    [Header("References")]
    public Transform player;

    [Header("Debug Info (Read Only)")]
    [SerializeField] private int currentTotalZombies = 0;
    [SerializeField] private int currentNearbyZombies = 0;
    [SerializeField] private int normalZombieCount = 0;
    [SerializeField] private int brightnessZombieCount = 0;
    [SerializeField] private int dynamiteZombieCount = 0;

    public enum ZombieType
    {
        Normal,
        Brightness,
        Dynamite
    }

    // Private variables
    private Dictionary<ZombieType, List<GameObject>> zombiesByType = new Dictionary<ZombieType, List<GameObject>>();
    private Vector3 lastPlayerPosition;

    void Start()
    {
        // Initialize zombie tracking lists
        zombiesByType[ZombieType.Normal] = new List<GameObject>();
        zombiesByType[ZombieType.Brightness] = new List<GameObject>();
        zombiesByType[ZombieType.Dynamite] = new List<GameObject>();

        // Find player if not assigned
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }

        if (player == null)
        {
            Debug.LogError("MultiZombieSpawner: No player found!");
            return;
        }

        lastPlayerPosition = player.position;
        StartCoroutine(SpawnZombiesOverTime());
        StartCoroutine(CheckPlayerMovement());
    }

    void Update()
    {
        UpdateZombieCounters();
        CleanUpDestroyedZombies();
    }

    IEnumerator SpawnZombiesOverTime()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnRate);

            if (CanSpawnZombie())
            {
                SpawnRandomZombie();
            }
        }
    }

    IEnumerator CheckPlayerMovement()
    {
        while (true)
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
        }
    }

    bool CanSpawnZombie()
    {
        if (player == null) return false;
        if (currentTotalZombies >= maxZombieCountAllMap) return false;
        if (currentNearbyZombies >= maxZombieCountPlayer) return false;
        return true;
    }

    void SpawnRandomZombie()
    {
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
            zombiesByType[typeToSpawn].Add(newZombie);

            // Configure zombie AI based on type
            SetupZombieAI(newZombie, typeToSpawn);

            Debug.Log($"Spawned {typeToSpawn} zombie! Total: {currentTotalZombies + 1}");
        }
    }

    ZombieType GetRandomZombieType()
    {
        float totalChance = normalZombieChance + brightnessZombieChance + dynamiteZombieChance;
        float randomValue = Random.Range(0f, totalChance);

        if (randomValue <= normalZombieChance)
            return ZombieType.Normal;
        else if (randomValue <= normalZombieChance + brightnessZombieChance)
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
        switch (type)
        {
            case ZombieType.Normal:
                ZombieAI normalAI = zombie.GetComponent<ZombieAI>();
                if (normalAI != null) normalAI.player = player;
                break;

            case ZombieType.Brightness:
                BrightnessZombieAI brightnessAI = zombie.GetComponent<BrightnessZombieAI>();
                if (brightnessAI != null) brightnessAI.player = player;
                break;

            case ZombieType.Dynamite:
                DynamiteZombieAI dynamiteAI = zombie.GetComponent<DynamiteZombieAI>();
                if (dynamiteAI != null) dynamiteAI.player = player;
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

    void OnDrawGizmosSelected()
    {
        if (player != null)
        {
            // Spawn area
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(player.position, new Vector3(spawnAreaSize.x, spawnAreaSize.y, 0f));

            // Draw zombies by type with different colors
            Gizmos.color = Color.red;
            foreach (GameObject zombie in zombiesByType[ZombieType.Normal])
            {
                if (zombie != null)
                    Gizmos.DrawWireSphere(zombie.transform.position, zombieRadius);
            }

            Gizmos.color = Color.yellow;
            foreach (GameObject zombie in zombiesByType[ZombieType.Brightness])
            {
                if (zombie != null)
                    Gizmos.DrawWireSphere(zombie.transform.position, zombieRadius);
            }

            Gizmos.color = Color.orange;
            foreach (GameObject zombie in zombiesByType[ZombieType.Dynamite])
            {
                if (zombie != null)
                    Gizmos.DrawWireSphere(zombie.transform.position, zombieRadius);
            }
        }
    }
}
