using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SwordplayPrototype.Editor
{
    public static class SwordplaySetup
    {
        [MenuItem("Swordplay/Create Combat Sandbox")]
        public static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Swordplay Arena").AddComponent<SwordplayArena>();
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeObject = Object.FindObjectOfType<SwordplayArena>();
        }
    }
}
