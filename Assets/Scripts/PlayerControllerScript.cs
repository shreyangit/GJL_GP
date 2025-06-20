using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 3f;
    public float runSpeed = 6f;
    public float currentSpeed;

    [Header("Collision Avoidance")]
    public LayerMask obstacleLayerMask = 64; // Walls layer
    public float collisionRadius = 0.5f;

    [Header("Ammo System")]
    public int maxClips = 5;
    public int bulletsPerClip = 10;
    public int totalBullets = 50;

    [Header("Current Ammo State (Read Only)")]
    [SerializeField] private int currentClip = 1;
    [SerializeField] private int bulletsInCurrentClip = 10;
    [SerializeField] private int[] clipAmmo = new int[5]; // Track bullets in each clip
    [SerializeField] private int previousClip = 1;

    [Header("Animation States")]
    public bool isReloading = false;
    public bool isAttacking = false;
    public bool isMoving = false;
    public bool isRunning = false;

    [Header("Debug Info")]
    [SerializeField] private Vector2 lastMovementInput;
    [SerializeField] private bool isInputBlocked = false;

    // Private components and variables
    private Rigidbody2D rb2d;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Vector2 movementInput;
    private Vector2 lastFacingDirection = Vector2.down;

    // Animation state tracking
    private float attackCooldown = 0f;
    private float reloadCooldown = 0f;
    private const float ATTACK_DURATION = 0.5f;
    private const float RELOAD_DURATION = 1f;

    // Input System variables
    private PlayerInputActions inputActions;
    private bool runPressed = false;

    void Awake()
    {
        // Initialize Input Actions
        inputActions = new PlayerInputActions();
    }

    void OnEnable()
    {
        inputActions.Enable();

        // Subscribe to input events
        inputActions.Player.Move.performed += OnMoveInput;
        inputActions.Player.Move.canceled += OnMoveInput;
        inputActions.Player.Run.performed += OnRunInput;
        inputActions.Player.Run.canceled += OnRunInput;
        inputActions.Player.Reload.performed += OnReloadInput;
        inputActions.Player.UndoReload.performed += OnUndoReloadInput;
        inputActions.Player.Attack.performed += OnAttackInput;
    }

    void OnDisable()
    {
        inputActions.Disable();

        // Unsubscribe from input events
        inputActions.Player.Move.performed -= OnMoveInput;
        inputActions.Player.Move.canceled -= OnMoveInput;
        inputActions.Player.Run.performed -= OnRunInput;
        inputActions.Player.Run.canceled -= OnRunInput;
        inputActions.Player.Reload.performed -= OnReloadInput;
        inputActions.Player.UndoReload.performed -= OnUndoReloadInput;
        inputActions.Player.Attack.performed -= OnAttackInput;
    }

    void Start()
    {
        // Get components
        rb2d = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Initialize Rigidbody2D settings
        if (rb2d != null)
        {
            rb2d.gravityScale = 0f;
            rb2d.freezeRotation = true;
        }

        // Initialize ammo system
        InitializeAmmoSystem();

        // Set initial speed
        currentSpeed = walkSpeed;

        Debug.Log($"Player initialized. Total bullets: {totalBullets}, Current clip: {currentClip}, Bullets in clip: {bulletsInCurrentClip}");
    }

    void Update()
    {
        UpdateMovement();
        UpdateAnimations();
        UpdateCooldowns();
    }

    // Input System callbacks
    void OnMoveInput(InputAction.CallbackContext context)
    {
        movementInput = context.ReadValue<Vector2>();

        if (movementInput.magnitude > 0.1f)
        {
            lastFacingDirection = movementInput.normalized;
            lastMovementInput = movementInput;
            isMoving = true;
        }
        else
        {
            isMoving = false;
        }
    }

    void OnRunInput(InputAction.CallbackContext context)
    {
        runPressed = context.ReadValueAsButton();
        isRunning = runPressed && isMoving;
        currentSpeed = isRunning ? runSpeed : walkSpeed;
    }

    void OnReloadInput(InputAction.CallbackContext context)
    {
        if (!isReloading && !isAttacking)
        {
            ReloadToNextClip();
        }
    }

    void OnUndoReloadInput(InputAction.CallbackContext context)
    {
        if (!isReloading && !isAttacking)
        {
            ReloadToPreviousClip();
        }
    }

    void OnAttackInput(InputAction.CallbackContext context)
    {
        if (!isReloading && !isAttacking)
        {
            PerformManualAttack();
        }
    }

    void UpdateMovement()
    {
        if (isReloading || isAttacking)
        {
            // Stop movement during animations
            rb2d.linearVelocity = Vector2.zero;
            return;
        }

        if (movementInput.magnitude > 0.1f)
        {
            // Update running state based on movement
            isRunning = runPressed && isMoving;
            currentSpeed = isRunning ? runSpeed : walkSpeed;

            // Check for collisions before moving
            Vector2 intendedMovement = movementInput.normalized * currentSpeed;
            Vector2 finalMovement = GetCollisionAvoidedMovement(intendedMovement);

            // Apply movement
            rb2d.linearVelocity = finalMovement;

            // Handle sprite flipping based on horizontal movement
            if (movementInput.x < 0)
                spriteRenderer.flipX = true;
            else if (movementInput.x > 0)
                spriteRenderer.flipX = false;
        }
        else
        {
            // Stop movement when no input
            rb2d.linearVelocity = Vector2.zero;
            isRunning = false;
        }
    }

    Vector2 GetCollisionAvoidedMovement(Vector2 intendedMovement)
    {
        // Check for collision in intended direction
        Vector2 nextPosition = (Vector2)transform.position + intendedMovement * Time.deltaTime;

        Collider2D hit = Physics2D.OverlapCircle(nextPosition, collisionRadius, obstacleLayerMask);

        if (hit == null)
        {
            // No collision, move as intended
            return intendedMovement;
        }
        else
        {
            // Try sliding along walls
            Vector2 slideMovement = Vector2.zero;

            // Try moving just horizontally
            Vector2 horizontalPosition = (Vector2)transform.position + Vector2.right * intendedMovement.x * Time.deltaTime;
            if (Physics2D.OverlapCircle(horizontalPosition, collisionRadius, obstacleLayerMask) == null)
            {
                slideMovement.x = intendedMovement.x;
            }

            // Try moving just vertically
            Vector2 verticalPosition = (Vector2)transform.position + Vector2.up * intendedMovement.y * Time.deltaTime;
            if (Physics2D.OverlapCircle(verticalPosition, collisionRadius, obstacleLayerMask) == null)
            {
                slideMovement.y = intendedMovement.y;
            }

            return slideMovement;
        }
    }

    void UpdateAnimations()
    {
        if (animator == null) return;

        // Set animation parameters
        animator.SetBool("IsMoving", isMoving && !isReloading && !isAttacking);
        animator.SetBool("IsRunning", isRunning && isMoving && !isReloading && !isAttacking);
        animator.SetBool("IsReloading", isReloading);
        animator.SetBool("IsAttacking", isAttacking);

        // Set movement direction for directional animations if needed
        animator.SetFloat("MoveX", lastFacingDirection.x);
        animator.SetFloat("MoveY", lastFacingDirection.y);
    }

    void UpdateCooldowns()
    {
        // Update attack cooldown
        if (attackCooldown > 0)
        {
            attackCooldown -= Time.deltaTime;
            if (attackCooldown <= 0)
            {
                isAttacking = false;
            }
        }

        // Update reload cooldown
        if (reloadCooldown > 0)
        {
            reloadCooldown -= Time.deltaTime;
            if (reloadCooldown <= 0)
            {
                isReloading = false;
            }
        }
    }

    void InitializeAmmoSystem()
    {
        // Initialize all clips with full ammo
        for (int i = 0; i < maxClips; i++)
        {
            clipAmmo[i] = bulletsPerClip;
        }

        currentClip = 1; // Start with clip 1 (index 0)
        previousClip = 1;
        bulletsInCurrentClip = clipAmmo[0];

        Debug.Log("Ammo system initialized. All clips full.");
    }

    void ReloadToNextClip()
    {
        if (isReloading) return;

        // Store current clip state
        clipAmmo[currentClip - 1] = bulletsInCurrentClip;
        previousClip = currentClip;

        // Move to next clip (circular)
        currentClip = (currentClip % maxClips) + 1;
        bulletsInCurrentClip = clipAmmo[currentClip - 1];

        // Start reload animation
        isReloading = true;
        reloadCooldown = RELOAD_DURATION;

        Debug.Log($"Reloading to clip {currentClip}. Bullets: {bulletsInCurrentClip}. Previous clip was {previousClip}.");
    }

    void ReloadToPreviousClip()
    {
        if (isReloading) return;

        // Store current clip state
        clipAmmo[currentClip - 1] = bulletsInCurrentClip;

        // Switch back to previous clip
        int tempClip = currentClip;
        currentClip = previousClip;
        previousClip = tempClip;

        bulletsInCurrentClip = clipAmmo[currentClip - 1];

        // Start reload animation
        isReloading = true;
        reloadCooldown = RELOAD_DURATION;

        Debug.Log($"Undoing reload - back to clip {currentClip}. Bullets: {bulletsInCurrentClip}. Previous was {previousClip}.");
    }

    void PerformManualAttack()
    {
        if (isAttacking || isReloading) return;

        // Start attack animation
        isAttacking = true;
        attackCooldown = ATTACK_DURATION;

        Debug.Log($"Manual attack performed! Facing direction: {lastFacingDirection}");
    }

    // Public methods for other scripts to access ammo info
    public int GetCurrentAmmo()
    {
        return bulletsInCurrentClip;
    }

    public int GetCurrentClip()
    {
        return currentClip;
    }

    public int GetTotalAmmo()
    {
        int total = 0;
        for (int i = 0; i < maxClips; i++)
        {
            total += clipAmmo[i];
        }
        return total;
    }

    public bool CanShoot()
    {
        return bulletsInCurrentClip > 0 && !isReloading && !isAttacking;
    }

    public void ConsumeBullet()
    {
        if (bulletsInCurrentClip > 0)
        {
            bulletsInCurrentClip--;
            clipAmmo[currentClip - 1] = bulletsInCurrentClip;
            Debug.Log($"Bullet consumed. Remaining in clip {currentClip}: {bulletsInCurrentClip}");
        }
    }

    void OnDrawGizmosSelected()
    {
        // Draw collision radius
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, collisionRadius);

        // Draw movement direction
        if (lastMovementInput.magnitude > 0.1f)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, lastMovementInput.normalized * 2f);
        }

        // Draw facing direction
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, lastFacingDirection * 1.5f);
    }
}
