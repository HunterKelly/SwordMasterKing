using UnityEngine;

namespace SwordKing
{
    public partial class PlayerController
    {
        void Attack()
        {
            float elapsed = Time.time - lastAttack;
            if (elapsed + .00001f < 1f / SwingModel.MaxRate(speed)) { rejected++; return; }
            activeAttack=queuedAttack;
            float charge=PlayerAttackModel.ChargeFraction(queuedHold,activeAttack);
            lastDamage=SwingModel.Damage(elapsed,power,recovery,speed) * PlayerAttackModel.DamageMultiplier(activeAttack,queuedHold,fullChargeDamageMultiplier);
            lastAttack=Time.time; totalAttacks++; attackHistory.Enqueue(Time.time);
            animationStart=Time.time; animationDuration=activeAttack==PlayerAttackKind.Spin ? .28f : activeAttack==PlayerAttackKind.Slash ? Mathf.Clamp(elapsed,.1f,.48f) : activeAttack==PlayerAttackKind.Thrust ? Mathf.Max(.1f,thrustDuration) : .32f;
            swingIndex++; swordPivot.localPosition=swordRestPosition;
            float strikeReach=reach * (activeAttack==PlayerAttackKind.Thrust ? Mathf.Max(1,thrustReachMultiplier)
                : activeAttack==PlayerAttackKind.JumpingOverhead ? Mathf.Max(1,jumpingOverheadReachMultiplier)
                : (activeAttack==PlayerAttackKind.Overhead || activeAttack==PlayerAttackKind.ChargedOverhead) ? Mathf.Max(1,overheadReachMultiplier) : 1);
            float strikeAngle=activeAttack==PlayerAttackKind.Spin ? 360 : activeAttack==PlayerAttackKind.Thrust ? 25 : activeAttack==PlayerAttackKind.Slash ? attackAngle : 55;
            float impactCharge=Mathf.Max(charge,SwingModel.Charge(elapsed,recovery));
            if(activeAttack==PlayerAttackKind.JumpingOverhead)
            {
                if(controller.isGrounded)
                {
                    verticalSpeed=Mathf.Sqrt(2f*Mathf.Max(1,gravityStrength)*controller.height*Mathf.Max(.1f,jumpHeightFraction));
                    airborne=true; RefreshHurtboxes();
                }
                jumpStrikePending=true; jumpDescending=false; jumpStrikeStarted=Time.time;
                jumpStrikeDamage=lastDamage; jumpStrikeReach=strikeReach; jumpStrikeAngle=strikeAngle; jumpStrikeCharge=impactCharge;
                animationDuration=.6f;
                if(Level!=null) Level.PlaySound("jump");
                return;
            }
            bool charged=queuedHold>=PlayerAttackModel.SpinChargeThreshold;
            bool special=activeAttack==PlayerAttackKind.Spin || activeAttack==PlayerAttackKind.Thrust;
            float recoveryDelay=special ? Mathf.Max(0,specialAttackRecovery) : charged ? Mathf.Max(0,chargedAttackRecovery) : 0;
            if(special || charged)
            {
                specialPoseUntil=Time.time+animationDuration;
                attackReadyAt=specialPoseUntil+recoveryDelay;
                rollReadyAt=Mathf.Max(rollReadyAt,attackReadyAt);
            }
            if(activeAttack==PlayerAttackKind.Thrust)
            {
                if(controller.isGrounded && thrustHopHeight>0)
                {
                    verticalSpeed=Mathf.Sqrt(2f*Mathf.Max(1,gravityStrength)*Mathf.Clamp(thrustHopHeight,0,.4f));
                    airborne=true; RefreshHurtboxes();
                }
                thrustSlideDirection=player.forward; thrustSlideRemaining=Mathf.Max(.05f,thrustSlideDuration);
                thrustWindow.Begin(Time.time,Mathf.Min(animationDuration,Mathf.Max(.1f,thrustDamageWindow)));
                thrustDamage=lastDamage; thrustReach=strikeReach; thrustAngle=strikeAngle; thrustCharge=impactCharge;
                ResolveAttack(lastDamage,strikeReach,strikeAngle,impactCharge,thrustWindow);
            }
            else if(activeAttack==PlayerAttackKind.ChargedOverhead)
            {
                // Split damage equally into the sword and slam; a target struck by both takes the original total.
                ResolveAttack(lastDamage*.5f,strikeReach,strikeAngle,impactCharge);
                chargedSlamDamage=lastDamage*.5f;
                chargedSlamAt=Time.time+animationDuration;
                chargedSlamPending=true;
            }
            else ResolveAttack(lastDamage,strikeReach,strikeAngle,impactCharge);
            while(overheadHistory.Count>0 && Time.time-overheadHistory.Peek()>.5f) overheadHistory.Dequeue();
            if(activeAttack==PlayerAttackKind.Overhead && !charged)
            {
                overheadHistory.Enqueue(Time.time);
            }
            while(horizontalHistory.Count>0 && Time.time-horizontalHistory.Peek()>.5f) horizontalHistory.Dequeue();
            if(activeAttack==PlayerAttackKind.Slash && !charged)
            {
                horizontalHistory.Enqueue(Time.time);
            }
            EmitAttackTrail(activeAttack,strikeReach,strikeAngle);
            // Hyper speed shows the normal slash plus two parallel copies: three bars total.
            if(activeAttack==PlayerAttackKind.Slash && HorizontalFlurry)
            {
                EmitAttackTrail(activeAttack,strikeReach,strikeAngle,Vector3.up*.22f);
                EmitAttackTrail(activeAttack,strikeReach,strikeAngle,Vector3.down*.22f);
            }
            else if(activeAttack==PlayerAttackKind.Overhead && OverheadFlurry)
            {
                EmitAttackTrail(activeAttack,strikeReach,strikeAngle,Vector3.right*.22f);
                EmitAttackTrail(activeAttack,strikeReach,strikeAngle,Vector3.left*.22f);
            }
        }
        void ResolveAttack(float damage,float strikeReach,float strikeAngle,float charge,HitOnceWindow window=null,bool playSound=true)
        {
            for(int i=0;i<dummies.Count;i++)
            {
                var d=dummies[i];
                Vector3 delta=d.root.position-player.position; delta.y=0;
                if(d.health<=0 || delta.magnitude>strikeReach || Vector3.Angle(player.forward,delta)>strikeAngle*.5f) continue;
                if(window!=null && !window.TryHit(i,Time.time)) continue;
                float dealt=Mathf.Min(d.health,damage); d.health-=dealt; d.flashUntil=Time.time+.08f;
                if(Feedback!=null) Feedback.Impact(d.root.position+Vector3.up*1.2f,delta,charge>=.8f,d.health<=0);
                damageHistory.Enqueue(new Vector2(Time.time,dealt));
                if(d.health<=0) d.resetAt=Time.time+1.5f;
            }
            if(Level!=null)
            {
                Level.ResolvePlayerAttack(damage,strikeReach,strikeAngle,charge,window,activeAttack);
                if(playSound) Level.PlaySound("swing");
            }
        }
        void ResolveChargedGroundSlam(float damage)
        {
            const float radius=4f;
            Vector3 center=player.position;
            foreach(var d in dummies)
            {
                Vector3 delta=d.root.position-center;
                if(d.health<=0 || Mathf.Abs(delta.y)>2) continue;
                delta.y=0;
                if(delta.sqrMagnitude>radius*radius) continue;
                float dealt=Mathf.Min(d.health,damage);
                d.health-=dealt; d.flashUntil=Time.time+.08f;
                damageHistory.Enqueue(new Vector2(Time.time,dealt));
                if(d.health<=0) d.resetAt=Time.time+1.5f;
            }
            if(Level!=null)
            {
                Level.ResolvePlayerAttack(damage,radius,360,0,null,PlayerAttackKind.Slash,center);
                Level.PlaySound("slam");
            }
            EmitLandingBlast(center,radius);
            if(cameraRig!=null) cameraRig.AddImpact(.25f,.12f);
        }
        void EmitLandingBlast(Vector3? impactCenter=null,float radius=3.5f)
        {
            var line=new GameObject("Landing shockwave").AddComponent<LineRenderer>();
            line.transform.SetParent(transform); line.sharedMaterial=slashMaterial;
            line.positionCount=49; line.widthMultiplier=.15f; line.useWorldSpace=true;
            var center=impactCenter ?? player.position;
            for(int i=0;i<49;i++) line.SetPosition(i,center+Vector3.up*.06f);
            slashes.Add(new Slash { line=line,born=Time.time,duration=.35f,shockwave=true,center=center,radius=radius });
        }
        void EmitAttackTrail(PlayerAttackKind kind,float strikeReach,float strikeAngle,Vector3 offset=default(Vector3))
        {
            var line=new GameObject("Cosmetic "+kind+" trail").AddComponent<LineRenderer>();
            line.transform.SetParent(transform); line.sharedMaterial=slashMaterial; line.positionCount=kind==PlayerAttackKind.Spin ? 49 : 18;
            line.useWorldSpace=kind!=PlayerAttackKind.Thrust;
            if(kind==PlayerAttackKind.Thrust) line.transform.SetParent(player,false);
            line.widthMultiplier=.075f; line.numCapVertices=3;
            for(int i=0;i<line.positionCount;i++)
            {
                float t=i/(float)(line.positionCount-1);
                Vector3 local;
                if(kind==PlayerAttackKind.Thrust) local=new Vector3(0,1.25f,Mathf.Lerp(.5f,strikeReach,t));
                else if(kind==PlayerAttackKind.Overhead || kind==PlayerAttackKind.ChargedOverhead || kind==PlayerAttackKind.JumpingOverhead)
                {
                    local=new Vector3(0,Mathf.Lerp(2.8f,.2f,t),Mathf.Lerp(.5f,strikeReach,Mathf.Sin(t*Mathf.PI*.5f)));
                }
                else
                {
                    float a=Mathf.Lerp(-strikeAngle*.5f,strikeAngle*.5f,t)*Mathf.Deg2Rad;
                    local=new Vector3(Mathf.Sin(a)*strikeReach,1.25f+Mathf.Sin(a)*.22f*(swingIndex%2==0?1:-1),Mathf.Cos(a)*strikeReach);
                }
                local+=offset;
                line.SetPosition(i,kind==PlayerAttackKind.Thrust ? local : player.TransformPoint(local));
            }
            slashes.Add(new Slash { line=line,born=Time.time,duration=kind==PlayerAttackKind.Thrust ? Mathf.Min(animationDuration,Mathf.Max(.1f,thrustDamageWindow)) : .18f });
        }
    }
}
