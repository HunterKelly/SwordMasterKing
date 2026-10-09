using UnityEngine;

namespace SwordKing
{
    public sealed class GateEnemy
    {
        enum State { Idle, Chase, Windup, Strike, Recover, Dead }
        public readonly int Id, Zone;
        public readonly bool IsBoss;
        public readonly string Name;
        public readonly Transform Root;
        public readonly float MaxHealth;
        public float Health { get; private set; }
        public bool Alive => Health > 0;
        public bool Telegraphing => state == State.Windup;
        public bool LowAttack => lowAttack;
        public bool Engaged => state != State.Idle && state != State.Dead;
        readonly BrokenGateLevel level;
        readonly Vector3 spawn;
        readonly CharacterController controller;
        readonly Transform graphics, weapon;
        readonly LineRenderer warning;
        readonly Material bodyMaterial;
        readonly Color baseColor;
        readonly int style;
        readonly EnemyCombatSettings tuning;
        Quaternion strikeFrom;
        float strikeStartedAt, recoilUntil;
        Vector3 recoilDirection;
        Vector3 pushDirection;
        float pushRemaining, popVelocity;
        bool popped;
        State state;
        float until, windupStart, windupDuration, flashUntil, staggerReady, deadAt;
        int comboIndex;
        bool lowAttack;
        Vector3 facing = Vector3.back;

