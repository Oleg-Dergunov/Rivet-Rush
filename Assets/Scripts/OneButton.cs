using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public static class OneButton
{
    public static bool Pressed =>
        (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) ||
        (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && !OverUI) ||
        (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame && !OverUI);

    static bool OverUI => EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
}