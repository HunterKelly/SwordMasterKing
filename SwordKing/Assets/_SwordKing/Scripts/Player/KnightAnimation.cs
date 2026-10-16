using UnityEngine;

namespace SwordKing
{
    // Presentation only. Combat owns attack timing, movement, and invulnerability.
    public sealed class KnightAnimation : MonoBehaviour
    {
        public Transform chest, head, leftArm, leftForearm, rightForearm;
        public Transform leftShin, rightShin, frontTabard, backTabard;
        [Range(0, 2)] public float movementAmount = 1f;
        [Range(0, 2)] public float attackAmount = 1f;
        [Min(1)] public float poseResponse = 18f;
        float phase, movement;

        public void Present(Vector2 input, bool sprint, bool airborne, bool rolling,
            bool charging, bool overheadCharge, bool risingHeavy, PlayerAttackKind attack,
            float progress, int swingIndex, Transform leftThigh, Transform rightThigh)
        {
            float dt = Time.deltaTime;
            float blend = 1f - Mathf.Exp(-poseResponse * dt);
            movement = Mathf.Lerp(movement, Mathf.Clamp01(input.magnitude), blend);
            phase += movement * (sprint ? 15f : 10f) * dt;
            float stride = Mathf.Sin(phase) * movement * movementAmount;
            float bounce = Mathf.Abs(Mathf.Cos(phase)) * movement;
            bool attacking = progress >= 0 && progress < 1;
            bool overhead = attack == PlayerAttackKind.Overhead || attack == PlayerAttackKind.ChargedOverhead
                || attack == PlayerAttackKind.JumpingOverhead;
            float strike = attacking ? Mathf.Sin(Mathf.Clamp01(progress) * Mathf.PI) * attackAmount : 0;
            float twist = attacking && attack == PlayerAttackKind.Slash
                ? Mathf.Lerp(-25, 25, Mathf.SmoothStep(0, 1, progress)) * (swingIndex % 2 == 0 ? 1 : -1) : 0;
            float lean = (sprint ? 12 * movement : 4 * movement)
                + Mathf.Sin(Time.time * 1.8f) * .7f * (1 - movement);
            if (charging) lean = overheadCharge ? -12 : -6;
            if (attacking && overhead) lean += Mathf.Lerp(-18, 26, progress) * attackAmount;
            if (attacking && attack == PlayerAttackKind.Thrust) lean += strike * 18;
            if (rolling) lean = 22;
            Pose(chest, new Vector3(lean, twist, -input.x * movement * 4), blend);
            Pose(head, new Vector3(-lean * .4f, -twist * .4f, 0), blend);
            Pose(leftArm, new Vector3(rolling ? -65 : charging || risingHeavy ? -65 : -stride * 22 - strike * 30,
                0, rolling ? -10 : -12), blend);
            Pose(leftForearm, new Vector3(rolling ? -95 : sprint ? -70 : -35 - strike * 35, 0, 0), blend);
            // Sword pivot remains controlled by PlayerPresentation; this only bends the visible elbow.
            Pose(rightForearm, new Vector3(0, 0, attacking && attack == PlayerAttackKind.Thrust ? 0 : -8), blend);
            if (!rolling)
            {
                float direction = input.y < -.1f ? -1 : 1;
                Pose(leftThigh, new Vector3(airborne ? -35 : stride * (sprint ? 42 : 30) * direction, 0, -2), blend);
                Pose(rightThigh, new Vector3(airborne ? -55 : -stride * (sprint ? 42 : 30) * direction, 0, 2), blend);
            }
            Pose(leftShin, new Vector3(rolling ? 105 : airborne ? 65 : Mathf.Max(0, -stride) * 65, 0, 0), blend);
            Pose(rightShin, new Vector3(rolling ? 105 : airborne ? 85 : Mathf.Max(0, stride) * 65, 0, 0), blend);
            Pose(frontTabard, new Vector3(-8 - bounce * 14 - (rolling ? 30 : 0), 0, stride * 5), blend);
            Pose(backTabard, new Vector3(8 + movement * (sprint ? 35 : 15) + Mathf.Sin(phase + .8f) * movement * 6,
                0, -stride * 5), blend);
        }

        static void Pose(Transform joint, Vector3 angles, float blend)
        {
            if (joint != null) joint.localRotation = Quaternion.Slerp(joint.localRotation, Quaternion.Euler(angles), blend);
        }
    }
}
