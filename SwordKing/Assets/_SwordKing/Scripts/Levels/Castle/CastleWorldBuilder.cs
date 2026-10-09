using System.Collections.Generic;
using UnityEngine;

namespace SwordKing
{
    public static class CastleWorldBuilder
    {
        public static LevelDefinition.EnemySpawn[] Encounters(LevelDefinition d)
        {
            var list=new List<LevelDefinition.EnemySpawn>();
            foreach(var p in new[]{new Vector3(-9,.15f,13),new Vector3(9,.15f,13),new Vector3(0,.15f,23),
                new Vector3(-17,8.15f,36),new Vector3(17,8.15f,36),new Vector3(-9,8.15f,49),new Vector3(9,8.15f,49),new Vector3(0,8.15f,59)})
                list.Add(new LevelDefinition.EnemySpawn(list.Count,p.y<4?0:1,"Cinder Guard "+(list.Count+1),p,list.Count%3));
            list.Add(new LevelDefinition.EnemySpawn(list.Count,2,d.bossName,new Vector3(0,30.15f,98),2,true));
            return list.ToArray();
        }
        public static readonly Vector3[] GlobePositions={new Vector3(-12,30.8f,88),new Vector3(12,30.8f,88),new Vector3(-12,30.8f,103),new Vector3(12,30.8f,103)};
        public static void Build(BrokenGateWorld w)
        {
            w.EnsureMaterials();
            var stone=w.Material(new Color(.36f,.32f,.29f));
            var snow=w.Material(new Color(.85f,.91f,.97f));
            var carpet=w.Material(new Color(.38f,.055f,.06f));
            // Snow approach leads into a 48 metre wide, double-height entrance hall.
            w.Box("Frozen approach",new Vector3(0,-.5f,-12),new Vector3(24,1,28),snow,true);
            w.Box("Great hall foundation",new Vector3(0,-.5f,18),new Vector3(48,1,36),stone,true);
            w.Box("Royal carpet",new Vector3(0,.015f,16),new Vector3(6,.025f,31),carpet);
            foreach(int side in new[]{-1,1})
            {
                w.Box("Castle outer wall",new Vector3(side*24,9,32),new Vector3(1,18,68),stone,true);
                w.Box("Entrance door pier",new Vector3(side*15,8,0),new Vector3(18,16,1),stone,true);
                w.Box("Upper side gallery",new Vector3(side*22,7.7f,15),new Vector3(4,.6f,30),stone,true);
                // Each stair rises 8m in 32 steps; broad treads stay under the motor's step limit.
                for(int i=0;i<32;i++)
                {
                    float h=(i+1)*.25f;
                    w.Box("Grand staircase "+side+" step "+i,new Vector3(side*17,h*.5f,9+i*.7f),new Vector3(7,h,.72f),stone,true);
                }
                for(float z=5;z<64;z+=10)
                {
                    w.Box("Hall buttress",new Vector3(side*23,6,z),new Vector3(2,12,2),w.Dark,true);
                    w.Box("Crimson banner",new Vector3(side*22,10,z),new Vector3(.1f,4,1.7f),carpet);
                    w.Torch(new Vector3(side*21,z<34?0:8,z));
                }
                // Inner rail guards the open balcony, with an opening at the stair landing.
                w.Box("Balcony rail",new Vector3(side*20,8.7f,16),new Vector3(.3f,1.4f,27),w.Gold,true);
            }
            foreach(int side in new[]{-1,1})
            {
                w.Box("Facade tower",new Vector3(side*27,12,1),new Vector3(7,24,8),stone,true);
                for(int i=0;i<5;i++) w.Box("Facade merlon",new Vector3(side*27-3+i*1.5f,24.7f,-3),new Vector3(.8f,1.4f,1),w.Dark);
                w.Box("Royal facade standard",new Vector3(side*27,16,-3.1f),new Vector3(3,7,.1f),carpet);
                w.Torch(new Vector3(side*7,0,-3));
            }
            w.Box("Entrance lintel",new Vector3(0,15,0),new Vector3(12,2,1),stone,true);
            w.Box("Hall ceiling",new Vector3(0,18,26),new Vector3(48,.7f,56),stone,true);
            w.Box("Upper royal gallery",new Vector3(0,7.7f,47.55f),new Vector3(48,.6f,32.9f),stone,true);
            w.Box("Upper carpet",new Vector3(0,8.015f,49),new Vector3(6,.025f,30),carpet);
            w.Box("Tower threshold",new Vector3(0,7.7f,65),new Vector3(12,.6f,4),stone,true);
            // Spiral: two full turns, 96 broad treads, from gallery at 8m to crown at 30m.
            for(int i=0;i<96;i++)
            {
                float a=Mathf.PI+i*Mathf.PI*4/96, y=8+(i+1)*22f/96;
                var tread=w.Box("Spiral tower tread "+i,new Vector3(Mathf.Sin(a)*7,y-.15f,72+Mathf.Cos(a)*7),new Vector3(1.05f,.3f,4),stone,true);
                tread.transform.rotation=Quaternion.Euler(0,a*Mathf.Rad2Deg,0);
                var rail=w.Box("Spiral outer rail",new Vector3(Mathf.Sin(a)*9,y+.65f,72+Mathf.Cos(a)*9),new Vector3(1.3f,1.3f,.2f),w.Gold,true);
                rail.transform.rotation=tread.transform.rotation;
            }
            w.Shape("Tower central column",PrimitiveType.Cylinder,new Vector3(0,19,72),new Vector3(8,11,8),w.Dark,true);
            for(int i=0;i<24;i++)
            {
                float a=i*Mathf.PI*2/24;
                if(Mathf.Cos(a)<-.7f) continue; // Door openings face both gallery and crown bridge.
                var wall=w.Box("Tower shell",new Vector3(Mathf.Sin(a)*11,19,72+Mathf.Cos(a)*11),new Vector3(3,24,1),stone,true);
                wall.transform.rotation=Quaternion.Euler(0,a*Mathf.Rad2Deg,0);
            }
            // Last tread rejoins the south landing. An elevated west bridge reaches the arena.
            w.Box("Tower crown landing",new Vector3(0,29.7f,63),new Vector3(20,.6f,4),stone,true);
            w.Box("Crown bridge",new Vector3(-15,29.7f,74.5f),new Vector3(10,.6f,27),stone,true);
            w.Box("Landing bridge connector",new Vector3(-10,29.7f,63),new Vector3(10,.6f,4),stone,true);
            w.Shape("Cinder King arena",PrimitiveType.Cylinder,new Vector3(0,29.7f,98),new Vector3(40,.3f,40),stone,true);
            for(int i=0;i<40;i++)
            {
                float a=i*Mathf.PI*2/40;
                if(Mathf.Sin(a)<-.5f && Mathf.Cos(a)<-.5f) continue;
                var wall=w.Box("Crown battlement",new Vector3(Mathf.Sin(a)*20,31,98+Mathf.Cos(a)*20),new Vector3(3.3f,2,1),stone,true);
                wall.transform.rotation=Quaternion.Euler(0,a*Mathf.Rad2Deg,0);
            }
            foreach(float x in new[]{-15f,15f}) foreach(float z in new[]{88f,108f}) w.Torch(new Vector3(x,30,z));
            w.Camp=new Vector3(0,8.15f,56); w.Torch(w.Camp+Vector3.left*3); w.Torch(w.Camp+Vector3.right*3);
            w.CachePosition=new Vector3(19,8.15f,44);
            w.Cache=w.Box("Royal supply coffer",w.CachePosition+Vector3.up*.4f,new Vector3(1,.8f,.7f),w.Gold);
            w.CourtyardGate=Marker(w,"Hall seal unused"); w.GatehouseGate=Marker(w,"Gallery seal unused");
            w.BossEntrance=Marker(w,"Crown encounter"); w.ExitGate=Marker(w,"Castle victory");
        }
        static GameObject Marker(BrokenGateWorld w,string name)
        { var g=new GameObject(name); g.transform.SetParent(w.Root,false); return g; }
    }
}
