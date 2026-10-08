using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SwordKing
{
    public struct PlayerInputFrame
    {
        public Vector2 Move, Look;
        public bool Attack, Pause, Jump, Roll, Interact, Heal, TestLow, TestHigh;
    }

    // One snapshot per frame keeps player, menus, and interactions consistent.
    public sealed class PlayerInputReader : MonoBehaviour
    {
        [SerializeField, Min(0)] float mouseSensitivity = .12f;
        int sampledFrame = -1;
        PlayerInputFrame frame;
        public PlayerInputFrame Read()
        {
            if (sampledFrame == Time.frameCount) return frame;
            sampledFrame = Time.frameCount;
            frame = new PlayerInputFrame();
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard != null)
            {
                frame.Move = new Vector2((keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0),
                    (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0));
                frame.Pause = keyboard.escapeKey.wasPressedThisFrame;
                frame.Jump = keyboard.spaceKey.wasPressedThisFrame;
                frame.Roll = keyboard.leftShiftKey.wasPressedThisFrame || keyboard.rightShiftKey.wasPressedThisFrame;
                frame.Interact = keyboard.eKey.wasPressedThisFrame; frame.Heal = keyboard.qKey.wasPressedThisFrame;
                frame.TestLow = keyboard.lKey.wasPressedThisFrame; frame.TestHigh = keyboard.hKey.wasPressedThisFrame;
            }
            if (mouse != null)
            {
                frame.Look = mouse.delta.ReadValue() * mouseSensitivity;
                frame.Attack = mouse.leftButton.wasPressedThisFrame;
            }
#else
            frame.Move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            frame.Look = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * (mouseSensitivity / .06f);
            frame.Attack = Input.GetMouseButtonDown(0); frame.Pause = Input.GetKeyDown(KeyCode.Escape);
            frame.Jump = Input.GetKeyDown(KeyCode.Space);
            frame.Roll = Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift);
            frame.Interact = Input.GetKeyDown(KeyCode.E); frame.Heal = Input.GetKeyDown(KeyCode.Q);
            frame.TestLow = Input.GetKeyDown(KeyCode.L); frame.TestHigh = Input.GetKeyDown(KeyCode.H);
#endif
            return frame;
        }
    }
}
