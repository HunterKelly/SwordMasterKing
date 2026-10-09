using System.Collections.Generic;
using UnityEngine;

namespace SwordKing
{
    // All timers use gameplay time, so pause freezes projectiles and burning ground.
    public sealed class CastleHazards : MonoBehaviour
    {
        class Shot { public GameObject visual, marker; public Vector3 from,to; public float born; }
        class Fire { public GameObject visual; public Vector3 position; public float expires,nextTick; }
        readonly List<Shot> shots=new List<Shot>();
        readonly List<Fire> fires=new List<Fire>();
        readonly List<GameObject> globes=new List<GameObject>();
        BrokenGateLevel level;
        GameObject pendingMarker;
        Material flame,health;
        public void Initialize(BrokenGateLevel owner)
        {
            level=owner; flame=level.World.Material(new Color(1,.25f,.025f),true);
            health=level.World.Material(new Color(.2f,1,.35f),true);
            ResetHazards();
        }
        public void ResetHazards()
        {
            foreach(var s in shots) { Destroy(s.visual); Destroy(s.marker); } shots.Clear();
            foreach(var f in fires) Destroy(f.visual); fires.Clear();
            Destroy(pendingMarker); pendingMarker=null;
            foreach(var g in globes) Destroy(g); globes.Clear();
            foreach(var p in CastleWorldBuilder.GlobePositions)
            {
                var g=level.World.Shape("Health globe - restores 40 HP",PrimitiveType.Sphere,p,Vector3.one*.8f,health);
                globes.Add(g);
            }
        }
        public void Telegraph(Vector3 target)
        {
            Destroy(pendingMarker);
            pendingMarker=level.World.Shape("Fireball landing warning",PrimitiveType.Cylinder,target+Vector3.up*.025f,new Vector3(5,.015f,5),flame);
        }
        public void Launch(Vector3 from,Vector3 target)
        {
            shots.Add(new Shot { from=from,to=target,born=Time.time,marker=pendingMarker,
                visual=level.World.Shape("Cinder fireball",PrimitiveType.Sphere,from,Vector3.one*1.1f,flame) });
            pendingMarker=null;
        }
        void Update()
        {
            if(level==null || level.InputBlocked) return;
            for(int i=shots.Count-1;i>=0;i--)
            {
                var s=shots[i]; float t=Mathf.Clamp01((Time.time-s.born)/1.1f);
                s.visual.transform.position=Vector3.Lerp(s.from,s.to,t)+Vector3.up*(Mathf.Sin(t*Mathf.PI)*3);
                if(t<1) continue;
                Destroy(s.visual); Destroy(s.marker); shots.RemoveAt(i);
                DamageAt(s.to,2.5f,24,false);
                var visual=new GameObject("Burning ground"); visual.transform.SetParent(level.World.Root,false); visual.transform.position=s.to;
                level.World.Shape("Fire footprint",PrimitiveType.Cylinder,Vector3.up*.04f,new Vector3(5,.025f,5),flame,false,visual.transform);
                for(int j=0;j<8;j++)
                {
                    float a=j*Mathf.PI/4;
                    level.World.Shape("Ground flame",PrimitiveType.Sphere,new Vector3(Mathf.Sin(a)*1.5f,.35f,Mathf.Cos(a)*1.5f),new Vector3(.3f,.8f,.3f),flame,false,visual.transform);
                }
                fires.Add(new Fire { position=s.to,visual=visual,expires=Time.time+level.Player.LevelDefinition.fireLifetime,nextTick=Time.time+.5f });
            }
            for(int i=fires.Count-1;i>=0;i--)
            {
                var f=fires[i];
                if(Time.time>=f.expires) { Destroy(f.visual); fires.RemoveAt(i); continue; }
                if(Time.time>=f.nextTick) { f.nextTick=Time.time+.5f; DamageAt(f.position,2.5f,8,true); }
            }
            for(int i=globes.Count-1;i>=0;i--)
            {
                var g=globes[i]; g.transform.Rotate(0,60*Time.deltaTime,0);
                if(Vector3.Distance(level.Player.PlayerTransform.position+Vector3.up*.7f,g.transform.position)<1.5f && level.Player.Heal(40))
                { level.PlaySound("heal"); Destroy(g); globes.RemoveAt(i); }
            }
        }
        void DamageAt(Vector3 center,float radius,float damage,bool ground)
        {
            Vector3 p=level.Player.PlayerTransform.position,delta=p-center; delta.y=0;
            if(delta.magnitude<=radius && Mathf.Abs(p.y-center.y)<(ground?.7f:2.5f))
                level.Player.TryReceiveDamage(damage,ground);
        }
    }
}
