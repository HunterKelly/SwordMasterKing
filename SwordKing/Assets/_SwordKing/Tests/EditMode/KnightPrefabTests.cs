using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace SwordKing.Tests
{
    public sealed class KnightPrefabTests
    {
        [Test]
        public void KnightSourceMeshesLoadWithoutModelImporterSubassetIds()
        {
            var asset = AssetDatabase.LoadAssetAtPath<PlayerRig>(
                "Assets/_SwordKing/Prefabs/Player/ArmoredKnight.prefab");
            Assert.That(asset, Is.Not.Null);
            var rig = Object.Instantiate(asset);
            try
            {
                Assert.That(rig.controller, Is.Not.Null);
                Assert.That(rig.visualRoot, Is.Not.Null);
                Assert.That(rig.visualRoot.GetComponentsInChildren<Collider>(true), Is.Empty);
                Assert.That(rig.swordPivot.localPosition, Is.EqualTo(new Vector3(.28f, .36f, .04f)));
                Assert.That(rig.leftLeg.localScale, Is.EqualTo(Vector3.one));
                Assert.That(rig.rightLeg.localScale, Is.EqualTo(Vector3.one));
                var sources = rig.GetComponentsInChildren<KnightMeshSource>(true);
                Assert.That(sources.Length, Is.GreaterThan(30));
                foreach (var source in sources)
                {
                    Assert.That(source.source, Is.Not.Null, source.name);
                    source.EnsureMesh();
                    var filter = source.GetComponent<MeshFilter>();
                    Assert.That(filter.sharedMesh, Is.Not.Null, source.name);
                    Assert.That(filter.sharedMesh.vertexCount, Is.GreaterThan(0));
                    Assert.That(source.GetComponent<MeshRenderer>().sharedMaterial, Is.Not.Null);
                }
                var animation = rig.GetComponent<KnightAnimation>();
                Assert.That(animation, Is.Not.Null);
                foreach (var joint in new[] { animation.chest, animation.head, animation.leftArm,
                    animation.rightArm, animation.leftForearm, animation.rightForearm, animation.leftShin,
                    animation.rightShin, animation.capeTop, animation.capeMid, animation.capeHem,
                    animation.weaponGrip })
                {
                    Assert.That(joint, Is.Not.Null);
                    Assert.That(joint.IsChildOf(rig.visualRoot), Is.True);
                    Assert.That(joint.localScale, Is.EqualTo(Vector3.one));
                }
            }
            finally { Object.DestroyImmediate(rig.gameObject); }
        }

        [Test]
        public void PlateArmorAnimationDoesNotMoveGameplayCollision()
        {
            var asset = AssetDatabase.LoadAssetAtPath<PlayerRig>(
                "Assets/_SwordKing/Prefabs/Player/ArmoredKnight.prefab");
            var rig = Object.Instantiate(asset);
            try
            {
                var collisionPosition = rig.controller.transform.position;
                var center = rig.controller.center;
                var animation = rig.GetComponent<KnightAnimation>();
                foreach (var kind in new[] { PlayerAttackKind.Slash, PlayerAttackKind.Overhead,
                    PlayerAttackKind.Spin, PlayerAttackKind.Thrust, PlayerAttackKind.JumpingOverhead })
                    for (int i = 0; i <= 10; i++)
                        animation.Present(Vector2.one, true, false, false, false, false,
                            false, kind, i / 10f, 0, rig.leftLeg, rig.rightLeg);
                Assert.That(rig.controller.transform.position, Is.EqualTo(collisionPosition));
                Assert.That(rig.controller.center, Is.EqualTo(center));
            }
            finally { Object.DestroyImmediate(rig.gameObject); }
        }
    }
}
