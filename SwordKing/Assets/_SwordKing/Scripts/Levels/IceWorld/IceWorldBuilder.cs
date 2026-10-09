using System.Collections.Generic;
using UnityEngine;

namespace SwordKing
{
    public static class IceWorldBuilder
    {
        public static bool InsideCover(Vector3 p, float margin = 1.5f)
        {
            foreach(float x in new[]{-10f,10f}) foreach(float z in new[]{-10f,10f})
                if(Mathf.Abs(p.x-x)<5+margin && Mathf.Abs(p.z-z)<2.5f+margin) return true;
            return Mathf.Abs(p.x)<3+margin && Mathf.Abs(p.z)<3+margin;
        }
        public static LevelDefinition.EnemySpawn[] Encounters(LevelDefinition definition)
        {
            var random = new System.Random(definition.placementSeed);
            var result = new List<LevelDefinition.EnemySpawn>();
            int count = Mathf.Clamp(definition.randomEnemyCount,1,24);
            for(int attempt=0; result.Count<count && attempt<10000; attempt++)
            {
                var p = new Vector3((float)random.NextDouble()*36-18,.15f,(float)random.NextDouble()*36-16);
                if(InsideCover(p) || Vector3.Distance(p,definition.arrival)<8 || Vector3.Distance(p,new Vector3(0,.15f,21))<6) continue;
                bool close=false;
                foreach(var spawn in result) if(Vector3.Distance(p,spawn.position)<3) {close=true; break;}
                if(close) continue;
                result.Add(new LevelDefinition.EnemySpawn(result.Count,0,"Frost Watch "+(result.Count+1),p,random.Next(3)));
            }
            if(result.Count!=count) throw new System.InvalidOperationException("Unable to place Ice World encounters safely.");
            result.Add(new LevelDefinition.EnemySpawn(count,2,definition.bossName,new Vector3(0,.15f,21),2,true));
            return result.ToArray();
        }
        public static void Build(BrokenGateWorld w)
        {
            w.EnsureMaterials();
            var snow=w.Material(new Color(.86f,.92f,.97f));
            var stone=w.Material(new Color(.48f,.56f,.64f));
            var wood=w.Material(new Color(.23f,.20f,.18f));
            w.Box("Snow courtyard 44 x 52 metres",new Vector3(0,-.5f,0),new Vector3(44,1,52),snow,true);
            Wall(w,stone,snow,new Vector3(-22,2,0),new Vector3(1.5f,4,52));
            Wall(w,stone,snow,new Vector3(22,2,0),new Vector3(1.5f,4,52));
            Wall(w,stone,snow,new Vector3(0,2,-26),new Vector3(44,4,1.5f));
            Wall(w,stone,snow,new Vector3(0,2,26),new Vector3(44,4,1.5f));
            foreach(float x in new[]{-10f,10f}) foreach(float z in new[]{-10f,10f})
            {
                Wall(w,stone,snow,new Vector3(x,1.6f,z),new Vector3(10,3.2f,5));
                foreach(int side in new[]{-1,1}) Crate(w,wood,snow,new Vector3(x+side*5.8f,0,z+side*2.5f));
            }
            Wall(w,stone,snow,new Vector3(0,1.3f,0),new Vector3(6,2.6f,6));
            foreach(float x in new[]{-20f,20f}) foreach(float z in new[]{-24f,24f})
            {
                // Hollow square crowns match the reference's corner towers.
                Wall(w,stone,snow,new Vector3(x,3.5f,z),new Vector3(4,7,4));
                foreach(int side in new[]{-1,1})
                {
                    Wall(w,stone,snow,new Vector3(x+side*1.7f,7.4f,z),new Vector3(.6f,.8f,4));
                    Wall(w,stone,snow,new Vector3(x,7.4f,z+side*1.7f),new Vector3(4,.8f,.6f));
                }
                Crate(w,wood,snow,new Vector3(x*.85f,0,z*.8f));
            }
            foreach(float z in new[]{-18f,0f,18f}) foreach(int side in new[]{-1,1}) w.Torch(new Vector3(side*20,0,z));
            foreach(float z in new[]{-21f,21f}) foreach(int side in new[]{-1,1})
            {
                Wall(w,stone,snow,new Vector3(side*6,2,z),new Vector3(2,4,2));
                w.Torch(new Vector3(side*5,0,z));
                // Broad low steps can be climbed by the existing character controller.
                for(int i=0;i<4;i++) w.Box("Snow stair",new Vector3(side*10,.1f+i*.15f,z-1+i*.55f),new Vector3(3,.2f+i*.3f,.6f),stone,true);
            }
            w.Camp=new Vector3(0,.15f,-22);
            w.Shape("Frost refuge",PrimitiveType.Cylinder,new Vector3(0,.15f,-22),new Vector3(2,.15f,2),w.Dark);
            w.Torch(new Vector3(-2,0,-22)); w.Torch(new Vector3(2,0,-22));
            w.CachePosition=new Vector3(18,.15f,3);
            w.Cache=w.Box("Frozen supply coffer",w.CachePosition+Vector3.up*.4f,new Vector3(1,.8f,.7f),w.Gold);
            w.CourtyardGate=Marker(w,"Unused courtyard seal"); w.GatehouseGate=Marker(w,"Unused gatehouse seal");
            w.BossEntrance=Marker(w,"Warden encounter marker"); w.ExitGate=Marker(w,"Victory marker");
            var random=new System.Random(91);
            for(int i=0;i<32;i++)
            {
                float a=i*Mathf.PI*2/32, distance=55+(float)random.NextDouble()*30;
                var mountain=w.Shape("Snow mountain",PrimitiveType.Cube,new Vector3(Mathf.Cos(a)*distance,4,Mathf.Sin(a)*distance),new Vector3(18,26+random.Next(25),18),snow);
                mountain.transform.rotation=Quaternion.Euler(0,random.Next(180),30);
                Vector3 p=new Vector3(Mathf.Cos(a)*35,0,Mathf.Sin(a)*42);
                w.Shape("Pine trunk",PrimitiveType.Cylinder,p+Vector3.up*2,new Vector3(.4f,2,.4f),wood);
                for(int j=0;j<3;j++) w.Shape("Snowy pine boughs",PrimitiveType.Capsule,p+Vector3.up*(3+j*1.3f),new Vector3(3-j*.6f,.9f,3-j*.6f),snow);
            }
        }
        static GameObject Marker(BrokenGateWorld w,string title)
        { var go=new GameObject(title); go.transform.SetParent(w.Root,false); return go; }
        static void Wall(BrokenGateWorld w,Material stone,Material snow,Vector3 p,Vector3 size)
        {
            w.Box("Frosted masonry",p,size,stone,true);
            w.Box("Snow cap",p+Vector3.up*(size.y*.5f+.06f),new Vector3(size.x+.15f,.12f,size.z+.15f),snow);
            // Shallow mortar courses add readable stone detail without extra colliders.
            for(float y=.8f;y<size.y;y+=.8f)
                w.Box("Mortar course",p+Vector3.up*(y-size.y*.5f),new Vector3(size.x+.012f,.025f,size.z+.012f),w.Dark);
        }
        static void Crate(BrokenGateWorld w,Material wood,Material snow,Vector3 p)
        {
            w.Box("Supply crate",p+Vector3.up*.75f,new Vector3(1.5f,1.5f,1.5f),wood,true);
            w.Box("Crate snow",p+Vector3.up*1.52f,new Vector3(1.6f,.1f,1.6f),snow);
            foreach(float x in new[]{-.55f,.55f}) w.Box("Crate iron strap",p+new Vector3(x,.75f,0),new Vector3(.08f,1.53f,1.53f),w.Iron);
        }
    }
}
