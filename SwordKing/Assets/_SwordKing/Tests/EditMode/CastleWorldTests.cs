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
                foreach(var p in new[]{new Vector3(0,8,56),new Vector3(-15,30,72),new Vector3(0,30,98)})
                {
                    Assert.That(Physics.Raycast(p+Vector3.up*.1f,Vector3.down,out var hit,.2f),Is.True);
                    Assert.That(hit.point.y,Is.EqualTo(p.y).Within(.02f));
                }
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
