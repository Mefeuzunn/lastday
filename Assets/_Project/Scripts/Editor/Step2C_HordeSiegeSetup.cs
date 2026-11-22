using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using ProjectSurvival.Events;
using ProjectSurvival.UI;

namespace ProjectSurvival.EditorTools
{
    /// <summary>
    /// Phase 2 (Step 2C): Scene Setup for Night Horde Migrations & Outpost Sieges.
    /// Programmatically installs:
    /// 1. NightHordeMigrationDirector under '--- MANAGERS ---'
    /// 2. OutpostSiegeEvent under '--- MANAGERS ---'
    /// 3. SiegeWarningHUD under 'HUD_Canvas'
    /// Saves Main_Prototype.unity cleanly.
    /// </summary>
    public static class Step2C_HordeSiegeSetup
    {
        private const string ScenePath = "Assets/_Project/Scenes/Main_Prototype.unity";

        [MenuItem("ProjectSurvival/Phase 2/Setup Step 2C Horde & Sieges", false, 303)]
        public static void SetupStep2C()
        {
            Debug.Log("==================================================================");
            Debug.Log(">>> EXECUTING STEP 2C: NIGHT HORDE MIGRATIONS & SIEGES SETUP <<<");
            Debug.Log("==================================================================");

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[Step2C_HordeSiegeSetup] Failed to open scene at: {ScenePath}");
                return;
            }

            // 1. Locate or create '--- MANAGERS ---'
            GameObject managersRoot = GameObject.Find("--- MANAGERS ---");
            if (managersRoot == null)
            {
                managersRoot = new GameObject("--- MANAGERS ---");
            }

            // 2. Setup NightHordeMigrationDirector
            NightHordeMigrationDirector migrationDir = managersRoot.GetComponentInChildren<NightHordeMigrationDirector>(true);
            if (migrationDir == null)
            {
                GameObject migrationObj = GameObject.Find("NightHordeMigrationDirector");
                if (migrationObj == null)
                {
                    migrationObj = new GameObject("NightHordeMigrationDirector");
                    migrationObj.transform.SetParent(managersRoot.transform, true);
                }
                migrationDir = migrationObj.GetComponent<NightHordeMigrationDirector>();
                if (migrationDir == null)
                {
                    migrationDir = migrationObj.AddComponent<NightHordeMigrationDirector>();
                }
            }
            migrationDir.InitializeDefaultCorridorsIfEmpty();

            // 3. Setup OutpostSiegeEvent
            OutpostSiegeEvent siegeEvent = managersRoot.GetComponentInChildren<OutpostSiegeEvent>(true);
            if (siegeEvent == null)
            {
                GameObject siegeObj = GameObject.Find("OutpostSiegeEvent");
                if (siegeObj == null)
                {
                    siegeObj = new GameObject("OutpostSiegeEvent");
                    siegeObj.transform.SetParent(managersRoot.transform, true);
                }
                siegeEvent = siegeObj.GetComponent<OutpostSiegeEvent>();
                if (siegeEvent == null)
                {
                    siegeEvent = siegeObj.AddComponent<OutpostSiegeEvent>();
                }
            }

            // 4. Setup SiegeWarningHUD under HUD_Canvas
            GameObject canvasObj = GameObject.Find("HUD_Canvas");
            if (canvasObj == null)
            {
                canvasObj = GameObject.Find("Canvas");
            }

            SiegeWarningHUD siegeHUD = Object.FindFirstObjectByType<SiegeWarningHUD>();
            if (siegeHUD == null && canvasObj != null)
            {
                Transform existingHUD = canvasObj.transform.Find("SiegeWarningHUD");
                GameObject hudObj = existingHUD != null ? existingHUD.gameObject : new GameObject("SiegeWarningHUD");
                hudObj.transform.SetParent(canvasObj.transform, false);

                siegeHUD = hudObj.GetComponent<SiegeWarningHUD>();
                if (siegeHUD == null) siegeHUD = hudObj.AddComponent<SiegeWarningHUD>();
            }

            if (siegeHUD != null)
            {
                siegeHUD.BuildUIHierarchyIfMissing();
            }

            EditorUtility.SetDirty(migrationDir.gameObject);
            EditorUtility.SetDirty(siegeEvent.gameObject);
            if (siegeHUD != null) EditorUtility.SetDirty(siegeHUD.gameObject);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[Step2C_HordeSiegeSetup] Successfully registered NightHordeMigrationDirector and OutpostSiegeEvent under '--- MANAGERS ---'!");
            Debug.Log($"[Step2C_HordeSiegeSetup] Verified {migrationDir.Corridors.Count} migration corridors.");
            Debug.Log("[Step2C_HordeSiegeSetup] Successfully configured SiegeWarningHUD under UI Canvas.");
            Debug.Log("==================================================================");
        }
    }
}
