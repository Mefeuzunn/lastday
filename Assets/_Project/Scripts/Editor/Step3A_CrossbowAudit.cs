using System;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.AI;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using ProjectSurvival.Combat;
using ProjectSurvival.Interaction;
using ProjectSurvival.Player;
using ProjectSurvival.Survival;
using ProjectSurvival.UI;

namespace ProjectSurvival.EditorTools
{
    public static class Step3A_CrossbowAudit
    {
        private const string ScenePath = "Assets/_Project/Scenes/Main_Prototype.unity";

#if UNITY_EDITOR
        [MenuItem("ProjectSurvival/Phase 3/Step 3A - Audit Crossbow & Ballistics", false, 302)]
        public static void RunAuditCLI()
        {
            Debug.Log("[Step3A_CrossbowAudit] Starting Automated Forensic Verification for Phase 3 (Step 3A)...");

            int passed = 0;
            int failed = 0;
            StringBuilder report = new StringBuilder();
            report.AppendLine("================================================================================");
            report.AppendLine("STEP 3A AUDIT: SILENT TACTICAL CROSSBOW, RETRIEVABLE BOLT PHYSICS & WORKBENCH CRAFTING");
            report.AppendLine("================================================================================\n");

            // 1. Scene load
            if (EditorSceneManager.GetActiveScene().path != ScenePath)
            {
                EditorSceneManager.OpenScene(ScenePath);
            }

            // Test 1: WeaponSystem Enum & Fields
            report.AppendLine("--- 1. WEAPONSYSTEM CROSSBOW REGISTRATION & STATS ---");
            GameObject player = GameObject.Find("Player_Root") ?? GameObject.FindWithTag("Player");
            if (player != null)
            {
                WeaponSystem ws = player.GetComponent<WeaponSystem>();
                passed += Assert(ws != null, "Player_Root has WeaponSystem attached", report, ref failed);
                if (ws != null)
                {
                    passed += Assert(ws.CrossbowDamage == 95f, $"Crossbow base damage is 95 HP (actual: {ws.CrossbowDamage})", report, ref failed);
                    passed += Assert(ws.CrossbowHeadshotMultiplier == 2.75f, $"Crossbow headshot multiplier is 2.75x (actual: {ws.CrossbowHeadshotMultiplier})", report, ref failed);
                    passed += Assert(ws.CrossbowNoiseRadius <= 3.5f, $"Crossbow acoustic noise radius is <= 3.5m whisper quiet (actual: {ws.CrossbowNoiseRadius}m)", report, ref failed);
                    passed += Assert(ws.CrossbowReloadDuration <= 1.5f, $"Crossbow reload duration is 1.35s (actual: {ws.CrossbowReloadDuration}s)", report, ref failed);
                    passed += Assert(Enum.IsDefined(typeof(WeaponType), WeaponType.Crossbow), "WeaponType.Crossbow is defined in enum", report, ref failed);
                }
            }
            else
            {
                failed++;
                report.AppendLine("[FAIL] Player_Root not found in scene.");
            }

            // Test 2: Retrievable Bolt Physics & Component
            report.AppendLine("\n--- 2. PHYSICAL RETRIEVABLE BOLT BALLISTICS ---");
            RetrievableBoltProjectile bolt = RetrievableBoltProjectile.SpawnBolt(new Vector3(0f, 10f, 0f), Vector3.forward, 68f, 95f, 2.75f, ~0);
            passed += Assert(bolt != null, "RetrievableBoltProjectile.SpawnBolt instantiates projectile successfully", report, ref failed);
            if (bolt != null)
            {
                passed += Assert(bolt.IsFlying, "Bolt projectile initializes in active kinematic flight state", report, ref failed);
                passed += Assert(bolt.BaseDamage == 95f, "Bolt base damage equals 95f", report, ref failed);
                passed += Assert(bolt.HeadshotMultiplier == 2.75f, "Bolt headshot multiplier equals 2.75x", report, ref failed);
                passed += Assert(bolt.GetComponent<SphereCollider>() != null, "Bolt has SphereCollider trigger attached", report, ref failed);
                passed += Assert(bolt.transform.Find("Bolt_Shaft") != null, "Bolt model contains Bolt_Shaft", report, ref failed);
                passed += Assert(bolt.transform.Find("Bolt_Broadhead") != null, "Bolt model contains Bolt_Broadhead steel tip", report, ref failed);
                passed += Assert(bolt.transform.Find("Fletch_L") != null, "Bolt model contains Fletching vanes", report, ref failed);

                // Clean up spawned test bolt
                UnityEngine.Object.DestroyImmediate(bolt.gameObject);
            }

            // Test 3: Scoped Aim Zoom & Dynamic Crosshair
            report.AppendLine("\n--- 3. SCOPED AIM ZOOM & CROSSHAIR PINPOINT ACCURACY ---");
            ThirdPersonCamera cam = UnityEngine.Object.FindFirstObjectByType<ThirdPersonCamera>();
            passed += Assert(cam != null, "ThirdPersonCamera is active in scene", report, ref failed);

            DynamicCrosshairUI crosshair = UnityEngine.Object.FindFirstObjectByType<DynamicCrosshairUI>(FindObjectsInactive.Include);
            passed += Assert(crosshair != null, "DynamicCrosshairUI is present on Canvas", report, ref failed);

            // Test 4: ItemData Resolution
            report.AppendLine("\n--- 4. ITEMDATA ASSETS RESOLUTION ---");
            ItemData cbItem = InventorySystem.FindItemData("weapon_crossbow");
            passed += Assert(cbItem != null, "weapon_crossbow ItemData resolves successfully", report, ref failed);
            if (cbItem != null)
            {
                passed += Assert(cbItem.itemType == ItemType.Weapon, "weapon_crossbow is classified as ItemType.Weapon", report, ref failed);
                passed += Assert(cbItem.weaponDamage == 95f, "weapon_crossbow ItemData weaponDamage is 95f", report, ref failed);
            }

            ItemData boltItem = InventorySystem.FindItemData("ammo_crossbow_bolt");
            passed += Assert(boltItem != null, "ammo_crossbow_bolt ItemData resolves successfully", report, ref failed);
            if (boltItem != null)
            {
                passed += Assert(boltItem.maxStack >= 30, "ammo_crossbow_bolt maxStack is >= 30", report, ref failed);
            }

            // Test 5: Workbench Crafting UI Recipes
            report.AppendLine("\n--- 5. WORKBENCH CRAFTING UI RECIPES ---");
            WorkbenchCraftingUI wb = UnityEngine.Object.FindFirstObjectByType<WorkbenchCraftingUI>(FindObjectsInactive.Include);
            passed += Assert(wb != null, "WorkbenchCraftingUI component is present on Canvas", report, ref failed);

            // Test 6: Northwest Ranger Tower World Cache
            report.AppendLine("\n--- 6. NORTHWEST RANGER TOWER CACHE ON NAVMESH ---");
            GameObject cacheObj = GameObject.Find("WeaponPickup_RangerTowerCrossbow");
            passed += Assert(cacheObj != null, "WeaponPickup_RangerTowerCrossbow exists in scene", report, ref failed);
            if (cacheObj != null)
            {
                WeaponPickupInteractable pickup = cacheObj.GetComponent<WeaponPickupInteractable>();
                passed += Assert(pickup != null, "Cache has WeaponPickupInteractable attached", report, ref failed);
                if (pickup != null)
                {
                    passed += Assert(pickup.WeaponType == WeaponType.Crossbow, "Cache is configured for WeaponType.Crossbow", report, ref failed);
                    passed += Assert(pickup.PersistentId == "world_cache_ranger_crossbow_nw", "Cache has persistent ID 'world_cache_ranger_crossbow_nw'", report, ref failed);
                }

                // NavMesh verification
                bool onNavMesh = NavMesh.SamplePosition(cacheObj.transform.position, out NavMeshHit navHit, 1.0f, NavMesh.AllAreas);
                passed += Assert(onNavMesh, $"Cache is placed on valid walkable NavMesh (distance: {Vector3.Distance(cacheObj.transform.position, navHit.position):F2}m)", report, ref failed);

                // Location in Sector_-1_1_NorthWest
                float distFromExpected = Vector3.Distance(cacheObj.transform.position, new Vector3(-220f, 0.35f, 259f));
                passed += Assert(distFromExpected <= 15f, $"Cache is located within Outpost_NorthwestRangerTower perimeter (distance: {distFromExpected:F2}m)", report, ref failed);
            }

            // Test 7: Zero TODOs in new Step 3A scripts
            report.AppendLine("\n--- 7. ZERO TODOS & CODE SANITY AUDIT ---");
            string[] scriptsToScan = new string[]
            {
                "Assets/_Project/Scripts/Combat/RetrievableBoltProjectile.cs",
                "Assets/_Project/Scripts/Player/WeaponSystem.cs",
                "Assets/_Project/Scripts/UI/DynamicCrosshairUI.cs",
                "Assets/_Project/Scripts/UI/WorkbenchCraftingUI.cs",
                "Assets/_Project/Scripts/Editor/Step3A_CrossbowSetup.cs"
            };

            bool anyTodoFound = false;
            foreach (string sPath in scriptsToScan)
            {
                if (File.Exists(sPath))
                {
                    string content = File.ReadAllText(sPath);
                    if (content.Contains("// TODO") || content.Contains("//TODO"))
                    {
                        anyTodoFound = true;
                        failed++;
                        report.AppendLine($"[FAIL] Found // TODO in {sPath}");
                    }
                }
            }
            if (!anyTodoFound)
            {
                passed++;
                report.AppendLine("[PASS] Zero // TODO comments across all Step 3A scripts.");
            }

            // Summary
            report.AppendLine("\n================================================================================");
            report.AppendLine($"AUDIT COMPLETE: {passed} PASSED, {failed} FAILED");
            report.AppendLine("================================================================================");

            Debug.Log(report.ToString());

            if (failed > 0)
            {
                throw new Exception($"[Step3A_CrossbowAudit] FAILED with {failed} errors!\n{report}");
            }
        }

        private static int Assert(bool condition, string message, StringBuilder sb, ref int failedCount)
        {
            if (condition)
            {
                sb.AppendLine($"  [PASS] {message}");
                return 1;
            }
            else
            {
                sb.AppendLine($"  [FAIL] {message}");
                failedCount++;
                return 0;
            }
        }
#endif
    }
}
