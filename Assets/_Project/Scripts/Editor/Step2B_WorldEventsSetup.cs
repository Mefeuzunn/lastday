using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using ProjectSurvival.Events;

namespace ProjectSurvival.EditorTools
{
    /// <summary>
    /// Phase 2 (Step 2B): Scene Setup for Dynamic World Events Director.
    /// Programmatically installs DynamicWorldEventsDirector under '--- MANAGERS ---'
    /// in Main_Prototype.unity and initializes curated outer-sector locations.
    /// </summary>
    public static class Step2B_WorldEventsSetup
    {
        private const string ScenePath = "Assets/_Project/Scenes/Main_Prototype.unity";

        [MenuItem("ProjectSurvival/Phase 2/Setup Step 2B World Events", false, 302)]
        public static void SetupStep2B()
        {
            Debug.Log("==================================================================");
            Debug.Log(">>> EXECUTING STEP 2B: DYNAMIC WORLD EVENTS DIRECTOR SETUP <<<");
            Debug.Log("==================================================================");

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[Step2B_WorldEventsSetup] Failed to open scene at: {ScenePath}");
                return;
            }

            // 1. Locate or create '--- MANAGERS ---'
            GameObject managersRoot = GameObject.Find("--- MANAGERS ---");
            if (managersRoot == null)
            {
                managersRoot = new GameObject("--- MANAGERS ---");
            }

            // 2. Locate or create DynamicWorldEventsDirector
            DynamicWorldEventsDirector director = managersRoot.GetComponentInChildren<DynamicWorldEventsDirector>(true);
            if (director == null)
            {
                GameObject directorObj = GameObject.Find("DynamicWorldEventsDirector");
                if (directorObj == null)
                {
                    directorObj = new GameObject("DynamicWorldEventsDirector");
                    directorObj.transform.SetParent(managersRoot.transform, true);
                }
                director = directorObj.GetComponent<DynamicWorldEventsDirector>();
                if (director == null)
                {
                    director = directorObj.AddComponent<DynamicWorldEventsDirector>();
                }
            }

            // 3. Initialize default curated locations
            director.InitializeDefaultLocationsIfEmpty();

            // 4. Serialize properties
            SerializedObject so = new SerializedObject(director);
            so.FindProperty("minSafeDistanceFromCenter").floatValue = 55.0f;
            so.FindProperty("airdropHourInterval").floatValue = 10.0f;
            so.FindProperty("minSosIntervalSeconds").floatValue = 180.0f;
            so.FindProperty("maxSosIntervalSeconds").floatValue = 240.0f;
            so.FindProperty("autoScheduleEvents").boolValue = true;
            so.ApplyModifiedProperties();

            EditorUtility.SetDirty(director.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log($"[Step2B_WorldEventsSetup] Successfully registered DynamicWorldEventsDirector under '{managersRoot.name}'!");
            Debug.Log($"[Step2B_WorldEventsSetup] Verified {director.LandingZones.Count} Airdrop Zones & {director.SOSLocations.Count} SOS Locations.");
            Debug.Log("==================================================================");
        }
    }
}
