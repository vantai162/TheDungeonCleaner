using UnityEngine;

public class FreeMovementStrategy : IBoxMovementStrategy
{
    public bool CanMove(Vector2 moveDirection)
    {
        return true;
    }
}
