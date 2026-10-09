using UnityEngine;

namespace SwordKing
{
    public partial class PlayerController
    {
        readonly ShiftGesture shiftGesture = new ShiftGesture();
        bool ignoreShiftUntilRelease, chargingAttack, chargeOverhead, chargeSprint;
        float chargeStarted, queuedHold;
        PlayerAttackKind queuedAttack, activeAttack;
        Vector3 swordRestPosition;
        bool jumpStrikePending;
        float jumpStrikeStarted, jumpStrikeDamage, jumpStrikeCharge, jumpStrikeReach, jumpStrikeAngle;
        bool SpinActive => activeAttack == PlayerAttackKind.Spin && Time.time-animationStart < animationDuration && !IsRolling;
        public float ChargeFraction => PlayerAttackModel.ChargeFraction(Time.time - chargeStarted);

        bool UpdateShiftGesture(bool held, Vector2 movement)
        {
            if(ignoreShiftUntilRelease)
            {
                if(!held) ignoreShiftUntilRelease=false;
                IsSprinting=false; return false;
            }
            bool roll=shiftGesture.Tick(held,Time.time,Mathf.Clamp(sprintHoldThreshold,.1f,.3f));
            IsSprinting=shiftGesture.SprintHeld && movement.sqrMagnitude>.01f && !IsRolling && !jumpStrikePending;
            return roll;
        }
        void ReadAttackInput(PlayerInputFrame input)
        {
            if(jumpStrikePending || SpinActive) { CancelAttackInput(); return; }
            if(!chargingAttack && (input.HeavyAttack || input.Attack))
            {
                chargingAttack=true; chargeOverhead=input.HeavyAttack;
                chargeSprint=IsSprinting; chargeStarted=Time.time;
                attackBuffer.Clear(); swordPivot.localPosition=swordRestPosition;
            }
            if(!chargingAttack) return;
            chargeSprint |= IsSprinting;
            bool released=chargeOverhead ? input.HeavyReleased : input.AttackReleased;
            bool held=chargeOverhead ? input.HeavyHeld : input.AttackHeld;
            if(released)
            {
                queuedHold=Mathf.Clamp(Time.time-chargeStarted,0,PlayerAttackModel.MaxChargeSeconds);
                queuedAttack=PlayerAttackModel.ChargedKind(chargeOverhead,chargeSprint,queuedHold);
                chargingAttack=false; attackBuffer.Press(Time.unscaledTime);
            }
            else if(!held) CancelAttackInput();
        }
        void CancelAttackInput()
        {
            attackBuffer.Clear(); chargingAttack=false;
            if(swordPivot!=null) swordPivot.localPosition=swordRestPosition;
        }
        void CancelCombatInput()
        {
            CancelAttackInput(); jumpStrikePending=false; shiftGesture.Clear(); IsSprinting=false;
            ignoreShiftUntilRelease=playerInput!=null && playerInput.Read().ShiftHeld;
        }
        void UpdateJumpStrike()
        {
            if(!jumpStrikePending || Time.time-jumpStrikeStarted<.08f) return;
            // Deliver the overhead on descent, rather than dealing damage on takeoff.
            if(verticalSpeed>0 && !controller.isGrounded) return;
            jumpStrikePending=false;
            animationStart=Time.time; animationDuration=.28f;
            ResolveAttack(jumpStrikeDamage,jumpStrikeReach,jumpStrikeAngle,jumpStrikeCharge);
            EmitAttackTrail(PlayerAttackKind.JumpingOverhead,jumpStrikeReach,jumpStrikeAngle);
        }
    }
}
