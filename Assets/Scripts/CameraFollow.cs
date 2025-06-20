using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target; // The player to follow

    [Header("Follow Settings")]
    public float followSpeed = 2f;
    public float lookAheadDistance = 2f; // How far ahead to look based on player movement

    [Header("Deadzone Settings")]
    public Vector2 deadZoneSize = new Vector2(1f, 1f); // Area where camera won't move
    public bool showDeadZone = true;

    [Header("Offset Settings")]
    public Vector3 offset = new Vector3(0f, 0f, -10f); // Camera offset from target

    [Header("Smoothing Settings")]
    public bool useSmoothDamping = true;
    public float smoothTime = 0.3f;

    [Header("Boundaries (Optional)")]
    public bool useCameraBounds = false;
    public Vector2 minBounds = new Vector2(-10f, -10f);
    public Vector2 maxBounds = new Vector2(10f, 10f);

    [Header("Debug Info")]
    [SerializeField] private Vector3 targetPosition;
    [SerializeField] private Vector3 currentVelocity;
    [SerializeField] private bool isInDeadZone;

    private Camera cam;
    private Vector3 lastTargetPosition;
    private Vector3 lookAheadTarget;

    void Start()
    {
        cam = GetComponent<Camera>();

        // Auto-find player if target not assigned
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
                Debug.Log("Camera auto-found player target: " + target.name);
            }
            else
            {
                Debug.LogWarning("No target assigned and no GameObject with 'Player' tag found!");
            }
        }

        // Initialize positions
        if (target != null)
        {
            lastTargetPosition = target.position;
            transform.position = target.position + offset;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        CalculateTargetPosition();
        CheckDeadZone();
        MoveCamera();
    }

    void CalculateTargetPosition()
    {
        // Calculate look-ahead based on player movement
        Vector3 playerVelocity = (target.position - lastTargetPosition) / Time.deltaTime;
        lookAheadTarget = target.position + playerVelocity.normalized * lookAheadDistance * Mathf.Clamp01(playerVelocity.magnitude);

        // The desired camera position
        targetPosition = lookAheadTarget + offset;

        // Apply camera bounds if enabled
        if (useCameraBounds)
        {
            targetPosition.x = Mathf.Clamp(targetPosition.x, minBounds.x, maxBounds.x);
            targetPosition.y = Mathf.Clamp(targetPosition.y, minBounds.y, maxBounds.y);
        }

        lastTargetPosition = target.position;
    }

    void CheckDeadZone()
    {
        // Check if target is within deadzone
        Vector3 deltaPosition = target.position - (transform.position - offset);

        isInDeadZone = Mathf.Abs(deltaPosition.x) <= deadZoneSize.x / 2f &&
                       Mathf.Abs(deltaPosition.y) <= deadZoneSize.y / 2f;

        // If in deadzone, don't update target position
        if (isInDeadZone)
        {
            targetPosition = transform.position;
        }
    }

    void MoveCamera()
    {
        if (isInDeadZone) return;

        Vector3 newPosition;

        if (useSmoothDamping)
        {
            // Smooth damping for organic movement
            newPosition = Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, smoothTime);
        }
        else
        {
            // Linear interpolation for consistent speed
            newPosition = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);
        }

        // Always keep the Z position (important for 2D camera)
        newPosition.z = offset.z;

        transform.position = newPosition;
    }

    void OnDrawGizmosSelected()
    {
        if (target == null) return;

        // Draw deadzone
        if (showDeadZone)
        {
            Gizmos.color = isInDeadZone ? Color.green : Color.yellow;
            Vector3 deadZoneCenter = transform.position - new Vector3(0, 0, offset.z);
            Gizmos.DrawWireCube(deadZoneCenter, new Vector3(deadZoneSize.x, deadZoneSize.y, 0f));
        }

        // Draw target position
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(targetPosition, 0.5f);

        // Draw look-ahead target
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(lookAheadTarget, 0.3f);

        // Draw line to target
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, target.position);

        // Draw camera bounds if enabled
        if (useCameraBounds)
        {
            Gizmos.color = Color.magenta;
            Vector3 boundsCenter = new Vector3((minBounds.x + maxBounds.x) / 2f, (minBounds.y + maxBounds.y) / 2f, 0f);
            Vector3 boundsSize = new Vector3(maxBounds.x - minBounds.x, maxBounds.y - minBounds.y, 0f);
            Gizmos.DrawWireCube(boundsCenter, boundsSize);
        }
    }
}
