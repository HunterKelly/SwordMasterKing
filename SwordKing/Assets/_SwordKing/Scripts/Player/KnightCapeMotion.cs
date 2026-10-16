using System.Collections.Generic;
using UnityEngine;

namespace SwordKing
{
    // Deform a continuous surface, pinning the shoulder edge. The gold hem uses
    // the same function, so it stays attached. Source/cache meshes remain immutable.
    public sealed class KnightCapeMotion : MonoBehaviour
    {
        sealed class Surface
        {
            public MeshFilter filter;
            public Mesh original, mesh;
            public Vector3[] rest, vertices;
        }

        readonly List<Surface> surfaces = new List<Surface>();
        float movement, sprint, side;
        bool airborne, rolling;
        Transform wearer;
        Quaternion lastRotation;
        float clothMovement, wavePhase, trail, trailVelocity, sideLag, sideVelocity;

        public void SetMotion(float movement, float sprint, bool airborne, bool rolling, float side)
        {
            this.movement = movement; this.sprint = sprint;
            this.airborne = airborne; this.rolling = rolling; this.side = side;
        }

        void Start()
        {
            var rig = GetComponentInParent<PlayerRig>();
            wearer = rig != null ? rig.transform : transform.parent;
            lastRotation = wearer != null ? wearer.rotation : transform.rotation;
            foreach (var source in GetComponentsInChildren<KnightMeshSource>()) source.EnsureMesh();
            foreach (var filter in GetComponentsInChildren<MeshFilter>())
            {
                if (filter.sharedMesh == null) continue;
                var original = filter.sharedMesh;
                var mesh = Instantiate(original);
                mesh.name = original.name + " animated";
                mesh.hideFlags = HideFlags.DontSave;
                mesh.MarkDynamic();
                var rest = mesh.vertices;
                surfaces.Add(new Surface { filter = filter, original = original, mesh = mesh,
                    rest = rest, vertices = new Vector3[rest.Length] });
                filter.sharedMesh = mesh;
            }
        }

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (dt <= 0) return;
            clothMovement = Mathf.Lerp(clothMovement, movement, 1 - Mathf.Exp(-6 * dt));
            wavePhase += dt * Mathf.Lerp(3.2f, 6.5f + sprint, clothMovement);
            float turn = 0;
            if (wearer != null)
            {
                float yaw = (Quaternion.Inverse(lastRotation) * wearer.rotation).eulerAngles.y;
                turn = Mathf.Clamp(Mathf.DeltaAngle(0, yaw) / Mathf.Max(.001f, dt) / 150, -1, 1);
                lastRotation = wearer.rotation;
            }
            float targetTrail = clothMovement * Mathf.Lerp(.08f, .20f, sprint)
                + (airborne ? .06f : 0) + (rolling ? .10f : 0);
            trail = Mathf.SmoothDamp(trail, targetTrail, ref trailVelocity, .22f, Mathf.Infinity, dt);
            sideLag = Mathf.SmoothDamp(sideLag, -side * .04f - turn * .065f,
                ref sideVelocity, .18f, Mathf.Infinity, dt);
            float waveAmount = .008f + clothMovement * Mathf.Lerp(.045f, .065f, sprint);
            foreach (var surface in surfaces)
            {
                for (int i = 0; i < surface.rest.Length; i++)
                {
                    Vector3 p = surface.rest[i];
                    float t = Mathf.Clamp01(-p.y / 1.35f);
                    float weight = t * t;
                    float ripple = Mathf.Sin(wavePhase - t * 6 + p.x * 5) * waveAmount
                        + Mathf.Sin(wavePhase * 1.7f - t * 10 - p.x * 7) * clothMovement * .012f;
                    p.z += (-trail + ripple) * weight;
                    p.x += (sideLag + Mathf.Sin(wavePhase * .7f - t * 4)
                        * (.006f + clothMovement * .022f)) * weight;
                    surface.vertices[i] = p;
                }
                surface.mesh.vertices = surface.vertices;
                surface.mesh.RecalculateNormals();
                surface.mesh.RecalculateBounds();
            }
        }

        void OnDestroy()
        {
            foreach (var surface in surfaces)
            {
                if (surface.filter != null && surface.filter.sharedMesh == surface.mesh)
                    surface.filter.sharedMesh = surface.original;
                if (surface.mesh == null) continue;
                if (Application.isPlaying) Destroy(surface.mesh);
                else DestroyImmediate(surface.mesh);
            }
        }
    }
}
