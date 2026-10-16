using UnityEngine;

namespace SwordKing
{
    // Presentation only. Combat owns attack timing, movement, and invulnerability.
    public sealed class KnightAnimation : MonoBehaviour
    {
        public Transform chest, head, leftArm, rightArm, leftForearm, rightForearm;
        public Transform leftShin, rightShin, weaponGrip;
        public Transform pelvis, leftFoot, rightFoot;
        [Header("Gait")]
        [Range(.05f, .4f)] public float walkingStride = .28f;
        [Range(.1f, .5f)] public float sprintingStride = .43f;
        [Range(.02f, .2f)] public float walkingFootLift = .07f;
        [Range(.04f, .25f)] public float sprintingFootLift = .13f;
        public Transform capeTop, capeMid, capeHem;
        [Min(.01f)] public float upperArmLength = .285f;
        [Min(.01f)] public float forearmLength = .29f;
        [Range(0, 2)] public float movementAmount = 1f;
        [Range(0, 2)] public float attackAmount = 1f;
        [Min(1)] public float poseResponse = 18f;
        float phase, movement, sprintWeight;
        Vector3 gaitDirection = Vector3.forward;

        public void Present(Vector2 input, bool sprint, bool airborne, bool rolling,
            bool charging, bool overheadCharge, bool risingHeavy, PlayerAttackKind attack,
            float progress, int swingIndex, Transform leftThigh, Transform rightThigh)
        {
            float dt = Time.deltaTime;
            float blend = 1f - Mathf.Exp(-poseResponse * dt);
            movement = Mathf.Lerp(movement, Mathf.Clamp01(input.magnitude), 1f - Mathf.Exp(-8f * dt));
            sprintWeight = Mathf.Lerp(sprintWeight, sprint ? 1 : 0, 1f - Mathf.Exp(-6f * dt));
            if (!airborne && !rolling) phase = Mathf.Repeat(phase + movement * Mathf.Lerp(9.4f, 12.6f, sprintWeight) * dt, Mathf.PI * 2);
            float gaitWeight = airborne || rolling ? 0 : movement * movementAmount;
            float stride = Mathf.Cos(phase) * gaitWeight;
            float weightShift = Mathf.Sin(phase) * gaitWeight;
            float bounce = Mathf.Cos(phase * 2) * gaitWeight;
            float shoulderTurn = stride * 3f;
            if (pelvis != null) Pose(pelvis, new Vector3(0, -stride * 3.5f, weightShift * 1.5f), blend);
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
            Pose(chest, new Vector3(lean, twist + shoulderTurn, -input.x * movement * 3 - weightShift * 1.5f), blend);
            Pose(head, new Vector3(-lean * .4f, -twist * .4f - shoulderTurn * .7f, weightShift), blend);
            Pose(leftArm, new Vector3(rolling ? -65 : charging || risingHeavy ? -65 : stride * 16 - strike * 30,
                0, rolling ? -10 : -12), blend);
            Pose(leftForearm, new Vector3(rolling ? -95 : -18 - sprintWeight * 18 - strike * 35 - Mathf.Max(0, stride) * 7, 0, 0), blend);

            if (!rolling && !airborne && leftFoot != null && rightFoot != null)
            {
                var visuals = leftThigh.parent;
                // Weight transfers over the support leg. The ankles stay on the same floor
                // plane while the pelvis rises/falls; only the recovering foot lifts.
                float drop = Mathf.Lerp(.07f, .135f, sprintWeight) * gaitWeight;
                visuals.localPosition = new Vector3(-weightShift * .012f, .91f - drop - bounce * .006f, 0);
                if (input.sqrMagnitude > .001f)
                {
                    Vector3 localMove = Quaternion.Inverse(visuals.localRotation)
                        * new Vector3(input.x, 0, input.y).normalized;
                    gaitDirection = Vector3.Lerp(gaitDirection, localMove, 1f - Mathf.Exp(-10 * dt));
                }
                AnimateStep(leftThigh, leftShin, leftFoot, visuals, phase / (Mathf.PI * 2), gaitWeight);
                AnimateStep(rightThigh, rightShin, rightFoot, visuals,
                    Mathf.Repeat(phase / (Mathf.PI * 2) + .5f, 1), gaitWeight);
            }
            else
            {
                if (!rolling)
                {
                    Pose(leftThigh, new Vector3(-35, 0, -2), blend);
                    Pose(rightThigh, new Vector3(-55, 0, 2), blend);
                }
                Pose(leftShin, new Vector3(rolling ? 105 : 65, 0, 0), blend);
                Pose(rightShin, new Vector3(rolling ? 105 : 85, 0, 0), blend);
                Pose(leftFoot, new Vector3(rolling ? 0 : -12, 0, 0), blend);
                Pose(rightFoot, new Vector3(rolling ? 0 : -12, 0, 0), blend);
            }
            float breeze = Mathf.Sin(Time.time * 3.2f + phase * .12f);
            float capeLift = rolling ? 25 : airborne ? 16 : movement * (sprint ? 27 : 11);
            Pose(capeTop, new Vector3(capeLift * .35f + breeze * 1.2f, 0, -input.x * 3), blend);
            Pose(capeMid, new Vector3(capeLift * .55f + breeze * 3, 0, stride * 2), blend);
            Pose(capeHem, new Vector3(capeLift * .4f + Mathf.Sin(Time.time * 3.2f - .7f) * (2 + movement * 3),
                0, -stride * 3), blend);
            FitSwordArm(rolling);
            if (!rolling && ((charging && overheadCharge) || risingHeavy || (attacking && overhead)))
            {
                if (weaponGrip != null)
                    SolveArm(leftArm, leftForearm, weaponGrip.position - weaponGrip.forward * .085f,
                        -chest.right, upperArmLength, forearmLength);
            }
        }

