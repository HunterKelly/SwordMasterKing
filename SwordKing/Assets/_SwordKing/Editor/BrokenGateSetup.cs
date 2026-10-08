using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SwordKing.Editor
{
    public static class BrokenGateSetup
    {
        public const string Root = "Assets/_SwordKing";
        [MenuItem("SwordKing/Setup/Create Level 1 - The Broken Gate")]
        public static void CreateLevel()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string path = Root + "/Scenes/BrokenGate.unity";
            if (File.Exists(path) && !EditorUtility.DisplayDialog("Rebuild Level 1?", "This replaces BrokenGate.unity. Save a copy first if you edited this scene.", "Rebuild", "Cancel")) return;
            EnsureFolders();
            CreateMaterial("BrokenGateLit", "Universal Render Pipeline/Lit", "Standard");
            CreateMaterial("BrokenGateUnlit", "Universal Render Pipeline/Unlit", "Unlit/Color");
            var settings = LoadOrCreate<PlayerSettings>(Root + "/Data/Player/DefaultPlayer.asset");
            var playerPrefab = CreatePlayerPrefab();
            var definition = LoadOrCreate<LevelDefinition>(Root + "/Data/Levels/BrokenGate.asset");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var world = new GameObject("World - editable Level 1").AddComponent<BrokenGateWorld>();
            world.Build();
            // Persist generator materials so scene edits and standalone builds retain them.
            var materials = new System.Collections.Generic.HashSet<Material>();
            foreach (var renderer in world.GetComponentsInChildren<Renderer>(true))
                foreach (var material in renderer.sharedMaterials) if (material != null) materials.Add(material);
            foreach (var material in new[] { world.Stone, world.Dark, world.Iron, world.Gold, world.Ember, world.Cloth, world.Bone })
                materials.Add(material);
            int index = 0;
            foreach (var material in materials)
                if (!AssetDatabase.Contains(material))
                    AssetDatabase.CreateAsset(material, AssetDatabase.GenerateUniqueAssetPath(Root + "/Art/Materials/Fortress_" + index++ + ".mat"));
            var game = new GameObject("Game - Player and Journey").AddComponent<PlayerController>();
            game.adventureMode = true;
            game.gameObject.AddComponent<PlayerInputReader>();
            var serialized = new SerializedObject(game);
            serialized.FindProperty("settings").objectReferenceValue = settings;
            serialized.FindProperty("playerPrefab").objectReferenceValue = playerPrefab;
            serialized.FindProperty("authoredWorld").objectReferenceValue = world;
            serialized.FindProperty("levelDefinition").objectReferenceValue = definition;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene, path);
            // Keep other build scenes; put Level 1 first so a standalone starts in the game.
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(path, true) };
            foreach (var entry in EditorBuildSettings.scenes) if (entry.path != path) scenes.Add(entry);
            EditorBuildSettings.scenes = scenes.ToArray();
            Selection.activeGameObject = game.gameObject;
            Debug.Log("Level 1 saved. Edit World in the Scene view; tune Data assets in the Inspector. Press Play to begin. Check active Build Profile scene overrides before building.");
        }
        static PlayerRig CreatePlayerPrefab()
        {
            string path = Root + "/Prefabs/Player/Swordsman.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<PlayerRig>(path);
            if (existing != null) return existing;
            var template = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Resources/BrokenGateLit.mat");
            var colors = new[] { new Color(.09f,.13f,.18f), new Color(.22f,.28f,.34f), new Color(.75f,.87f,.93f), new Color(.95f,.65f,.18f), new Color(.12f,.8f,.8f) };
            var materials = new Material[colors.Length];
            for (int i=0; i<materials.Length; i++)
            {
                string materialPath = Root + "/Art/Materials/Player_" + i + ".mat";
                materials[i] = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (materials[i] == null)
                {
                    materials[i] = new Material(template) { color = colors[i] };
                    AssetDatabase.CreateAsset(materials[i], materialPath);
                }
            }
            var rig = PlayerRig.CreateDefault(materials[0], materials[1], materials[2], materials[3], materials[4]);
            try { return PrefabUtility.SaveAsPrefabAsset(rig.gameObject, path).GetComponent<PlayerRig>(); }
            finally { Object.DestroyImmediate(rig.gameObject); }
        }
        public static void EnsureFolders()
        {
            foreach (var folder in new[] { "Scenes", "Resources", "Data/Player", "Data/Levels", "Art/Materials", "Art/Models", "Art/Textures", "Audio", "Prefabs/Player", "Prefabs/Enemies", "Prefabs/Environment", "UI" })
                Directory.CreateDirectory(Root + "/" + folder);
            AssetDatabase.Refresh();
        }
        static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) { asset = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset, path); }
            return asset;
        }
        static void CreateMaterial(string name, string preferred, string fallback)
        {
            string path = Root + "/Resources/" + name + ".mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(path) != null) return;
            var shader = Shader.Find(preferred);
            if (shader == null) shader = Shader.Find(fallback);
            if (shader == null) throw new System.InvalidOperationException("No supported shader found.");
            AssetDatabase.CreateAsset(new Material(shader), path);
        }
    }
}
