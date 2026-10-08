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
        State state;
        float until, windupStart, windupDuration, flashUntil, staggerReady, deadAt;
        int comboIndex;
        bool lowAttack;
        Vector3 facing = Vector3.back;

        public GateEnemy(BrokenGateLevel owner, int id, int zone, string title, Vector3 position, int attackStyle, bool boss = false)
        {
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
            if(defeated) Root.gameObject.SetActive(false);
        }
        public void Tick(float dt)
        {
            if(!Root.gameObject.activeSelf) return;
            if(!Alive)
            {
                graphics.localRotation=Quaternion.Slerp(graphics.localRotation,Quaternion.Euler(0,0,85),dt*5);
                if(Time.time-deadAt>1.5f) Root.gameObject.SetActive(false);
                return;
            }
            bodyMaterial.color=Time.time<flashUntil?Color.white:baseColor;
            Vector3 delta=level.Player.PlayerTransform.position-Root.position; delta.y=0;
            float distance=delta.magnitude;
            if(state==State.Idle)
            {
                if(level.CanEngage(this) && distance<(IsBoss?18:12)) state=State.Chase;
                return;
            }
            if(state==State.Chase)
            {
                if(!level.CanEngage(this)) { state=State.Idle; return; }
                if(delta.sqrMagnitude>.01f) facing=delta.normalized;
                Root.rotation=Quaternion.Slerp(Root.rotation,Quaternion.LookRotation(facing),dt*8);
                if(distance > (IsBoss?3.1f:2.15f))
                {
                    float speed=IsBoss?2.5f:2.25f;
                    Vector3 destination=Root.position+Steer(facing)*speed*dt;
                    float minZ=Zone==0?17:Zone==1?65:102, maxZ=Zone==0?38:Zone==1?84:124;
                    destination.x=Mathf.Clamp(destination.x,Zone==2?-10:-8,Zone==2?10:8);
                    destination.z=Mathf.Clamp(destination.z,minZ,maxZ);
                    controller.Move(destination-Root.position+Vector3.down*dt*3);
                    graphics.localPosition=Vector3.up*(Mathf.Sin(Time.time*10+Id)*.035f);
                }
                else if(Time.time>=until && level.TryClaimAttack(this)) BeginWindup();
            }
            else if(state==State.Windup)
            {
                float progress=Mathf.Clamp01((Time.time-windupStart)/windupDuration);
                weapon.localRotation=lowAttack?Quaternion.Euler(10,-90*progress,0):Quaternion.Euler(-120*progress,0,0);
                DrawWarning(progress);
                if(Time.time>=until)
                {
                    warning.enabled=false; state=State.Strike; until=Time.time+.16f;
                    level.ResolveEnemyAttack(this,lowAttack,IsBoss?3.7f:2.8f,IsBoss?26:16);
                }
            }
            else if(state==State.Strike)
            {
                weapon.localRotation=lowAttack?Quaternion.Euler(5,95,0):Quaternion.Euler(70,0,0);
                if(Time.time>=until)
                {
                    state=State.Recover;
                    // Both build extremes get a punish window, shortened in phase two.
                    until=Time.time+(IsBoss?(Health<MaxHealth*.5f?.85f:1.3f):1.25f);
                }
            }
            else if(state==State.Recover)
            {
                weapon.localRotation=Quaternion.Slerp(weapon.localRotation,Quaternion.identity,dt*5);
                if(Time.time>=until) { state=State.Chase; until=Time.time+.15f; }
            }
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
            windupDuration=IsBoss?(Health<MaxHealth*.5f?.65f:.9f):.9f;
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
    }
}
