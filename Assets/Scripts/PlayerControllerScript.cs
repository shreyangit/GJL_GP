using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 3f;
    public float runSpeed = 6f;
    public float currentSpeed;

    [Header("Collision Avoidance")]
    public LayerMask obstacleLayerMask = 64; // Walls layer
    public float collisionRadius = 0.5f;

    [Header("Shooting Settings")]
    public Transform firePoint; // Where bullets spawn from
    public GameObject bulletPrefab; // Bullet prefab to spawn
    public float bulletSpeed = 10f;
    public float fireRate = 0.1f; // Time between shots
    public LayerMask enemyLayerMask = 128; // Zombies layer

    [Header("Ammo System")]
    public int maxClips = 5;
    public int bulletsPerClip = 10;
    public int totalBullets = 50;

    [Header("Health & Light System")]
    public Light2D playerLight; // Reference to player's Light2D
    public float initialLightIntensity = 3f;
    public float lightDecreaseAmount = 1f;
    public float invulnerabilityDuration = 1f; // After taking damage

    [Header("Current Ammo State (Read Only)")]
    [SerializeField] private int currentClip = 1;
    [SerializeField] private int bulletsInCurrentClip = 10;
    [SerializeField] private int[] clipAmmo = new int[5]; // Track bullets in each clip
    [SerializeField] private int previousClip = 1;

    [Header("Animation States")]
    public bool isReloading = false;
    public bool isAttacking = false;
    public bool isShooting = false;
    public bool isMoving = false;
    public bool isRunning = false;

    [Header("Debug Info")]
    [SerializeField] private Vector2 lastMovementInput;
    [SerializeField] private bool isInputBlocked = false;
    [SerializeField] private float lastShotTime = 0f;

    // Private components and variables
    private Rigidbody2D rb2d;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Vector2 movementInput;
    private Vector2 lastFacingDirection = Vector2.down;

    // Health and light system private fields
    private HealthSystem healthSystem;
    private float currentLightIntensity;

    // Animation state tracking
    private float attackCooldown = 0f;
    private float reloadCooldown = 0f;
    private float shootCooldown = 0f;
    private const float ATTACK_DURATION = 0.5f;
    private const float RELOAD_DURATION = 1f;
    private const float SHOOT_DURATION = 0.1f;

    // Input System variables
    private PlayerInputActions inputActions;
    private bool runPressed = false;
    private bool shootPressed = false;

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

        // Add shooting input if it exists
        if (inputActions.Player.Shoot != null)
        {
            inputActions.Player.Shoot.performed += OnShootInput;
            inputActions.Player.Shoot.canceled += OnShootInput;
        }
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

        if (inputActions.Player.Shoot != null)
        {
            inputActions.Player.Shoot.performed -= OnShootInput;
            inputActions.Player.Shoot.canceled -= OnShootInput;
        }
    }

    void Start()
    {
        // Get components
        rb2d = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Create firePoint if not assigned
        if (firePoint == null)
        {
            GameObject firePointObj = new GameObject("FirePoint");
            firePointObj.transform.SetParent(transform);
            firePointObj.transform.localPosition = new Vector3(0f, 0.5f, 0f); // In front of player
            firePoint = firePointObj.transform;
        }

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

        // Initialize health system
        healthSystem = GetComponent<HealthSystem>();
        if (healthSystem == null)
        {
            healthSystem = gameObject.AddComponent<HealthSystem>();
            healthSystem.maxHealth = 20f; // Player starts with 20 HP
        }

        // Subscribe to health events
        healthSystem.OnDeath.AddListener(OnPlayerDeath);

        // Initialize light system
        if (playerLight == null)
        {
            playerLight = GetComponentInChildren<Light2D>();
        }

        if (playerLight != null)
        {
            currentLightIntensity = initialLightIntensity;
            playerLight.intensity = currentLightIntensity;
            Debug.Log($"Player light initialized with intensity: {currentLightIntensity}");
        }
        else
        {
            Debug.LogWarning("No Light2D found on player! Please add a Light2D component.");
        }

        Debug.Log($"Player initialized. Total bullets: {totalBullets}, Current clip: {currentClip}, Bullets in clip: {bulletsInCurrentClip}");
    }

    void Update()
    {
        UpdateMovement();
        UpdateShooting();
        UpdateAnimations();
        UpdateCooldowns();
    }

    // Input System callbacks
    void OnMoveInput(InputAction.CallbackContext context)
    {
        if (isInputBlocked) return;

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
        if (isInputBlocked) return;

        runPressed = context.ReadValueAsButton();
        isRunning = runPressed && isMoving;
        currentSpeed = isRunning ? runSpeed : walkSpeed;
    }

    void OnReloadInput(InputAction.CallbackContext context)
    {
        if (isInputBlocked) return;

        // Allow reloading during movement, just not during other reload
        if (!isReloading)
        {
            ReloadToNextClip();
        }
    }

    void OnUndoReloadInput(InputAction.CallbackContext context)
    {
        if (isInputBlocked) return;

        // Allow undo reload during movement
        if (!isReloading)
        {
            ReloadToPreviousClip();
        }
    }

    void OnAttackInput(InputAction.CallbackContext context)
    {
        if (isInputBlocked) return;

        // Allow manual attack during movement! Only prevent during reload
        if (!isReloading)
        {
            PerformManualAttack();
        }
    }

    void OnShootInput(InputAction.CallbackContext context)
    {
        if (isInputBlocked) return;

        shootPressed = context.ReadValueAsButton();
    }

    void UpdateMovement()
    {
        // Only stop movement during reload, allow movement during attack and shooting
        if (isReloading)
        {
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

    void UpdateShooting()
    {
        // Handle continuous shooting while button is held
        if (shootPressed && CanShoot() && Time.time >= lastShotTime + fireRate)
        {
            Shoot();
            lastShotTime = Time.time;
        }
    }

    void Shoot()
    {
        if (!CanShoot()) return;

        // Consume bullet
        ConsumeBullet();

        // Set shooting state for animation
        isShooting = true;
        shootCooldown = SHOOT_DURATION;

        // Create bullet
        if (bulletPrefab != null)
        {
            CreateBullet();
        }
        else
        {
            // Fallback: create simple bullet
            CreateSimpleBullet();
        }

        Debug.Log($"Shot fired! Bullets remaining: {bulletsInCurrentClip}");
    }

    void CreateBullet()
    {
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);

        // Add Bullet script if it doesn't have one
        Bullet bulletScript = bullet.GetComponent<Bullet>();
        if (bulletScript == null)
        {
            bulletScript = bullet.AddComponent<Bullet>();
        }

        // Set bullet properties
        bulletScript.Initialize(lastFacingDirection, bulletSpeed, enemyLayerMask);
    }

    void CreateSimpleBullet()
    {
        // Create a simple bullet GameObject
        GameObject bullet = new GameObject("Bullet");
        bullet.transform.position = firePoint.position;

        // Add visual (yellow circle)
        SpriteRenderer bulletRenderer = bullet.AddComponent<SpriteRenderer>();
        bulletRenderer.sprite = CreateCircleSprite();
        bulletRenderer.color = Color.yellow;
        bullet.transform.localScale = Vector3.one * 0.2f;

        // Add physics
        Rigidbody2D bulletRb = bullet.AddComponent<Rigidbody2D>();
        bulletRb.gravityScale = 0f;
        bulletRb.linearVelocity = lastFacingDirection * bulletSpeed;

        // Add collider
        CircleCollider2D bulletCollider = bullet.AddComponent<CircleCollider2D>();
        bulletCollider.isTrigger = true;

        // Add bullet script
        Bullet bulletScript = bullet.AddComponent<Bullet>();
        bulletScript.Initialize(lastFacingDirection, bulletSpeed, enemyLayerMask);
    }

    Sprite CreateCircleSprite()
    {
        // Create a simple circle texture for bullet
        Texture2D texture = new Texture2D(32, 32);
        Color[] colors = new Color[32 * 32];

        for (int i = 0; i < colors.Length; i++)
        {
            int x = i % 32;
            int y = i / 32;
            float distance = Vector2.Distance(new Vector2(x, y), new Vector2(16, 16));
            colors[i] = distance <= 16 ? Color.white : Color.clear;
        }

        texture.SetPixels(colors);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));
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

        // Set animation parameters - allow running and attacking/shooting simultaneously
        animator.SetBool("IsMoving", isMoving && !isReloading);
        animator.SetBool("IsRunning", isRunning && isMoving && !isReloading);
        animator.SetBool("IsReloading", isReloading);
        animator.SetBool("IsAttacking", isAttacking);
        animator.SetBool("IsShooting", isShooting);

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

        // Update shoot cooldown
        if (shootCooldown > 0)
        {
            shootCooldown -= Time.deltaTime;
            if (shootCooldown <= 0)
            {
                isShooting = false;
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
        if (isAttacking) return; // Only prevent if already attacking

        // Start attack animation
        isAttacking = true;
        attackCooldown = ATTACK_DURATION;

        Debug.Log($"Manual attack performed! Facing direction: {lastFacingDirection}");
    }

    // Health system methods
    public void TakeDamage(float damage, string source = "Unknown")
    {
        if (healthSystem != null)
        {
            bool damaged = healthSystem.TakeDamage(damage, source);
            if (damaged)
            {
                // Add invulnerability after taking damage
                healthSystem.SetInvulnerable(invulnerabilityDuration);

                Debug.Log($"Player took {damage} damage from {source}");
            }
        }
    }

    public void DecreaseLightIntensity(float decreaseAmount = 1f)
    {
        if (playerLight == null) return;

        currentLightIntensity -= decreaseAmount;
        currentLightIntensity = Mathf.Max(0f, currentLightIntensity); // Don't go below 0

        playerLight.intensity = currentLightIntensity;

        Debug.Log($"Player light intensity decreased by {decreaseAmount}. Current intensity: {currentLightIntensity}");
    }

    public void IncreaseLightIntensity(float increaseAmount = 1f)
    {
        if (playerLight == null) return;

        currentLightIntensity += increaseAmount;
        currentLightIntensity = Mathf.Min(initialLightIntensity, currentLightIntensity); // Don't exceed initial

        playerLight.intensity = currentLightIntensity;

        Debug.Log($"Player light intensity increased by {increaseAmount}. Current intensity: {currentLightIntensity}");
    }

    private void OnPlayerDeath()
    {
        Debug.Log("Player has died! Game Over!");

        // Stop all input
        isInputBlocked = true;

        // Stop movement
        if (rb2d != null)
            rb2d.linearVelocity = Vector2.zero;

        // You can add game over logic here
        // Example: Time.timeScale = 0f; // Pause game
        // Example: Load game over scene
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
        return bulletsInCurrentClip > 0 && !isReloading;
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

    // Add this property for easy access
    public HealthSystem Health => healthSystem;

    void OnDrawGizmosSelected()
    {
        // Draw collision radius
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, collisionRadius);

        // Draw fire point
        if (firePoint != null)
        {
            Gizmos.color = Color.orange;
            Gizmos.DrawWireSphere(firePoint.position, 0.2f);
        }

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
