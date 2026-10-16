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

        public void SetMotion(float movement, float sprint, bool airborne, bool rolling, float side)
        {
            this.movement = movement; this.sprint = sprint;
            this.airborne = airborne; this.rolling = rolling; this.side = side;
        }

        void Start()
        {
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
            float trail = movement * Mathf.Lerp(.05f, .16f, sprint) + (airborne ? .06f : 0) + (rolling ? .10f : 0);
            foreach (var surface in surfaces)
            {
                for (int i = 0; i < surface.rest.Length; i++)
                {
                    Vector3 p = surface.rest[i];
                    float t = Mathf.Clamp01(-p.y / 1.35f);
                    float weight = t * t;
                    p.z += (-trail + Mathf.Sin(Time.time * 3.5f - t * 4 + p.x * 4)
                        * (.009f + movement * .012f)) * weight;
                    p.x += (side * .025f + Mathf.Sin(Time.time * 2.3f - t * 3) * .008f) * weight;
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
