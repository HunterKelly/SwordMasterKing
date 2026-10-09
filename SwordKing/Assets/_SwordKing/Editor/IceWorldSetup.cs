using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SwordKing.Editor
{
    public static class IceWorldSetup
    {
        [MenuItem("SwordKing/Setup/Create Level 2 - Ice World")]
        public static void CreateLevel()
        {
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            const string path="Assets/_SwordKing/Scenes/IceWorld.unity";
            if(!EditorUtility.DisplayDialog("Build editable Ice World?","This replaces the IceWorld scene geometry. Save a copy first if you edited it.","Build","Cancel")) return;
            BrokenGateSetup.EnsureFolders();
            var scene=EditorSceneManager.OpenScene(path);
            var player=Object.FindFirstObjectByType<PlayerController>();
            if(player==null) throw new System.InvalidOperationException("IceWorld scene is missing its PlayerController.");
            var previous=player.AuthoredWorld;
            if(previous!=null) Object.DestroyImmediate(previous.gameObject);
            var world=new GameObject("World - editable Ice World").AddComponent<BrokenGateWorld>();
            IceWorldBuilder.Build(world);
            var materials=new HashSet<Material>();
            foreach(var renderer in world.GetComponentsInChildren<Renderer>(true)) foreach(var mat in renderer.sharedMaterials) if(mat!=null) materials.Add(mat);
            foreach(var mat in new[]{world.Stone,world.Dark,world.Iron,world.Gold,world.Ember,world.Cloth,world.Bone}) materials.Add(mat);
            int index=0;
            foreach(var mat in materials) if(!AssetDatabase.Contains(mat)) AssetDatabase.CreateAsset(mat,AssetDatabase.GenerateUniqueAssetPath(BrokenGateSetup.Root+"/Art/Materials/IceWorld_"+(index++)+".mat"));
            var serialized=new SerializedObject(player);
            serialized.FindProperty("authoredWorld").objectReferenceValue=world;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject=world.gameObject;
            Debug.Log("Ice World is editable in Scene view. Tune Data/Levels/IceWorld.asset; press Play to begin.");
        }
    }
}
