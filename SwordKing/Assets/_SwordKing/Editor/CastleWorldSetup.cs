using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SwordKing.Editor
{
    public static class CastleWorldSetup
    {
        [MenuItem("SwordKing/Setup/Create Level 3 - Cinder Castle")]
        public static void CreateLevel()
        {
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if(!EditorUtility.DisplayDialog("Build editable castle?","This replaces CinderCastle geometry. Save a copy first if you edited it.","Build","Cancel")) return;
            var scene=EditorSceneManager.OpenScene("Assets/_SwordKing/Scenes/CinderCastle.unity");
            var player=Object.FindFirstObjectByType<PlayerController>();
            if(player==null) throw new System.InvalidOperationException("Castle scene requires a player.");
            if(player.AuthoredWorld!=null) Object.DestroyImmediate(player.AuthoredWorld.gameObject);
            var world=new GameObject("World - editable Cinder Castle").AddComponent<BrokenGateWorld>();
            CastleWorldBuilder.Build(world);
            var materials=new HashSet<Material>();
            foreach(var renderer in world.GetComponentsInChildren<Renderer>(true)) foreach(var mat in renderer.sharedMaterials) if(mat!=null) materials.Add(mat);
            foreach(var mat in new[]{world.Stone,world.Dark,world.Iron,world.Gold,world.Ember,world.Cloth,world.Bone}) materials.Add(mat);
            int index=0;
            foreach(var mat in materials) if(!AssetDatabase.Contains(mat)) AssetDatabase.CreateAsset(mat,AssetDatabase.GenerateUniqueAssetPath(BrokenGateSetup.Root+"/Art/Materials/Castle_"+(index++)+".mat"));
            var serialized=new SerializedObject(player);
            serialized.FindProperty("authoredWorld").objectReferenceValue=world;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject=world.gameObject;
        }
    }
}
