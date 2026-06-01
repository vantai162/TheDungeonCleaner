using UnityEngine;

/// <summary>
/// Target Interface in the Adapter Pattern.
/// Defines all input queries that the player and game systems need,
/// abstracting them away from specific hardware APIs.
/// </summary>
public interface IInputAdapter
{
    /// <summary>
    /// Gets the current movement input vector (normalized or raw direction).
    /// </summary>
    Vector2 GetMovementDirection();

    /// <summary>
    /// Checks if the Grab/Release box action key/button was pressed this frame.
    /// </summary>
    bool IsGrabPressed();

    /// <summary>
    /// Checks if the Undo action key/button was pressed this frame.
    /// </summary>
    bool IsUndoPressed();

    /// <summary>
    /// Checks if the Reset Level action key/button was pressed this frame.
    /// </summary>
    bool IsResetPressed();

    /// <summary>
    /// Checks if the Pause Menu action key/button was pressed this frame.
    /// </summary>
    bool IsPausePressed();
}
