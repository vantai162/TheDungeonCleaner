using UnityEngine;

/// <summary>
/// Concrete Adapter in the Adapter Pattern.
/// Adapts PC Keyboard inputs (WASD, Arrows, H, U, R, Escape) to the IInputAdapter interface.
/// </summary>
public class KeyboardInputAdapter : IInputAdapter
{
    public Vector2 GetMovementDirection()
    {
        Vector2 direction = Vector2.zero;

        // Check vertical movement
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            direction.y = 1f;
        else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            direction.y = -1f;

        // Check horizontal movement (only if not moving vertically to maintain grid-like movement)
        if (direction.y == 0f)
        {
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
                direction.x = 1f;
            else if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
                direction.x = -1f;
        }

        return direction;
    }

    public bool IsGrabPressed()
    {
        // Maps Grab/Release to H, E, or Spacebar for maximum convenience on keyboard
        return Input.GetKeyDown(KeyCode.H) || Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space);
    }

    public bool IsUndoPressed()
    {
        // Maps Undo to U or Z
        return Input.GetKeyDown(KeyCode.U) || Input.GetKeyDown(KeyCode.Z);
    }

    public bool IsResetPressed()
    {
        // Maps Reset Level to R
        return Input.GetKeyDown(KeyCode.R);
    }

    public bool IsPausePressed()
    {
        // Maps Pause Menu to Escape
        return Input.GetKeyDown(KeyCode.Escape);
    }
}
