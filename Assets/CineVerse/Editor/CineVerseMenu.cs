using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using CineVerse.Core;

namespace CineVerse.EditorTools
{
    /// <summary>Menu: CineVerse > Create Phase 1 Scene. Builds and saves the starter scene for you.</summary>
    public static class CineVerseMenu
    {
        const string ScenePath = "Assets/CineVerse/Scenes/Phase1_Theatre.unity";

        [MenuItem("CineVerse/Create Phase 1 Scene")]
        public static void CreatePhase1Scene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Directory.CreateDirectory("Assets/CineVerse/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // The bootstrap builds the hall and creates the camera at runtime, so the scene only
            // needs one object. RequireComponent adds the CinemaScreen automatically.
            GameObject go = new GameObject("CineVerse");
            go.AddComponent<CineVerseBootstrap>();

            EditorSceneManager.SaveScene(scene, ScenePath);

            // Add to Build Settings if it is not there yet.
            var scenes = EditorBuildSettings.scenes.ToList();
            if (!scenes.Any(s => s.path == ScenePath))
            {
                scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }

            AssetDatabase.Refresh();
            Selection.activeGameObject = go;
            Debug.Log("[CineVerse] Created " + ScenePath + ". Set a video on the CinemaScreen component, then press Play.");
        }
    }
}
