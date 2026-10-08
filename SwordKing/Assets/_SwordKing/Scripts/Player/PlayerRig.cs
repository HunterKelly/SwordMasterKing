using System.Collections.Generic;
using UnityEngine;

namespace SwordKing
{
    // Authorable prefab references; replace geometry while retaining these pivots.
    public sealed class PlayerRig : MonoBehaviour
    {
        public CharacterController controller;
        public Transform visualRoot, swordPivot, leftLeg, rightLeg;

        public static PlayerRig CreateDefault(Material floor, Material stone, Material metal, Material gold, Material cloth)
        {
            var player = new GameObject("Swordsman").transform;
            var controller = player.gameObject.AddComponent<CharacterController>();
            controller.height = 1.9f; controller.radius = .32f; controller.center = Vector3.up * .95f;
            Shape("Torso", PrimitiveType.Capsule, new Vector3(0, 1.15f, 0), new Vector3(.65f, .5f, .4f), cloth, player);
            Shape("Helmet", PrimitiveType.Sphere, new Vector3(0, 1.83f, 0), Vector3.one * .43f, metal, player);
            Shape("Visor", PrimitiveType.Cube, new Vector3(0, 1.84f, .21f), new Vector3(.32f, .07f, .05f), floor, player);
            var leftLeg = Shape("Left leg", PrimitiveType.Cube, new Vector3(-.18f, .4f, 0), new Vector3(.22f, .75f, .25f), stone, player).transform;
            var rightLeg = Shape("Right leg", PrimitiveType.Cube, new Vector3(.18f, .4f, 0), new Vector3(.22f, .75f, .25f), stone, player).transform;
            Shape("Left arm", PrimitiveType.Capsule, new Vector3(-.43f, 1.2f, 0), new Vector3(.2f, .35f, .2f), metal, player);
            var swordPivot = new GameObject("Procedural sword arm").transform;
            swordPivot.SetParent(player, false); swordPivot.localPosition = new Vector3(.4f, 1.2f, .05f);
            Shape("Sword arm", PrimitiveType.Cube, new Vector3(0, 0, .25f), new Vector3(.2f, .2f, .5f), metal, swordPivot);
            Shape("Grip", PrimitiveType.Cube, new Vector3(0, 0, .58f), new Vector3(.1f, .12f, .3f), stone, swordPivot);
            Shape("Guard", PrimitiveType.Cube, new Vector3(0, 0, .75f), new Vector3(.48f, .12f, .09f), gold, swordPivot);
            Shape("Blade", PrimitiveType.Cube, new Vector3(0, 0, 1.35f), new Vector3(.16f, .055f, 1.15f), metal, swordPivot);
            // Animate only the graphics. Movement collision and damage volumes stay upright.
            var graphics = new List<Transform>();
            foreach (Transform child in player) graphics.Add(child);
            var visualRoot = new GameObject("Visuals only - roll pivot").transform;
            visualRoot.SetParent(player, false); visualRoot.localPosition = Vector3.up * .95f;
            foreach (var child in graphics) child.SetParent(visualRoot, true);
            var rig = player.gameObject.AddComponent<PlayerRig>();
            rig.controller = controller; rig.visualRoot = visualRoot; rig.swordPivot = swordPivot;
            rig.leftLeg = leftLeg; rig.rightLeg = rightLeg;
            return rig;
        }
        static GameObject Shape(string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material, Transform parent)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name; go.transform.SetParent(parent, false);
            go.transform.localPosition = position; go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            var collider = go.GetComponent<Collider>(); collider.enabled = false;
            if (Application.isPlaying) Object.Destroy(collider); else Object.DestroyImmediate(collider);
            return go;
        }
    }
}
