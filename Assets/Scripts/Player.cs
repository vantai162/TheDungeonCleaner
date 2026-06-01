using System.Collections;
using UnityEngine;

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
    
    [Header("Touch Controls")]
    private Vector2 currentButtonInput = Vector2.zero;
    private bool isButtonPressed = false;

    private bool isMoving;
    private Vector2 origPos;
    private float xInput;
    private float yInput;
    private PlayerBoxInteraction playerBoxInteraction;

    private void Awake()
    {
        playerBoxInteraction = GetComponent<PlayerBoxInteraction>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        ApplySkin();
    }

    private void Update()
    {
        if (isMoving)
            AttemptBufferMove();
        else
            HandleInput();

        HandleFlip();
    }

    public Vector2 GetTouchInputDirection()
    {
        return isButtonPressed ? currentButtonInput : Vector2.zero;
    }

    public Vector2 GetMovementDirection()
    {
        // Adapter Design Pattern: Delegate to active adapter via InputManager
        if (InputManager.instance != null)
        {
            return InputManager.instance.GetMovementDirection();
        }

        // Fallback for editor/testing sandbox scenes without InputManager
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

        Vector2 movementDir = Vector2.zero;
        if (InputManager.instance != null)
        {
            movementDir = InputManager.instance.GetMovementDirection();
        }
        else
        {
            movementDir = isButtonPressed ? currentButtonInput : Vector2.zero;
        }

        // Flip based on either current input or last movement direction
        if (movementDir.x < 0 || (movementDir == Vector2.zero && lastMovementDirection.x < 0))
            transform.localScale = new Vector3(-1, 1, 1);
        else if (movementDir.x > 0 || (movementDir == Vector2.zero && lastMovementDirection.x > 0))
            transform.localScale = new Vector3(1, 1, 1);
    }

    public void ResetState()
    {
        StopAllCoroutines();
        isMoving = false;
        bufferedDirection = Vector2.zero;
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
        timeToMove = newSpeed;
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
}