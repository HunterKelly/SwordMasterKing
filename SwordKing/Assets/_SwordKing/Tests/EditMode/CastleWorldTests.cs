using NUnit.Framework;
using UnityEngine;

namespace SwordKing.Tests
{
    public sealed class CastleWorldTests
    {
        [Test]
        public void CastleRouteHasWalkableLandingsAndNoCeilingAboveGrandStairs()
        {
            var root=new GameObject("Castle test"); var world=root.AddComponent<BrokenGateWorld>();
            try
            {
                CastleWorldBuilder.Build(world); Physics.SyncTransforms();
                foreach(int side in new[]{-1,1})
                    for(int i=0;i<32;i++)
                    {
                        var p=new Vector3(side*17,(i+1)*.25f,9+i*.7f);
                        Assert.That(Physics.Raycast(p+Vector3.up*.1f,Vector3.down,out var hit,.2f),Is.True,"Missing stair "+i);
                        Assert.That(hit.point.y,Is.EqualTo(p.y).Within(.02f));
                        Assert.That(Physics.Raycast(p+Vector3.up*.1f,Vector3.up,2),Is.False,"Blocked stair "+i);
                    }
                foreach(var p in new[]{new Vector3(0,0,48),new Vector3(31,0,44),new Vector3(31,8,44),new Vector3(0,8,61),new Vector3(-15,30,72),new Vector3(-15,30,86),new Vector3(0,30,98)})
                {
                    Assert.That(Physics.Raycast(p+Vector3.up*.1f,Vector3.down,out var hit,.2f),Is.True);
                    Assert.That(hit.point.y,Is.EqualTo(p.y).Within(.02f));
                }
                foreach(int side in new[]{-1,1}) foreach(float y in new[]{0f,8f}) foreach(float z in new[]{17f,44f})
                    Assert.That(Physics.SphereCast(new Vector3(side*22,y+1.2f,z),.35f,Vector3.right*side,out _,5),Is.False,"Side room doorway blocked");
                Assert.That(Physics.Raycast(new Vector3(0,1,48),Vector3.up,25),Is.False,"Courtyard must be open to sky");
                Assert.That(Physics.SphereCast(new Vector3(-15,31,81),.35f,Vector3.forward,out _,8),Is.False,"Boss arena approach blocked");
            }
            finally
            {
                var mats=new System.Collections.Generic.HashSet<Material>();
                foreach(var renderer in root.GetComponentsInChildren<Renderer>()) foreach(var mat in renderer.sharedMaterials) mats.Add(mat);
                foreach(var mat in new[]{world.Stone,world.Dark,world.Iron,world.Gold,world.Ember,world.Cloth,world.Bone}) mats.Add(mat);
                Object.DestroyImmediate(root);
                foreach(var mat in mats) if(mat!=null) Object.DestroyImmediate(mat);
            }
        }
    }
}
