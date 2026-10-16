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
                Assert.That(animation.capeTop.GetComponent<KnightCapeMotion>(), Is.Not.Null);
                Assert.That(animation.capeTop.GetComponentsInChildren<MeshFilter>().Length,
                    Is.EqualTo(2), "Cape should have one cloth mesh and its gold trim");
                foreach (var joint in new[] { animation.chest, animation.head, animation.leftArm,
                    animation.rightArm, animation.leftForearm, animation.rightForearm, animation.leftShin,
                    animation.rightShin, animation.capeTop,
                    animation.weaponGrip, animation.pelvis, animation.leftFoot, animation.rightFoot })
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
        [Test]
        public void SupportingAnkleStaysInWorldSpaceWhileRootMoves()
        {
            var asset = AssetDatabase.LoadAssetAtPath<PlayerRig>(
                "Assets/_SwordKing/Prefabs/Player/ArmoredKnight.prefab");
            var rig = Object.Instantiate(asset);
            try
            {
                var animation = rig.GetComponent<KnightAnimation>();
                Vector3 plantedPosition = Vector3.zero;
                for (int frame = 0; frame < 20; frame++)
                {
                    Vector3 displacement = Vector3.forward * .02f;
                    rig.transform.position += displacement;
                    animation.Present(Vector2.up, false, false, false, false, false,
                        false, PlayerAttackKind.Slash, 2, 0, rig.leftLeg, rig.rightLeg,
                        displacement, 1f / 60);
                    if (frame == 0) plantedPosition = animation.leftFoot.position;
                    else Assert.That(Vector3.Distance(animation.leftFoot.position, plantedPosition),
                        Is.LessThan(.003f), "Support foot slid with the character root");
                }
            }
            finally { Object.DestroyImmediate(rig.gameObject); }
        }

        [Test]
        public void IdlePoseStandsTallWithoutForwardHunch()
        {
            var asset = AssetDatabase.LoadAssetAtPath<PlayerRig>(
                "Assets/_SwordKing/Prefabs/Player/ArmoredKnight.prefab");
            var rig = Object.Instantiate(asset);
            try
            {
                var animation = rig.GetComponent<KnightAnimation>();
                for (int frame = 0; frame < 60; frame++)
                    animation.Present(Vector2.zero, false, false, false, false, false,
                        false, PlayerAttackKind.Slash, 2, 0, rig.leftLeg, rig.rightLeg,
                        Vector3.zero, 1f / 60);
                Assert.That(rig.visualRoot.localPosition.y, Is.GreaterThanOrEqualTo(.905f));
                Assert.That(Quaternion.Angle(animation.chest.localRotation, Quaternion.identity), Is.LessThan(2));
            }
            finally { Object.DestroyImmediate(rig.gameObject); }
        }

        [TestCase(false, 6f)]
        [TestCase(true, 9.6f)]
        public void GroundedLocomotionDoesNotPumpBodyHeight(bool sprint, float speed)
        {
            var asset = AssetDatabase.LoadAssetAtPath<PlayerRig>(
                "Assets/_SwordKing/Prefabs/Player/ArmoredKnight.prefab");
            var rig = Object.Instantiate(asset);
            try
            {
                var animation = rig.GetComponent<KnightAnimation>();
                for (int frame = 0; frame < 240; frame++)
                {
                    var displacement = Vector3.forward * (speed / 60);
                    rig.transform.position += displacement;
                    animation.Present(Vector2.up, sprint, false, false, false, false,
                        false, PlayerAttackKind.Slash, 2, 0, rig.leftLeg, rig.rightLeg,
                        displacement, 1f / 60);
                    Assert.That(rig.visualRoot.localPosition.y, Is.EqualTo(.91f).Within(.0001f));
                }
            }
            finally { Object.DestroyImmediate(rig.gameObject); }
        }

        [TestCase(false, 6f)]
        [TestCase(true, 9.6f)]
        public void FreeLeftArmSwingsOppositeTheSolvedLeftLeg(bool sprint, float speed)
        {
            var asset = AssetDatabase.LoadAssetAtPath<PlayerRig>(
                "Assets/_SwordKing/Prefabs/Player/ArmoredKnight.prefab");
            var rig = Object.Instantiate(asset);
            try
            {
                var animation = rig.GetComponent<KnightAnimation>();
                bool sawForward = false, sawBackward = false;
                for (int frame = 0; frame < 180; frame++)
                {
                    Vector3 displacement = Vector3.forward * (speed / 60);
                    rig.transform.position += displacement;
                    animation.Present(Vector2.up, sprint, false, false, false, false,
                        false, PlayerAttackKind.Slash, 2, 0, rig.leftLeg, rig.rightLeg,
                        displacement, 1f / 60);
                    float footForward = rig.visualRoot.InverseTransformPoint(animation.leftFoot.position).z
                        - rig.leftLeg.localPosition.z - .015f;
                    float armForward = (animation.leftArm.localRotation * Vector3.down).z;
                    if (Mathf.Abs(footForward) > .03f)
                        Assert.That(footForward * armForward, Is.LessThan(0), "Arm and leg swung the same way");
                    sawForward |= footForward > .03f;
                    sawBackward |= footForward < -.03f;
                    Assert.That(rig.visualRoot.localPosition.y, Is.EqualTo(.91f).Within(.0001f));
                }
                Assert.That(sawForward && sawBackward, Is.True);
            }
            finally { Object.DestroyImmediate(rig.gameObject); }
        }

    }
}
