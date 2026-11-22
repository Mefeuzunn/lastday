using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using ProjectSurvival.OpenWorld;

namespace ProjectSurvival.EditorTools
{
    public static class Phase4ManagerIntegrator
    {
        private const string ScenePath = "Assets/_Project/Scenes/Main_Prototype.unity";

#if UNITY_EDITOR
        [MenuItem("ProjectSurvival/Integrate Phase 4 Open World Managers", false, 36)]
        public static void IntegrateManagers()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Debug.Log("[Phase4ManagerIntegrator] Integrating Phase 4 Open-World Managers...");

            GameObject root = GameObject.Find("--- OPEN WORLD MANAGERS ---");
            if (root == null)
            {
                root = new GameObject("--- OPEN WORLD MANAGERS ---");
            }

            var outpostLib = root.GetComponent<OutpostLiberationManager>();
            if (outpostLib == null)
            {
                outpostLib = root.AddComponent<OutpostLiberationManager>();
                Debug.Log("[Phase4ManagerIntegrator] Added OutpostLiberationManager.");
            }

            var metroTravel = root.GetComponent<MetroFastTravelSystem>();
            if (metroTravel == null)
            {
                metroTravel = root.AddComponent<MetroFastTravelSystem>();
                Debug.Log("[Phase4ManagerIntegrator] Added MetroFastTravelSystem.");
            }

            var caravanDir = root.GetComponent<InterSectorCaravanDirector>();
            if (caravanDir == null)
            {
                caravanDir = root.AddComponent<InterSectorCaravanDirector>();
                Debug.Log("[Phase4ManagerIntegrator] Added InterSectorCaravanDirector.");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Phase4ManagerIntegrator] Phase 4 Managers saved to Main_Prototype.unity successfully.");

            EditorApplication.Exit(0);
        }
#endif
    }
}
