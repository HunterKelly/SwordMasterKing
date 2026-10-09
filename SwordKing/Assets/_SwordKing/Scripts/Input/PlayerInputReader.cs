using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SwordKing
{
    public struct PlayerInputFrame
    {
        public Vector2 Move, Look;
        public bool Attack, AttackHeld, AttackReleased, HeavyAttack, HeavyHeld, HeavyReleased;
        public bool ShiftHeld, Pause, Jump, Roll, Interact, Heal, TestLow, TestHigh;
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
                frame.ShiftHeld = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
                frame.Interact = keyboard.eKey.wasPressedThisFrame; frame.Heal = keyboard.qKey.wasPressedThisFrame;
                frame.TestLow = keyboard.lKey.wasPressedThisFrame; frame.TestHigh = keyboard.hKey.wasPressedThisFrame;
            }
            if (mouse != null)
            {
                frame.Look = mouse.delta.ReadValue() * mouseSensitivity;
                frame.Attack = mouse.leftButton.wasPressedThisFrame;
                frame.AttackHeld = mouse.leftButton.isPressed; frame.AttackReleased = mouse.leftButton.wasReleasedThisFrame;
                frame.HeavyAttack = mouse.rightButton.wasPressedThisFrame;
                frame.HeavyHeld = mouse.rightButton.isPressed; frame.HeavyReleased = mouse.rightButton.wasReleasedThisFrame;
            }
#else
            frame.Move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            frame.Look = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * (mouseSensitivity / .06f);
            frame.Attack = Input.GetMouseButtonDown(0);
            frame.AttackHeld = Input.GetMouseButton(0); frame.AttackReleased = Input.GetMouseButtonUp(0);
            frame.HeavyAttack = Input.GetMouseButtonDown(1); frame.HeavyHeld = Input.GetMouseButton(1); frame.HeavyReleased = Input.GetMouseButtonUp(1);
            frame.Pause = Input.GetKeyDown(KeyCode.Escape);
            frame.Jump = Input.GetKeyDown(KeyCode.Space);
            frame.ShiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            frame.Interact = Input.GetKeyDown(KeyCode.E); frame.Heal = Input.GetKeyDown(KeyCode.Q);
            frame.TestLow = Input.GetKeyDown(KeyCode.L); frame.TestHigh = Input.GetKeyDown(KeyCode.H);
#endif
            return frame;
        }
    }
}
