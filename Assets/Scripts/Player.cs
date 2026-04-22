using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float timeToMove = 0.135f;
    [SerializeField] private float bounceDistance = 0.25f;
    [SerializeField] private float bounceTime = 0.05f;
    [SerializeField] private float bufferMoveWindow = 0.065f;
    private float bufferMoveActivated = -1f;
    private Vector2 bufferedDirection = Vector2.zero;
    public Vector2 lastMovementDirection = Vector2.right;
    private bool isDraggingMode = false;

    [Header("Collision")]
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private LayerMask boxCollisionLayer;

    [Header("Player Visuals")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite[] availableSkins;
    [SerializeField] private ParticleSystem dustFx;

    [Header("Critical Visuals")]
    [SerializeField] private Color criticalBlinkColor = Color.red;
    [SerializeField] private float criticalBlinkInterval = 0.2f;
    private Coroutine criticalBlinkRoutine;
    private Color defaultSpriteColor = Color.white;

    [Header("Death Visuals")]
    [SerializeField] private Color deadTintColor = new(0.5f, 0.5f, 0.5f, 1f);
    [SerializeField] private float deathFadeDuration = 0.5f;
    [SerializeField] private float deathScaleMultiplier = 0.6f;
    [SerializeField] private float deathScaleDuration = 0.5f;
    [SerializeField] private float deathLoseUIDelay = 0.35f;
    private Coroutine deathRoutine;
    private Vector3 defaultScale;

    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float healthDrainPerSecond = 1f;
    [SerializeField, Range(0f, 1f)] private float criticalThreshold = 0.3f;
    [SerializeField] private float criticalSpeedMultiplier = 0.75f;
    [SerializeField] private UI_HealthBar healthBar;

    [Header("Touch Controls")]
    private Vector2 currentButtonInput = Vector2.zero;
    private bool isButtonPressed = false;

    private bool isMoving;
    private Vector2 origPos;
    private float xInput;
    private float yInput;
    private PlayerBoxInteraction playerBoxInteraction;


    // Health management variables
    private float baseTimeToMove;
    private float currentSpeedMultiplier = 1f;
    private float currentHealth;
    private float healthDrainTimer;
    private bool inputEnabled = true;

    // State management
    private IPlayerState currentState;
    private readonly IPlayerState healthyState = new PlayerHealthyState();
    private readonly IPlayerState criticalState = new PlayerCriticalState();
    private readonly IPlayerState deadState = new PlayerDeadState();



    // Properties for state access
    public IPlayerState HealthyState => healthyState;
    public IPlayerState CriticalState => criticalState;
    public IPlayerState DeadState => deadState;

    // Properties for health and status
    public bool IsDead => currentHealth <= 0f;
    public bool IsCriticalHealth => currentHealth > 0f && currentHealth <= maxHealth * criticalThreshold;
    public float CriticalSpeedMultiplier => criticalSpeedMultiplier;



    private void Awake()
    {
        playerBoxInteraction = GetComponent<PlayerBoxInteraction>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        baseTimeToMove = timeToMove;

        if (spriteRenderer != null)
            defaultSpriteColor = spriteRenderer.color;

        defaultScale = transform.localScale;
    }

    private void Start()
    {
        ApplySkin();
        InitializeHealth();
    }

    private void Update()
    {
        UpdateHealth(Time.deltaTime);
        currentState?.Tick(this);

        if (!inputEnabled)
            return;

        if (isMoving)
            AttemptBufferMove();
        else
            HandleInput();

        HandleFlip();
    }

    public Vector2 GetMovementDirection()
    {
        if (isButtonPressed)
            return currentButtonInput;

#if UNITY_EDITOR
        if (Input.GetKey(KeyCode.W))
            return Vector2.up;
        else if (Input.GetKey(KeyCode.S))
            return Vector2.down;
        else if (Input.GetKey(KeyCode.D))
            return Vector2.right;
        else if (Input.GetKey(KeyCode.A))
            return Vector2.left;
#endif

        return Vector2.zero;
    }

    private void HandleInput()
    {
        Vector2 direction = GetMovementDirection();
        if (direction == Vector2.zero) return;

        lastMovementDirection = direction;

        if (!isDraggingMode)
        {
            bufferMoveActivated = Time.time;
            StartCoroutine(MovePlayer(direction));
        }
    }

    private void AttemptBufferMove()
    {
        if (bufferedDirection != Vector2.zero && Time.time < bufferMoveActivated + bufferMoveWindow)
        {
            Vector2 moveDir = bufferedDirection;
            bufferedDirection = Vector2.zero;
            StartCoroutine(MovePlayer(moveDir));
        }
    }

    private IEnumerator MovePlayer(Vector2 direction)
    {
        isMoving = true;
        origPos = new Vector2(transform.position.x, transform.position.y);

        Vector2 targetPos = origPos + direction;
        Vector3 targetPosition = new Vector3(targetPos.x, targetPos.y, transform.position.z);

        RaycastHit2D hitWall = Physics2D.Raycast(transform.position, direction, 1f, wallLayer);
        RaycastHit2D hitBox = Physics2D.Raycast(transform.position, direction, 1f, boxCollisionLayer);
        bool canMove = (hitWall.collider == null && hitBox.collider == null);

        if (canMove)
        {
            AudioManager.instance.PlaySFX(1);
            dustFx.Play();
            playerBoxInteraction.SaveState(null, null);
            yield return PerformMove(transform.position, targetPosition, timeToMove);
        }
        else
        {
            AudioManager.instance.PlaySFX(0);
            yield return PerformBounce(transform.position, direction);
        }
        isMoving = false;
    }

    private IEnumerator PerformMove(Vector3 start, Vector3 end, float duration)
    {
        float elapsedTime = 0;
        while (elapsedTime < duration)
        {
            transform.position = Vector3.Lerp(start, end, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        transform.position = end;
    }

    private IEnumerator PerformBounce(Vector3 startPos, Vector2 direction)
    {
        Vector2 bouncePos2D = new Vector2(startPos.x, startPos.y) + direction * bounceDistance;
        Vector3 bouncePos = new Vector3(bouncePos2D.x, bouncePos2D.y, startPos.z);
        yield return PerformMove(startPos, bouncePos, bounceTime);
        yield return PerformMove(bouncePos, startPos, bounceTime);
    }

    private void HandleFlip()
    {
        if (isDraggingMode)
            return;

        // Flip based on either current input or last movement direction
        if (currentButtonInput.x < 0 || (currentButtonInput == Vector2.zero && lastMovementDirection.x < 0))
            transform.localScale = new Vector3(-1, 1, 1);
        else if (currentButtonInput.x > 0 || (currentButtonInput == Vector2.zero && lastMovementDirection.x > 0))
            transform.localScale = new Vector3(1, 1, 1);
    }

    public void StartDeathSequence()
    {
        if (deathRoutine != null)
            return;

        deathRoutine = StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        StopCriticalBlink();

        Color startColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
        Vector3 startScale = transform.localScale;
        Vector3 targetScale = startScale * deathScaleMultiplier;

        float time = 0f;
        float duration = Mathf.Max(deathFadeDuration, deathScaleDuration);

        while (time < duration)
        {
            time += Time.deltaTime;
            float tFade = Mathf.Clamp01(time / deathFadeDuration);
            float tScale = Mathf.Clamp01(time / deathScaleDuration);

            if (spriteRenderer != null)
                spriteRenderer.color = Color.Lerp(startColor, deadTintColor, tFade);

            transform.localScale = Vector3.Lerp(startScale, targetScale, tScale);
            yield return null;
        }

        if (deathLoseUIDelay > 0f)
            yield return new WaitForSeconds(deathLoseUIDelay);

        if (GameManager.instance != null)
            GameManager.instance.ShowLoseUI();
    }

    public void ResetState()
    {
        StopAllCoroutines();
        isMoving = false;
        bufferedDirection = Vector2.zero;
        inputEnabled = true;

        currentHealth = maxHealth;
        healthDrainTimer = 0f;
        UpdateHealthUI(currentHealth);
        StopCriticalBlink();

        if (spriteRenderer != null)
            spriteRenderer.color = defaultSpriteColor;

        transform.localScale = defaultScale;
        deathRoutine = null;

        SetState(healthyState);

        if (playerBoxInteraction != null)
            playerBoxInteraction.enabled = true;

        enabled = true;
    }

    public void PlayDustEffect() => dustFx.Play();

    private void ApplySkin()
    {
        if (SkinManager.instance == null || spriteRenderer == null || availableSkins == null || availableSkins.Length == 0)
            return;
        int skinId = SkinManager.instance.GetSkinId();
        if (skinId >= 0 && skinId < availableSkins.Length)
            spriteRenderer.sprite = availableSkins[skinId];
    }

    public void SetMovementSpeed(float newSpeed)
    {
        baseTimeToMove = newSpeed;
        timeToMove = baseTimeToMove * currentSpeedMultiplier;
    }

    public void SetDraggingMode(bool isDragging)
    {
        isDraggingMode = isDragging;
    }

    public void SetDirectionalInput(Vector2 direction, bool pressed)
    {
        currentButtonInput = direction;
        isButtonPressed = pressed;
    }

    public void SetState(IPlayerState newState)
    {
        if (newState == null || currentState == newState)
            return;

        currentState?.Exit(this);
        currentState = newState;
        currentState.Enter(this);
    }

    public void SetSpeedMultiplier(float multiplier)
    {
        currentSpeedMultiplier = Mathf.Max(0.01f, multiplier);
        timeToMove = baseTimeToMove * currentSpeedMultiplier;
    }

    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;
        if (playerBoxInteraction != null)
            playerBoxInteraction.enabled = enabled;
    }

    public void StopMovement()
    {
        StopAllCoroutines();
        isMoving = false;
        bufferedDirection = Vector2.zero;
    }

    public void StartCriticalBlink()
    {
        if (criticalBlinkRoutine != null || spriteRenderer == null)
            return;

        criticalBlinkRoutine = StartCoroutine(CriticalBlinkRoutine());
    }

    public void StopCriticalBlink()
    {
        if (criticalBlinkRoutine != null)
        {
            StopCoroutine(criticalBlinkRoutine);
            criticalBlinkRoutine = null;
        }

        if (spriteRenderer != null)
            spriteRenderer.color = defaultSpriteColor;
    }

    private IEnumerator CriticalBlinkRoutine()
    {
        while (true)
        {
            spriteRenderer.color = criticalBlinkColor;
            yield return new WaitForSeconds(criticalBlinkInterval);
            spriteRenderer.color = defaultSpriteColor;
            yield return new WaitForSeconds(criticalBlinkInterval);
        }
    }

    private void InitializeHealth()
    {
        currentHealth = maxHealth;
        healthDrainTimer = 0f;

        if (healthBar != null)
            healthBar.SetMaxHealth(maxHealth);

        SetState(healthyState);
    }

    private void UpdateHealth(float deltaTime)
    {
        if (currentHealth <= 0f)
            return;

        healthDrainTimer += deltaTime;

        if (healthDrainTimer >= 1f)
        {
            int ticks = Mathf.FloorToInt(healthDrainTimer);
            healthDrainTimer -= ticks;
            ModifyHealth(-healthDrainPerSecond * ticks);
        }
    }

    private void ModifyHealth(float delta)
    {
        currentHealth = Mathf.Clamp(currentHealth + delta, 0f, maxHealth);
        UpdateHealthUI(currentHealth);
        UpdateState();
    }

    private void UpdateState()
    {
        if (IsDead)
            SetState(deadState);
        else if (IsCriticalHealth)
            SetState(criticalState);
        else
            SetState(healthyState);
    }

    private void UpdateHealthUI(float health)
    {
        if (healthBar != null)
            healthBar.SetHealth(health);
    }
}