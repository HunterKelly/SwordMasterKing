using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SwordplayPrototype.Editor
{
    public static class BrokenGateSetup
    {
        [MenuItem("Swordplay/Create Level 1 - The Broken Gate")]
        public static void CreateLevel()
        {
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            const string path="Assets/SwordplayPrototype/Scenes/BrokenGate.unity";
            if(File.Exists(path) && !EditorUtility.DisplayDialog("Rebuild Level 1 scene?","This replaces the generated BrokenGate scene. Your other scenes and saved journey are not changed.","Rebuild","Cancel")) return;
            Directory.CreateDirectory("Assets/SwordplayPrototype/Scenes");
            Directory.CreateDirectory("Assets/SwordplayPrototype/Resources");
            AssetDatabase.Refresh();
            // Runtime materials clone these retained assets; player builds keep their shaders.
            CreateMaterial("BrokenGateLit", "Universal Render Pipeline/Lit", "Standard");
            CreateMaterial("BrokenGateUnlit", "Universal Render Pipeline/Unlit", "Unlit/Color");
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var go=new GameObject("The Broken Gate - Game");
            go.AddComponent<SwordplayArena>().adventureMode=true;
            EditorSceneManager.SaveScene(scene,path);
            Selection.activeGameObject=go;
            Debug.Log("Level 1 created. Press Play. To make a standalone game, add BrokenGate.unity to your Build Profile scene list.");
        }
        static void CreateMaterial(string name,string preferred,string fallback)
        {
            string path="Assets/SwordplayPrototype/Resources/"+name+".mat";
            if(AssetDatabase.LoadAssetAtPath<Material>(path)!=null) return;
            Shader shader=Shader.Find(preferred);
            if(shader==null) shader=Shader.Find(fallback);
            if(shader==null) throw new System.InvalidOperationException("No supported shader found. Use a Universal 3D or built-in 3D project.");
            AssetDatabase.CreateAsset(new Material(shader),path);
            AssetDatabase.SaveAssets();
        }
    }
}
