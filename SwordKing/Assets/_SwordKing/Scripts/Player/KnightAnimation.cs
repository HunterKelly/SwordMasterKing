using System.Collections.Generic;
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
        [Range(.05f, .4f)] public float walkingStride = .38f;
        [Range(.1f, .5f)] public float sprintingStride = .50f;
        [Range(.02f, .2f)] public float walkingFootLift = .10f;
        [Range(.04f, .25f)] public float sprintingFootLift = .18f;
        public Transform capeTop, capeMid, capeHem;
        [Min(.01f)] public float upperArmLength = .285f;
        [Min(.01f)] public float forearmLength = .29f;
        [Range(0, 2)] public float movementAmount = 1f;
        [Range(0, 2)] public float attackAmount = 1f;
        [Min(1)] public float poseResponse = 18f;
        float phase, movement, sprintWeight;
        Vector3 gaitDirection = Vector3.forward;
        LegExtension leftExtension, rightExtension;
        readonly FootContact leftContact = new FootContact();
        readonly FootContact rightContact = new FootContact();

        sealed class FootContact
        {
            public bool locked;
            public Vector3 position;
        }

        struct StepPose
        {
            public Vector3 target;
            public float pitch;
        }

        public void Present(Vector2 input, bool sprint, bool airborne, bool rolling,
            bool charging, bool overheadCharge, bool risingHeavy, PlayerAttackKind attack,
            float progress, int swingIndex, Transform leftThigh, Transform rightThigh,
            Vector3 worldDisplacement = default(Vector3), float frameSeconds = -1f)
        {
            float dt = frameSeconds >= 0 ? frameSeconds : Time.deltaTime;
            float blend = 1f - Mathf.Exp(-poseResponse * dt);
            float distanceMoved = worldDisplacement.magnitude;
            float actualSpeed = dt > .000001f ? distanceMoved / dt : 0;
            bool moving = actualSpeed > .05f && !airborne && !rolling;
            movement = Mathf.Lerp(movement, moving ? Mathf.Clamp01(actualSpeed / .6f) : 0,
                1f - Mathf.Exp(-8f * dt));
            sprintWeight = Mathf.Lerp(sprintWeight, sprint ? 1 : 0, 1f - Mathf.Exp(-6f * dt));
            float stanceDuration = Mathf.Lerp(.30f, .22f, sprintWeight);
            float strideLength = Mathf.Lerp(walkingStride, sprintingStride, sprintWeight);
            // A support foot travels 2*stride relative to the character per stance.
            // Match that distance exactly to collision-resolved root movement.
            if (moving)
                phase = Mathf.Repeat(phase + distanceMoved * stanceDuration
                    / Mathf.Max(.02f, 2 * strideLength) * Mathf.PI * 2, Mathf.PI * 2);
            float gaitWeight = airborne || rolling ? 0 : movement * movementAmount;
            float stride = Mathf.Cos(phase) * gaitWeight;
            float weightShift = Mathf.Sin(phase) * gaitWeight;
            float shoulderTurn = stride * 3f;
            if (pelvis != null) Pose(pelvis, new Vector3(0, -stride * 3.5f, weightShift * 1.5f), blend);
            bool attacking = progress >= 0 && progress < 1;
            bool overhead = attack == PlayerAttackKind.Overhead || attack == PlayerAttackKind.ChargedOverhead
                || attack == PlayerAttackKind.JumpingOverhead;
            float strike = attacking ? Mathf.Sin(Mathf.Clamp01(progress) * Mathf.PI) * attackAmount : 0;
            float twist = attacking && attack == PlayerAttackKind.Slash
                ? Mathf.Lerp(-25, 25, Mathf.SmoothStep(0, 1, progress)) * (swingIndex % 2 == 0 ? 1 : -1) : 0;
            float lean = Mathf.Lerp(.75f, 4f, sprintWeight) * movement
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
                visuals.localPosition = new Vector3(-weightShift * .009f, .91f, 0);
                if (leftExtension == null) leftExtension = new LegExtension(leftThigh, leftShin, leftFoot);
                if (rightExtension == null) rightExtension = new LegExtension(rightThigh, rightShin, rightFoot);
                if (moving)
                    gaitDirection = visuals.InverseTransformDirection(worldDisplacement.normalized);
                float legWeight = moving ? 1 : movement;
                var leftStep = PrepareStep(leftThigh, visuals, phase / (Mathf.PI * 2), legWeight,
                    stanceDuration, moving, leftContact);
                var rightStep = PrepareStep(rightThigh, visuals,
                    Mathf.Repeat(phase / (Mathf.PI * 2) + .5f, 1), legWeight,
                    stanceDuration, moving, rightContact);
                // Keep the hips at a steady height. Soft visual leg extension absorbs
                // the extra reach instead of pumping the entire body up and down.
                AnimateStep(leftThigh, leftShin, leftFoot, visuals, leftStep, leftExtension);
                AnimateStep(rightThigh, rightShin, rightFoot, visuals, rightStep, rightExtension);
            }
            else
            {
                leftContact.locked = rightContact.locked = false;
                leftExtension?.Reset();
                rightExtension?.Reset();
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

        StepPose PrepareStep(Transform hip, Transform visuals, float cycle, float weight,
            float stanceDuration, bool moving, FootContact contact)
        {
            bool planted = cycle < stanceDuration;
            float t = planted ? cycle / stanceDuration : (cycle - stanceDuration) / (1 - stanceDuration);
            float travel = Mathf.Lerp(walkingStride, sprintingStride, sprintWeight) * weight;
            float along = planted ? Mathf.Lerp(travel, -travel, t)
                : Mathf.Lerp(-travel, travel, Mathf.SmoothStep(0, 1, t));
            float lift = planted ? 0 : Mathf.Pow(Mathf.Max(0, Mathf.Sin(t * Mathf.PI)), 1.25f)
                * Mathf.Lerp(walkingFootLift, sprintingFootLift, sprintWeight) * weight;
            Vector3 step = gaitDirection * along;
            var target = visuals.TransformPoint(new Vector3(hip.localPosition.x - visuals.localPosition.x + step.x,
                .105f - visuals.localPosition.y + lift, .015f + step.z));
            if (planted && moving)
            {
                // Keep the support ankle in world space rather than dragging it with the root.
                if (!contact.locked || Vector3.Distance(contact.position, target) > 1.5f)
                    contact.position = target;
                contact.locked = true;
                target = contact.position;
            }
            else contact.locked = false;
            float footRoll = planted
                ? -10 * (1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0, .20f, t)))
                    + 12 * Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.76f, 1, t))
                : Mathf.Lerp(12, -10, Mathf.SmoothStep(0, 1, t));
            return new StepPose { target = target, pitch = footRoll * weight };
        }

        // Stretch only visual geometry and joint offsets, never the controller or
        // damage volumes. Save authored dimensions so extension cannot accumulate.
        sealed class LegExtension
        {
            readonly Transform hip, knee, ankle;
            readonly Vector3 kneeRest, ankleRest;
            readonly List<Transform> parts = new List<Transform>();
            readonly List<Vector3> scales = new List<Vector3>();

            public LegExtension(Transform hip, Transform knee, Transform ankle)
            {
                this.hip = hip; this.knee = knee; this.ankle = ankle;
                kneeRest = knee.localPosition; ankleRest = ankle.localPosition;
                foreach (Transform parent in new[] { hip, knee })
                    foreach (Transform child in parent)
                        if (child.GetComponent<MeshFilter>() != null)
                        {
                            parts.Add(child); scales.Add(child.localScale);
                        }
            }

            public void Fit(Vector3 target)
            {
                float length = kneeRest.magnitude + ankleRest.magnitude;
                float extension = Mathf.Clamp(Vector3.Distance(hip.position, target)
                    / Mathf.Max(.01f, length - .004f), 1, 1.22f);
                Apply(extension);
            }

            public void Reset() => Apply(1);

            void Apply(float extension)
            {
                knee.localPosition = kneeRest * extension;
                ankle.localPosition = ankleRest * extension;
                for (int i = 0; i < parts.Count; i++)
                    parts[i].localScale = new Vector3(scales[i].x, scales[i].y * extension, scales[i].z);
            }
        }

        static void AnimateStep(Transform hip, Transform knee, Transform ankle,
            Transform visuals, StepPose step, LegExtension extension)
        {
            extension.Fit(step.target);
            float upper = knee.localPosition.magnitude, lower = ankle.localPosition.magnitude;
            Vector3 delta = step.target - hip.position;
            float distance = Mathf.Clamp(delta.magnitude, Mathf.Abs(upper - lower) + .001f, upper + lower - .001f);
            Vector3 forward = delta.normalized;
            Vector3 bend = Vector3.ProjectOnPlane(visuals.forward, forward).normalized;
            float alongBone = (upper * upper - lower * lower + distance * distance) / (2 * distance);
            float outwards = Mathf.Sqrt(Mathf.Max(0, upper * upper - alongBone * alongBone));
            Vector3 joint = hip.position + forward * alongBone + bend * outwards;
            hip.rotation = Quaternion.FromToRotation(knee.localPosition.normalized, joint - hip.position);
            knee.rotation = Quaternion.FromToRotation(ankle.localPosition.normalized, step.target - knee.position);
            ankle.rotation = visuals.rotation * Quaternion.Euler(step.pitch, 0, 0);
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
