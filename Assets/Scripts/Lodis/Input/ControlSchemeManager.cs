using UnityEngine;
using UnityEngine.InputSystem;

public class ControlSchemeManager : MonoBehaviour
{
    public PlayerInput playerInput;

    public void SwitchToKeyboard(bool disableOnFail)
    {
        if (Keyboard.current != null)
            playerInput.SwitchCurrentControlScheme("Keyboard", Keyboard.current, Mouse.current);
        else if (disableOnFail)
            playerInput.enabled = false;
    }

    public void SwitchToGamepad(bool disableOnFail)
    {
        if (Gamepad.current != null)
            playerInput.SwitchCurrentControlScheme("Gamepad", Gamepad.current);
        else if (disableOnFail)
            playerInput.enabled = false;
    }
}
