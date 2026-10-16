using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace SwordKing.Tests
{
    public sealed class KnightPrefabTests
    {
        [Test]
        public void KnightImportsWithVisibleMeshesAndIsolatedCollision()
        {
            var rig = AssetDatabase.LoadAssetAtPath<PlayerRig>(
                "Assets/_SwordKing/Prefabs/Player/ArmoredKnight.prefab");
            Assert.That(rig, Is.Not.Null);
            Assert.That(rig.controller, Is.Not.Null);
            Assert.That(rig.visualRoot, Is.Not.Null);
            Assert.That(rig.visualRoot.GetComponentsInChildren<Collider>(true), Is.Empty);
            Assert.That(rig.swordPivot.localPosition, Is.EqualTo(new Vector3(.4f, .25f, .05f)));
            Assert.That(rig.leftLeg.localScale, Is.EqualTo(Vector3.one));
            Assert.That(rig.rightLeg.localScale, Is.EqualTo(Vector3.one));
            var meshes = rig.GetComponentsInChildren<MeshFilter>(true);
            Assert.That(meshes.Length, Is.GreaterThan(50));
            foreach (var mesh in meshes)
                Assert.That(mesh.sharedMesh, Is.Not.Null, mesh.name + " did not import its mesh");
            var animation = rig.GetComponent<KnightAnimation>();
            Assert.That(animation, Is.Not.Null);
            foreach (var joint in new[] { animation.chest, animation.head, animation.leftArm,
                animation.leftForearm, animation.rightForearm, animation.leftShin,
                animation.rightShin, animation.frontTabard, animation.backTabard })
            {
                Assert.That(joint, Is.Not.Null);
                Assert.That(joint.IsChildOf(rig.visualRoot), Is.True);
                Assert.That(joint.localScale, Is.EqualTo(Vector3.one));
            }
        }
    }
}
