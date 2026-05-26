using UnityEngine;

public class OneDirectionStrategy : IBoxMovementStrategy
{
    public Vector2 AllowedDirection { get; private set; }

    public OneDirectionStrategy()
    {
        AllowedDirection = Vector2.right;
    }

    public OneDirectionStrategy(Vector2 direction)
    {
        AllowedDirection = direction.normalized;
    }

    public bool CanMove(Vector2 moveDirection)
    {
        return moveDirection == AllowedDirection || moveDirection == AllowedDirection.normalized;
    }
}
