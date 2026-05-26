using UnityEngine;

public interface IBoxMovementStrategy
{
    bool CanMove(Vector2 moveDirection);
}
