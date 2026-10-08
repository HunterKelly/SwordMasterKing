using System.Collections.Generic;
using UnityEngine;

namespace SwordKing
{
    // Deterministic, original low-poly geometry; no external models or textures.
    public sealed class BrokenGateWorld : MonoBehaviour
    {
        public Transform Root => transform;
        public Material Stone, Dark, Iron, Gold, Ember, Cloth, Bone;
        public GameObject CourtyardGate, GatehouseGate, BossEntrance, ExitGate, Cache;
        public Vector3 Camp = new Vector3(0, .15f, 93);
        public Vector3 CachePosition = new Vector3(10, .1f, 60);
        readonly List<Material> materials = new List<Material>();

        public void EnsureMaterials()
        {
            if (Stone != null) return;
            Stone = Material(new Color(.30f,.35f,.39f)); Dark = Material(new Color(.13f,.18f,.21f));
            Iron = Material(new Color(.43f,.51f,.55f)); Gold = Material(new Color(.85f,.60f,.24f));
            Ember = Material(new Color(1f,.42f,.09f), true); Cloth = Material(new Color(.15f,.45f,.43f));
            Bone = Material(new Color(.75f,.69f,.52f));
        }
        public Material Material(Color color, bool unlit = false)
        {
            var template = Resources.Load<Material>(unlit ? "BrokenGateUnlit" : "BrokenGateLit");
            Material mat;
            if (template != null) mat = new Material(template);
            else
            {
                Shader shader = Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find(unlit ? "Unlit/Color" : "Standard");
                mat = new Material(shader);
            }
            mat.color = color; materials.Add(mat); return mat;
        }
        public GameObject Shape(string name, PrimitiveType type, Vector3 position, Vector3 scale, Material mat, bool solid = false, Transform parent = null)
        {
            var go = GameObject.CreatePrimitive(type); go.name = name;
            go.transform.SetParent(parent == null ? Root : parent, false);
            go.transform.localPosition = position; go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!solid) { var col = go.GetComponent<Collider>(); col.enabled = false; if (Application.isPlaying) Object.Destroy(col); else Object.DestroyImmediate(col); }
            return go;
        }
        public GameObject Box(string name, Vector3 p, Vector3 size, Material mat, bool solid = false, Transform parent = null)
            => Shape(name, PrimitiveType.Cube, p, size, mat, solid, parent);
        void Floor(float x, float z, float width, float length)
        {
            Box("Fortress foundation", new Vector3(x,-1,z), new Vector3(width,2,length), Stone, true);
            for (float zz = z-length/2+1; zz < z+length/2; zz+=2)
                Box("Worn paving seam",new Vector3(x,.005f,zz), new Vector3(width-.2f,.012f,.045f), Dark);
        }
        void Wall(float x, float z, float length)
        {
            Box("Rampart",new Vector3(x,1.8f,z),new Vector3(1,3.6f,length),Stone,true);
            for (float zz=z-length/2+.5f; zz<z+length/2; zz+=2)
                Box("Battlement",new Vector3(x,4,zz),new Vector3(1.1f,.9f,1),Dark,true);
        }
        void Tower(float x,float z)
        {
            Shape("Watchtower",PrimitiveType.Cylinder,new Vector3(x,3,z),new Vector3(4,3,4),Dark,true);
            Shape("Tower crown",PrimitiveType.Cylinder,new Vector3(x,6,z),new Vector3(4.5f,.35f,4.5f),Stone);
            for(int i=0;i<8;i++)
            {
                float a=i*Mathf.PI/4;
                Box("Crown merlon",new Vector3(x+Mathf.Sin(a)*1.9f,6.7f,z+Mathf.Cos(a)*1.9f),new Vector3(.7f,1,.7f),Stone);
            }
            Banner(new Vector3(x,4.6f,z-2.1f));
        }
        void Banner(Vector3 p)
        {
            Box("Banner pole",p+Vector3.up*.5f,new Vector3(1.5f,.08f,.08f),Gold);
            Box("Faded teal standard",p-Vector3.up*.4f,new Vector3(1.2f,1.8f,.07f),Cloth);
            Box("Gold crest",p-Vector3.up*.4f,new Vector3(.25f,.8f,.09f),Gold);
        }
        public void Torch(Vector3 p)
        {
            Shape("Brazier stand",PrimitiveType.Cylinder,p+Vector3.up*.6f,new Vector3(.18f,.6f,.18f),Iron);
            Shape("Fire basket",PrimitiveType.Cylinder,p+Vector3.up*1.15f,new Vector3(.6f,.15f,.6f),Gold);
            Shape("Flame",PrimitiveType.Sphere,p+Vector3.up*1.45f,new Vector3(.36f,.65f,.36f),Ember);
            var light = new GameObject("Warm torchlight").AddComponent<Light>(); light.transform.SetParent(Root);
            light.transform.position = p+Vector3.up*1.5f; light.type=LightType.Point; light.color=new Color(1,.48f,.15f); light.range=5; light.intensity=2;
        }
        GameObject Gate(float z,float width,string title)
        {
            Box(title+" west wall",new Vector3(-(width+6)/4,2.5f,z),new Vector3((width-6)/2,5,1.2f),Stone,true);
            Box(title+" east wall",new Vector3((width+6)/4,2.5f,z),new Vector3((width-6)/2,5,1.2f),Stone,true);
            Box(title+" lintel",new Vector3(0,5,z),new Vector3(7,1,1.5f),Dark,true);
            for(int i=-1;i<=1;i+=2)
            {
                Box("Gate pillar",new Vector3(i*3.3f,2.4f,z),new Vector3(.7f,4.8f,1.6f),Dark,true);
                Torch(new Vector3(i*4.2f,0,z-1.4f));
            }
            var gate=new GameObject(title).transform; gate.SetParent(Root); gate.position=new Vector3(0,0,z);
            Box("Gate collision",new Vector3(0,2.1f,0),new Vector3(6,4.2f,.25f),Iron,true,gate).GetComponent<Renderer>().enabled=false;
            for(int i=-3;i<=3;i++) Box("Portcullis bar",new Vector3(i*.9f,2.1f,0),new Vector3(.12f,4.2f,.18f),Iron,false,gate);
            Box("Portcullis brace",new Vector3(0,1.3f,0),new Vector3(5.8f,.18f,.22f),Gold,false,gate);
            Box("Portcullis brace",new Vector3(0,3.1f,0),new Vector3(5.8f,.18f,.22f),Gold,false,gate);
            return gate.gameObject;
        }
        public void Build()
        {
            EnsureMaterials();
            Floor(0,3,10,26); Wall(-5,3,26); Wall(5,3,26);
            Box("Arrival cliff barrier",new Vector3(0,2,-10),new Vector3(10,4,1),Dark,true);
            Floor(0,28,20,24); Wall(-10,28,24); Wall(10,28,24);
            Tower(-8,19); Tower(8,19); Banner(new Vector3(-9.4f,2.4f,31));
            CourtyardGate=Gate(40,20,"Courtyard seal");
            Floor(0,43.5f,6,7); Floor(0,51.5f,6,6); Floor(0,60,6,8);
            // Jump gaps: 47–48.5 and 54.5–56. Side reward is optional.
            Floor(6,59,6,2); Floor(10,60,4,8);
            Cache = Box("Lost knight's coffer",CachePosition+Vector3.up*.4f,new Vector3(1.1f,.8f,.7f),Gold);
            Box("Coffer band",new Vector3(0,.15f,0),new Vector3(.18f,.85f,.73f),Iron,false,Cache.transform);
            Torch(new Vector3(11,0,62));
            for(int i=0;i<3;i++)
            {
                float z=43+i*8;
                Box("Broken bridge pier",new Vector3(0,-5,z),new Vector3(2,8,2),Dark);
                Box("Broken parapet",new Vector3(-2.8f,.45f,z),new Vector3(.3f,.9f,2),Stone,true);
            }
            Floor(0,75,20,22); Wall(-10,75,22); Wall(10,75,22);
            Tower(-8,67); Tower(8,67); GatehouseGate=Gate(86,20,"Gatehouse seal");
            Floor(0,93,12,14); Wall(-6,93,14); Wall(6,93,14);
            Shape("Rest shrine",PrimitiveType.Cylinder,new Vector3(0,.18f,93),new Vector3(2,.18f,2),Dark);
            Shape("Shrine ember",PrimitiveType.Sphere,new Vector3(0,1.25f,93),new Vector3(.4f,.65f,.4f),Ember);
            Torch(new Vector3(-3,0,94)); Torch(new Vector3(3,0,94));
            Floor(0,113,24,26); Wall(-12,113,26); Wall(12,113,26);
            BossEntrance=Gate(100,24,"Boss entrance"); BossEntrance.SetActive(false);
            ExitGate=Gate(126,24,"The Broken Gate");
            Tower(-10,123); Tower(10,123);
            Floor(0,133,10,14); Wall(-5,133,14); Wall(5,133,14);
            Box("Far balcony",new Vector3(0,1,140),new Vector3(10,2,1),Stone,true);
            // Inlaid boss-ring markers are cosmetic and cannot snag the controller.
            for(int i=0;i<36;i++)
            {
                float a=i*Mathf.PI/18;
                Box("Duel ring inlay",new Vector3(Mathf.Sin(a)*8,.015f,114+Mathf.Cos(a)*8),new Vector3(.15f,.025f,.4f),Gold);
            }
            var random=new System.Random(117);
            for(int i=0;i<42;i++)
            {
                float side=i%2==0?-1:1, z=(float)random.NextDouble()*165-15;
                float x=side*(18+(float)random.NextDouble()*30), height=10+(float)random.NextDouble()*35;
                var rock=Shape("Distant mountain",PrimitiveType.Cube,new Vector3(x,-10,z),new Vector3(8,height,12),Dark);
                rock.transform.rotation=Quaternion.Euler(0,random.Next(180),side*random.Next(5,30));
            }
            foreach(float z in new[]{3f,12f,27f,36f,70f,81f,107f,120f})
                foreach(int side in new[]{-1,1}) Torch(new Vector3(side*(z<16?3.7f:z<40?8.5f:z<86?8.5f:10),0,z));
        }
        public void Dispose()
        {
            foreach (var mat in materials) if (mat != null) Object.Destroy(mat);
            materials.Clear();
        }

    }
}
