using UnityEngine.InputSystem;

public static class OneButton
{
    public static bool Pressed =>
        (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) ||
        (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) ||
        (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame);
}