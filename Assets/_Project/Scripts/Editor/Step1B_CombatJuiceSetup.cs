using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using ProjectSurvival.Combat;
using ProjectSurvival.UI;
using ProjectSurvival.Player;

namespace ProjectSurvival.EditorTools
{
    public static class Step1B_CombatJuiceSetup
    {
        private const string ScenePath = "Assets/_Project/Scenes/Main_Prototype.unity";
        private const string PlayerPrefabPath = "Assets/_Project/Prefabs/Player/Player_Root.prefab";

        [MenuItem("Project Survival/Combat/Setup Step 1B Combat Juice Systems", false, 20)]
        public static void RunSetupFromMenu()
        {
            ExecuteSetup();
        }

        public static void RunSetupCLI()
        {
            ExecuteSetup();
            EditorApplication.Exit(0);
        }

        public static void ExecuteSetup()
        {
            Debug.Log("================================================================================");
            Debug.Log(">>> EXECUTING STEP 1B: COMBAT JUICE, RECOIL, DIRECTIONAL HUD & DECALS SETUP <<<");
            Debug.Log("================================================================================");

            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            // 1. Setup Player_Root with WeaponSwayAndRecoil
            var player = GameObject.FindWithTag("Player");
            if (player == null)
            {
                var tps = UnityEngine.Object.FindFirstObjectByType<TPSPlayerController>();
                if (tps != null) player = tps.gameObject;
            }

            if (player != null)
            {
                var sway = player.GetComponent<WeaponSwayAndRecoil>();
                if (sway == null)
                {
                    sway = player.AddComponent<WeaponSwayAndRecoil>();
                }
                sway.AutoFindSocket();

                EditorUtility.SetDirty(sway);
                EditorUtility.SetDirty(player);
                Debug.Log("[Step1B_CombatJuiceSetup] Attached and configured WeaponSwayAndRecoil on Player_Root.");
            }

            // 2. Setup HUD_Canvas with CombatJuiceController, DirectionalDamageHUD, DynamicCrosshairUI
            Canvas hudCanvas = null;
            var canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var c in canvases)
            {
                if (c.gameObject.name == "HUD_Canvas")
                {
                    hudCanvas = c;
                    break;
                }
            }
            if (hudCanvas == null && canvases.Length > 0) hudCanvas = canvases[0];

            if (hudCanvas != null)
            {
                // A. CombatJuiceController
                var juice = hudCanvas.GetComponent<CombatJuiceController>();
                if (juice == null) juice = hudCanvas.gameObject.AddComponent<CombatJuiceController>();
                EditorUtility.SetDirty(juice);

                // B. DirectionalDamageHUD
                var dirDmg = hudCanvas.GetComponentInChildren<DirectionalDamageHUD>(true);
                if (dirDmg == null)
                {
                    GameObject ddObj = new GameObject("DirectionalDamageHUD");
                    ddObj.transform.SetParent(hudCanvas.transform, false);
                    dirDmg = ddObj.AddComponent<DirectionalDamageHUD>();
                }
                EditorUtility.SetDirty(dirDmg);

                // C. DynamicCrosshairUI
                var crossPanel = hudCanvas.transform.Find("Crosshair_Panel");
                if (crossPanel != null)
                {
                    var dynCross = crossPanel.GetComponent<DynamicCrosshairUI>();
                    if (dynCross == null) dynCross = crossPanel.gameObject.AddComponent<DynamicCrosshairUI>();
                    EditorUtility.SetDirty(dynCross);

                    // Ensure all child images have raycastTarget = false
                    var images = crossPanel.GetComponentsInChildren<Image>(true);
                    foreach (var img in images)
                    {
                        img.raycastTarget = false;
                        EditorUtility.SetDirty(img);
                    }
                }

                // D. Ensure HitMarker_Panel raycastTarget = false
                var hitMarkerPanel = hudCanvas.transform.Find("HitMarker_Panel");
                if (hitMarkerPanel != null)
                {
                    var images = hitMarkerPanel.GetComponentsInChildren<Image>(true);
                    foreach (var img in images)
                    {
                        img.raycastTarget = false;
                        EditorUtility.SetDirty(img);
                    }
                }

                EditorUtility.SetDirty(hudCanvas.gameObject);
                Debug.Log("[Step1B_CombatJuiceSetup] Configured CombatJuiceController, DirectionalDamageHUD, and DynamicCrosshairUI on HUD_Canvas.");
            }

            // 3. Setup ImpactDecalPoolManager in Scene
            var decalManager = UnityEngine.Object.FindFirstObjectByType<ImpactDecalPoolManager>();
            if (decalManager == null)
            {
                GameObject mgrObj = new GameObject("ImpactDecalPoolManager");
                decalManager = mgrObj.AddComponent<ImpactDecalPoolManager>();

                // Parent under Managers if present
                GameObject managersRoot = GameObject.Find("Managers") ?? GameObject.Find("Core_Systems");
                if (managersRoot != null)
                {
                    mgrObj.transform.SetParent(managersRoot.transform, false);
                }
                EditorUtility.SetDirty(mgrObj);
                Debug.Log("[Step1B_CombatJuiceSetup] Created and configured ImpactDecalPoolManager in scene.");
            }

            // 4. Update Player_Root Prefab
            try
            {
                var contents = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
                if (contents != null)
                {
                    var sway = contents.GetComponent<WeaponSwayAndRecoil>();
                    if (sway == null) sway = contents.AddComponent<WeaponSwayAndRecoil>();
                    sway.AutoFindSocket();

                    PrefabUtility.SaveAsPrefabAsset(contents, PlayerPrefabPath);
                    PrefabUtility.UnloadPrefabContents(contents);
                    Debug.Log($"[Step1B_CombatJuiceSetup] Saved updated Prefab `{PlayerPrefabPath}`.");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[Step1B_CombatJuiceSetup] Error updating player prefab: {ex.Message}");
            }

            // 5. Save Scene and Assets
            EditorSceneManager.MarkSceneDirty(scene);
            bool saved = EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log($"[Step1B_CombatJuiceSetup] Scene saved: {saved}. All Combat Juice systems deployed.");
            Debug.Log("================================================================================");
        }
    }
}