        public GateEnemy(BrokenGateLevel owner, int id, int zone, string title, Vector3 position, int attackStyle, bool boss = false)
        {
            tuning=owner.EnemyCombat;
            level=owner; Id=id; Zone=zone; Name=title; spawn=position; IsBoss=boss; style=attackStyle;
            MaxHealth=boss?1000:attackStyle==1?160:120; Health=MaxHealth;
            var w=level.World;
            Root=new GameObject(title).transform; Root.SetParent(w.Root); Root.position=position;
            controller=Root.gameObject.AddComponent<CharacterController>();
            controller.height=boss?2.8f:1.9f; controller.radius=boss?.65f:.35f; controller.center=Vector3.up*controller.height*.5f;
            graphics=new GameObject("Armored body").transform; graphics.SetParent(Root,false);
            float scale=boss?1.45f:1;
            baseColor=boss?new Color(.24f,.20f,.25f):new Color(.38f,.27f,.23f);
            bodyMaterial=w.Material(baseColor);
            w.Shape("Cuirass",PrimitiveType.Capsule,new Vector3(0,1.1f,0),new Vector3(.75f,.55f,.5f),bodyMaterial,false,graphics);
            w.Shape("Helmet",PrimitiveType.Sphere,new Vector3(0,1.8f,0),new Vector3(.48f,.48f,.48f),w.Iron,false,graphics);
            w.Box("Visor slit",new Vector3(0,1.8f,.24f),new Vector3(.32f,.08f,.06f),w.Ember,false,graphics);
            for(int side=-1;side<=1;side+=2)
            {
                w.Box("Greave",new Vector3(side*.2f,.4f,0),new Vector3(.26f,.75f,.3f),w.Iron,false,graphics);
                w.Shape("Shoulder plate",PrimitiveType.Sphere,new Vector3(side*.47f,1.45f,0),new Vector3(.4f,.35f,.4f),w.Gold,false,graphics);
            }
            w.Box("Tabard",new Vector3(0,.9f,.28f),new Vector3(.4f,.8f,.06f),w.Dark,false,graphics);
            weapon=new GameObject("Weapon pivot").transform; weapon.SetParent(graphics,false); weapon.localPosition=new Vector3(.4f,1.2f,0);
            w.Box("Grip",new Vector3(0,0,.35f),new Vector3(.14f,.14f,.6f),w.Dark,false,weapon);
            w.Box("Crossguard",new Vector3(0,0,.65f),new Vector3(.55f,.12f,.12f),w.Gold,false,weapon);
            w.Box("Blade",new Vector3(0,0,1.25f),new Vector3(boss?.3f:.18f,.09f,1.2f),w.Iron,false,weapon);
            if(boss)
            {
                w.Box("Crest",new Vector3(0,2.12f,0),new Vector3(.12f,.4f,.4f),w.Gold,false,graphics);
                w.Box("Great mantle",new Vector3(0,1.15f,-.33f),new Vector3(.8f,1.3f,.08f),w.Cloth,false,graphics);
            }
            graphics.localScale=Vector3.one*scale;
            warning=new GameObject("Attack warning arc").AddComponent<LineRenderer>(); warning.transform.SetParent(Root,false);
            warning.sharedMaterial=w.Material(new Color(1,.55f,.1f),true); warning.useWorldSpace=true; warning.positionCount=26; warning.widthMultiplier=.06f; warning.enabled=false;
            Reset(false);
        }
        public void Reset(bool defeated)
        {
            Root.gameObject.SetActive(true); controller.enabled=false; Root.position=spawn; Root.rotation=Quaternion.Euler(0,180,0);
            controller.enabled=!defeated; Health=defeated?0:MaxHealth;
            state=defeated?State.Dead:State.Idle; until=Time.time+.5f; staggerReady=0; comboIndex=0;
            graphics.localRotation=Quaternion.identity; graphics.localPosition=Vector3.zero; warning.enabled=false;
            weapon.localRotation=Quaternion.identity; recoilUntil=0; facing=Vector3.back;
            pushRemaining=0; popVelocity=0; popped=false;
            if(defeated) Root.gameObject.SetActive(false);
        }
        public void Tick(float dt)
        {
            if(!Root.gameObject.activeSelf) return;
            if(pushRemaining>0)
            {
                float step=Mathf.Min(dt,pushRemaining);
                MoveAlong(pushDirection,Mathf.Max(0,tuning.thrustPushDistance)/Mathf.Max(.05f,tuning.thrustPushDuration),step,false);
                pushRemaining=Mathf.Max(0,pushRemaining-step);
                if(!Alive && pushRemaining<=0) controller.enabled=false;
                if(Alive && !popped) return;
            }
            if(!Alive)
            {
                graphics.localRotation=Quaternion.Slerp(graphics.localRotation,Quaternion.Euler(10,0,85),dt*8);
                if(Time.time-deadAt>1.5f) Root.gameObject.SetActive(false);
                return;
            }
            bodyMaterial.color=Time.time<flashUntil?Color.white:baseColor;
            float recoil = Mathf.Clamp01((recoilUntil-Time.time)/.18f);
            graphics.localRotation = Quaternion.Euler(-12f*recoil, 0, (Id%2==0?1:-1)*8f*recoil);
            graphics.localPosition = recoilDirection * (.14f*recoil);

            // Physical reactions pause attacks and pursuit; walls and encounter edges still block pushes.
            if(popped)
            {
                const float gravity=20f;
                float vertical=popVelocity*dt-.5f*gravity*dt*dt;
                popVelocity-=gravity*dt;
                var collisions=controller.Move(Vector3.up*vertical);
                if((collisions & CollisionFlags.Above)!=0 && popVelocity>0) popVelocity=0;
                if(popVelocity<=0 && (collisions & CollisionFlags.Below)!=0)
                { popped=false; popVelocity=0; until=Time.time+.1f; }
                return;
            }

            Vector3 delta=level.Player.PlayerTransform.position-Root.position; delta.y=0;
            float distance=delta.magnitude;
            if(state==State.Idle)
            {
                if(level.CanEngage(this) && distance<(IsBoss?tuning.bossDetectionRange:tuning.detectionRange)) state=State.Chase;
                return;
            }
            if(state==State.Chase)
            {
                if(!level.CanEngage(this)) { state=State.Idle; return; }
                if(delta.sqrMagnitude>.01f) facing=delta.normalized;
                Root.rotation=Quaternion.Slerp(Root.rotation,Quaternion.LookRotation(facing),dt*8);
                float attackDistance = IsBoss ? 3.1f : 2.15f;
                if (distance > attackDistance)
                    MoveAlong(facing + level.Separation(this) * .7f, tuning.ChaseSpeed(IsBoss), dt);
                else if (Time.time >= until && level.TryClaimAttack(this)) BeginWindup();
                else
                {
                    // Non-attacking enemies circle and keep space instead of standing in a pile.
                    float side = Id % 2 == 0 ? 1 : -1;
                    if (Mathf.Sin(Time.time * .8f + Id) < -.6f) side = -side;
                    Vector3 tangent = Vector3.Cross(Vector3.up, facing) * side;
                    Vector3 radial = facing * Mathf.Clamp((distance - attackDistance * .9f) * 1.5f, -.7f, .7f);
                    MoveAlong(tangent + radial + level.Separation(this), Mathf.Max(0, tuning.strafeSpeed), dt);
                }
            }
            else if(state==State.Windup)
            {
                float progress=Mathf.Clamp01((Time.time-windupStart)/windupDuration);
                weapon.localRotation=lowAttack?Quaternion.Euler(10,-90*progress,0):Quaternion.Euler(-120*progress,0,0);
                DrawWarning(progress);
                if(Time.time>=until)
                {
                    warning.enabled=false; state=State.Strike; strikeStartedAt=Time.time;
                    strikeFrom=weapon.localRotation; until=Time.time+Mathf.Max(.08f,tuning.strikeDuration);
                    level.ResolveEnemyAttack(this,lowAttack,IsBoss?3.7f:2.8f,IsBoss?26:16);
                }
            }
            else if(state==State.Strike)
            {
                float progress = Mathf.Clamp01((Time.time-strikeStartedAt)/Mathf.Max(.08f,tuning.strikeDuration));
                var strikeTo = lowAttack?Quaternion.Euler(5,95,0):Quaternion.Euler(70,0,0);
                weapon.localRotation=Quaternion.Slerp(strikeFrom,strikeTo,1f-(1f-progress)*(1f-progress));
                if(Time.time>=until)
                {
                    state=State.Recover;
                    // Both build extremes get a punish window, shortened in phase two.
                    until=Time.time+tuning.Recovery(IsBoss,Health<MaxHealth*.5f);
                }
            }
            else if(state==State.Recover)
            {
                weapon.localRotation=Quaternion.Slerp(weapon.localRotation,Quaternion.identity,dt*5);
                if(Time.time>=until) { state=State.Chase; until=Time.time+.1f; }
            }
        }
        void MoveAlong(Vector3 desired, float speed, float dt, bool steer=true)
        {
            if (desired.sqrMagnitude < .001f) return;
            Vector3 movement = (steer?Steer(desired.normalized):desired.normalized) * speed * dt;
            Vector3 destination = Root.position + movement;
            // Stay on this encounter's route; bridge gaps remain traversal challenges for the player.
            bool ice = level.Player.LevelDefinition != null && level.Player.LevelDefinition.iceWorld;
            float minZ = ice ? -23 : Zone==0?10:Zone==1?64.5f:101;
            float maxZ = ice ? 23 : Zone==0?44:Zone==1?98:125;
            float halfWidth = ice ? 19 : Zone==2?10:(destination.z<16 || destination.z>86?4:8);
            destination.x = Mathf.Clamp(destination.x,-halfWidth,halfWidth);
            destination.z = Mathf.Clamp(destination.z,minZ,maxZ);
            destination = Root.position + Vector3.ClampMagnitude(destination-Root.position, speed*dt);
            // Never chase off a ledge. Ground normals distinguish walkable support from a wall.
            bool supported = false;
            foreach (var hit in Physics.RaycastAll(destination+Vector3.up*.7f, Vector3.down, 1.6f, ~0, QueryTriggerInteraction.Ignore))
                if (!(hit.collider is CharacterController) && !hit.collider.transform.IsChildOf(Root) && !hit.collider.transform.IsChildOf(level.Player.PlayerTransform)
                    && hit.normal.y > .6f) { supported=true; break; }
            if (!supported) return;
            controller.Move(destination-Root.position+(popped?Vector3.zero:Vector3.down*dt*3));
            graphics.localPosition += Vector3.up*(Mathf.Sin(Time.time*12+Id)*.035f);
        }
        Vector3 Steer(Vector3 desired)
        {
            // Short obstacle avoidance for the open courtyards; no NavMesh bake required.
            foreach(var hit in Physics.SphereCastAll(Root.position+Vector3.up,controller.radius,
                desired,1.1f,~0,QueryTriggerInteraction.Ignore))
            {
                if(hit.collider.transform.IsChildOf(Root) || hit.collider.transform.IsChildOf(level.Player.PlayerTransform)) continue;
                Vector3 tangent=Vector3.Cross(Vector3.up,hit.normal); tangent.y=0;
                if(tangent.sqrMagnitude<.01f) continue;
                tangent.Normalize();
                if(Vector3.Dot(tangent,desired)<0) tangent=-tangent;
                return (tangent+desired*.2f).normalized;
            }
            return desired;
        }
        void BeginWindup()
        {
            comboIndex++;
            lowAttack=IsBoss?comboIndex%3!=0:style==1 || (style==2 && comboIndex%2==0);
            windupDuration=tuning.Windup(IsBoss,Health<MaxHealth*.5f);
            windupStart=Time.time; until=Time.time+windupDuration; state=State.Windup;
            Root.rotation=Quaternion.LookRotation(facing); // Attack direction locks at the tell.
            level.PlaySound(lowAttack?"tellLow":"tellHigh");
        }
        void DrawWarning(float progress)
        {
            warning.enabled=true;
            float radius=IsBoss?3.7f:2.8f;
            warning.sharedMaterial.color=lowAttack?new Color(1,.7f,.15f):new Color(1,.2f,.18f);
            warning.widthMultiplier=.04f+progress*.08f;
            for(int i=0;i<26;i++)
            {
                float a=Mathf.Lerp(-65,65,i/25f)*Mathf.Deg2Rad;
                warning.SetPosition(i,Root.position+Root.rotation*new Vector3(Mathf.Sin(a)*radius,.05f,Mathf.Cos(a)*radius));
            }
        }
        public float ReceiveHit(float damage,float charge)
        {
            if(!Alive) return 0;
            float dealt=Mathf.Min(Health,damage); Health-=dealt; flashUntil=Time.time+.1f;
            recoilUntil=Time.time+.18f;
            Vector3 away=Root.position-level.Player.PlayerTransform.position; away.y=0;
            recoilDirection=Root.InverseTransformDirection(away.normalized);
            if (state==State.Idle && level.CanEngage(this)) state=State.Chase;
            if(Health<=0)
            {
                state=State.Dead; deadAt=Time.time; controller.enabled=false; warning.enabled=false;
                level.EnemyDefeated(this);
            }
            else if(!IsBoss && charge>=.8f && Time.time>=staggerReady)
            {
                state=State.Recover; until=Time.time+.5f; staggerReady=Time.time+2.5f; warning.enabled=false;
            }
            return dealt;
        }
        public void ReactToSpecial(PlayerAttackKind kind, Vector3 direction)
        {
            if(IsBoss) return;
            if(kind==PlayerAttackKind.Thrust && tuning.thrustPushDistance>0)
            {
                direction.y=0;
                if(direction.sqrMagnitude<.001f) return;
                pushDirection=direction.normalized;
                pushRemaining=Mathf.Max(.05f,tuning.thrustPushDuration);
                // ReceiveHit disables movement on death; let the killing thrust push the body first.
                controller.enabled=true;
            }
            else if(Alive && kind==PlayerAttackKind.JumpingOverhead && tuning.landingPopHeight>0)
            {
                pushRemaining=0;
                popVelocity=Mathf.Sqrt(2f*20f*tuning.landingPopHeight);
                popped=true;
            }
            else return;
            if(!Alive) return;
            state=State.Recover; warning.enabled=false;
            until=Time.time+Mathf.Max(pushRemaining,.1f);
        }
    }
}
