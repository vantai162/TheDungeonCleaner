using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerBoxInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private KeyCode grabToggleKey = KeyCode.H;
    [SerializeField] private KeyCode undoKey = KeyCode.U;
    [SerializeField] private KeyCode resetKey = KeyCode.R;
    [SerializeField] private float detectionDistance = 0.6f;
    [SerializeField] private float detectionRadius = 0.3f;
    [SerializeField] private LayerMask boxLayer;
    private Vector2 lastProcessedDragInput = Vector2.zero;
    private bool hasDragInputChanged = false;
    private Vector2 savedLastInputDirection;
    public bool isBoxHighlighted => highlightedBox != null;
    public bool hasGrabbableBoxInRange => highlightedBox != null;

    [Header("Movement Settings")]
    [SerializeField] private float moveDuration = 0.135f;
    [SerializeField] private float bounceDistance = 0.25f;
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private float grabBufferWindow = 0.135f;
    [SerializeField] private float releaseBufferWindow = 0.135f;

    private Box currentBox;
    private Box highlightedBox;
    public bool isDragging = false;
    private Vector2 lastInputDirection = Vector2.right;
    private Vector2 grabDirection = Vector2.zero;
    public bool isMoving = false;
    
    private bool bufferedGrab = false;
    private float bufferedGrabActivated = 0f;
    private bool bufferedRelease = false;
    private float bufferedReleaseActivated = 0f;

    private Transform playerTransform;
    private Player playerComponent;
    
    private Stack<(Vector3 playerPos, Vector2 facingDirection, Box box, Vector3? boxPos)> undoStack = new();

    private void Awake()
    {
        playerTransform = transform;
        playerComponent = GetComponent<Player>();
    }

    private void Start()
    {
        SaveState(null, null);
    }

    private void Update()
    {
        if (Input.GetKeyDown(grabToggleKey))
        {
            if (isDragging)
            {
                if (!isMoving)
                    ReleaseBox();
                else
                {
                    bufferedRelease = true;
                    bufferedReleaseActivated = Time.time;
                }
            }
            else
            {
                if (!isMoving)
                    TryGrabBox();
                else
                {
                    bufferedGrab = true;
                    bufferedGrabActivated = Time.time;
                }
            }
        }
        
        if (!isMoving && bufferedGrab && Time.time < bufferedGrabActivated + grabBufferWindow)
        {
            TryGrabBox();
            bufferedGrab = false;
        }
        
        if (!isMoving && bufferedRelease && Time.time < bufferedReleaseActivated + releaseBufferWindow)
        {
            ReleaseBox();
            bufferedRelease = false;
        }

        if (!isDragging && !isMoving)
        {
            // Vector2 input = Vector2.zero;
            // PC keyboard input
            // if (Input.GetKey(KeyCode.W))
            //     input.y = 1;
            // else if (Input.GetKey(KeyCode.S))
            //     input.y = -1;
            // if (Input.GetKey(KeyCode.D))
            //     input.x = 1;
            // else if (Input.GetKey(KeyCode.A))
            //     input.x = -1;
            
            // joystick input
            Vector2 input = playerComponent.lastMovementDirection;

            if (input != Vector2.zero)
                lastInputDirection = input;

            HighlightGrabbableBox();
        }

        if (Input.GetKeyDown(undoKey) && !isMoving)
        {
            UndoMove();
        }

        if (Input.GetKeyDown(resetKey))
        {
            ResetLevel();
        }
        
        if (isDragging && !isMoving)
        {
            Vector2 direction = playerComponent.GetMovementDirection();
            
            if (direction != Vector2.zero)
            {
                Vector2 allowedDir = -grabDirection;
                
                if (direction == allowedDir)
                {
                    StartCoroutine(DragMove(new Vector3(direction.x, direction.y, 0)));
                }
            }
        }
    }

    private void HighlightGrabbableBox()
    {
        Vector3 detectPoint = playerTransform.position + (Vector3)(lastInputDirection.normalized * detectionDistance);
        Collider2D hit = Physics2D.OverlapCircle(detectPoint, detectionRadius, boxLayer);

        if (hit && hit.CompareTag("Box"))
        {
            Box box = hit.GetComponent<Box>();
            if (!box || box == highlightedBox)
                return;
            if (highlightedBox)
                highlightedBox.SetOutlineColor(false);

            highlightedBox = box;
            highlightedBox.SetOutlineColor(true);
        }
        else if (highlightedBox)
        {
            highlightedBox.SetOutlineColor(false);
            highlightedBox = null;
        }
    }

    public void TryGrabBox()
    {
        if (lastInputDirection == Vector2.zero)
        {
            AudioManager.instance.PlaySFX(0);
            return;
        }

        Vector3 detectPoint = playerTransform.position + (Vector3)(lastInputDirection.normalized * detectionDistance);
        Collider2D hit = Physics2D.OverlapCircle(detectPoint, detectionRadius, boxLayer);
        if (!hit || !hit.CompareTag("Box"))
            return;
        
        AudioManager.instance.PlaySFX(10);
        currentBox = hit.GetComponent<Box>();
        grabDirection = lastInputDirection.normalized;
        isDragging = true;
        
        savedLastInputDirection = lastInputDirection;

        currentBox.SetDraggingState(true);
        playerComponent.SetDraggingMode(true);
    }

    public void ReleaseBox()
    {
        AudioManager.instance.PlaySFX(11);
        isDragging = false;
        
        lastInputDirection = savedLastInputDirection;
        
        playerComponent.SetDraggingMode(false);
        
        playerComponent.SetDirectionalInput(Vector2.zero, false);
        
        playerComponent.lastMovementDirection = savedLastInputDirection;
        
        if (savedLastInputDirection.x < 0)
            playerTransform.localScale = new Vector3(-1, 1, 1);
        else if (savedLastInputDirection.x > 0)
            playerTransform.localScale = new Vector3(1, 1, 1);
        
        currentBox.SetDraggingState(false);
        currentBox.SetOutlineColor(true);
        highlightedBox = currentBox;
        currentBox = null;
        
        grabDirection = Vector2.zero;
    }

    private IEnumerator DragMove(Vector3 direction)
    {
        isMoving = true;
        Vector3 startPosPlayer = playerTransform.position;
        Vector3 startPosBox = currentBox.transform.position;
        Vector3 endPosPlayer = startPosPlayer + direction;

        bool playerBlockedByWall = Physics2D.OverlapCircle(endPosPlayer, 0.1f, wallLayer);

        Collider2D[] playerPathColliders = Physics2D.OverlapCircleAll(endPosPlayer, 0.1f, boxLayer);
        bool playerBlockedByBox = playerPathColliders.Any(col => col.gameObject != currentBox.gameObject);

        bool isBlocked = playerBlockedByWall || playerBlockedByBox;

        // Check box movement strategy
        bool boxCanMove = currentBox.CanMoveInDirection(direction);

        if (isBlocked || !boxCanMove)
        {
            Vector3 bouncePos = startPosPlayer + direction * bounceDistance;
            AudioManager.instance.PlaySFX(0);
            yield return MoveBoth(startPosPlayer, bouncePos, moveDuration * 0.33f);
            yield return MoveBoth(bouncePos, startPosPlayer, moveDuration * 0.33f);
        }
        else
        {
            SaveState(currentBox, startPosBox);
            AudioManager.instance.PlaySFX(7);
            yield return MoveBoth(startPosPlayer, endPosPlayer, moveDuration);
        }
        
        isMoving = false;
    }

    private IEnumerator MoveBoth(Vector3 startPlayer, Vector3 endPlayer, float duration)
    {
        float elapsed = 0f;
        Vector3 startBox = currentBox.transform.position;
        Vector3 endBox = startBox + (endPlayer - startPlayer);

        playerComponent.PlayDustEffect();
        currentBox.PlayDustEffect();

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            playerTransform.position = Vector3.Lerp(startPlayer, endPlayer, t);
            currentBox.transform.position = Vector3.Lerp(startBox, endBox, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        playerTransform.position = endPlayer;
        currentBox.transform.position = endBox;
    }

    public void SaveState(Box box, Vector3? boxPos)
    {
        undoStack.Push((playerTransform.position, lastInputDirection, box, boxPos));
    }

    public void UndoMove()
    {
        if (undoStack.Count <= 0)
            return;

        AudioManager.instance.PlaySFX(8);

        if (isDragging)
        {
            isDragging = false;
            if (currentBox != null)
            {
                currentBox.SetDraggingState(false); // Make sure box state is reset
                currentBox.SetOutlineColor(true);
            }
            currentBox = null;
            grabDirection = Vector2.zero;
            playerComponent.SetDraggingMode(false); // Fix: properly reset player dragging state
            playerComponent.SetDirectionalInput(Vector2.zero, false); // Reset any active inputs
        }

        var (playerPos, facingDirection, box, boxPos) = undoStack.Pop();
        playerTransform.position = playerPos;
        lastInputDirection = facingDirection;

        if (lastInputDirection.x < 0)
            playerTransform.localScale = new Vector3(-1, 1, 1);
        else if (lastInputDirection.x > 0)
            playerTransform.localScale = new Vector3(1, 1, 1);

        if (box && boxPos.HasValue)
            box.transform.position = boxPos.Value;
    }

    public void ResetLevel()
    {
        AudioManager.instance.PlaySFX(9);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ResetState()
    {
        StopAllCoroutines();
        isDragging = false;
        isMoving = false;
        currentBox = null;
        highlightedBox = null;

        if (playerComponent != null)
            playerComponent.enabled = true;

        enabled = true;
    }
    
    public void SetMoveDuration(float newDuration)
    {
        moveDuration = newDuration;
    }
    
    public void BufferGrabAction()
    {
        bufferedGrab = true;
        bufferedGrabActivated = Time.time;
    }

    public void BufferReleaseAction()
    {
        bufferedRelease = true;
        bufferedReleaseActivated = Time.time;
    }
}