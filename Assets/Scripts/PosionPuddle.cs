using UnityEngine;
using System.Collections;

public class SimplePoisonPuddle : MonoBehaviour
{
    [Header("Poison Settings")]
    public float totalDamage = 6f;        // Total damage (-6 HP)
    public float tickInterval = 1f;       // 1 second between damage ticks
    public int damagePerTick = 1;         // -1 HP per tick

    [Header("Debug")]
    public bool showDebugLogs = true;

    // Private state
    private bool isPoisoning = false;
    private PlayerController currentPlayer;

    void Start()
    {
        // Check and configure collider
        Collider2D col = GetComponent<Collider2D>();
        if (col == null)
        {
            Debug.LogError($"❌ {gameObject.name} is missing a Collider2D!");
            return;
        }

        if (!col.isTrigger)
        {
            col.isTrigger = true;
            if (showDebugLogs)
                Debug.Log($"✅ {gameObject.name} collider converted to trigger at runtime");
        }

        if (showDebugLogs)
            Debug.Log($"✅ {gameObject.name} poison puddle initialized successfully!");
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"🚪 Trigger entered by: {other.name}");

        // Check for player tag
        if (other.CompareTag("Player"))
        {
            Debug.Log($"✅ {other.name} is tagged as Player");

            if (!isPoisoning)
            {
                // Try to get the PlayerController
                PlayerController player = other.GetComponent<PlayerController>();
                if (player == null)
                {
                    Debug.LogWarning($"⚠️ PlayerController not found on {other.name}. Trying parent...");
                    player = other.GetComponentInParent<PlayerController>();
                }

                if (player != null)
                {
                    Debug.Log($"🧪 {player.name} entered poison puddle: {gameObject.name}");
                    currentPlayer = player;
                    StartCoroutine(PoisonPlayer());
                    PoisonMessageUI.Instance.ShowPoisonMessage();

                }
                else
                {
                    Debug.LogError($"❌ No PlayerController found on {other.name} or its parent.");
                }
            }
            else
            {
                Debug.Log($"⏳ Poison already active. Skipping duplicate trigger.");
            }
        }
        else
        {
            Debug.Log($"❌ {other.name} is not tagged 'Player'. Ignoring.");
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log($"👋 {other.name} exited poison puddle: {gameObject.name}");
        }
    }

    IEnumerator PoisonPlayer()
    {
        if (currentPlayer == null || isPoisoning)
        {
            Debug.Log($"⛔ Poison effect aborted - null player or already active.");
            yield break;
        }

        isPoisoning = true;

        int tickCount = Mathf.RoundToInt(totalDamage / damagePerTick);

        Debug.Log($"💀 Poison started on {currentPlayer.name}: {tickCount} ticks at {tickInterval}s per tick");

        for (int i = 0; i < tickCount; i++)
        {
            if (currentPlayer == null)
            {
                Debug.LogWarning($"❌ PlayerController was destroyed during poison effect");
                break;
            }

            Debug.Log($"☠️ Tick {i + 1}/{tickCount} - Dealing {damagePerTick} damage");
            currentPlayer.TakeDamage(damagePerTick, $"Poison Puddle ({gameObject.name})");

            yield return new WaitForSeconds(tickInterval);
        }

        Debug.Log($"✅ Poison completed for {currentPlayer.name}. Waiting to reset...");

        yield return new WaitForSeconds(5f);

        isPoisoning = false;
        currentPlayer = null;

        Debug.Log($"🔄 Poison puddle reset - ready to trigger again.");
    }

    void OnDrawGizmosSelected()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            Gizmos.color = isPoisoning ? Color.red : Color.green;
            Gizmos.DrawWireCube(transform.position, col.bounds.size);
        }
    }
}
