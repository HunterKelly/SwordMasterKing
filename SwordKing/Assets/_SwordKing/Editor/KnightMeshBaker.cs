using System.IO;
using UnityEditor;
using UnityEngine;

namespace SwordKing.Editor
{
    public static class KnightMeshBaker
    {
        // Optional: source meshes already work in Edit Mode and standalone builds.
        [MenuItem("SwordKing/Player/Bake Knight Mesh Assets")]
        public static void Bake()
        {
            const string prefabPath = "Assets/_SwordKing/Prefabs/Player/ArmoredKnight.prefab";
            const string meshFolder = "Assets/_SwordKing/Art/Models/Knight/Baked";
            Directory.CreateDirectory(meshFolder);
            AssetDatabase.Refresh();
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                foreach (var source in root.GetComponentsInChildren<KnightMeshSource>(true))
                {
                    string path = meshFolder + "/" + source.source.name + ".asset";
                    var fresh = source.CreateMesh();
                    var asset = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                    if (asset == null)
                    {
                        AssetDatabase.CreateAsset(fresh, path);
                        asset = fresh;
                    }
                    else
                    {
                        EditorUtility.CopySerialized(fresh, asset);
                        EditorUtility.SetDirty(asset);
                        Object.DestroyImmediate(fresh);
                    }
                    source.GetComponent<MeshFilter>().sharedMesh = asset;
                }
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                AssetDatabase.SaveAssets();
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            Debug.Log("Knight meshes baked as native Unity assets. Prefab references updated.");
        }
    }
}
