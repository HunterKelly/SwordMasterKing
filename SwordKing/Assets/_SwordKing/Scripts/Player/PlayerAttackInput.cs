using UnityEngine;

namespace SwordKing
{
    public partial class PlayerController
    {
        bool shiftSpecialHeld;
        bool chargedSlamPending;
        float chargedSlamAt, chargedSlamDamage;
        bool sprintExhausted;
        bool ignoreShiftUntilRelease, chargingAttack, chargeOverhead, chargeSprint;
        float chargeStarted, queuedHold;
        PlayerAttackKind queuedAttack, activeAttack;
        Vector3 swordRestPosition;
        bool jumpStrikePending, jumpDescending;
        float attackReadyAt, specialPoseUntil;
        readonly HitOnceWindow thrustWindow = new HitOnceWindow();
        float thrustDamage, thrustReach, thrustAngle, thrustCharge;
        Vector3 thrustSlideDirection;
        float thrustSlideRemaining;
        readonly System.Collections.Generic.Queue<float> horizontalHistory = new System.Collections.Generic.Queue<float>();
        bool ShiftSpecialReady => shiftSpecialHeld && !IsRolling && !jumpStrikePending;
        bool HorizontalFlurry => activeAttack==PlayerAttackKind.Slash && horizontalHistory.Count>=3 && Time.time-lastAttack<.2f;
        readonly System.Collections.Generic.Queue<float> overheadHistory = new System.Collections.Generic.Queue<float>();
        bool OverheadFlurry => overheadHistory.Count >= 3 && Time.time-lastAttack < .2f;
        float jumpStrikeStarted, jumpStrikeDamage, jumpStrikeCharge, jumpStrikeReach, jumpStrikeAngle;
        bool SpinActive => activeAttack == PlayerAttackKind.Spin && Time.time-animationStart < animationDuration && !IsRolling;
        public float ChargeFraction => PlayerAttackModel.ChargeFraction(Time.time-chargeStarted,
            !chargeOverhead && !chargeSprint ? PlayerAttackKind.Spin : PlayerAttackModel.Kind(chargeOverhead,chargeSprint));

        void UpdateSprint(bool held, Vector2 movement)
        {
            if(ignoreShiftUntilRelease)
            {
                if(!held) ignoreShiftUntilRelease=false;
                shiftSpecialHeld=false; IsSprinting=false; return;
            }
            shiftSpecialHeld=held;
            // Refill a little before resuming so empty stamina cannot flicker sprint every frame.
            if(!held || stamina==null || stamina.Current>=Mathf.Min(8f,stamina.Maximum)) sprintExhausted=false;
            IsSprinting=shiftSpecialHeld && !sprintExhausted && movement.sqrMagnitude>.01f && !IsRolling && !jumpStrikePending && (stamina==null || stamina.Current>0);
        }
        void ReadAttackInput(PlayerInputFrame input)
        {
            if(jumpStrikePending || Time.time < specialPoseUntil || SpinActive) { CancelAttackInput(); return; }
            if(!chargingAttack && (input.HeavyAttack || input.Attack))
            {
                chargingAttack=true; chargeOverhead=input.HeavyAttack;
                chargeSprint=ShiftSpecialReady; chargeStarted=Time.time;
                attackBuffer.Clear(); swordPivot.localPosition=swordRestPosition;
            }
            if(!chargingAttack) return;
            chargeSprint |= ShiftSpecialReady;
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
            CancelAttackInput(); jumpStrikePending=false; jumpDescending=false; thrustWindow.Cancel();
            chargedSlamPending=false;
            specialPoseUntil=0; attackReadyAt=0; thrustSlideRemaining=0; horizontalHistory.Clear(); overheadHistory.Clear(); shiftSpecialHeld=false; IsSprinting=false;
            ignoreShiftUntilRelease=playerInput!=null && playerInput.Read().ShiftHeld;
        }
        void UpdateAttackWindows()
        {
            if(chargedSlamPending && Time.time>=chargedSlamAt)
            {
                chargedSlamPending=false;
                ResolveChargedGroundSlam(chargedSlamDamage);
            }
            if(thrustWindow.Active(Time.time)) ResolveAttack(thrustDamage,thrustReach,thrustAngle,thrustCharge,thrustWindow,false);
        }
        void UpdateJumpStrike()
        {
            if(!jumpStrikePending || Time.time-jumpStrikeStarted<.08f) return;
            const float swingLead=.04f;
            if(!jumpDescending && (verticalSpeed<=Mathf.Max(1,gravityStrength)*swingLead || controller.isGrounded))
            {
                jumpDescending=true; animationStart=Time.time;
                animationDuration=Mathf.Sqrt(2f*controller.height*Mathf.Max(.1f,jumpHeightFraction)/Mathf.Max(1,gravityStrength))+swingLead;
            }
            // Begin the swing about 40 ms before the apex; impact still lands with the player.
            if(!controller.isGrounded) return;
            jumpStrikePending=false;
            ResolveAttack(jumpStrikeDamage,jumpStrikeReach,jumpStrikeAngle,jumpStrikeCharge);
            EmitAttackTrail(PlayerAttackKind.JumpingOverhead,jumpStrikeReach,jumpStrikeAngle);
            EmitLandingBlast();
            if(cameraRig!=null) cameraRig.AddImpact(1.1f,.28f);
            if(Feedback!=null) Feedback.Impact(player.position+player.forward,player.forward,true,false);
            if(Level!=null) Level.PlaySound("slam");
            attackReadyAt=Time.time+Mathf.Max(0,jumpingHeavyRecovery);
            specialPoseUntil=attackReadyAt; rollReadyAt=Mathf.Max(rollReadyAt,attackReadyAt);
        }
    }
}
