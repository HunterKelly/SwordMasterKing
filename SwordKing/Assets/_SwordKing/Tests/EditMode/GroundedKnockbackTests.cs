using NUnit.Framework;
using UnityEngine;

namespace SwordKing.Tests
{
    public sealed class GroundedKnockbackTests
    {
        [Test]
        public void ThrustMovesAwayFromHoppingCapsuleWithoutGainingHeight()
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var player = new GameObject("Hopping player");
            var enemy = new GameObject("Pushed enemy");
            try
            {
                floor.transform.position = new Vector3(0, -.25f, 0);
                floor.transform.localScale = new Vector3(10, .5f, 10);
                var playerController = player.AddComponent<CharacterController>();
                playerController.height = 1.9f; playerController.center = Vector3.up * .95f;
                playerController.radius = .32f;
                player.transform.position = new Vector3(0, .12f, 0);
                var controller = enemy.AddComponent<CharacterController>();
                controller.height = 1.9f; controller.center = Vector3.up * .95f;
                controller.radius = .35f; controller.stepOffset = .3f; controller.slopeLimit = 45;
                enemy.transform.position = new Vector3(0, .02f, .61f);
                Physics.SyncTransforms();
                float startY = enemy.transform.position.y, startZ = enemy.transform.position.z;
                for (int frame = 0; frame < 12; frame++)
                {
                    GroundedKnockback.Move(controller, new Vector3(0, .2f, .125f), 1f / 60);
                    Assert.That(enemy.transform.position.y, Is.LessThanOrEqualTo(startY + .001f));
                    Assert.That(controller.stepOffset, Is.EqualTo(.3f));
                    Assert.That(controller.slopeLimit, Is.EqualTo(45));
                }
                Assert.That(enemy.transform.position.z, Is.GreaterThan(startZ + .5f));
            }
            finally
            {
                Object.DestroyImmediate(enemy);
                Object.DestroyImmediate(player);
                Object.DestroyImmediate(floor);
            }
        }
    }
}
