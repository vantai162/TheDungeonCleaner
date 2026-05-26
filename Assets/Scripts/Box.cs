using UnityEngine;

public enum BoxState { Default, Highlighted, Dragging, OnPoint }

public enum MovementStrategyType { Free, OneDirection }

public class Box : MonoBehaviour
{
    public Color boxColor = Color.white;
    private bool isOnBoxPoint = false;
    private bool isDragging = false;

    [Header("Box Visuals")]
    [SerializeField] private Sprite blackOutlineSprite;
    [SerializeField] private Sprite whiteOutlineSprite;
    [SerializeField] private Sprite greenOutlineSprite;
    [SerializeField] private Sprite orangeOutlineSprite;
    [SerializeField] private ParticleSystem dustFx;
    [SerializeField] private float darknessFactor = 0.935f;

    [Header("Movement Strategy")]
    [SerializeField] private MovementStrategyType strategyType = MovementStrategyType.Free;
    [SerializeField] private Vector2 oneDirection = Vector2.right;

    private IBoxMovementStrategy currentStrategy;
    private SpriteRenderer spriteRenderer;
    private BoxState currentState = BoxState.Default;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        InitializeStrategy();
    }

    private void InitializeStrategy()
    {
        switch (strategyType)
        {
            case MovementStrategyType.OneDirection:
                currentStrategy = new OneDirectionStrategy(oneDirection);
                break;
            case MovementStrategyType.Free:
            default:
                currentStrategy = new FreeMovementStrategy();
                break;
        }
    }

    private void Start()
    {
        UpdateOutlineSprite();
    }

    public void SetMovementStrategy(IBoxMovementStrategy strategy)
    {
        if (strategy != null)
            currentStrategy = strategy;
    }

    public IBoxMovementStrategy GetMovementStrategy()
    {
        return currentStrategy;
    }

    public bool CanMoveInDirection(Vector2 direction)
    {
        if (currentStrategy == null)
            return true;
        return currentStrategy.CanMove(direction);
    }

    public MovementStrategyType GetStrategyType() => strategyType;
    public Vector2 GetOneDirection() => oneDirection;
    
    public void SetOutlineColor(bool isWhite) 
    {
        if (!isOnBoxPoint)
        {
            currentState = isWhite ? BoxState.Highlighted : BoxState.Default;
            UpdateOutlineSprite();
        }
    }
    
    public void SetDraggingState(bool isDragging)
    {
        this.isDragging = isDragging;
        
        if (isDragging)
        {
            // Don't change the state if on a box point, just apply darkness
            if (!isOnBoxPoint)
                currentState = BoxState.Dragging;
        }
        else
        {
            // When released, return to OnPoint state if it's on a point
            currentState = isOnBoxPoint ? BoxState.OnPoint : BoxState.Default;
        }
        
        UpdateOutlineSprite();
    }
    
    public void SetOnPointState(bool isOnPoint)
    {
        isOnBoxPoint = isOnPoint;
        
        if (isOnPoint)
        {
            currentState = BoxState.OnPoint;
        }
        else
        {
            currentState = isDragging ? BoxState.Dragging : BoxState.Default;
        }
        
        UpdateOutlineSprite();
    }
    
    private void UpdateOutlineSprite()
    {
        // First, set the appropriate sprite based on state
        switch (currentState)
        {
            case BoxState.Default:
                spriteRenderer.sprite = blackOutlineSprite;
                break;
            case BoxState.Highlighted:
                spriteRenderer.sprite = whiteOutlineSprite;
                break;
            case BoxState.Dragging:
                spriteRenderer.sprite = orangeOutlineSprite;
                break;
            case BoxState.OnPoint:
                spriteRenderer.sprite = greenOutlineSprite;
                break;
        }
        
        // Then apply darkness if being dragged
        if (isDragging)
        {
            spriteRenderer.color = new Color(darknessFactor, darknessFactor, darknessFactor, 1f);
        }
        else
        {
            spriteRenderer.color = Color.white;
        }
    }
    
    public void PlayDustEffect() => dustFx.Play();
}
