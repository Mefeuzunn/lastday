using System;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.AI;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using ProjectSurvival.AI;
using ProjectSurvival.Interaction;
using ProjectSurvival.Survival;
using ProjectSurvival.UI;

namespace ProjectSurvival.EditorTools
{
    public static class Step3B_HuntingFishingAudit
    {
        private const string ScenePath = "Assets/_Project/Scenes/Main_Prototype.unity";

#if UNITY_EDITOR
        [MenuItem("ProjectSurvival/Phase 3/Step 3B - Audit Hunting & Fishing", false, 304)]
        public static void RunAuditCLI()
        {
            Debug.Log("[Step3B_HuntingFishingAudit] Starting Automated Forensic Verification for Phase 3 (Step 3B)...");

            int passed = 0;
            int failed = 0;
            StringBuilder report = new StringBuilder();
            report.AppendLine("================================================================================");
            report.AppendLine("STEP 3B AUDIT: WESTERN FOREST WILDLIFE, LAKE FISHING & CAMP STOVE GRILLING");
            report.AppendLine("================================================================================\n");

            // 1. Scene load
            if (EditorSceneManager.GetActiveScene().path != ScenePath)
            {
                EditorSceneManager.OpenScene(ScenePath);
            }

            // Test 1: Western Wildlife Population & Grounding
            report.AppendLine("--- 1. WILDLIFE POPULATION & SECTOR GROUNDING ---");
            WildlifeAnimalAI[] animals = UnityEngine.Object.FindObjectsByType<WildlifeAnimalAI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            passed += Assert(animals.Length == 6, $"Exactly 6 wildlife animals present in scene (Found: {animals.Length})", report, ref failed);

            int deerCount = 0;
            int boarCount = 0;
            int groundedCount = 0;
            int starterZoneViolations = 0;

            foreach (var a in animals)
            {
                if (a.Species == WildlifeType.ForestDeer) deerCount++;
                else if (a.Species == WildlifeType.FeralBoar) boarCount++;

                // NavMesh grounding check
                if (NavMesh.SamplePosition(a.transform.position, out NavMeshHit hit, 1.0f, NavMesh.AllAreas))
                {
                    groundedCount++;
                }

                // Starter zone protection check (no wildlife in Sector_0_0_Center)
                if (Vector3.Distance(a.transform.position, Vector3.zero) < 65f)
                {
                    starterZoneViolations++;
                }
            }

            passed += Assert(deerCount == 4, $"Forest Deer count is exactly 4 (Found: {deerCount})", report, ref failed);
            passed += Assert(boarCount == 2, $"Feral Boar count is exactly 2 (Found: {boarCount})", report, ref failed);
            passed += Assert(groundedCount == 6, $"All 6 wildlife animals sit on valid NavMesh (Grounded: {groundedCount}/6)", report, ref failed);
            passed += Assert(starterZoneViolations == 0, $"Starter Zone Protection: 0 wildlife inside Sector_0_0_Center (Violations: {starterZoneViolations})", report, ref failed);

            // Test 2: Northwest Lake Pier Fishing Spot
            report.AppendLine("\n--- 2. NORTHWEST LAKE PIER FISHING SPOT ---");
            GameObject fishingSpot = GameObject.Find("FishingSpot_NorthwestLakePier");
            passed += Assert(fishingSpot != null, "FishingSpot_NorthwestLakePier exists in scene", report, ref failed);

            if (fishingSpot != null)
            {
                LakeFishingSpotInteractable fis = fishingSpot.GetComponent<LakeFishingSpotInteractable>();
                passed += Assert(fis != null, "LakeFishingSpotInteractable is attached to fishing spot", report, ref failed);
                if (fis != null)
                {
                    var verbs = fis.GetAvailableVerbs();
                    bool hasFishVerb = false;
                    for (int v = 0; v < verbs.Count; v++)
                    {
                        if (verbs[v].verbId == "fish_lake") hasFishVerb = true;
                    }
                    passed += Assert(hasFishVerb, "Fishing spot exposes 'fish_lake' interaction verb", report, ref failed);
                }

                bool onNavMesh = NavMesh.SamplePosition(fishingSpot.transform.position, out NavMeshHit navHit, 1.5f, NavMesh.AllAreas);
                passed += Assert(onNavMesh, "Fishing spot is placed on valid walkable pier NavMesh", report, ref failed);
            }

            // Test 3: FishingMiniGameUI on Canvas
            report.AppendLine("\n--- 3. FISHING MINI-GAME UI & SAFE AREA ---");
            FishingMiniGameUI fishUI = UnityEngine.Object.FindFirstObjectByType<FishingMiniGameUI>(FindObjectsInactive.Include);
            passed += Assert(fishUI != null, "FishingMiniGameUI is present on Canvas", report, ref failed);
            if (fishUI != null)
            {
                var safeArea = fishUI.GetComponentInChildren<SafeAreaHandler>(true);
                passed += Assert(safeArea != null, "FishingMiniGameUI contains SafeAreaHandler", report, ref failed);
            }

            // Test 4: Consumable ItemData Assets
            report.AppendLine("\n--- 4. CONSUMABLE RAW & COOKED ITEMDATA ASSETS ---");
            string[] itemsToVerify = new string[] { "item_raw_meat", "item_cooked_meat", "item_raw_fish", "item_grilled_fish" };
            foreach (var itemId in itemsToVerify)
            {
                ItemData item = InventorySystem.FindItemData(itemId);
                passed += Assert(item != null, $"ItemData for '{itemId}' resolves successfully", report, ref failed);
                if (item != null)
                {
                    passed += Assert(item.itemType == ItemType.Food, $"'{itemId}' is classified as ItemType.Food", report, ref failed);
                }
            }

            // Test 5: Cooking Stove Recipes in SurvivalAlchemyUI
            report.AppendLine("\n--- 5. CAMP STOVE GRILLING RECIPES ---");
            SurvivalAlchemyUI alchemyUI = UnityEngine.Object.FindFirstObjectByType<SurvivalAlchemyUI>(FindObjectsInactive.Include);
            passed += Assert(alchemyUI != null, "SurvivalAlchemyUI is present in scene", report, ref failed);
            if (alchemyUI != null)
            {
                if (alchemyUI.AllRecipes.Count == 0)
                {
                    typeof(SurvivalAlchemyUI).GetMethod("InitializeRecipes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(alchemyUI, null);
                }

                bool hasCookedMeatRecipe = false;
                bool hasGrilledFishRecipe = false;

                foreach (var r in alchemyUI.AllRecipes)
                {
                    if (r.outputItemId == "item_cooked_meat") hasCookedMeatRecipe = true;
                    if (r.outputItemId == "item_grilled_fish") hasGrilledFishRecipe = true;
                }

                passed += Assert(hasCookedMeatRecipe, "SurvivalAlchemyUI has 'recipe_cooked_meat' registered", report, ref failed);
                passed += Assert(hasGrilledFishRecipe, "SurvivalAlchemyUI has 'recipe_grilled_fish' registered", report, ref failed);
            }

            // Test 6: SurvivalStats Stamina Buff
            report.AppendLine("\n--- 6. SURVIVALSTATS STAMINA REGEN BUFF ---");
            SurvivalStats stats = UnityEngine.Object.FindFirstObjectByType<SurvivalStats>(FindObjectsInactive.Include);
            passed += Assert(stats != null, "SurvivalStats is active on Player", report, ref failed);
            if (stats != null)
            {
                stats.ApplyStaminaRegenBuff(120f, 1.6f);
                passed += Assert(stats.HasStaminaRegenBuff, "SurvivalStats applies stamina regen buff successfully", report, ref failed);
            }

            // Test 7: Zero TODOs
            report.AppendLine("\n--- 7. ZERO TODOS & CODE SANITY AUDIT ---");
            string[] scriptsToScan = new string[]
            {
                "Assets/_Project/Scripts/AI/WildlifeAnimalAI.cs",
                "Assets/_Project/Scripts/Interaction/AnimalHarvestInteractable.cs",
                "Assets/_Project/Scripts/Interaction/LakeFishingSpotInteractable.cs",
                "Assets/_Project/Scripts/UI/FishingMiniGameUI.cs",
                "Assets/_Project/Scripts/Editor/Step3B_HuntingFishingSetup.cs"
            };

            bool anyTodoFound = false;
            foreach (string sPath in scriptsToScan)
            {
                if (File.Exists(sPath))
                {
                    string content = File.ReadAllText(sPath);
                    if (content.Contains("//" + " TODO") || content.Contains("//" + "TODO"))
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
                report.AppendLine("[PASS] Zero // TODO comments across all Step 3B scripts.");
            }

            // Summary
            report.AppendLine("\n================================================================================");
            report.AppendLine($"AUDIT COMPLETE: {passed} PASSED, {failed} FAILED");
            report.AppendLine("================================================================================");

            Debug.Log(report.ToString());

            if (failed > 0)
            {
                throw new Exception($"[Step3B_HuntingFishingAudit] FAILED with {failed} errors!\n{report}");
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
