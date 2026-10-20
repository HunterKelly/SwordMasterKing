using UnityEngine;

namespace SwordKing
{
    public static class GroundedKnockback
    {
        // Only thrust slides use this path. Capsule depenetration can add upward
        // movement even when the requested impulse has no Y component.
        public static void Move(CharacterController controller, Vector3 displacement, float dt)
        {
            if (controller == null || !controller.enabled) return;
            var root = controller.transform;
            float startHeight = root.position.y;
            float step = controller.stepOffset, slope = controller.slopeLimit;
            controller.stepOffset = 0;
            controller.slopeLimit = 0;
            try
            {
                displacement.y = -Mathf.Max(0, dt) * 3;
                controller.Move(displacement);
                if (root.position.y > startHeight)
                {
                    // Retain wall-blocked horizontal motion and normal downward
                    // grounding, but reject a capsule-induced launch.
                    Vector3 grounded = root.position;
                    grounded.y = startHeight;
                    controller.enabled = false;
                    root.position = grounded;
                    controller.enabled = true;
                }
            }
            finally
            {
                controller.stepOffset = step;
                controller.slopeLimit = slope;
            }
        }
    }
}