        void AnimateStep(Transform hip, Transform knee, Transform ankle, Transform visuals,
            float cycle, float weight)
        {
            const float stanceDuration = .62f;
            bool planted = cycle < stanceDuration;
            float t = planted ? cycle / stanceDuration : (cycle - stanceDuration) / (1 - stanceDuration);
            float travel = Mathf.Lerp(walkingStride, sprintingStride, sprintWeight) * weight;
            float along = planted ? Mathf.Lerp(travel, -travel, t)
                : Mathf.Lerp(-travel, travel, Mathf.SmoothStep(0, 1, t));
            float lift = planted ? 0 : Mathf.Pow(Mathf.Max(0, Mathf.Sin(t * Mathf.PI)), 1.25f)
                * Mathf.Lerp(walkingFootLift, sprintingFootLift, sprintWeight) * weight;
            Vector3 step = gaitDirection * along;
            // Fit the sabatons to the floor while the body shifts over the support leg.
            Vector3 localTarget = new Vector3(hip.localPosition.x - visuals.localPosition.x + step.x,
                .105f - visuals.localPosition.y + lift, .015f + step.z);
            Vector3 target = visuals.TransformPoint(localTarget);
            float upper = knee.localPosition.magnitude, lower = ankle.localPosition.magnitude;
            Vector3 delta = target - hip.position;
            float distance = Mathf.Clamp(delta.magnitude, Mathf.Abs(upper - lower) + .001f, upper + lower - .001f);
            Vector3 forward = delta.normalized;
            Vector3 bend = Vector3.ProjectOnPlane(visuals.forward, forward).normalized;
            float alongBone = (upper * upper - lower * lower + distance * distance) / (2 * distance);
            float outwards = Mathf.Sqrt(Mathf.Max(0, upper * upper - alongBone * alongBone));
            Vector3 joint = hip.position + forward * alongBone + bend * outwards;
            hip.rotation = Quaternion.FromToRotation(knee.localPosition.normalized, joint - hip.position);
            knee.rotation = Quaternion.FromToRotation(ankle.localPosition.normalized, target - knee.position);
            // Heel contact -> flat sole -> toe push-off. Swing clears the toes.
            float footRoll = planted
                ? -10 * (1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0, .20f, t)))
                    + 12 * Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.76f, 1, t))
                : Mathf.Lerp(12, -10, Mathf.SmoothStep(0, 1, t));
            ankle.rotation = visuals.rotation * Quaternion.Euler(footRoll * weight, 0, 0);
        }

        void FitSwordArm(bool rolling)
        {
            if (rightArm == null || rightForearm == null || weaponGrip == null) return;
            var target = weaponGrip.position;
            float reach = Mathf.Max(.02f, upperArmLength + forearmLength - .003f);
            Vector3 offset = target - rightArm.position;
            if (offset.sqrMagnitude > reach * reach)
            {
                // Fit only the visible sword to a human arm. Combat volumes still use PlayerController.
                Vector3 clamped = rightArm.position + offset.normalized * reach;
                weaponGrip.parent.position += clamped - target;
                target = clamped;
            }
            SolveArm(rightArm, rightForearm, target,
                chest.right - chest.forward * (rolling ? 1 : .35f),
                upperArmLength, forearmLength);
        }

        static void SolveArm(Transform shoulder, Transform elbow, Vector3 target,
            Vector3 pole, float upperLength, float lowerLength)
        {
            if (shoulder == null || elbow == null) return;
            Vector3 delta = target - shoulder.position;
            float distance = Mathf.Clamp(delta.magnitude, .001f, upperLength + lowerLength - .001f);
            Vector3 forward = delta.sqrMagnitude > .000001f ? delta.normalized : Vector3.down;
            Vector3 bend = Vector3.ProjectOnPlane(pole, forward).normalized;
            if (bend.sqrMagnitude < .001f) bend = Vector3.Cross(forward, Vector3.up).normalized;
            if (bend.sqrMagnitude < .001f) bend = Vector3.right;
            float along = (upperLength * upperLength - lowerLength * lowerLength + distance * distance)
                / (2 * distance);
            float outwards = Mathf.Sqrt(Mathf.Max(0, upperLength * upperLength - along * along));
            Vector3 elbowPosition = shoulder.position + forward * along + bend * outwards;
            shoulder.rotation = Quaternion.FromToRotation(Vector3.down, elbowPosition - shoulder.position);
            elbow.rotation = Quaternion.FromToRotation(Vector3.down, target - elbow.position);
        }

        static void Pose(Transform joint, Vector3 angles, float blend)
        {
            if (joint != null) joint.localRotation = Quaternion.Slerp(joint.localRotation, Quaternion.Euler(angles), blend);
        }
    }
}
