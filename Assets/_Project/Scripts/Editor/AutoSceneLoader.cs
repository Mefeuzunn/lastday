using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace ProjectSurvival.EditorTools
{
    [InitializeOnLoad]
    public static class AutoSceneLoader
    {
        private const string TargetScenePath = "Assets/_Project/Scenes/Main_Prototype.unity";

        static AutoSceneLoader()
        {
            EditorApplication.delayCall += OnEditorLoaded;
        }

        private static void OnEditorLoaded()
        {
            // Register in EditorBuildSettings
            EnsureSceneInBuildSettings();

            // If no scene or empty scene is loaded, open Main_Prototype automatically
            string currentScenePath = EditorSceneManager.GetActiveScene().path;
            if (string.IsNullOrEmpty(currentScenePath) || currentScenePath == "Assets/Scenes/SampleScene.unity")
            {
                if (File.Exists(TargetScenePath))
                {
                    EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
                    Debug.Log($"[AutoSceneLoader] Successfully loaded main scene: {TargetScenePath}");
                }
            }
        }

        private static void EnsureSceneInBuildSettings()
        {
            if (!File.Exists(TargetScenePath)) return;

            var existingScenes = EditorBuildSettings.scenes;
            bool alreadyExists = false;

            foreach (var scene in existingScenes)
            {
                if (scene.path == TargetScenePath)
                {
                    alreadyExists = true;
                    break;
                }
            }

            if (!alreadyExists)
            {
                var newScenes = new EditorBuildSettingsScene[existingScenes.Length + 1];
                newScenes[0] = new EditorBuildSettingsScene(TargetScenePath, true);
                for (int i = 0; i < existingScenes.Length; i++)
                {
                    newScenes[i + 1] = existingScenes[i];
                }
                EditorBuildSettings.scenes = newScenes;
                Debug.Log($"[AutoSceneLoader] Registered {TargetScenePath} as default scene in EditorBuildSettings.");
            }
        }

        [MenuItem("ProjectSurvival/Open & Play Main Prototype", false, 0)]
        public static void OpenAndPlayScene()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(TargetScenePath);
                EditorApplication.isPlaying = true;
            }
        }
    }
}
