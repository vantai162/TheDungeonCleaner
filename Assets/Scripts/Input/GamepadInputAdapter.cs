using UnityEngine;

/// <summary>
/// Concrete Adapter in the Adapter Pattern.
/// Adapts Controller / Gamepad inputs (Joystick buttons and axes) to the IInputAdapter interface.
/// </summary>
public class GamepadInputAdapter : IInputAdapter
{
    private float lastAxisX = 0f;
    private float lastAxisY = 0f;
    private const float threshold = 0.5f;

    public Vector2 GetMovementDirection()
    {
        Vector2 direction = Vector2.zero;

        // In Unity, D-Pad or Left Stick usually map to "Horizontal" and "Vertical"
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        // Simple threshold check for analog stick / D-pad
        if (Mathf.Abs(v) > threshold)
        {
            direction.y = Mathf.Sign(v);
        }
        else if (Mathf.Abs(h) > threshold)
        {
            direction.x = Mathf.Sign(h);
        }

        return direction;
    }

    public bool IsGrabPressed()
    {
        // Xbox A / PlayStation Cross (JoystickButton0) or Xbox X / PlayStation Square (JoystickButton2)
        return Input.GetKeyDown(KeyCode.JoystickButton0) || Input.GetKeyDown(KeyCode.JoystickButton2);
    }

    public bool IsUndoPressed()
    {
        // Xbox B / PlayStation Circle (JoystickButton1)
        return Input.GetKeyDown(KeyCode.JoystickButton5);
    }

    public bool IsResetPressed()
    {
        // Xbox Y / PlayStation Triangle (JoystickButton3)
        return Input.GetKeyDown(KeyCode.JoystickButton6);
    }

    public bool IsPausePressed()
    {
        // Start button (JoystickButton7)
        return Input.GetKeyDown(KeyCode.JoystickButton7);
    }
}
