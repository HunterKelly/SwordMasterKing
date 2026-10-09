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
            float charge=PlayerAttackModel.ChargeFraction(queuedHold);
            lastDamage=SwingModel.Damage(elapsed,power,recovery,speed) * PlayerAttackModel.DamageMultiplier(activeAttack,queuedHold,fullChargeDamageMultiplier);
            lastAttack=Time.time; totalAttacks++; attackHistory.Enqueue(Time.time);
            animationStart=Time.time; animationDuration=activeAttack==PlayerAttackKind.Spin ? .45f : activeAttack==PlayerAttackKind.Slash ? Mathf.Clamp(elapsed,.1f,.48f) : activeAttack==PlayerAttackKind.Thrust ? .22f : .32f;
            swingIndex++; swordPivot.localPosition=swordRestPosition;
            float strikeReach=reach * (activeAttack==PlayerAttackKind.Thrust ? Mathf.Max(1,thrustReachMultiplier)
                : activeAttack==PlayerAttackKind.JumpingOverhead ? Mathf.Max(1,jumpingOverheadReachMultiplier)
                : activeAttack==PlayerAttackKind.Overhead ? Mathf.Max(1,overheadReachMultiplier) : 1);
            float strikeAngle=activeAttack==PlayerAttackKind.Spin ? 360 : activeAttack==PlayerAttackKind.Thrust ? 25 : activeAttack==PlayerAttackKind.Slash ? attackAngle : 55;
            float impactCharge=Mathf.Max(charge,SwingModel.Charge(elapsed,recovery));
            if(activeAttack==PlayerAttackKind.JumpingOverhead)
            {
                if(controller.isGrounded)
                {
                    verticalSpeed=Mathf.Sqrt(2f*Mathf.Max(1,gravityStrength)*controller.height*Mathf.Max(.1f,jumpHeightFraction));
                    airborne=true; RefreshHurtboxes();
                }
                jumpStrikePending=true; jumpStrikeStarted=Time.time;
                jumpStrikeDamage=lastDamage; jumpStrikeReach=strikeReach; jumpStrikeAngle=strikeAngle; jumpStrikeCharge=impactCharge;
                animationDuration=.6f;
                if(Level!=null) Level.PlaySound("jump");
                return;
            }
            ResolveAttack(lastDamage,strikeReach,strikeAngle,impactCharge);
            EmitAttackTrail(activeAttack,strikeReach,strikeAngle);
        }
        void ResolveAttack(float damage,float strikeReach,float strikeAngle,float charge)
        {
            foreach(var d in dummies)
            {
                Vector3 delta=d.root.position-player.position; delta.y=0;
                if(d.health<=0 || delta.magnitude>strikeReach || Vector3.Angle(player.forward,delta)>strikeAngle*.5f) continue;
                float dealt=Mathf.Min(d.health,damage); d.health-=dealt; d.flashUntil=Time.time+.08f;
                if(Feedback!=null) Feedback.Impact(d.root.position+Vector3.up*1.2f,delta,charge>=.8f,d.health<=0);
                damageHistory.Enqueue(new Vector2(Time.time,dealt));
                if(d.health<=0) d.resetAt=Time.time+1.5f;
            }
            if(Level!=null)
            {
                Level.ResolvePlayerAttack(damage,strikeReach,strikeAngle,charge);
                Level.PlaySound("swing");
            }
        }
        void EmitAttackTrail(PlayerAttackKind kind,float strikeReach,float strikeAngle)
        {
            var line=new GameObject("Cosmetic "+kind+" trail").AddComponent<LineRenderer>();
            line.transform.SetParent(transform); line.sharedMaterial=slashMaterial; line.positionCount=kind==PlayerAttackKind.Spin ? 49 : 18;
            line.useWorldSpace=true; line.widthMultiplier=.075f; line.numCapVertices=3;
            for(int i=0;i<line.positionCount;i++)
            {
                float t=i/(float)(line.positionCount-1);
                Vector3 local;
                if(kind==PlayerAttackKind.Thrust) local=new Vector3(0,1.25f,Mathf.Lerp(.5f,strikeReach,t));
                else if(kind==PlayerAttackKind.Overhead || kind==PlayerAttackKind.JumpingOverhead)
                {
                    local=new Vector3(0,Mathf.Lerp(2.8f,.2f,t),Mathf.Lerp(.5f,strikeReach,Mathf.Sin(t*Mathf.PI*.5f)));
                }
                else
                {
                    float a=Mathf.Lerp(-strikeAngle*.5f,strikeAngle*.5f,t)*Mathf.Deg2Rad;
                    local=new Vector3(Mathf.Sin(a)*strikeReach,1.25f+Mathf.Sin(a)*.22f*(swingIndex%2==0?1:-1),Mathf.Cos(a)*strikeReach);
                }
                line.SetPosition(i,player.TransformPoint(local));
            }
            slashes.Add(new Slash { line=line,born=Time.time });
        }
    }
}
