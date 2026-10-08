using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SwordKing.Editor
{
    public static class SwordplaySetup
    {
        [MenuItem("SwordKing/Setup/Create Combat Sandbox")]
        public static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            BrokenGateSetup.EnsureFolders();
            string path = BrokenGateSetup.Root + "/Scenes/CombatSandbox.unity";
            if (System.IO.File.Exists(path) && !EditorUtility.DisplayDialog("Rebuild sandbox?", "This replaces the sandbox scene.", "Rebuild", "Cancel")) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var game = new GameObject("Combat Sandbox").AddComponent<PlayerController>();
            game.gameObject.AddComponent<PlayerInputReader>();
            EditorSceneManager.SaveScene(scene, path);
            Selection.activeGameObject = game.gameObject;
        }
    }
}
