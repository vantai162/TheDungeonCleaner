using UnityEngine;

/// <summary>
/// Concrete Adapter in the Adapter Pattern.
/// Adapts Mobile Touch screen UI inputs (joysticks / onscreen touch buttons) to the IInputAdapter interface.
/// Direct button interactions (like pressing grab/undo/reset/pause in the mobile UI canvas)
/// can still call their UI_InGame callback methods directly, while movement reads from here.
/// </summary>
public class MobileInputAdapter : IInputAdapter
{
    private Player player;

    public MobileInputAdapter(Player player)
    {
        this.player = player;
    }

    public Vector2 GetMovementDirection()
    {
        if (player != null)
        {
            // Delegates directly to the player's touchscreen-specific state
            return player.GetTouchInputDirection();
        }
        return Vector2.zero;
    }

    // Mobile touchscreen UI buttons have direct onClick event listeners wired in the scene.
    // However, we satisfy the interface to allow fallback or hybrid touch inputs.
    public bool IsGrabPressed() => false;
    public bool IsUndoPressed() => false;
    public bool IsResetPressed() => false;
    public bool IsPausePressed() => false;
}
