using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace SwordKing
{
    // Source data is a normal TextAsset. No guessed model-importer subasset IDs.
    // ExecuteAlways also supplies meshes in Prefab Mode and scene previews.
    [ExecuteAlways, DisallowMultipleComponent]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class KnightMeshSource : MonoBehaviour
    {
        public TextAsset source;
        static readonly Dictionary<TextAsset, Mesh> Cache = new Dictionary<TextAsset, Mesh>();

        void OnEnable() => EnsureMesh();

        public void EnsureMesh()
        {
            if (source == null) return;
            var filter = GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh != null) return;
            if (!Cache.TryGetValue(source, out var mesh) || mesh == null)
            {
                try
                {
                    mesh = CreateMesh();
                    mesh.hideFlags = HideFlags.HideAndDontSave;
                    Cache[source] = mesh;
                }
                catch (Exception error)
                {
                    Debug.LogError("Cannot load knight mesh " + source.name + ": " + error.Message, this);
                    return;
                }
            }
            filter.sharedMesh = mesh;
        }

        public Mesh CreateMesh()
        {
            if (source == null) throw new InvalidDataException("Missing mesh source asset.");
            var data = source.bytes;
            using (var reader = new BinaryReader(new MemoryStream(data, false)))
            {
                if (data.Length < 12 || reader.ReadUInt32() != 0x4B4D5331)
                    throw new InvalidDataException("Invalid knight mesh header.");
                int vertexCount = reader.ReadInt32(), indexCount = reader.ReadInt32();
                if (vertexCount <= 0 || vertexCount > 1000000 || indexCount <= 0
                    || indexCount % 3 != 0 || 12L + vertexCount * 32L + indexCount * 4L != data.LongLength)
                    throw new InvalidDataException("Invalid knight mesh dimensions.");
                var vertices = new Vector3[vertexCount];
                var normals = new Vector3[vertexCount];
                var uv = new Vector2[vertexCount];
                var indices = new int[indexCount];
                for (int i = 0; i < vertexCount; i++)
                {
                    vertices[i] = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                    normals[i] = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                    uv[i] = new Vector2(reader.ReadSingle(), reader.ReadSingle());
                }
                for (int i = 0; i < indexCount; i++)
                {
                    indices[i] = reader.ReadInt32();
                    if ((uint)indices[i] >= (uint)vertexCount)
                        throw new InvalidDataException("Knight mesh index is out of range.");
                }
                var mesh = new Mesh { name = source.name,
                    indexFormat = vertexCount > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
                mesh.vertices = vertices;
                mesh.normals = normals;
                mesh.uv = uv;
                mesh.triangles = indices;
                mesh.RecalculateBounds();
                return mesh;
            }
        }
    }
}
