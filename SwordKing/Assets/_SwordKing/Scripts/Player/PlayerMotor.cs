using System.Collections.Generic;
using UnityEngine;

namespace SwordKing
{
    public partial class PlayerController
    {
        void UpdateMovement(Vector2 input, bool jump, bool roll)
        {
            float dt = Time.deltaTime;
            bool grounded = controller.isGrounded;
            Vector3 direction = Quaternion.Euler(0, yaw, 0) * new Vector3(input.x, 0, input.y).normalized;
            // Roll wins if jump and roll arrive together. Neither action is buffered.
            if (roll && grounded && !IsRolling && Time.time >= rollReadyAt)
            {
                rollDirection = direction.sqrMagnitude > .01f ? direction : player.forward;
                activeRollDuration = Mathf.Max(.1f, rollDuration);
                activeRollDistance = Mathf.Max(.1f, rollDistance);
                rollStartedAt = Time.time; rollEndsAt = Time.time + activeRollDuration;
                rollReadyAt = rollEndsAt + Mathf.Max(0, rollRecovery); rollTravelTime = 0;
                CancelAttackInput(); jumpStrikePending = false; thrustWindow.Cancel(); thrustSlideRemaining=0; IsSprinting = false;
                attackHistory.Clear(); animationStart = -100f;
                foreach (var slash in slashes) Destroy(slash.line.gameObject);
                slashes.Clear();
                if (Level != null) Level.PlaySound("roll");
            }
            if (grounded && verticalSpeed < 0) verticalSpeed = -2f;
            float gravity = Mathf.Max(1f, gravityStrength);
            if (jump && grounded && !IsRolling)
            {
                verticalSpeed = Mathf.Sqrt(2f * gravity * controller.height * Mathf.Max(.1f, jumpHeightFraction));
                if (Level != null) Level.PlaySound("jump");
            }

            Vector3 horizontal;
            if (IsRolling || rollTravelTime < activeRollDuration)
            {
                // Integrate only the remaining roll time, so distance doesn't vary with FPS.
                float step = Mathf.Min(dt, Mathf.Max(0, activeRollDuration - rollTravelTime));
                horizontal = rollDirection * (activeRollDistance / activeRollDuration) * step;
                rollTravelTime += step;
            }
            else horizontal = direction * moveSpeed * (IsSprinting ? Mathf.Max(1, sprintMultiplier) : 1) * dt;
            if(!IsRolling && thrustSlideRemaining>0)
            {
                float slideStep=Mathf.Min(dt,thrustSlideRemaining);
                horizontal+=thrustSlideDirection*(Mathf.Max(0,thrustSlideDistance)/Mathf.Max(.05f,thrustSlideDuration))*slideStep;
                thrustSlideRemaining-=slideStep;
            }
            float rise = verticalSpeed * dt - .5f * gravity * dt * dt;
            verticalSpeed -= gravity * dt;
            CollisionFlags flags = controller.Move(horizontal + Vector3.up * rise);
            if ((flags & CollisionFlags.Above) != 0 && verticalSpeed > 0) verticalSpeed = 0;
            if ((flags & CollisionFlags.Below) != 0 && verticalSpeed < 0) verticalSpeed = -2;
            airborne = !controller.isGrounded;

            if (IsRolling)
            {
                strafeLean = 0;
                float progress = Mathf.Clamp01((Time.time - rollStartedAt) / activeRollDuration);
                float tuck = Mathf.Sin(progress * Mathf.PI);
                visualRoot.localPosition = Vector3.up * (.95f - .22f * tuck);
                visualRoot.localRotation = Quaternion.Inverse(player.rotation) * Quaternion.LookRotation(rollDirection) * Quaternion.Euler(360 * progress, 0, 0);
                visualRoot.localScale = Vector3.one * (1 - .2f * tuck);
                leftLeg.localRotation = rightLeg.localRotation = Quaternion.Euler(-50 * tuck, 0, 0);
            }
            else
            {
                visualRoot.localPosition = Vector3.up * .95f;
                // Turn the visual body toward the strafe direction while keeping it upright.
                float targetLean = Mathf.Clamp(input.x, -1, 1) * Mathf.Clamp(strafeLeanAngle, 0, 60);
                strafeLean = Mathf.MoveTowards(strafeLean, targetLean, Mathf.Max(1, strafeLeanSpeed) * dt);
                float spinTurn = SpinActive ? 360f * Mathf.Clamp01((Time.time-animationStart)/animationDuration) : 0;
                visualRoot.localRotation = Quaternion.Euler(0, strafeLean + spinTurn, 0); visualRoot.localScale = Vector3.one;
                walkPhase += direction.magnitude * moveSpeed * dt * 2;
                float legAngle = airborne ? -30 : (direction.sqrMagnitude > .01f ? Mathf.Sin(walkPhase) * 25 : 0);
                leftLeg.localRotation = Quaternion.Euler(legAngle, 0, 0);
                rightLeg.localRotation = Quaternion.Euler(airborne ? -45 : -legAngle, 0, 0);
            }
        }

        PlayerHurtbox CreateHurtbox(string label, bool lower, float height)
        {
            var go = new GameObject(label); go.transform.SetParent(player, false);
            go.transform.localPosition = Vector3.up * height;
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true; box.size = new Vector3(.65f, .95f, .55f);
            var hurtbox = go.AddComponent<PlayerHurtbox>();
            hurtbox.Initialize(this, lower, box);
            return hurtbox;
        }

        void RefreshHurtboxes()
        {
            upperHurtbox.Volume.enabled = !IsRolling;
            lowerHurtbox.Volume.enabled = !IsRolling && !LowerBodyProtected;
        }

        public bool TryReceiveDamage(float amount, bool lowerBody)
        {
            if (adventureMode && (PlayerHealth <= 0 || Level == null || Level.InputBlocked || Time.time < damageGraceUntil)) return false;
            // The authoritative guard also rejects hits from a stale physics overlap.
            if (amount <= 0 || float.IsNaN(amount) || float.IsInfinity(amount)
                || IsRolling || (lowerBody && LowerBodyProtected)) return false;
            PlayerHealth = Mathf.Max(0, PlayerHealth - amount);
            if (adventureMode)
            {
                damageGraceUntil = Time.time + .3f;
                Level.OnPlayerHurt();
            }
            return true;
        }

        public void RestoreAt(Vector3 position)
        {
            controller.enabled = false; player.position = position; controller.enabled = true;
            if(stamina!=null) stamina.Reset();
            PlayerHealth = 100; verticalSpeed = 0; damageGraceUntil = Time.time + 1f;
            yaw = 0; pitch = 22; player.rotation = Quaternion.identity;
            rollEndsAt = -100; rollReadyAt = 0; activeRollDuration = 0; rollTravelTime = 0;
            lastAttack = -100; animationStart = -100; attackHistory.Clear();
            CancelCombatInput(); jumpStrikePending = false;
            strafeLean = 0;
            airborne = false; visualRoot.localRotation = Quaternion.identity;
            visualRoot.localPosition = Vector3.up * .95f; visualRoot.localScale = Vector3.one;
            foreach (var slash in slashes) Destroy(slash.line.gameObject);
            slashes.Clear(); RefreshHurtboxes();
        }

        public bool Heal(float amount)
        {
            if (PlayerHealth <= 0 || PlayerHealth >= 100) return false;
            PlayerHealth = Mathf.Min(100, PlayerHealth + amount); return true;
        }
    }
}
